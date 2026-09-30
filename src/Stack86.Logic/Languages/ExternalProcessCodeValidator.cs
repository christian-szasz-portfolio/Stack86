namespace Stack86.Logic.Languages;

using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;
using Stack86.Logic.Compilation.Models;
using Stack86.Logic.Pipeline.Ir;

/// <summary>
/// Reusable base for <see cref="IExternalCodeValidator"/> implementations that shell out to
/// a real compiler in syntax-check mode (e.g. <c>rustc</c>, <c>go build</c>, <c>javac</c>,
/// <c>python -m py_compile</c>, <c>tsc --noEmit</c>). Handles the common subprocess
/// machinery: source-size guarding, temp-file management, concurrent stdout/stderr draining,
/// timeout, and—crucially—graceful degradation to a <see cref="DiagnosticSeverity.Warning"/>
/// when the tool is missing, so a compilation can still proceed without the external check.
/// </summary>
/// <param name="settings">The tool's executable path and limits.</param>
public abstract class ExternalProcessCodeValidator(ExternalToolSettings settings) : IExternalCodeValidator
{
    /// <summary>
    /// UTF-8 encoding without a Byte Order Mark — most compilers reject a leading BOM.
    /// </summary>
    private static readonly Encoding Utf8NoBom = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);

    /// <inheritdoc />
    public abstract SupportedLanguage Language { get; }

    /// <summary>Gets the human-readable tool name used in diagnostic messages (e.g. <c>rustc</c>).</summary>
    protected abstract string ToolName { get; }

    /// <summary>Gets the temp-file extension (including the dot) the tool expects (e.g. <c>.rs</c>).</summary>
    protected abstract string FileExtension { get; }

    /// <inheritdoc />
    public async Task<IReadOnlyList<IrDiagnostic>> ValidateAsync(string sourceCode, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(sourceCode);

        if (sourceCode.Length > settings.MaxSourceSizeBytes)
        {
            return
            [
                Diag(DiagnosticSeverity.Error, $"Source code exceeds maximum size of {settings.MaxSourceSizeBytes} bytes."),
            ];
        }

        var workDir = Path.Combine(Path.GetTempPath(), $"stack86_{Guid.NewGuid():N}");
        Directory.CreateDirectory(workDir);
        var sourceFile = Path.Combine(workDir, this.GetSourceFileName(sourceCode));

        try
        {
            await File.WriteAllTextAsync(sourceFile, sourceCode, Utf8NoBom, cancellationToken);

            var nullDevice = OperatingSystem.IsWindows() ? "NUL" : "/dev/null";

            using var process = new Process();
            process.StartInfo = new ProcessStartInfo
            {
                FileName = settings.ExecutablePath,
                Arguments = this.BuildArguments(sourceFile, workDir, nullDevice),
                WorkingDirectory = workDir,
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
                // Tool not installed / not on PATH — degrade gracefully to a warning.
                return
                [
                    Diag(
                        DiagnosticSeverity.Warning,
                        $"{this.ToolName} not found at '{settings.ExecutablePath}'. External validation skipped."),
                ];
            }

            // Drain both pipes concurrently *before* awaiting exit to avoid a pipe-buffer
            // deadlock when the child fills one stream while we block reading the other.
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
                    Diag(DiagnosticSeverity.Warning, $"{this.ToolName} validation timed out. External validation skipped."),
                ];
            }
            catch (OperationCanceledException)
            {
                KillProcessQuietly(process);
                throw;
            }

            var stdout = await stdoutTask;
            var stderr = await stderrTask;
            return this.ParseDiagnostics(stdout, stderr, process.ExitCode);
        }
        finally
        {
            TryDeleteDirectory(workDir);
        }
    }

    /// <summary>
    /// Creates an <see cref="IrDiagnostic"/> with the given severity, message and location.
    /// </summary>
    /// <param name="severity">The diagnostic severity.</param>
    /// <param name="message">The diagnostic message.</param>
    /// <param name="line">The 1-based source line, or 0 if unknown.</param>
    /// <param name="column">The 1-based source column, or 0 if unknown.</param>
    /// <returns>The constructed diagnostic.</returns>
    protected static IrDiagnostic Diag(DiagnosticSeverity severity, string message, int line = 0, int column = 0)
        => new()
        {
            Severity = severity,
            Message = message,
            Line = line,
            Column = column,
        };

    /// <summary>
    /// Builds the command-line arguments for a syntax-only check of <paramref name="sourceFilePath"/>.
    /// </summary>
    /// <param name="sourceFilePath">The full path to the temp source file.</param>
    /// <param name="workingDirectory">The temp working directory (for tools that emit output files).</param>
    /// <param name="nullDevice">The platform null device (<c>NUL</c> / <c>/dev/null</c>).</param>
    /// <returns>The argument string passed to the tool.</returns>
    protected abstract string BuildArguments(string sourceFilePath, string workingDirectory, string nullDevice);

    /// <summary>
    /// Parses the tool's output into diagnostics. The default implementation treats a zero
    /// exit code as success and otherwise surfaces each non-empty <c>stderr</c> line, using
    /// <see cref="DiagnosticLineRegex"/> when available to extract line numbers.
    /// </summary>
    /// <param name="stdout">The captured standard output.</param>
    /// <param name="stderr">The captured standard error.</param>
    /// <param name="exitCode">The process exit code.</param>
    /// <returns>The parsed diagnostics.</returns>
    protected virtual IReadOnlyList<IrDiagnostic> ParseDiagnostics(string stdout, string stderr, int exitCode)
    {
        if (exitCode == 0)
        {
            return [];
        }

        var combined = string.IsNullOrWhiteSpace(stderr) ? stdout : stderr;
        if (string.IsNullOrWhiteSpace(combined))
        {
            return [Diag(DiagnosticSeverity.Error, $"{this.ToolName} reported an error (exit code {exitCode}).")];
        }

        var pattern = this.DiagnosticLineRegex();
        var diagnostics = new List<IrDiagnostic>();

        foreach (var raw in combined.Split('\n', StringSplitOptions.RemoveEmptyEntries))
        {
            var line = raw.Trim();
            if (line.Length == 0)
            {
                continue;
            }

            var match = pattern?.Match(line);
            if (match is { Success: true })
            {
                var lineNo = match.Groups["line"].Success && int.TryParse(match.Groups["line"].Value, out var ln) ? ln : 0;
                var col = match.Groups["col"].Success && int.TryParse(match.Groups["col"].Value, out var c) ? c : 1;
                var severity = match.Groups["severity"].Success
                    && match.Groups["severity"].Value.StartsWith("warn", StringComparison.OrdinalIgnoreCase)
                    ? DiagnosticSeverity.Warning
                    : DiagnosticSeverity.Error;
                var message = match.Groups["message"].Success ? match.Groups["message"].Value.Trim() : line;
                diagnostics.Add(Diag(severity, message, lineNo, col));
            }
            else
            {
                diagnostics.Add(Diag(DiagnosticSeverity.Error, line));
            }
        }

        return diagnostics.Count > 0
            ? diagnostics
            : [Diag(DiagnosticSeverity.Error, $"{this.ToolName} reported an error (exit code {exitCode}).")];
    }

    /// <summary>
    /// Gets an optional regular expression used to extract <c>line</c>, <c>col</c>,
    /// <c>severity</c> and <c>message</c> capture groups from a single diagnostic line.
    /// Returns <see langword="null"/> to surface lines verbatim.
    /// </summary>
    /// <returns>The per-tool diagnostic pattern, or <see langword="null"/>.</returns>
    protected virtual Regex? DiagnosticLineRegex() => null;

    /// <summary>
    /// Gets the temp source file name for the given <paramref name="sourceCode"/>. Defaults
    /// to <c>source{FileExtension}</c>; languages whose compiler requires the file name to
    /// match a declared type (e.g. Java's public class) override this.
    /// </summary>
    /// <param name="sourceCode">The source being validated.</param>
    /// <returns>The temp file name (without directory).</returns>
    protected virtual string GetSourceFileName(string sourceCode) => $"source{this.FileExtension}";

    /// <summary>
    /// Attempts to terminate the process tree, swallowing the benign race where the process
    /// exits between the timeout firing and the kill request.
    /// </summary>
    private static void KillProcessQuietly(Process process)
    {
        try
        {
            process.Kill(entireProcessTree: true);
        }
        catch (InvalidOperationException)
        {
            // Process already exited.
        }
        catch (System.ComponentModel.Win32Exception)
        {
            // Process is exiting and cannot be accessed.
        }
    }

    private static void TryDeleteDirectory(string path)
    {
        try
        {
            if (Directory.Exists(path))
            {
                Directory.Delete(path, recursive: true);
            }
        }
        catch (IOException)
        {
            // Best-effort cleanup; a leftover temp dir is harmless.
        }
        catch (UnauthorizedAccessException)
        {
            // Best-effort cleanup.
        }
    }
}
