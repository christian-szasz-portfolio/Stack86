namespace Stack86.Logic.Languages.JavaScript;

using System.Text;
using System.Text.RegularExpressions;
using Stack86.Logic.Compilation.Stages.Models;

/// <summary>
/// JavaScript preprocessor: merges all <c>.js</c> files into a single translation unit and
/// resolves intra-project ES module imports. An <c>import { add } from './helper.js'</c> whose
/// relative specifier names another project file is treated as a module dependency: the imported
/// file is emitted before the importer (dependency order) and the import statement is stripped,
/// since all project symbols share one flat namespace after merging. <c>export</c> modifiers are
/// removed from declarations (and standalone <c>export { ... }</c> / <c>export default</c>
/// statements dropped) so the merged source parses as plain script code. Imports whose specifier
/// does not resolve to a project file are left in place for the capability validator to flag.
/// </summary>
public sealed partial class JsSourcePreprocessor : MergingLanguagePreprocessor
{
    /// <inheritdoc />
    public override SupportedLanguage Language => SupportedLanguage.JavaScript;

    /// <inheritdoc />
    protected override IReadOnlyList<string> MainFileCandidates => ["main.js", "index.js"];

    /// <inheritdoc />
    protected override PreprocessedSource Merge(IReadOnlyList<KeyValuePair<string, string>> orderedFiles)
    {
        var entryFile = orderedFiles[0].Key;
        var contentByFile = new Dictionary<string, string>(StringComparer.Ordinal);
        var moduleToFile = new Dictionary<string, string>(StringComparer.Ordinal);

        foreach (var (fileName, content) in orderedFiles)
        {
            contentByFile[fileName] = content;
            moduleToFile[ModuleName(fileName)] = fileName;
        }

        var dependencies = new Dictionary<string, List<string>>(StringComparer.Ordinal);
        var strippedLines = new Dictionary<string, HashSet<int>>(StringComparer.Ordinal);

        foreach (var (fileName, content) in orderedFiles)
        {
            var deps = new List<string>();
            var strip = new HashSet<int>();
            var lines = content.Split('\n');

            for (var i = 0; i < lines.Length; i++)
            {
                var specifier = TryGetImportSpecifier(lines[i]);
                if (specifier is not null &&
                    ResolveSpecifier(specifier, moduleToFile) is { } depFile &&
                    !string.Equals(depFile, fileName, StringComparison.Ordinal))
                {
                    deps.Add(depFile);
                    strip.Add(i);
                }
            }

            dependencies[fileName] = deps;
            strippedLines[fileName] = strip;
        }

        var order = OrderByDependencies(entryFile, orderedFiles, dependencies);
        return BuildMergedSource(order, contentByFile, strippedLines);
    }

    private static string ModuleName(string fileName)
    {
        var name = fileName;
        var separator = name.LastIndexOfAny(['/', '\\']);
        if (separator >= 0)
        {
            name = name[(separator + 1)..];
        }

        foreach (var extension in new[] { ".js", ".mjs", ".cjs" })
        {
            if (name.EndsWith(extension, StringComparison.Ordinal))
            {
                return name[..^extension.Length];
            }
        }

        return name;
    }

    private static string? TryGetImportSpecifier(string line)
    {
        var fromMatch = ImportFromRegex().Match(line);
        if (fromMatch.Success)
        {
            return fromMatch.Groups["spec"].Value;
        }

        var sideEffectMatch = SideEffectImportRegex().Match(line);
        return sideEffectMatch.Success ? sideEffectMatch.Groups["spec"].Value : null;
    }

    private static string? ResolveSpecifier(string specifier, Dictionary<string, string> moduleToFile)
    {
        // Only relative specifiers can name a project file; bare specifiers are external.
        if (!specifier.StartsWith("./", StringComparison.Ordinal) &&
            !specifier.StartsWith("../", StringComparison.Ordinal) &&
            !specifier.StartsWith('/'))
        {
            return null;
        }

        var stem = ModuleName(specifier);
        return moduleToFile.TryGetValue(stem, out var file) ? file : null;
    }

