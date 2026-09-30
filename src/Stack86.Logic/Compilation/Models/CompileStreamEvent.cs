namespace Stack86.Logic.Compilation.Models;

/// <summary>
/// Represents a single event in the compile stream.
/// </summary>
public abstract record CompileStreamEvent
{
    /// <summary>
    /// An informational log line produced by a pipeline stage.
    /// </summary>
    public sealed record Log(string Text) : CompileStreamEvent;

    /// <summary>
    /// The final compilation result containing assembly output and diagnostics.
    /// </summary>
    public sealed record Complete(CompilationResultDto Result) : CompileStreamEvent;

    /// <summary>
    /// Keep-alive ping emitted while no other event has been produced. Allows
    /// proxies and clients to detect that the connection is still healthy and
    /// resets any client-side idle-timeout.
    /// </summary>
    public sealed record Heartbeat : CompileStreamEvent;
}
