namespace Stack86.Logic.Languages;

using Microsoft.Extensions.DependencyInjection;

/// <summary>
/// DI helpers for registering per-language pipeline services in a way that the
/// <see cref="LanguageRegistry"/> can enumerate them.
/// </summary>
public static class LanguageServiceCollectionExtensions
{
    /// <summary>
    /// Registers <typeparamref name="TValidator"/> as a singleton both as itself
    /// and as a forwarded <see cref="ILanguageService"/>, so the
    /// <see cref="LanguageRegistry"/>'s <see cref="IEnumerable{T}"/> injection
    /// captures it without producing a second instance.
    /// </summary>
    /// <typeparam name="TValidator">The concrete validator type.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <returns>The same service collection for chaining.</returns>
    public static IServiceCollection AddLanguageCapabilityValidator<TValidator>(this IServiceCollection services)
        where TValidator : class, ILanguageCapabilityValidator
    {
        ArgumentNullException.ThrowIfNull(services);
        services.AddSingleton<TValidator>();
        services.AddSingleton<ILanguageService>(sp => sp.GetRequiredService<TValidator>());
        return services;
    }

    /// <summary>
    /// Registers <typeparamref name="TFrontend"/> as a singleton both as itself
    /// and as a forwarded <see cref="ILanguageService"/>.
    /// </summary>
    /// <typeparam name="TFrontend">The concrete frontend type.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <returns>The same service collection for chaining.</returns>
    public static IServiceCollection AddLanguageFrontend<TFrontend>(this IServiceCollection services)
        where TFrontend : class, ILanguageFrontend
    {
        ArgumentNullException.ThrowIfNull(services);
        services.AddSingleton<TFrontend>();
        services.AddSingleton<ILanguageService>(sp => sp.GetRequiredService<TFrontend>());
        return services;
    }

    /// <summary>
    /// Registers <typeparamref name="TTranspiler"/> as a singleton both as itself
    /// and as a forwarded <see cref="ILanguageService"/>.
    /// </summary>
    /// <typeparam name="TTranspiler">The concrete transpiler type.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <returns>The same service collection for chaining.</returns>
    public static IServiceCollection AddLanguageTranspiler<TTranspiler>(this IServiceCollection services)
        where TTranspiler : class, ILanguageTranspiler
    {
        ArgumentNullException.ThrowIfNull(services);
        services.AddSingleton<TTranspiler>();
        services.AddSingleton<ILanguageService>(sp => sp.GetRequiredService<TTranspiler>());
        return services;
    }

    /// <summary>
    /// Registers <typeparamref name="TPreprocessor"/> as a singleton both as itself
    /// and as a forwarded <see cref="ILanguageService"/>.
    /// </summary>
    /// <typeparam name="TPreprocessor">The concrete preprocessor type.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <returns>The same service collection for chaining.</returns>
    public static IServiceCollection AddLanguagePreprocessor<TPreprocessor>(this IServiceCollection services)
        where TPreprocessor : class, ILanguagePreprocessor
    {
        ArgumentNullException.ThrowIfNull(services);
        services.AddSingleton<TPreprocessor>();
        services.AddSingleton<ILanguageService>(sp => sp.GetRequiredService<TPreprocessor>());
        return services;
    }

    /// <summary>
    /// Registers <typeparamref name="TValidator"/> as a singleton both as itself
    /// and as a forwarded <see cref="ILanguageService"/>.
    /// </summary>
    /// <typeparam name="TValidator">The concrete external-code-validator type.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <returns>The same service collection for chaining.</returns>
    public static IServiceCollection AddExternalCodeValidator<TValidator>(this IServiceCollection services)
        where TValidator : class, IExternalCodeValidator
    {
        ArgumentNullException.ThrowIfNull(services);
        services.AddSingleton<TValidator>();
        services.AddSingleton<ILanguageService>(sp => sp.GetRequiredService<TValidator>());
        return services;
    }
}
