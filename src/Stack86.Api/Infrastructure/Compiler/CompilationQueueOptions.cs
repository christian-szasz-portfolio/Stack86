namespace Stack86.Api.Infrastructure.Compiler;

/// <summary>
/// Tunables for the channel-backed compilation work queue that protects the CPU-heavy
/// compile endpoints from thread-pool starvation and unbounded concurrency. Defaults
/// mirror <c>ResilienceOptions</c> so the HTTP-facing guard and the in-pipeline guard agree.
/// </summary>
public sealed class CompilationQueueOptions
{
    /// <summary>Configuration section name for binding.</summary>
    public const string SectionName = "Compilation:Queue";

    /// <summary>Gets or sets the maximum number of compilations processed concurrently.</summary>
    public int MaxConcurrency { get; set; } = 4;

    /// <summary>Gets or sets the additional number of compilations that may wait in the queue.</summary>
    public int QueueCapacity { get; set; } = 8;

    /// <summary>Gets or sets the maximum number of concurrent streaming compilations.</summary>
    public int MaxConcurrentStreams { get; set; } = 4;
}
