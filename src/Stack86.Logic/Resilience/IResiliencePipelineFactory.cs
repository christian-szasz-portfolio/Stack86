namespace Stack86.Logic.Resilience;

using Polly;

/// <summary>
/// Resolves named <see cref="ResiliencePipeline"/> instances composed of timeout,
/// retry, circuit-breaker and concurrency-limiter strategies.
/// </summary>
public interface IResiliencePipelineFactory
{
    /// <summary>
    /// Pipeline guarding the overall compilation request: outer timeout + bulkhead.
    /// </summary>
    public ResiliencePipeline CompileOverall { get; }

    /// <summary>
    /// Pipeline guarding the external syntax validator (TCC): timeout + retry + circuit breaker.
    /// </summary>
    public ResiliencePipeline ExternalValidator { get; }

    /// <summary>
    /// Pipeline guarding the external transpiler (Node/TypeScript): timeout + retry + circuit breaker.
    /// </summary>
    public ResiliencePipeline ExternalTranspiler { get; }

    /// <summary>
    /// Pipeline guarding a single dispatched stage: per-stage timeout.
    /// </summary>
    public ResiliencePipeline DispatchStage { get; }
}
