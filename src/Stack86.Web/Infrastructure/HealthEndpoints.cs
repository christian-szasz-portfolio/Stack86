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

    /// <summary>Maps both probes, with no rate-limiting policy.</summary>
    public static IEndpointRouteBuilder MapStack86Health(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        endpoints.MapGet(LivenessPath, () => Results.Ok(new { status = "alive" }));

        // Ready whenever the process is serving: a full queue is busy, not broken.
        endpoints.MapGet(ReadinessPath, (ICompilationQueue queue) => Results.Ok(new
        {
            status = "ready",
            queued = queue.Reader.CanCount ? queue.Reader.Count : (int?)null,
        }));

        return endpoints;
    }
}
