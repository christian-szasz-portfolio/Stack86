namespace Stack86.Logic.Languages.C;

using System.Text;
using System.Text.RegularExpressions;
using Stack86.Common.Exceptions;

/// <summary>
/// Resolves <c>#include "file.h"</c> directives and expands object-like <c>#define</c> macros.
/// Angle-bracket includes (<c>&lt;stdio.h&gt;</c>) are left as-is for the external validator (TCC).
/// </summary>
public static partial class CIncludePreprocessor
{
    /// <summary>
    /// Resolves quoted includes starting from <paramref name="mainFile"/> and returns the merged source.
    /// </summary>
    /// <param name="files">All project files keyed by filename.</param>
    /// <param name="mainFile">The entry-point file name (e.g. "main.c").</param>
    /// <returns>A <see cref="PreprocessResult"/> with the merged source.</returns>
    public static PreprocessResult Preprocess(
        IReadOnlyDictionary<string, string> files,
        string mainFile)
    {
        if (!files.ContainsKey(mainFile))
        {
            throw new CompilationFailedException($"Main file '{mainFile}' not found in project files.");
        }

        var output = new StringBuilder();
        var mappings = new List<LineMapping>();
        var macros = new Dictionary<string, string>();
        var includedOnce = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var systemHeaders = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        // Process the main file (and everything it #includes).
        var error = ProcessFile(files, mainFile, output, mappings, macros, includedOnce, systemHeaders);
        if (error is not null)
        {
            throw new CompilationFailedException(error);
        }

        // Merge remaining .c files so multi-file projects compile as a single translation unit.
        foreach (var fileName in files.Keys)
        {
            if (fileName.EndsWith(".c", StringComparison.OrdinalIgnoreCase)
                && !includedOnce.Contains(fileName))
            {
                error = ProcessFile(files, fileName, output, mappings, macros, includedOnce, systemHeaders);
                if (error is not null)
                {
                    throw new CompilationFailedException(error);
                }
            }
        }

        return new PreprocessResult(output.ToString(), mappings, systemHeaders);
    }

    /// <summary>
    /// Maps a merged-source line number back to the original file and line.
    /// </summary>
    /// <param name="mappings">The line mappings from <see cref="PreprocessResult"/>.</param>
    /// <param name="mergedLine">The 1-based line number in the merged output.</param>
    /// <returns>The original file name and line number, or <c>null</c> if out of range.</returns>
    public static LineMapping? MapLine(IReadOnlyList<LineMapping> mappings, int mergedLine)
    {
        var index = mergedLine - 1;
        if (index < 0 || index >= mappings.Count)
        {
            return null;
        }

        return mappings[index];
    }

    private static string? ProcessFile(
        IReadOnlyDictionary<string, string> files,
        string fileName,
        StringBuilder output,
        List<LineMapping> mappings,
        Dictionary<string, string> macros,
        HashSet<string> includedOnce,
        HashSet<string> systemHeaders)
    {
        // Include-once: skip files that have already been processed.
        if (!includedOnce.Add(fileName))
        {
            return null;
        }

        if (!files.TryGetValue(fileName, out var content))
        {
            return $"Include file not found: '{fileName}'.";
        }

        var lines = content.Split('\n');
        for (var i = 0; i < lines.Length; i++)
        {
            var line = lines[i];

            var includeMatch = QuotedIncludeRegex().Match(line);
            if (includeMatch.Success)
            {
                var includedFile = includeMatch.Groups[1].Value;
                var error = ProcessFile(files, includedFile, output, mappings, macros, includedOnce, systemHeaders);
                if (error is not null)
                {
                    return error;
                }

                continue;
            }

            // Track angle-bracket system includes (e.g. <stdio.h>)
            var sysIncludeMatch = SystemIncludeRegex().Match(line);
            if (sysIncludeMatch.Success)
            {
                systemHeaders.Add(sysIncludeMatch.Groups[1].Value);
            }

            var defineMatch = DefineRegex().Match(line);
            if (defineMatch.Success)
            {
                var name = defineMatch.Groups[1].Value;
                var value = defineMatch.Groups[2].Value.TrimEnd('\r');

                // Expand any previously defined macros in the replacement value
                macros[name] = ExpandMacros(value, macros);
                continue;
            }

            output.AppendLine(ExpandMacros(line, macros));
            mappings.Add(new LineMapping(fileName, i + 1));
        }

        return null;
    }

    private static string ExpandMacros(string line, Dictionary<string, string> macros)
    {
        if (macros.Count == 0 || line.Length == 0)
        {
            return line;
        }

        var pattern = string.Join("|", macros.Keys.Select(k => $@"\b{Regex.Escape(k)}\b"));
        return Regex.Replace(line, pattern, m => macros[m.Value]);
    }

    [GeneratedRegex(@"^\s*#\s*include\s+""([^""]+)""", RegexOptions.Compiled)]
    private static partial Regex QuotedIncludeRegex();

    [GeneratedRegex(@"^\s*#\s*include\s+<([^>]+)>", RegexOptions.Compiled)]
    private static partial Regex SystemIncludeRegex();

    [GeneratedRegex(@"^\s*#\s*define\s+([A-Za-z_]\w*)(?:\s+(.*?))?\s*$", RegexOptions.Compiled)]
    private static partial Regex DefineRegex();

    /// <summary>
    /// Tracks the original file and line number for each line in the merged output.
    /// </summary>
    public sealed record LineMapping(string File, int OriginalLine);

    /// <summary>
    /// The result of preprocessing: merged source and per-line origin mapping.
    /// </summary>
    /// <summary>
    /// The result of preprocessing: merged source, per-line origin mapping, and system headers found.
    /// </summary>
    public sealed record PreprocessResult(
        string MergedSource,
        IReadOnlyList<LineMapping> LineMappings,
        IReadOnlySet<string> SystemHeaders);
}
