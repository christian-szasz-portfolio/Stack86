namespace Stack86.Logic.Languages.C;

using Stack86.Logic.Pipeline.Ir;

/// <summary>
/// C frontend: tokenises with <see cref="CLexer"/>, parses with <see cref="CParser"/>,
/// and lowers to IR via <see cref="CIrGenerator"/>. Also serves as the post-transpile
/// frontend for C++ source (which is rewritten to C before this stage runs).
/// </summary>
public sealed class CFrontend : ILanguageFrontend
{
    /// <inheritdoc />
    public SupportedLanguage Language => SupportedLanguage.C;

    /// <inheritdoc />
    public IrProgram Lower(LanguageFrontendInput input)
    {
        ArgumentNullException.ThrowIfNull(input);
        var tokens = new CLexer(input.Source).Tokenize();
        var ast = new CParser(tokens).Parse();
        var ir = new CIrGenerator(input.SystemHeaders).Generate(ast);
        return ir;
    }
}
