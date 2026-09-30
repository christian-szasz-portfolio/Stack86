namespace Stack86.Logic.Compilation.Pipeline;

using Stack86.Logic.Compilation.Stages.Models;

/// <summary>
/// A single compilation stage in a <see cref="CompilationPipeline"/>. Identified by a
/// human-readable name (used in streaming log events) and a delegate that mutates the
/// shared <see cref="CompilationContext"/>.
/// </summary>
/// <param name="Name">Display name shown in streaming log events.</param>
/// <param name="Run">The stage delegate.</param>
internal sealed record PipelineStage(string Name, Func<CompilationContext, CancellationToken, Task<StageOutcome>> Run);
