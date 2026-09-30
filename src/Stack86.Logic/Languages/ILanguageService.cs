namespace Stack86.Logic.Languages;

/// <summary>
/// Marker interface for any per-language pipeline service (capability validator,
/// frontend, transpiler, preprocessor, external code validator). The
/// <see cref="ILanguageRegistry"/> enumerates and indexes implementations by their
/// declared <see cref="Language"/>.
/// </summary>
public interface ILanguageService
{
    /// <summary>
    /// Gets the source language this service handles. Each per-language service
    /// targets exactly one language; languages that share infrastructure (e.g. C and
    /// C++ pre-transpile) use thin wrapper classes to expose the shared logic under
    /// each language identifier.
    /// </summary>
    SupportedLanguage Language { get; }
}
