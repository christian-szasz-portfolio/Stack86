namespace Stack86.Logic.Languages.Cpp;

/// <summary>
/// Maps a line in the emitted C output back to a line in the original C++ source.
/// </summary>
/// <param name="CLine">The 1-based line number in the generated C source.</param>
/// <param name="CppLine">The 1-based line number in the original C++ source.</param>
/// <param name="CppFile">The filename of the original C++ source.</param>
public sealed record CppLineMapping(int CLine, int CppLine, string CppFile);
