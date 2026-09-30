namespace Stack86.Logic.Languages.Cpp;

/// <summary>
/// Maps C++ standard library includes to their C equivalents.
/// </summary>
public static class CppIncludeMapper
{
    private static readonly Dictionary<string, string?> IncludeMap = new(StringComparer.OrdinalIgnoreCase)
    {
        ["iostream"] = "stdio.h",
        ["cstdio"] = "stdio.h",
        ["cstdlib"] = "stdlib.h",
        ["cstring"] = "string.h",
        ["cmath"] = null,           // no 8086 equivalent
        ["string"] = "string.h",    // string class → char* with string.h helpers
        ["cassert"] = null,
        ["climits"] = null,
        ["cctype"] = null,
    };

    /// <summary>
    /// Converts a C++ include name to a C include name, or <c>null</c> if no mapping exists.
    /// </summary>
    /// <param name="cppInclude">The C++ include name without angle brackets (e.g. "iostream").</param>
    /// <returns>The C include name (e.g. "stdio.h"), or <c>null</c> if the include should be removed.</returns>
    public static string? MapInclude(string cppInclude)
    {
        return IncludeMap.TryGetValue(cppInclude, out var mapped) ? mapped : null;
    }

    /// <summary>
    /// Transforms all <c>#include &lt;...&gt;</c> directives in the source from C++ to C.
    /// Unknown includes are preserved as-is.
    /// </summary>
    public static string TransformIncludes(string source)
    {
        var lines = source.Split('\n');
        var result = new List<string>();
        var emittedIncludes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var line in lines)
        {
            var trimmed = line.TrimStart();

            if (trimmed.StartsWith("#include", StringComparison.Ordinal))
            {
                var include = ExtractIncludeName(trimmed);
                if (include is not null)
                {
                    var mapped = MapInclude(include);
                    if (mapped is not null && emittedIncludes.Add(mapped))
                    {
                        result.Add($"#include <{mapped}>");
                    }

                    // Else: mapped to null → suppress the include
                    continue;
                }
            }

            result.Add(line);
        }

        return string.Join("\n", result);
    }

    /// <summary>
    /// Extracts the C include directives needed for the given C++ source.
    /// Returns lines like <c>#include &lt;stdio.h&gt;</c>, deduplicated.
    /// </summary>
    public static IReadOnlyList<string> ExtractCIncludes(string source)
    {
        var lines = source.Split('\n');
        var includes = new List<string>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var line in lines)
        {
            var trimmed = line.TrimStart();
            if (!trimmed.StartsWith("#include", StringComparison.Ordinal))
            {
                continue;
            }

            var name = ExtractIncludeName(trimmed);
            if (name is null)
            {
                continue;
            }

            var mapped = MapInclude(name);
            if (mapped is not null && seen.Add(mapped))
            {
                includes.Add($"#include <{mapped}>");
            }
        }

        return includes;
    }

    private static string? ExtractIncludeName(string directive)
    {
        var start = directive.IndexOf('<');
        var end = directive.IndexOf('>');
        if (start >= 0 && end > start)
        {
            return directive[(start + 1)..end];
        }

        start = directive.IndexOf('"');
        if (start >= 0)
        {
            end = directive.IndexOf('"', start + 1);
            if (end > start)
            {
                return directive[(start + 1)..end];
            }
        }

        return null;
    }
}
