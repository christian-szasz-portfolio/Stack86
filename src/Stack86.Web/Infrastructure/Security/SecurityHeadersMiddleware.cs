namespace Stack86.Web.Infrastructure.Security;

using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Stack86.Common.Security.Options;

/// <summary>
/// Adds standard security response headers (frame, content-type, referrer, permissions-policy).
/// </summary>
public sealed class SecurityHeadersMiddleware(RequestDelegate next, IOptions<SecurityHeadersOptions> options)
{
    private readonly SecurityHeadersOptions options = options.Value;

    /// <summary>Invokes the middleware.</summary>
    public Task InvokeAsync(HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var headers = context.Response.Headers;
        headers.XContentTypeOptions = "nosniff";
        headers.XFrameOptions = "DENY";
        headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
        headers["Permissions-Policy"] = "camera=(), geolocation=(), microphone=(), payment=()";
        headers["Cross-Origin-Opener-Policy"] = "same-origin";
        headers["Cross-Origin-Resource-Policy"] = "same-origin";

        if (this.options.EnableHsts && context.Request.IsHttps)
        {
            headers.StrictTransportSecurity = $"max-age={this.options.HstsMaxAgeSeconds}; includeSubDomains";
        }

        return next(context);
    }
}
