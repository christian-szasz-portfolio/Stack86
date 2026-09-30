namespace Stack86.Logic.Languages.Cpp;

using Stack86.Logic.Languages.C;
using Stack86.Logic.Pipeline.Ir;

/// <summary>
/// Capability validator for C++. Pre-transpile, the C++ source is scanned with the
/// same target-keyword rules as plain C, since the unsupported types
/// (<c>float</c>, <c>double</c>, <c>long</c>) and the <c>goto</c> statement are
/// equally unsupported by the 8086 backend.
/// </summary>
/// <param name="inner">The C validator whose rules are reused.</param>
public sealed class CppCapabilityValidator(CCapabilityValidator inner) : ILanguageCapabilityValidator
{
    /// <inheritdoc />
    public SupportedLanguage Language => SupportedLanguage.Cpp;

    /// <inheritdoc />
    public IReadOnlyList<IrDiagnostic> Validate(string source) => inner.Validate(source);
}
