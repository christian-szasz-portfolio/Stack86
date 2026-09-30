namespace Stack86.Logic.Compilation.Pipeline;

/// <summary>
/// Result of running a single pipeline stage.
/// </summary>
internal enum StageOutcome
{
    /// <summary>Stage succeeded; pipeline continues.</summary>
    Continue,

    /// <summary>Stage produced an error-level diagnostic; pipeline stops gracefully and returns the result so far.</summary>
    Stop,

    /// <summary>Stage encountered a fatal failure; pipeline aborts with a failure result.</summary>
    Fail,
}
