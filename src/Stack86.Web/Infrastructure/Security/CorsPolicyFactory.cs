namespace Stack86.Web.Infrastructure.Security;

using Microsoft.AspNetCore.Cors.Infrastructure;
using StackCorsOptions = Stack86.Common.Security.Options.CorsOptions;

/// <summary>Builds the frontend CORS policy from configuration, closed when no origin is set.</summary>
public static class CorsPolicyFactory
{
    /// <summary>No configured origin denies every cross-origin caller.</summary>
    public static void Configure(CorsPolicyBuilder policy, StackCorsOptions options)
    {
        if (options.AllowedOrigins.Length == 0)
        {
            policy.SetIsOriginAllowed(_ => false);
            return;
        }

        // Credentials are never allowed: see CorsOptions.
        policy
            .WithOrigins(options.AllowedOrigins)
            .WithMethods(options.AllowedMethods)
            .WithHeaders(options.AllowedHeaders);
    }
}
