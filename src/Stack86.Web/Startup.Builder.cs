namespace Stack86.Web;

using global::Common.Diagnostics.Azure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.HttpOverrides;
using Serilog;
using Stack86.Api.Infrastructure.Compiler;
using Stack86.Common.DI;
using Stack86.Common.Json;
using Stack86.Common.Security.Options;
using Stack86.Common.Time;
using Stack86.Common.Validation;
using Stack86.Logic.Compilation;
using Stack86.Logic.Compilation.Models;
using Stack86.Logic.Languages;
using Stack86.Logic.Languages.C;
using Stack86.Logic.Languages.TypeScript;
using Stack86.Web.Infrastructure;
using Stack86.Web.Infrastructure.Security;

/// <summary>
/// Service registration and DI wiring.
/// </summary>
public static partial class Startup
{
    /// <summary>The name this app stamps on the entries it captures.</summary>
    /// <remarks>Declared with the others in the analytics API, which sections the digest.</remarks>
    private const string CapturedApp = "Stack86";

    public static WebApplicationBuilder ConfigureSerilog(this WebApplicationBuilder builder)
    {
        Log.Logger = new LoggerConfiguration()
            .ReadFrom.Configuration(builder.Configuration)
            .CreateLogger();

        // writeToProviders keeps the digest capture in the chain: Serilog otherwise takes
        // the pipeline for itself and no other provider ever sees an event.
        builder.Host.UseSerilog(
            (context, configuration) => configuration.ReadFrom.Configuration(context.Configuration),
            writeToProviders: true);

        return builder;
    }

    public static WebApplication ConfigureServices(this WebApplicationBuilder builder)
    {
        builder.Services
            .AddSingleton<IClock, SystemClock>();

        // Writes captured entries into the shared log table the analytics API reads back.
        // With no storage connection this registers nothing and console logging is unchanged.
        builder.Services.AddLogCapture(builder.Configuration, CapturedApp);

        builder.Services.AddRouting(options =>
        {
            options.LowercaseUrls = true;
        });

        builder.Services
            .AddControllers(options =>
            {
                options.Filters.Add<PropertyInjectionActionFilter>();
                options.Filters.Add<ValidationActionFilter>();
            })
            .AddJsonOptions(options => Stack86JsonOptions.ApplyTo(options.JsonSerializerOptions));

        // Security options (CORS, security headers)
        builder.Services
            .AddStack86Security(builder.Configuration);

        // Without this the limiter keys every caller on the proxy. Known proxies are
        // cleared because the ingress has no fixed address and is the only way in.
        builder.Services.Configure<ForwardedHeadersOptions>(options =>
        {
            options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
            options.KnownIPNetworks.Clear();
            options.KnownProxies.Clear();
        });

        builder.Services.AddStack86RateLimiting(builder.Configuration);

        builder.Services.AddExceptionHandler<ApiExceptionHandler>();
        builder.Services.AddProblemDetails();

        // TCC validation
        var tccSettings = builder.Configuration.GetSection("Tcc").Get<TccSettings>() ?? new TccSettings();
        builder.Services
            .AddSingleton(tccSettings)
            .AddExternalCodeValidator<TccCodeValidator>();

        // Compilation pipeline (per-language services + registry + IR validator + Roslyn)
        builder.Services.AddStack86CompilationPipeline();

        // Channel-backed work queue + background workers guarding the CPU-heavy compile endpoints.
        var compilationQueueOptions = builder.Configuration.GetSection(CompilationQueueOptions.SectionName).Get<CompilationQueueOptions>() ?? new CompilationQueueOptions();
        builder.Services
            .AddSingleton(compilationQueueOptions)
            .AddSingleton<ICompilationQueue, CompilationQueue>()
            .AddHostedService<CompilationQueueWorker>();

        // Optional configuration override for the external JavaScript validator. The pipeline
        // registers a sensible default; binding here lets operators point at a specific toolchain.
        builder.Services
            .AddSingleton(builder.Configuration.GetSection("NodeCheck").Get<NodeCheckSettings>() ?? new NodeCheckSettings());

        // TypeScript transpiler
        var tsSettings = builder.Configuration.GetSection("TypeScriptTranspiler").Get<TsTranspilerSettings>() ?? new TsTranspilerSettings();
        builder.Services
            .AddSingleton(tsSettings)
            .AddSingleton<ITypeScriptTranspiler, NodeTypeScriptTranspiler>();

        builder.Services.AddPropertyInjectedServices();

        builder.Services.AddOpenApi();

        // Reads the same CorsOptions AddStack86Security already binds, so the policy actually
        // applied matches what appsettings and the environment configure.
        var corsOptions = builder.Configuration.GetSection(CorsOptions.SectionName).Get<CorsOptions>()
            ?? new CorsOptions();
        builder.Services.AddCors(options => options.AddPolicy(
            "AllowFrontend",
            policy => CorsPolicyFactory.Configure(policy, corsOptions)));

        return builder.Build();
    }
}
