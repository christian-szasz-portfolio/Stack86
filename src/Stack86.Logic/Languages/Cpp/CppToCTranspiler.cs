namespace Stack86.Logic.Languages.Cpp;

using Stack86.Common.Exceptions;
using Stack86.Logic.Compilation.Stages.Models;

/// <summary>
/// C++ → C transpiler. Locates the entry-point <c>.cpp</c> file in the project,
/// rewrites it to plain C via <see cref="CppLexer"/> + <see cref="CppParser"/> +
/// <see cref="CppToCEmitter"/>, and emits the original include directives plus any
/// extra C runtime headers required by the emitter.
/// </summary>
public sealed class CppToCTranspiler : ILanguageTranspiler
{
    /// <inheritdoc />
    public SupportedLanguage Language => SupportedLanguage.Cpp;

    /// <inheritdoc />
    public Task<TranspiledSource> TranspileAsync(LanguageTranspilerInput input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);

        return Task.FromResult(Transpile(input));
    }

    private static TranspiledSource Transpile(LanguageTranspilerInput input)
    {
        try
        {
            var mainFileName = input.Files.Keys
                .FirstOrDefault(k => k.EndsWith(".cpp", StringComparison.OrdinalIgnoreCase))
                ?? input.Files.Keys.First();

            var cppSource = input.Files[mainFileName];
            var cIncludes = CppIncludeMapper.ExtractCIncludes(cppSource);

            var tokens = new CppLexer(cppSource).Tokenize();
            var ast = new CppParser(tokens).Parse();

            var emitter = new CppToCEmitter();
            var cBody = emitter.Emit(ast);

            var includeLines = new List<string>(cIncludes);
            if (emitter.NeedsStdlib && !includeLines.Exists(l => l.Contains("stdlib.h")))
            {
                includeLines.Add("#include <stdlib.h>");
            }

            var cSource = string.Join("\n", includeLines) + "\n\n" + cBody;

            var mappings = emitter.LineMappings
                .Select(m => new SourceLineMapping(mainFileName, m.CppLine))
                .ToList();

            return new TranspiledSource(SupportedLanguage.C, cSource, mappings);
        }
        catch (Exception ex) when (ex is UnsupportedSyntaxException or CompilerInvariantException)
        {
            throw new CompilationFailedException(ex.Message);
        }
    }
}
