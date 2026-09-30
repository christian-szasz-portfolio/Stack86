namespace Stack86.Logic.Languages.TypeScript;

using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;
using Stack86.Common.Exceptions;
using Stack86.Logic.Pipeline.Ir;

/// <summary>
/// Transpiles TypeScript to JavaScript by invoking a Node.js script as a child process.
/// Follows the same shell-out pattern as <see cref="C.TccCodeValidator"/>.
/// </summary>
public sealed partial class NodeTypeScriptTranspiler(TsTranspilerSettings settings) : ITypeScriptTranspiler
{
    /// <summary>
    /// Pattern that matches a transpiler diagnostic line: <c>:line: error|warning: message</c>.
    /// </summary>
    private static readonly Regex DiagnosticPattern = TranspilerDiagnosticRegex();

    /// <summary>
    /// UTF-8 encoding without a Byte Order Mark.
    /// </summary>
    private static readonly Encoding Utf8NoBom = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);

    /// <inheritdoc />
    public async Task<string> TranspileAsync(string typeScriptSource, CancellationToken cancellationToken = default)
    {
        if (typeScriptSource.Length > settings.MaxSourceSizeBytes)
        {
            throw new ExternalToolException(
                $"Source code exceeds maximum size of {settings.MaxSourceSizeBytes} bytes.");
        }

        string tempFile = Path.Combine(Path.GetTempPath(), $"stack86_{Guid.NewGuid():N}.ts");

        try
        {
            await File.WriteAllTextAsync(tempFile, typeScriptSource, Utf8NoBom, cancellationToken);

            var scriptPath = Path.IsPathRooted(settings.ScriptPath)
                ? settings.ScriptPath
                : Path.Combine(AppContext.BaseDirectory, settings.ScriptPath);
            var nodeModulesPath = Path.IsPathRooted(settings.NodeModulesPath)
                ? settings.NodeModulesPath
                : Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, settings.NodeModulesPath));

            using var process = new Process();
            process.StartInfo = new ProcessStartInfo
            {
                FileName = settings.NodePath,
                Arguments = $"\"{scriptPath}\" \"{tempFile}\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            };

            process.StartInfo.Environment["NODE_PATH"] = nodeModulesPath;

            try
            {
                process.Start();
            }
            catch (Exception ex)
            {
                throw new ExternalToolException(
                    $"Node.js not found at '{settings.NodePath}'. " +
                    "Ensure Node.js is installed and available on PATH for TypeScript support.",
                    ex);
            }

            // Drain both pipes concurrently before awaiting exit to avoid a deadlock
            // when the child fills one buffer while we block reading the other.
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
                throw new ExternalToolException("TypeScript transpilation timed out.");
            }
            catch (OperationCanceledException)
            {
                KillProcessQuietly(process);
                throw;
            }

            var stdout = await stdoutTask;
            var stderr = await stderrTask;

            if (process.ExitCode != 0)
            {
                var errorMessage = ParseErrorMessage(stderr);
                throw new ExternalToolException(
                    $"TypeScript transpilation failed: {errorMessage}");
            }

            return stdout;
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
    /// Extracts diagnostic information from the transpiler's stderr output.
    /// </summary>
    internal static IReadOnlyList<IrDiagnostic> ParseDiagnostics(string stderr)
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

    /// <summary>
    /// Builds a concise error message from stderr output.
    /// </summary>
    private static string ParseErrorMessage(string stderr)
    {
        if (string.IsNullOrWhiteSpace(stderr))
        {
            return "Unknown error";
        }

        var lines = stderr.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        return lines.Length > 0 ? lines[0].Trim() : "Unknown error";
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

    [GeneratedRegex(@":(?<line>\d+):\s*(?<severity>error|warning):\s*(?<message>.+)$", RegexOptions.IgnoreCase)]
    private static partial Regex TranspilerDiagnosticRegex();
}