    private static List<string> OrderByDependencies(
        string entryFile,
        IReadOnlyList<KeyValuePair<string, string>> orderedFiles,
        IReadOnlyDictionary<string, List<string>> dependencies)
    {
        var order = new List<string>(orderedFiles.Count);
        var visited = new HashSet<string>(StringComparer.Ordinal);

        // Depth-first post-order from the entry point emits each file after its
        // project dependencies. Files unreachable from the entry point are appended
        // afterwards in their original (entry-first, then name-sorted) order.
        Visit(entryFile, dependencies, visited, order);
        foreach (var (fileName, _) in orderedFiles)
        {
            Visit(fileName, dependencies, visited, order);
        }

        return order;
    }

    private static void Visit(
        string file,
        IReadOnlyDictionary<string, List<string>> dependencies,
        HashSet<string> visited,
        List<string> order)
    {
        if (!visited.Add(file))
        {
            return;
        }

        foreach (var dependency in dependencies[file])
        {
            Visit(dependency, dependencies, visited, order);
        }

        order.Add(file);
    }

    private static PreprocessedSource BuildMergedSource(
        IReadOnlyList<string> order,
        Dictionary<string, string> contentByFile,
        Dictionary<string, HashSet<int>> strippedLines)
    {
        var output = new StringBuilder();
        var mappings = new List<SourceLineMapping>();

        foreach (var file in order)
        {
            var lines = contentByFile[file].Split('\n');
            var strip = strippedLines[file];

            for (var i = 0; i < lines.Length; i++)
            {
                output.Append(strip.Contains(i) ? string.Empty : RewriteExport(lines[i]));
                output.Append('\n');
                mappings.Add(new SourceLineMapping(file, i + 1));
            }
        }

        return new PreprocessedSource(output.ToString(), mappings, new HashSet<string>());
    }

    private static string RewriteExport(string line)
    {
        var declMatch = ExportDeclarationRegex().Match(line);
        if (declMatch.Success)
        {
            return declMatch.Groups["indent"].Value + declMatch.Groups["rest"].Value;
        }

        var defaultDeclMatch = ExportDefaultDeclarationRegex().Match(line);
        if (defaultDeclMatch.Success)
        {
            return defaultDeclMatch.Groups["indent"].Value + defaultDeclMatch.Groups["rest"].Value;
        }

        // Standalone `export { ... }` lists and `export default <expr>;` statements have no
        // runtime effect once everything shares one namespace, so drop them entirely.
        if (ExportListRegex().IsMatch(line) || ExportDefaultExpressionRegex().IsMatch(line))
        {
            return string.Empty;
        }

        return line;
    }

    [GeneratedRegex(@"^\s*import\s+.+?\s+from\s+['""](?<spec>[^'""]+)['""]\s*;?\s*$")]
    private static partial Regex ImportFromRegex();

    [GeneratedRegex(@"^\s*import\s+['""](?<spec>[^'""]+)['""]\s*;?\s*$")]
    private static partial Regex SideEffectImportRegex();

    [GeneratedRegex(@"^(?<indent>\s*)export\s+(?<rest>(?:async\s+)?(?:function|const|let|var|class)\b.*)$")]
    private static partial Regex ExportDeclarationRegex();

    [GeneratedRegex(@"^(?<indent>\s*)export\s+default\s+(?<rest>(?:async\s+)?(?:function|class)\b.*)$")]
    private static partial Regex ExportDefaultDeclarationRegex();

    [GeneratedRegex(@"^\s*export\s*\{[^}]*\}\s*(?:from\s+['""][^'""]+['""])?\s*;?\s*$")]
    private static partial Regex ExportListRegex();

    [GeneratedRegex(@"^\s*export\s+default\s+.+;?\s*$")]
    private static partial Regex ExportDefaultExpressionRegex();
}
