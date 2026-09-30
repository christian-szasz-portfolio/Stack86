namespace Stack86.Api.Infrastructure.Compiler;

using System.Threading;
using System.Threading.Channels;

/// <summary>
/// Default <see cref="ICompilationQueue"/> backed by a bounded <see cref="Channel{T}"/> for the
/// synchronous compile endpoint and a <see cref="SemaphoreSlim"/> admission gate for the streaming
/// endpoint. Both reject excess work immediately rather than blocking the request thread.
/// </summary>
public sealed class CompilationQueue : ICompilationQueue, IDisposable
{
    private readonly Channel<CompilationJob> channel;
    private readonly SemaphoreSlim streamSlots;

    /// <summary>Initialises a new <see cref="CompilationQueue"/> sized from the supplied options.</summary>
    /// <param name="options">Queue capacity and concurrency tunables.</param>
    public CompilationQueue(CompilationQueueOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        var capacity = Math.Max(1, options.MaxConcurrency + options.QueueCapacity);
        this.channel = Channel.CreateBounded<CompilationJob>(new BoundedChannelOptions(capacity)
        {
            FullMode = BoundedChannelFullMode.Wait,
            SingleReader = false,
            SingleWriter = false,
        });

        this.streamSlots = new SemaphoreSlim(Math.Max(1, options.MaxConcurrentStreams));
    }

    /// <inheritdoc />
    public ChannelReader<CompilationJob> Reader => this.channel.Reader;

    /// <inheritdoc />
    public bool TryEnqueue(CompilationJob job)
    {
        ArgumentNullException.ThrowIfNull(job);
        return this.channel.Writer.TryWrite(job);
    }

    /// <inheritdoc />
    public bool TryAcquireStreamSlot() => this.streamSlots.Wait(0);

    /// <inheritdoc />
    public void ReleaseStreamSlot() => this.streamSlots.Release();

    /// <inheritdoc />
    public void Dispose() => this.streamSlots.Dispose();
}
