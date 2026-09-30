namespace Stack86.Logic.Compilation.Stages.Models;

/// <summary>
/// Maps a line in a merged/transpiled source back to its original file and line number.
/// </summary>
/// <param name="File">The original source file.</param>
/// <param name="OriginalLine">The line number within the original file (1-based).</param>
public sealed record SourceLineMapping(string File, int OriginalLine);
