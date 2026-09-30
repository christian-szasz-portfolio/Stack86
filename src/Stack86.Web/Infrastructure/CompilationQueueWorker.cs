namespace Stack86.Web.Infrastructure;

using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using Stack86.Api.Infrastructure.Compiler;
using Stack86.Common.Exceptions;
using Stack86.Logic.Compilation.Providers;

/// <summary>
/// Drains the <see cref="ICompilationQueue"/> using a fixed pool of consumer loops, capping the
/// number of compilations that run at once. Each job is processed through a freshly scoped
/// <see cref="CompilerProvider"/> and its result is handed back to the awaiting request.
/// </summary>
public sealed class CompilationQueueWorker(
    IServiceProvider services,
    ICompilationQueue queue,
    CompilationQueueOptions options) : BackgroundService
{
    /// <inheritdoc />
    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var workerCount = Math.Max(1, options.MaxConcurrency);
        var workers = Enumerable
            .Range(0, workerCount)
            .Select(_ => Task.Run(() => this.ConsumeAsync(stoppingToken), stoppingToken));

        return Task.WhenAll(workers);
    }

    private async Task ConsumeAsync(CancellationToken stoppingToken)
    {
        try
        {
            await foreach (var job in queue.Reader.ReadAllAsync(stoppingToken).ConfigureAwait(false))
            {
                await this.ProcessAsync(job, stoppingToken).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // Host is shutting down — expected.
        }
    }

    private async Task ProcessAsync(CompilationJob job, CancellationToken stoppingToken)
    {
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken, job.CancellationToken);

        try
        {
            using var scope = services.CreateScope();
            var compiler = scope.ServiceProvider.GetRequiredService<CompilerProvider>();

            var result = await compiler.CompileAsync(job.Language, job.Files, linked.Token).ConfigureAwait(false);
            job.Completion.TrySetResult(result);
        }
        catch (OperationCanceledException) when (job.CancellationToken.IsCancellationRequested)
        {
            job.Completion.TrySetCanceled(job.CancellationToken);
        }
        catch (CompilationFailedException ex)
        {
            job.Completion.TrySetException(ex);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Compilation job failed unexpectedly.");
            job.Completion.TrySetException(new CompilationFailedException("Compilation failed unexpectedly."));
        }
    }
}
