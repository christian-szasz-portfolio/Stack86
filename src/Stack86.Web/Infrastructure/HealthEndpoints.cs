namespace Stack86.Web.Infrastructure;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Stack86.Api.Infrastructure.Compiler;

/// <summary>The probes a host asks: alive, and ready for traffic. Neither touches the compiler.</summary>
public static class HealthEndpoints
{
    /// <summary>Where a platform probe knocks to see the process is up.</summary>
    public const string LivenessPath = "/health";

    /// <summary>Where it knocks to see whether this instance should be sent requests.</summary>
    public const string ReadinessPath = "/health/ready";

    /// <summary>The configuration key listing the sites that may wake this instance.</summary>
    public const string WakeOriginsKey = "Health:WakeOrigins";

    /// <summary>Maps both probes, with no rate-limiting policy.</summary>
    /// <param name="endpoints">The route builder.</param>
    /// <param name="wakeOrigins">Sites allowed to read liveness from a browser; none closes it.</param>
    public static IEndpointRouteBuilder MapStack86Health(this IEndpointRouteBuilder endpoints, string[] wakeOrigins)
    {
        ArgumentNullException.ThrowIfNull(endpoints);
        ArgumentNullException.ThrowIfNull(wakeOrigins);

        // The portfolio pings liveness when its project page opens, so the demo is warm by the click.
        endpoints.MapGet(LivenessPath, () => Results.Ok(new { status = "alive" }))
            .RequireCors(policy => policy.WithOrigins(wakeOrigins).WithMethods("GET"));

        // Ready whenever the process is serving: a full queue is busy, not broken.
        endpoints.MapGet(ReadinessPath, (ICompilationQueue queue) => Results.Ok(new
        {
            status = "ready",
            queued = queue.Reader.CanCount ? queue.Reader.Count : (int?)null,
        }));

        return endpoints;
    }
}
