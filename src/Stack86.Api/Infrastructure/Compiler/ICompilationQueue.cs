namespace Stack86.Api.Infrastructure.Compiler;

using System.Threading.Channels;

/// <summary>
/// A bounded producer/consumer queue that decouples incoming compile requests from the
/// background workers that execute them. A full queue is rejected without blocking, providing
/// natural backpressure and protecting the host from request floods.
/// </summary>
public interface ICompilationQueue
{
    /// <summary>Gets the reader consumed by the background workers.</summary>
    ChannelReader<CompilationJob> Reader { get; }

    /// <summary>
    /// Attempts to enqueue a job without blocking.
    /// </summary>
    /// <param name="job">The job to enqueue.</param>
    /// <returns><see langword="true"/> if the job was accepted; <see langword="false"/> if the queue is full.</returns>
    bool TryEnqueue(CompilationJob job);

    /// <summary>
    /// Attempts to reserve a slot for a streaming compilation without blocking.
    /// </summary>
    /// <returns><see langword="true"/> if a slot was acquired; otherwise <see langword="false"/>.</returns>
    bool TryAcquireStreamSlot();

    /// <summary>Releases a streaming slot previously acquired via <see cref="TryAcquireStreamSlot"/>.</summary>
    void ReleaseStreamSlot();
}
