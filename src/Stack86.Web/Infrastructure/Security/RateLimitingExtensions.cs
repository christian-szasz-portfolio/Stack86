namespace Stack86.Web.Infrastructure.Security;

using System.Globalization;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Stack86.Common.Security.Constants;
using Stack86.Common.Security.Options;

/// <summary>
/// Registers ASP.NET Core rate limiting policies that guard the API against request floods and
/// starvation. Rejects excess requests with an RFC 6585 problem document. When disabled, every
/// policy degrades to a no-op limiter.
/// </summary>
public static class RateLimitingExtensions
{
    /// <summary>Registers the configured rate limiter and its named policies.</summary>
    /// <param name="services">The service collection to extend.</param>
    /// <param name="configuration">Configuration source bound to <see cref="RateLimitOptions"/>.</param>
    /// <returns>The same service collection for chaining.</returns>
    public static IServiceCollection AddStack86RateLimiting(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        var options = configuration.GetSection(RateLimitOptions.SectionName).Get<RateLimitOptions>() ?? new RateLimitOptions();

        if (!options.Enabled)
        {
            services.AddRateLimiter(limiter =>
            {
                limiter.AddPolicy(RateLimitPolicies.GeneralApi, _ => RateLimitPartition.GetNoLimiter(string.Empty));
                limiter.AddPolicy(RateLimitPolicies.Compile, _ => RateLimitPartition.GetNoLimiter(string.Empty));
            });

            return services;
        }

        services.AddRateLimiter(limiter =>
        {
            limiter.RejectionStatusCode = options.RejectionStatusCode;

            // Partitioned by caller: a shared limit lets one visitor refuse the rest.
            limiter.AddPolicy(RateLimitPolicies.GeneralApi, context => RateLimitPartition.GetFixedWindowLimiter(
                Caller(context),
                _ => new FixedWindowRateLimiterOptions
                {
                    // General API traffic — generous fixed window with a small queue.
                    PermitLimit = options.GeneralApiLimitPerMinute,
                    Window = TimeSpan.FromMinutes(1),
                    QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                    QueueLimit = options.GeneralApiQueueLimit,
                }));

            limiter.AddPolicy(RateLimitPolicies.Compile, context => RateLimitPartition.GetFixedWindowLimiter(
                Caller(context),
                _ => new FixedWindowRateLimiterOptions
                {
                    // Compile endpoints — CPU heavy, kept stricter than general traffic.
                    PermitLimit = options.CompileLimitPerMinute,
                    Window = TimeSpan.FromMinutes(1),
                    QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                    QueueLimit = options.CompileQueueLimit,
                }));

            limiter.OnRejected = async (context, cancellationToken) =>
            {
                context.HttpContext.Response.StatusCode = options.RejectionStatusCode;
                context.HttpContext.Response.ContentType = "application/problem+json";

                var retryAfter = context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfterValue)
                    ? retryAfterValue.TotalSeconds
                    : 60;

                context.HttpContext.Response.Headers.RetryAfter = retryAfter.ToString(CultureInfo.InvariantCulture);

                await context.HttpContext.Response.WriteAsJsonAsync(
                    new
                    {
                        type = "https://tools.ietf.org/html/rfc6585#section-4",
                        title = "Too Many Requests",
                        status = options.RejectionStatusCode,
                        detail = $"Rate limit exceeded. Please retry after {retryAfter} seconds.",
                    },
                    cancellationToken);
            };
        });

        return services;
    }

    /// <summary>Whose allowance this is, read after UseForwardedHeaders has set the real address.</summary>
    internal static string Caller(HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    }
}
