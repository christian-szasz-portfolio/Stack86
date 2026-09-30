namespace Stack86.Logic.Languages.CSharp;

using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Stack86.Common.Exceptions;
using Stack86.Logic.Compilation.Models;

/// <summary>
/// Compiles C# source code in-memory using Roslyn and produces a PE byte array.
/// No user code is executed — only compiled to IL.
/// </summary>
public sealed class RoslynAnalyzer
{
    /// <summary>
    /// The set of BCL assemblies referenced during compilation.
    /// Covers the core types needed for basic programs (Console, Math, String, etc.).
    /// </summary>
    private static readonly Lazy<List<MetadataReference>> CoreReferences = new(LoadCoreReferences);

    /// <summary>
    /// Extracts string literals from the Roslyn syntax trees for later embedding in the data segment.
    /// </summary>
    public static IReadOnlyDictionary<string, string> ExtractStringLiterals(
        IReadOnlyDictionary<string, string> files)
    {
        var literals = new Dictionary<string, string>();
        var counter = 0;

        foreach (var (_, source) in files)
        {
            var tree = CSharpSyntaxTree.ParseText(source);
            var root = tree.GetRoot();

            foreach (var token in root.DescendantTokens())
            {
                if (token.IsKind(SyntaxKind.StringLiteralToken))
                {
                    var value = token.ValueText;
                    if (!string.IsNullOrEmpty(value) && !literals.ContainsValue(value))
                    {
                        literals[$"_str_{counter++}"] = value;
                    }
                }
            }
        }

        return literals;
    }

    /// <summary>
    /// Analyses the provided C# source files via Roslyn and returns the
    /// <see cref="CSharpCompilation"/> for semantic inspection. No PE is emitted.
    /// </summary>
    /// <param name="files">Source files keyed by filename.</param>
    /// <returns>
    /// The Roslyn compilation (with semantic model) and mapped diagnostics.
    /// </returns>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "DI-injected service; must remain instance method.")]
    public (CSharpCompilation Compilation, IReadOnlyList<DiagnosticDto> Diagnostics) Analyze(
        IReadOnlyDictionary<string, string> files)
    {
        var compilation = CreateCompilation(files);
        var roslynDiags = compilation.GetDiagnostics();
        var diagnostics = MapDiagnostics(roslynDiags);

        var hasErrors = roslynDiags.Any(d => d.Severity == DiagnosticSeverity.Error);
        if (hasErrors)
        {
            var errors = diagnostics.Where(d =>
                d.Message.StartsWith("[Error]", StringComparison.Ordinal)).ToList();

            var errorSummary = errors.Count > 0
                ? errors[0].Message
                : "Roslyn compilation failed";

            throw new CompilationFailedException(errorSummary);
        }

        return (compilation, diagnostics);
    }

    /// <summary>
    /// Compiles the provided C# source files into an in-memory PE image.
    /// </summary>
    /// <param name="files">Source files keyed by filename.</param>
    /// <returns>The compiled PE bytes and mapped diagnostics.</returns>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "DI-injected service; must remain instance method.")]
    public (byte[] PeBytes, IReadOnlyList<DiagnosticDto> Diagnostics) Compile(
        IReadOnlyDictionary<string, string> files)
    {
        var compilation = CreateCompilation(files);

        using var peStream = new MemoryStream();
        var emitResult = compilation.Emit(peStream);

        var diagnostics = MapDiagnostics(emitResult.Diagnostics);

        if (!emitResult.Success)
        {
            var errors = diagnostics.Where(d =>
                d.Message.StartsWith("[Error]", StringComparison.Ordinal)).ToList();

            var errorSummary = errors.Count > 0
                ? errors[0].Message
                : "Roslyn compilation failed";

            throw new CompilationFailedException(errorSummary);
        }

        return (peStream.ToArray(), diagnostics);
    }

    private static CSharpCompilation CreateCompilation(IReadOnlyDictionary<string, string> files)
    {
        var syntaxTrees = new List<SyntaxTree>(files.Count);
        foreach (var (fileName, source) in files)
        {
            var tree = CSharpSyntaxTree.ParseText(
                source,
                CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.Latest),
                path: fileName);
            syntaxTrees.Add(tree);
        }

        return CSharpCompilation.Create(
            assemblyName: "UserAssembly",
            syntaxTrees: syntaxTrees,
            references: CoreReferences.Value,
            options: new CSharpCompilationOptions(OutputKind.ConsoleApplication)
                .WithOptimizationLevel(OptimizationLevel.Release)
                .WithPlatform(Platform.AnyCpu));
    }

    private static List<DiagnosticDto> MapDiagnostics(
        IEnumerable<Diagnostic> roslynDiagnostics)
    {
        var result = new List<DiagnosticDto>();

        foreach (var diag in roslynDiagnostics)
        {
            if (diag.Severity == DiagnosticSeverity.Hidden)
            {
                continue;
            }

            var lineSpan = diag.Location.GetMappedLineSpan();
            var prefix = diag.Severity == DiagnosticSeverity.Error ? "[Error]" : "[Warn]";

            result.Add(new DiagnosticDto
            {
                Message = $"{prefix} {diag.GetMessage()}",
                Line = lineSpan.StartLinePosition.Line + 1,
                Column = lineSpan.StartLinePosition.Character + 1,
                SourceFile = lineSpan.Path,
            });
        }

        return result;
    }

    private static List<MetadataReference> LoadCoreReferences()
    {
        var references = new List<MetadataReference>();

        // Reference the runtime assemblies needed for basic programs
        var trustedAssemblies = AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES") as string;
        if (trustedAssemblies is not null)
        {
            var neededAssemblies = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "System.Runtime",
                "System.Console",
                "System.Private.CoreLib",
                "System.Runtime.Extensions",
                "System.Collections",
                "System.Linq",
                "netstandard",
            };

            foreach (var path in trustedAssemblies.Split(Path.PathSeparator))
            {
                var fileName = Path.GetFileNameWithoutExtension(path);
                if (neededAssemblies.Contains(fileName))
                {
                    references.Add(MetadataReference.CreateFromFile(path));
                }
            }
        }

        return references;
    }
}
