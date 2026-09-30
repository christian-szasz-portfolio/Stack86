namespace Stack86.Logic.Languages.C;

using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;
using Stack86.Logic.Compilation.Models;
using Stack86.Logic.Pipeline.Ir;

/// <summary>
/// Validates C source code by invoking TCC (Tiny C Compiler) as a subprocess.
/// Returns diagnostics parsed from TCC's stderr output.
/// </summary>
public sealed partial class TccCodeValidator(TccSettings settings) : IExternalCodeValidator
{
    /// <summary>
    /// Pattern that matches a TCC diagnostic line: <c>file:line: error|warning: message</c>.
    /// </summary>
    private static readonly Regex DiagnosticPattern = TccDiagnosticRegex();

    /// <summary>
    /// UTF-8 encoding without a Byte Order Mark — TCC does not tolerate BOM.
    /// </summary>
    private static readonly Encoding Utf8NoBom = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);

    /// <inheritdoc />
    public SupportedLanguage Language => SupportedLanguage.C;

    /// <inheritdoc />
    public async Task<IReadOnlyList<IrDiagnostic>> ValidateAsync(string sourceCode, CancellationToken cancellationToken = default)
    {
        if (sourceCode.Length > settings.MaxSourceSizeBytes)
        {
            return
            [
                new IrDiagnostic
                {
                    Severity = DiagnosticSeverity.Error,
                    Message = $"Source code exceeds maximum size of {settings.MaxSourceSizeBytes} bytes.",
                    Line = 0,
                    Column = 0,
                },
            ];
        }

        string tempFile = Path.Combine(Path.GetTempPath(), $"stack86_{Guid.NewGuid():N}.c");

        try
        {
            await File.WriteAllTextAsync(tempFile, sourceCode, Utf8NoBom, cancellationToken);

            var nullDevice = OperatingSystem.IsWindows() ? "NUL" : "/dev/null";

            using var process = new Process();
            process.StartInfo = new ProcessStartInfo
            {
                FileName = settings.ExecutablePath,
                Arguments = $"-c -o {nullDevice} \"{tempFile}\"",
                RedirectStandardError = true,
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            };

            try
            {
                process.Start();
            }
            catch (Exception)
            {
                // TCC not found — return a warning and allow the pipeline to continue
                return
                [
                    new IrDiagnostic
                    {
                        Severity = DiagnosticSeverity.Warning,
                        Message = $"TCC not found at '{settings.ExecutablePath}'. External validation skipped.",
                        Line = 0,
                        Column = 0,
                    },
                ];
            }

            // Drain both pipes concurrently *before* awaiting exit. Reading a single
            // stream to completion while the child fills the other can deadlock once an
            // OS pipe buffer is full, so start both reads first and await them together.
            var stdoutTask = process.StandardOutput.ReadToEndAsync(cancellationToken);
            var stderrTask = process.StandardError.ReadToEndAsync(cancellationToken);

            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeoutCts.CancelAfter(settings.TimeoutMs);
            try
            {
                await process.WaitForExitAsync(timeoutCts.Token);
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                KillProcessQuietly(process);

                return
                [
                    new IrDiagnostic
                    {
                        Severity = DiagnosticSeverity.Warning,
                        Message = "TCC validation timed out. External validation skipped.",
                        Line = 0,
                        Column = 0,
                    },
                ];
            }
            catch (OperationCanceledException)
            {
                KillProcessQuietly(process);
                throw;
            }

            var stderr = await stderrTask;
            await stdoutTask;
            return ParseDiagnostics(stderr);
        }
        finally
        {
            if (File.Exists(tempFile))
            {
                File.Delete(tempFile);
            }
        }
    }

    /// <summary>
    /// Attempts to terminate the process tree, swallowing the benign race where the
    /// process exits between the timeout firing and the kill request.
    /// </summary>
    private static void KillProcessQuietly(Process process)
    {
        try
        {
            process.Kill(entireProcessTree: true);
        }
        catch (InvalidOperationException)
        {
            // Process already exited between the timeout firing and Kill being called.
        }
        catch (System.ComponentModel.Win32Exception)
        {
            // Process is exiting and cannot be accessed; nothing further to do.
        }
    }

    /// <summary>
    /// Parses TCC stderr output into a list of diagnostics.
    /// </summary>
    private static List<IrDiagnostic> ParseDiagnostics(string stderr)
    {
        if (string.IsNullOrWhiteSpace(stderr))
        {
            return [];
        }

        var diagnostics = new List<IrDiagnostic>();

        foreach (var line in stderr.Split('\n', StringSplitOptions.RemoveEmptyEntries))
        {
            var trimmed = line.Trim();
            if (trimmed.Length == 0)
            {
                continue;
            }

            var match = DiagnosticPattern.Match(trimmed);
            if (match.Success)
            {
                var lineNumber = int.TryParse(match.Groups["line"].Value, out var ln) ? ln : 0;
                var severityText = match.Groups["severity"].Value;
                var message = match.Groups["message"].Value.Trim();

                var severity = severityText.Equals("warning", StringComparison.OrdinalIgnoreCase)
                    ? DiagnosticSeverity.Warning
                    : DiagnosticSeverity.Error;

                diagnostics.Add(new IrDiagnostic
                {
                    Severity = severity,
                    Message = message,
                    Line = lineNumber,
                    Column = 1,
                });
            }
            else
            {
                // Unrecognised line — include it as an error for visibility
                diagnostics.Add(new IrDiagnostic
                {
                    Severity = DiagnosticSeverity.Error,
                    Message = trimmed,
                    Line = 0,
                    Column = 0,
                });
            }
        }

        return diagnostics;
    }

    [GeneratedRegex(@":(?<line>\d+):\s*(?<severity>error|warning):\s*(?<message>.+)$", RegexOptions.IgnoreCase)]
    private static partial Regex TccDiagnosticRegex();
}
