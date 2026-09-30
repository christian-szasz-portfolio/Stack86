namespace Stack86.Logic.Languages;

/// <summary>
/// Lookup over per-language pipeline services. Stage handlers depend on this single
/// abstraction instead of injecting every concrete per-language service explicitly.
/// </summary>
public interface ILanguageRegistry
{
    /// <summary>
    /// Returns the registered <typeparamref name="TService"/> for <paramref name="language"/>,
    /// or <see langword="null"/> if no implementation is registered for that combination.
    /// </summary>
    /// <typeparam name="TService">The per-language service abstraction (must extend <see cref="ILanguageService"/>).</typeparam>
    /// <param name="language">The source language to look up.</param>
    TService? Get<TService>(SupportedLanguage language)
        where TService : class, ILanguageService;
}
