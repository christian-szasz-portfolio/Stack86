namespace Stack86.Logic.Compilation.Pipeline;

using Stack86.Logic.Languages;

/// <summary>
/// Static specification of a source language's default compilation pipeline. Each spec
/// declares the language identifier and the ordered sequence of <see cref="PipelineStageKind"/>
/// stages that <see cref="CompilationPipeline.UseDefaultsFor"/> applies for that language.
/// </summary>
/// <param name="Language">The source language this spec targets.</param>
/// <param name="Stages">The ordered stage sequence to apply.</param>
public sealed record LanguageSpec(
    SupportedLanguage Language,
    IReadOnlyList<PipelineStageKind> Stages);
