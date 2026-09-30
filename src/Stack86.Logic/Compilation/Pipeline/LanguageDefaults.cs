namespace Stack86.Logic.Compilation.Pipeline;

using Stack86.Common.Exceptions;
using Stack86.Logic.Languages;

/// <summary>
/// Applies the recommended stage chain for a source language to a
/// <see cref="CompilationPipeline"/>. Stage chains are declared as static
/// <see cref="LanguageSpec"/> values in <see cref="LanguageSpecs"/>; this class
/// is a thin dispatch loop over the spec.
/// </summary>
public static class LanguageDefaults
{
    /// <summary>
    /// Adds the default stages for <paramref name="language"/> to the pipeline.
    /// </summary>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="pipeline"/> or <paramref name="language"/> is <see langword="null"/>.</exception>
    /// <exception cref="UnknownLanguageException">Thrown when no <see cref="LanguageSpec"/> is registered for <paramref name="language"/>.</exception>
    public static void ConfigureFor(CompilationPipeline pipeline, SupportedLanguage language)
    {
        ArgumentNullException.ThrowIfNull(pipeline);
        ArgumentNullException.ThrowIfNull(language);

        var spec = LanguageSpecs.For(language)
            ?? throw new UnknownLanguageException($"No LanguageSpec registered for '{language.Value}'.");

        foreach (var stage in spec.Stages)
        {
            ApplyStage(pipeline, stage);
        }
    }

    private static void ApplyStage(CompilationPipeline pipeline, PipelineStageKind stage)
    {
        switch (stage)
        {
            case PipelineStageKind.Preprocess:
                pipeline.Preprocess();
                break;
            case PipelineStageKind.ValidateExternal:
                pipeline.ValidateExternal();
                break;
            case PipelineStageKind.ValidateCapabilities:
                pipeline.ValidateCapabilities();
                break;
            case PipelineStageKind.Transpile:
                pipeline.Transpile();
                break;
            case PipelineStageKind.LowerToIr:
                pipeline.LowerToIr();
                break;
            case PipelineStageKind.OptimizeIr:
                pipeline.OptimizeIr();
                break;
            case PipelineStageKind.ValidateIr:
                pipeline.ValidateIr();
                break;
            case PipelineStageKind.EmitAssembly:
                pipeline.EmitAssembly();
                break;
            default:
                throw new UnknownPipelineStageException($"Unknown pipeline stage: '{stage}'.");
        }
    }
}
