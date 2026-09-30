namespace Stack86.Logic.Languages.TypeScript;

using System.Text.RegularExpressions;

/// <summary>
/// Post-processes JavaScript emitted by TypeScript's <c>transpileModule</c>
/// to inline enum member values and remove the IIFE enum declarations that
/// the Stack86 JS frontend cannot parse.
/// </summary>
/// <remarks>
/// <para>
/// TypeScript's single-file <c>transpileModule</c> API emits enums as an IIFE:
/// <code>
/// var Direction;
/// (function (Direction) {
///     Direction[Direction["Up"] = 0] = "Up";
///     Direction[Direction["Down"] = 1] = "Down";
/// })(Direction || (Direction = {}));
/// </code>
/// This class extracts the name → value mappings, replaces all
/// <c>EnumName.MemberName</c> references with the numeric literal,
/// and strips the IIFE block.
/// </para>
/// </remarks>
public static partial class TsEnumInliner
{
    /// <summary>
    /// Pattern matching a complete TypeScript enum IIFE block:
    /// <c>var Name;\n(function (Name) { ... })(Name || (Name = {}));</c>.
    /// </summary>
    private static readonly Regex EnumIifePattern = BuildEnumIifeRegex();

    /// <summary>
    /// Pattern matching a single enum member assignment inside the IIFE:
    /// <c>Name[Name["Member"] = value] = "Member";</c>.
    /// </summary>
    private static readonly Regex MemberPattern = BuildMemberRegex();

    /// <summary>
    /// Inlines all TypeScript enum references in the transpiled JavaScript
    /// and removes the IIFE enum declarations.
    /// </summary>
    /// <param name="js">Transpiled JavaScript containing enum IIFEs.</param>
    /// <returns>JavaScript with enum member references replaced by numeric literals.</returns>
    public static string InlineEnums(string js)
    {
        // Collect all enum definitions first, then apply replacements
        var enums = new Dictionary<string, Dictionary<string, string>>();

        var stripped = EnumIifePattern.Replace(js, match =>
        {
            var enumName = match.Groups["name"].Value;
            var body = match.Groups["body"].Value;
            var members = new Dictionary<string, string>();

            foreach (Match m in MemberPattern.Matches(body))
            {
                if (m.Groups["enum"].Value == enumName)
                {
                    members[m.Groups["member"].Value] = m.Groups["value"].Value;
                }
            }

            enums[enumName] = members;
            return string.Empty;
        });

        // Replace EnumName.MemberName with the numeric value
        foreach (var (enumName, members) in enums)
        {
            foreach (var (memberName, value) in members)
            {
                stripped = stripped.Replace($"{enumName}.{memberName}", value);
            }
        }

        // Clean up excessive blank lines left by removal
        stripped = CollapseBlankLines().Replace(stripped, "\n");

        return stripped;
    }

    [GeneratedRegex(
        @"var\s+(?<name>[A-Za-z_]\w*)\s*;\s*\n\(function\s*\(\k<name>\)\s*\{(?<body>[^}]*(?:\{[^}]*\}[^}]*)*)\}\)\(\k<name>\s*\|\|\s*\(\k<name>\s*=\s*\{\}\)\);",
        RegexOptions.Multiline)]
    private static partial Regex BuildEnumIifeRegex();

    [GeneratedRegex(
        @"(?<enum>[A-Za-z_]\w*)\[(?:\k<enum>)\[""(?<member>[^""]+)""\]\s*=\s*(?<value>-?\d+)\]",
        RegexOptions.None)]
    private static partial Regex BuildMemberRegex();

    [GeneratedRegex(@"\n{3,}")]
    private static partial Regex CollapseBlankLines();
}
