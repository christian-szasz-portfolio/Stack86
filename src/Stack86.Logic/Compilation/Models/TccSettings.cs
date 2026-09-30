namespace Stack86.Logic.Compilation.Models;

/// <summary>
/// Configuration for the TCC (Tiny C Compiler) external validation step.
/// </summary>
public sealed class TccSettings
{
    /// <summary>
    /// Path to the TCC executable. Defaults to <c>tcc</c> (assumes PATH).
    /// </summary>
    public string ExecutablePath { get; set; } = "tcc";

    /// <summary>
    /// Maximum time in milliseconds to wait for TCC to finish. Defaults to 5 000 ms.
    /// </summary>
    public int TimeoutMs { get; set; } = 5000;

    /// <summary>
    /// Maximum source code size in bytes. Defaults to 65 536 (64 KB).
    /// </summary>
    public int MaxSourceSizeBytes { get; set; } = 65536;
}
