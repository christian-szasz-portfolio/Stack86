namespace Stack86.Logic.Compilation;

using Microsoft.Extensions.DependencyInjection;
using Stack86.Logic.Compilation.Models;
using Stack86.Logic.Compilation.Providers;
using Stack86.Logic.Languages;
using Stack86.Logic.Languages.C;
using Stack86.Logic.Languages.Cpp;
using Stack86.Logic.Languages.CSharp;
using Stack86.Logic.Languages.JavaScript;
using Stack86.Logic.Languages.TypeScript;
using Stack86.Logic.Pipeline;
using Stack86.Logic.Pipeline.Ir;
using Stack86.Logic.Pipeline.X86Conversion;
using Stack86.Logic.Resilience;

/// <summary>
/// Composition root for the Stack86 compilation pipeline. Registers every per-language
/// service in a single place so the production host (<c>Startup</c>) and test fixtures
/// share the same wiring.
/// </summary>
public static class CompilationPipelineServiceCollectionExtensions
{
    /// <summary>
    /// Registers all per-language pipeline services (capability validators, registry,
    /// IR validator, target profile, Roslyn analyser) along with the read-side
    /// <see cref="CompilerProvider"/> and <see cref="LanguageProvider"/>. External integrations
    /// that require configuration (<c>TccSettings</c>, <c>TsTranspilerSettings</c>) remain the
    /// caller's responsibility.
    /// </summary>
    /// <param name="services">The service collection to extend.</param>
    /// <returns>The same service collection for chaining.</returns>
    public static IServiceCollection AddStack86CompilationPipeline(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services
            .AddLanguageCapabilityValidator<CCapabilityValidator>()
            .AddLanguageCapabilityValidator<CppCapabilityValidator>()
            .AddLanguageCapabilityValidator<CSharpCapabilityValidator>()
            .AddLanguageCapabilityValidator<JsCapabilityValidator>()
            .AddLanguageCapabilityValidator<TsCapabilityValidator>();

        services
            .AddLanguageFrontend<CFrontend>()
            .AddLanguageFrontend<CSharpFrontend>()
            .AddLanguageFrontend<JsFrontend>();

        services
            .AddLanguageTranspiler<CppToCTranspiler>()
            .AddLanguageTranspiler<TypeScriptToJsTranspiler>();

        services.AddLanguagePreprocessor<CSourcePreprocessor>();
        services.AddLanguagePreprocessor<CSharpSourcePreprocessor>();
        services.AddLanguagePreprocessor<JsSourcePreprocessor>();

        // External compiler validators (real toolchains in syntax-check mode). Each degrades
        // to a warning when its tool is missing, so compilation still proceeds. Settings are
        // registered with defaults here; the host may override them from configuration.
        services.AddSingleton(new NodeCheckSettings());
        services
            .AddExternalCodeValidator<JsExternalValidator>();

        services.AddSingleton<ILanguageRegistry, LanguageRegistry>();

        services.AddSingleton<ITargetCapabilityProfile, X8086CapabilityProfile>();
        services.AddSingleton<IIrValidator, IrCapabilityValidator>();
        services.AddSingleton<RoslynAnalyzer>();

        services.AddSingleton<ResilienceOptions>();
        services.AddSingleton<IResiliencePipelineFactory, ResiliencePipelineFactory>();

        services.AddScoped<CompilerProvider>();

        return services;
    }
}
