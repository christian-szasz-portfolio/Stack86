namespace Stack86.Logic.Languages.CSharp;

using Stack86.Logic.Pipeline.Ir;

/// <summary>
/// C# frontend: drives <see cref="RoslynAnalyzer"/> to produce a Roslyn compilation,
/// then lowers it to IR via <see cref="CSharpIrGenerator"/>. Roslyn parser warnings
/// are surfaced as IR diagnostics.
/// </summary>
public sealed class CSharpFrontend(RoslynAnalyzer analyzer) : ILanguageFrontend
{
    /// <inheritdoc />
    public SupportedLanguage Language => SupportedLanguage.CSharp;

    /// <inheritdoc />
    public IrProgram Lower(LanguageFrontendInput input)
    {
        ArgumentNullException.ThrowIfNull(input);

        var (compilation, roslynDiagnostics) = analyzer.Analyze(input.Files);
        var irProgram = new CSharpIrGenerator().Generate(compilation);

        if (roslynDiagnostics.Count > 0)
        {
            var merged = new List<IrDiagnostic>(irProgram.Diagnostics);
            foreach (var d in roslynDiagnostics)
            {
                merged.Add(new IrDiagnostic
                {
                    Severity = DiagnosticSeverity.Warning,
                    Message = d.Message,
                    Line = d.Line,
                    Column = d.Column,
                    SourceFile = d.SourceFile,
                });
            }

            irProgram = irProgram with { Diagnostics = merged };
        }

        return irProgram;
    }
}
