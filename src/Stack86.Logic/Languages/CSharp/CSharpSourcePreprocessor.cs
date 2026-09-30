namespace Stack86.Logic.Languages.CSharp;

/// <summary>
/// C# preprocessor: merges all <c>.cs</c> files with <c>Main.cs</c> (or <c>Program.cs</c>)
/// emitted first, recording per-line mappings for diagnostics.
/// </summary>
public sealed class CSharpSourcePreprocessor : MergingLanguagePreprocessor
{
    /// <inheritdoc />
    public override SupportedLanguage Language => SupportedLanguage.CSharp;

    /// <inheritdoc />
    protected override IReadOnlyList<string> MainFileCandidates => ["Main.cs", "Program.cs"];
}
