namespace Stack86.Logic.Resilience;

using System.IO;
using System.Threading.RateLimiting;
using Polly;
using Polly.CircuitBreaker;
using Polly.RateLimiting;
using Polly.Retry;
using Polly.Timeout;

/// <summary>
/// Default <see cref="IResiliencePipelineFactory"/> implementation. Pipelines are
/// constructed once at startup and reused for the lifetime of the process.
/// </summary>
public sealed class ResiliencePipelineFactory : IResiliencePipelineFactory, IDisposable
{
    private readonly ConcurrencyLimiter concurrencyLimiter;

    /// <summary>
    /// Initialises a new <see cref="ResiliencePipelineFactory"/> with the configured pipelines.
    /// </summary>
    /// <param name="options">Options controlling timeouts, retry and breaker thresholds.</param>
    public ResiliencePipelineFactory(ResilienceOptions? options = null)
    {
        var opt = options ?? new ResilienceOptions();

        this.concurrencyLimiter = new ConcurrencyLimiter(new ConcurrencyLimiterOptions
        {
            PermitLimit = opt.CompileMaxConcurrency,
            QueueLimit = opt.CompileQueueLimit,
            QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
        });

        this.CompileOverall = new ResiliencePipelineBuilder()
            .AddTimeout(opt.CompileOverallTimeout)
            .AddRateLimiter(this.concurrencyLimiter)
            .Build();

        this.ExternalValidator = new ResiliencePipelineBuilder()
            .AddCircuitBreaker(new CircuitBreakerStrategyOptions
            {
                FailureRatio = 0.5,
                MinimumThroughput = 4,
                SamplingDuration = TimeSpan.FromSeconds(30),
                BreakDuration = opt.ExternalValidatorBreakDuration,
                ShouldHandle = new PredicateBuilder()
                    .Handle<TimeoutRejectedException>()
                    .Handle<IOException>(),
            })
            .AddRetry(new RetryStrategyOptions
            {
                MaxRetryAttempts = opt.ExternalValidatorMaxRetries,
                BackoffType = DelayBackoffType.Exponential,
                UseJitter = true,
                Delay = TimeSpan.FromMilliseconds(200),
                ShouldHandle = new PredicateBuilder()
                    .Handle<TimeoutRejectedException>()
                    .Handle<IOException>(),
            })
            .AddTimeout(opt.ExternalValidatorTimeout)
            .Build();

        this.ExternalTranspiler = new ResiliencePipelineBuilder()
            .AddCircuitBreaker(new CircuitBreakerStrategyOptions
            {
                FailureRatio = 0.5,
                MinimumThroughput = 4,
                SamplingDuration = TimeSpan.FromSeconds(30),
                BreakDuration = opt.ExternalTranspilerBreakDuration,
                ShouldHandle = new PredicateBuilder()
                    .Handle<TimeoutRejectedException>()
                    .Handle<IOException>(),
            })
            .AddRetry(new RetryStrategyOptions
            {
                MaxRetryAttempts = opt.ExternalTranspilerMaxRetries,
                BackoffType = DelayBackoffType.Exponential,
                UseJitter = true,
                Delay = TimeSpan.FromMilliseconds(200),
                ShouldHandle = new PredicateBuilder()
                    .Handle<TimeoutRejectedException>()
                    .Handle<IOException>(),
            })
            .AddTimeout(opt.ExternalTranspilerTimeout)
            .Build();

        this.DispatchStage = new ResiliencePipelineBuilder()
            .AddTimeout(opt.DispatchStageTimeout)
            .Build();
    }

    /// <inheritdoc />
    public ResiliencePipeline CompileOverall { get; }

    /// <inheritdoc />
    public ResiliencePipeline ExternalValidator { get; }

    /// <inheritdoc />
    public ResiliencePipeline ExternalTranspiler { get; }

    /// <inheritdoc />
    public ResiliencePipeline DispatchStage { get; }

    /// <inheritdoc />
    public void Dispose() => this.concurrencyLimiter.Dispose();
}
