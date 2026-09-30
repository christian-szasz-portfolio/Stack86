namespace Stack86.Web.Infrastructure.Security;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Stack86.Common.Security.Options;

/// <summary>
/// Registers security-related options (CORS, security headers) for the demo host.
/// Authentication and authorization have been removed for the public demo build.
/// </summary>
public static class SecurityExtensions
{
    /// <summary>Binds CORS and security-header options.</summary>
    public static IServiceCollection AddStack86Security(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddOptions<CorsOptions>()
            .Bind(configuration.GetSection(CorsOptions.SectionName));

        services.AddOptions<SecurityHeadersOptions>()
            .Bind(configuration.GetSection(SecurityHeadersOptions.SectionName));

        return services;
    }
}
