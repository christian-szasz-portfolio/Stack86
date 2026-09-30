namespace Stack86.Logic.Resilience;

/// <summary>
/// Tunables for <see cref="ResiliencePipelineFactory"/>. All defaults are conservative and
/// suitable for the in-process compilation pipeline.
/// </summary>
public sealed class ResilienceOptions
{
    /// <summary>Gets or sets the overall compilation timeout.</summary>
    public TimeSpan CompileOverallTimeout { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>Gets or sets the maximum concurrent compilations permitted.</summary>
    public int CompileMaxConcurrency { get; set; } = 4;

    /// <summary>Gets or sets the additional queue depth for compilation requests.</summary>
    public int CompileQueueLimit { get; set; } = 8;

    /// <summary>Gets or sets the per-stage dispatch timeout.</summary>
    public TimeSpan DispatchStageTimeout { get; set; } = TimeSpan.FromSeconds(10);

    /// <summary>Gets or sets the external (TCC) validator timeout.</summary>
    public TimeSpan ExternalValidatorTimeout { get; set; } = TimeSpan.FromSeconds(5);

    /// <summary>Gets or sets the retry attempt limit for the external validator.</summary>
    public int ExternalValidatorMaxRetries { get; set; } = 2;

    /// <summary>Gets or sets the circuit-breaker open duration for the external validator.</summary>
    public TimeSpan ExternalValidatorBreakDuration { get; set; } = TimeSpan.FromSeconds(15);

    /// <summary>Gets or sets the external transpiler (Node/TypeScript) timeout.</summary>
    public TimeSpan ExternalTranspilerTimeout { get; set; } = TimeSpan.FromSeconds(10);

    /// <summary>Gets or sets the retry attempt limit for the external transpiler.</summary>
    public int ExternalTranspilerMaxRetries { get; set; } = 1;

    /// <summary>Gets or sets the circuit-breaker open duration for the external transpiler.</summary>
    public TimeSpan ExternalTranspilerBreakDuration { get; set; } = TimeSpan.FromSeconds(15);
}
