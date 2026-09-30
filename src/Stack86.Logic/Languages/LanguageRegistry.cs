namespace Stack86.Logic.Languages;

using Stack86.Common.Exceptions;

/// <summary>
/// Default <see cref="ILanguageRegistry"/> implementation. Indexes every registered
/// <see cref="ILanguageService"/> instance under each <see cref="ILanguageService"/>-derived
/// interface it implements, keyed by the service's declared <see cref="ILanguageService.Language"/>.
/// </summary>
public sealed class LanguageRegistry : ILanguageRegistry
{
    private readonly Dictionary<(Type ServiceType, SupportedLanguage Language), ILanguageService> services = [];

    /// <summary>
    /// Initialises a new <see cref="LanguageRegistry"/> from the DI-resolved
    /// collection of per-language services.
    /// </summary>
    /// <param name="services">All registered <see cref="ILanguageService"/> instances.</param>
    /// <exception cref="DuplicateRegistrationException">
    /// Thrown when two services of the same abstraction claim the same language.
    /// </exception>
    public LanguageRegistry(IEnumerable<ILanguageService> services)
    {
        ArgumentNullException.ThrowIfNull(services);

        foreach (var service in services)
        {
            var serviceInterfaces = service
                .GetType()
                .GetInterfaces()
                .Where(i => typeof(ILanguageService).IsAssignableFrom(i) && i != typeof(ILanguageService));

            foreach (var iface in serviceInterfaces)
            {
                var key = (iface, service.Language);
                if (this.services.ContainsKey(key))
                {
                    throw new DuplicateRegistrationException(
                        $"Duplicate language service registration: '{iface.FullName}' for language '{service.Language.Value}'.");
                }

                this.services[key] = service;
            }
        }
    }

    /// <inheritdoc />
    public TService? Get<TService>(SupportedLanguage language)
        where TService : class, ILanguageService
    {
        ArgumentNullException.ThrowIfNull(language);
        return this.services.TryGetValue((typeof(TService), language), out var service)
            ? (TService)service
            : null;
    }
}
