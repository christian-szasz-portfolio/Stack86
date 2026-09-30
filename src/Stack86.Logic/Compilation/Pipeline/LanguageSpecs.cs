namespace Stack86.Logic.Compilation.Pipeline;

using Stack86.Logic.Languages;
using static Stack86.Logic.Compilation.Pipeline.PipelineStageKind;

/// <summary>
/// Registry of static <see cref="LanguageSpec"/> instances — one per supported source
/// language. Adding a new language is a one-line addition here plus the corresponding
/// per-language services. <see cref="LanguageDefaults"/> consumes this registry to
/// configure compilation pipelines without per-language branches.
/// </summary>
public static class LanguageSpecs
{
    /// <summary>C: preprocess includes, run TCC, validate capabilities, lower, optimise, validate IR, emit.</summary>
    public static readonly LanguageSpec C = new(
        SupportedLanguage.C,
        [Preprocess, ValidateExternal, ValidateCapabilities, LowerToIr, OptimizeIr, ValidateIr, EmitAssembly]);

    /// <summary>C++: validate capabilities pre-transpile, transpile to C, run TCC on the C output, then standard backend.</summary>
    public static readonly LanguageSpec Cpp = new(
        SupportedLanguage.Cpp,
        [ValidateCapabilities, Transpile, ValidateExternal, LowerToIr, OptimizeIr, ValidateIr, EmitAssembly]);

    /// <summary>TypeScript: preprocess (merge project files into source), validate capabilities pre-transpile, transpile to JS, then standard backend.</summary>
    public static readonly LanguageSpec TypeScript = new(
        SupportedLanguage.TypeScript,
        [Preprocess, ValidateCapabilities, Transpile, LowerToIr, OptimizeIr, ValidateIr, EmitAssembly]);

    /// <summary>C#: standard backend (Roslyn handles parsing inside the frontend).</summary>
    public static readonly LanguageSpec CSharp = new(
        SupportedLanguage.CSharp,
        [Preprocess, ValidateCapabilities, LowerToIr, OptimizeIr, ValidateIr, EmitAssembly]);

    /// <summary>JavaScript: preprocess, syntax-check via <c>node --check</c>, then standard backend.</summary>
    public static readonly LanguageSpec JavaScript = new(
        SupportedLanguage.JavaScript,
        [Preprocess, ValidateExternal, ValidateCapabilities, LowerToIr, OptimizeIr, ValidateIr, EmitAssembly]);

    private static readonly Dictionary<SupportedLanguage, LanguageSpec> All = new()
    {
        [SupportedLanguage.C] = C,
        [SupportedLanguage.Cpp] = Cpp,
        [SupportedLanguage.TypeScript] = TypeScript,
        [SupportedLanguage.CSharp] = CSharp,
        [SupportedLanguage.JavaScript] = JavaScript,
    };

    /// <summary>
    /// Returns the <see cref="LanguageSpec"/> for <paramref name="language"/>, or
    /// <see langword="null"/> if no spec is registered for that language.
    /// </summary>
    public static LanguageSpec? For(SupportedLanguage language)
    {
        ArgumentNullException.ThrowIfNull(language);
        return All.TryGetValue(language, out var spec) ? spec : null;
    }
}
