namespace Stack86.Logic.Resilience;

using Polly;

/// <summary>
/// No-op <see cref="IResiliencePipelineFactory"/> useful for unit tests that should
/// not exercise timeouts, retries or circuit breakers.
/// </summary>
public sealed class NoOpResiliencePipelineFactory : IResiliencePipelineFactory
{
    /// <summary>Gets the singleton instance.</summary>
    public static NoOpResiliencePipelineFactory Instance { get; } = new();

    /// <inheritdoc />
    public ResiliencePipeline CompileOverall => ResiliencePipeline.Empty;

    /// <inheritdoc />
    public ResiliencePipeline ExternalValidator => ResiliencePipeline.Empty;

    /// <inheritdoc />
    public ResiliencePipeline ExternalTranspiler => ResiliencePipeline.Empty;

    /// <inheritdoc />
    public ResiliencePipeline DispatchStage => ResiliencePipeline.Empty;
}
