namespace Stack86.Logic.Languages.JavaScript;

using Stack86.Logic.Pipeline.Ir;

/// <summary>
/// JavaScript frontend: tokenises with <see cref="JsLexer"/>, parses with
/// <see cref="JsParser"/>, and lowers to IR via <see cref="JsIrGenerator"/>.
/// Parser diagnostics are merged with IR diagnostics. Also serves as the
/// post-transpile frontend for TypeScript.
/// </summary>
public sealed class JsFrontend : ILanguageFrontend
{
    /// <inheritdoc />
    public SupportedLanguage Language => SupportedLanguage.JavaScript;

    /// <inheritdoc />
    public IrProgram Lower(LanguageFrontendInput input)
    {
        ArgumentNullException.ThrowIfNull(input);
        var tokens = new JsLexer(input.Source).Tokenize();
        var parser = new JsParser(tokens);
        var ast = parser.Parse();
        var ir = new JsIrGenerator().Generate(ast);
        if (parser.Diagnostics.Count > 0)
        {
            var merged = new List<IrDiagnostic>(ir.Diagnostics);
            merged.AddRange(parser.Diagnostics);
            ir = ir with { Diagnostics = merged };
        }

        return ir;
    }
}
