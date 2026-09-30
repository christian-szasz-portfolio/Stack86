namespace Stack86.Api.Test.Infrastructure.Compiler;

using System.Threading;
using System.Threading.Tasks;
using Stack86.Api.Infrastructure.Compiler;
using Stack86.Logic.Compilation.Models;

/// <summary>
/// The queue is the guard between the HTTP surface and the CPU-heavy compiler, so what is
/// asserted here is the refusal behaviour: a full queue and an exhausted stream gate must both
/// say no immediately rather than block a request thread.
/// </summary>
[TestClass]
public sealed class CompilationQueueTests
{
    /// <summary>Gets or sets the context for the running test.</summary>
    public required TestContext TestContext { get; set; }

    [TestMethod]
    public void Constructor_WhenOptionsAreNull_Throws()
    {
        // Act + Assert
        Assert.ThrowsExactly<ArgumentNullException>(() => new CompilationQueue(null!));
    }

    [TestMethod]
    public void TryEnqueue_WhenThereIsRoom_AcceptsTheJob()
    {
        // Arrange
        using var queue = new CompilationQueue(Options(1, 1, 1));

        // Act
        var accepted = queue.TryEnqueue(Job());

        // Assert
        Assert.IsTrue(accepted);
    }

    [TestMethod]
    public void TryEnqueue_WhenJobIsNull_Throws()
    {
        // Arrange
        using var queue = new CompilationQueue(Options(1, 1, 1));

        // Act + Assert
        Assert.ThrowsExactly<ArgumentNullException>(() => queue.TryEnqueue(null!));
    }

    [TestMethod]
    public void TryEnqueue_WhenTheQueueIsFull_RefusesRatherThanBlocking()
    {
        // Arrange: capacity is concurrency plus queue capacity, so two here.
        using var queue = new CompilationQueue(Options(1, 1, 1));
        Assert.IsTrue(queue.TryEnqueue(Job()));
        Assert.IsTrue(queue.TryEnqueue(Job()));

        // Act
        var accepted = queue.TryEnqueue(Job());

        // Assert
        Assert.IsFalse(accepted);
    }

    [TestMethod]
    public void Constructor_WhenOptionsAreZero_StillAcceptsOneJob()
    {
        // Arrange: capacity floors at one rather than throwing on a zero-sized channel.
        using var queue = new CompilationQueue(Options(0, 0, 0));

        // Act
        var accepted = queue.TryEnqueue(Job());

        // Assert
        Assert.IsTrue(accepted);
    }

    [TestMethod]
    public async Task Reader_AfterEnqueue_YieldsTheSameJob()
    {
        // Arrange
        using var queue = new CompilationQueue(Options(2, 2, 1));
        var job = Job();
        queue.TryEnqueue(job);

        // Act
        var read = await queue.Reader.ReadAsync(this.TestContext.CancellationTokenSource.Token);

        // Assert
        Assert.AreSame(job, read);
    }

    [TestMethod]
    public async Task Reader_WhenAWorkerCompletesAJob_TheProducerObservesTheResult()
    {
        // Arrange
        using var queue = new CompilationQueue(Options(2, 2, 1));
        var job = Job();
        queue.TryEnqueue(job);

        // Act: this is the whole contract between controller and worker.
        var read = await queue.Reader.ReadAsync(this.TestContext.CancellationTokenSource.Token);
        read.Completion.SetResult(new CompilationResultDto { Assembly = ".CODE" });

        // Assert
        var result = await job.Completion.Task;
        Assert.AreEqual(".CODE", result.Assembly);
    }

    [TestMethod]
    public void TryAcquireStreamSlot_UpToTheLimit_Succeeds()
    {
        // Arrange
        using var queue = new CompilationQueue(Options(1, 1, 2));

        // Act + Assert
        Assert.IsTrue(queue.TryAcquireStreamSlot());
        Assert.IsTrue(queue.TryAcquireStreamSlot());
    }

    [TestMethod]
    public void TryAcquireStreamSlot_BeyondTheLimit_RefusesImmediately()
    {
        // Arrange
        using var queue = new CompilationQueue(Options(1, 1, 1));
        Assert.IsTrue(queue.TryAcquireStreamSlot());

        // Act
        var second = queue.TryAcquireStreamSlot();

        // Assert
        Assert.IsFalse(second);
    }

    [TestMethod]
    public void ReleaseStreamSlot_AfterExhaustion_LetsTheNextStreamIn()
    {
        // Arrange
        using var queue = new CompilationQueue(Options(1, 1, 1));
        queue.TryAcquireStreamSlot();
        Assert.IsFalse(queue.TryAcquireStreamSlot());

        // Act
        queue.ReleaseStreamSlot();

        // Assert
        Assert.IsTrue(queue.TryAcquireStreamSlot());
    }

    [TestMethod]
    public void TryAcquireStreamSlot_WhenStreamsAreConfiguredAsZero_StillAllowsOne()
    {
        // Arrange
        using var queue = new CompilationQueue(Options(1, 1, 0));

        // Act + Assert
        Assert.IsTrue(queue.TryAcquireStreamSlot());
        Assert.IsFalse(queue.TryAcquireStreamSlot());
    }

    [TestMethod]
    public void Dispose_WhenCalledTwice_DoesNotThrow()
    {
        // Arrange
        var queue = new CompilationQueue(Options(1, 1, 1));

        // Act
        queue.Dispose();

        // Assert
        queue.Dispose();
    }

    private static CompilationJob Job() => new()
    {
        Language = "c",
        Files = new Dictionary<string, string> { ["main.c"] = "int main(){return 0;}" },
        CancellationToken = CancellationToken.None,
    };

    private static CompilationQueueOptions Options(int concurrency, int capacity, int streams) => new()
    {
        MaxConcurrency = concurrency,
        QueueCapacity = capacity,
        MaxConcurrentStreams = streams,
    };
}
