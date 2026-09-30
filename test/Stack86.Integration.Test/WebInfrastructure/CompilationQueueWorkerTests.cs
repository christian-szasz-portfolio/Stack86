namespace Stack86.Integration.Test.WebInfrastructure;

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Stack86.Api.Infrastructure.Compiler;
using Stack86.Common.Exceptions;

/// <summary>
/// The worker is what stands between an enqueued job and a compiled result. It is driven here
/// through the real host, because the thing worth asserting is that a job put on the singleton
/// queue is actually picked up, scoped, compiled and handed back: mocking the queue would prove
/// only that the test wired itself up.
/// </summary>
[TestClass]
public sealed class CompilationQueueWorkerTests : RestClientTestBase
{
    [TestMethod]
    public async Task Enqueue_WhenTheSourceCompiles_CompletesTheJobWithAssembly()
    {
        // Arrange
        var queue = Factory.Services.GetRequiredService<ICompilationQueue>();
        var job = new CompilationJob
        {
            Language = "c",
            Files = new Dictionary<string, string> { ["main.c"] = "int main() { return 0; }" },
            CancellationToken = CancellationToken.None,
        };

        // Act
        var accepted = queue.TryEnqueue(job);
        var result = await job.Completion.Task.WaitAsync(TimeSpan.FromSeconds(30));

        // Assert
        Assert.IsTrue(accepted);
        Assert.IsNotNull(result.Assembly);
        StringAssert.Contains(result.Assembly, ".CODE");
    }

    [TestMethod]
    public async Task Enqueue_WhenTheLanguageIsUnknown_FaultsTheJobRatherThanTheWorker()
    {
        // Arrange
        var queue = Factory.Services.GetRequiredService<ICompilationQueue>();
        var job = new CompilationJob
        {
            Language = "klingon",
            Files = new Dictionary<string, string> { ["main.kl"] = "nuqneH" },
            CancellationToken = CancellationToken.None,
        };

        // Act
        queue.TryEnqueue(job);

        // Assert
        await Assert.ThrowsExactlyAsync<CompilationFailedException>(
            () => job.Completion.Task.WaitAsync(TimeSpan.FromSeconds(30)));
    }

    [TestMethod]
    public async Task Enqueue_WhenTheCallerHasAlreadyGoneAway_CancelsTheJob()
    {
        // Arrange
        using var caller = new CancellationTokenSource();
        await caller.CancelAsync();

        var queue = Factory.Services.GetRequiredService<ICompilationQueue>();
        var job = new CompilationJob
        {
            Language = "c",
            Files = new Dictionary<string, string> { ["main.c"] = "int main() { return 0; }" },
            CancellationToken = caller.Token,
        };

        // Act
        queue.TryEnqueue(job);

        // Assert
        await Assert.ThrowsExactlyAsync<TaskCanceledException>(
            () => job.Completion.Task.WaitAsync(TimeSpan.FromSeconds(30)));
    }

    [TestMethod]
    public async Task Enqueue_AfterAFailedJob_TheWorkerKeepsDraining()
    {
        // Arrange: a faulted job must not take the consumer loop down with it.
        var queue = Factory.Services.GetRequiredService<ICompilationQueue>();
        var poisoned = new CompilationJob
        {
            Language = "klingon",
            Files = new Dictionary<string, string> { ["main.kl"] = "nuqneH" },
            CancellationToken = CancellationToken.None,
        };

        queue.TryEnqueue(poisoned);
        await Assert.ThrowsExactlyAsync<CompilationFailedException>(
            () => poisoned.Completion.Task.WaitAsync(TimeSpan.FromSeconds(30)));

        var healthy = new CompilationJob
        {
            Language = "c",
            Files = new Dictionary<string, string> { ["main.c"] = "int main() { return 0; }" },
            CancellationToken = CancellationToken.None,
        };

        // Act
        queue.TryEnqueue(healthy);
        var result = await healthy.Completion.Task.WaitAsync(TimeSpan.FromSeconds(30));

        // Assert
        Assert.IsNotNull(result.Assembly);
    }
}
