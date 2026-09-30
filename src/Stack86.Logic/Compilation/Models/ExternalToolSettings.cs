namespace Stack86.Logic.Compilation.Models;

/// <summary>
/// Common configuration for an external (out-of-process) code-validation tool such as
/// <c>rustc</c>, <c>go</c>, <c>javac</c>, <c>python</c> or <c>tsc</c>. Each tool binds its
/// own derived settings instance from configuration so paths and limits can be tuned
/// independently.
/// </summary>
public abstract class ExternalToolSettings
{
    /// <summary>
    /// Gets or sets the path to the tool executable. Defaults to the bare command name,
    /// which resolves against the system <c>PATH</c>.
    /// </summary>
    public string ExecutablePath { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the maximum time in milliseconds to wait for the tool to finish.
    /// Defaults to 8 000 ms.
    /// </summary>
    public int TimeoutMs { get; set; } = 8000;

    /// <summary>
    /// Gets or sets the maximum source size in bytes that will be validated. Larger
    /// sources are rejected with an error. Defaults to 65 536 (64 KB).
    /// </summary>
    public int MaxSourceSizeBytes { get; set; } = 65536;
}
