namespace Stack86.Web.Infrastructure.Security;

using System.Security.Cryptography;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Stack86.Common.Security.Options;

/// <summary>
/// Per-request CSP nonce + Content-Security-Policy header. Scripts use a nonce; Monaco needs
/// <c>wasm-unsafe-eval</c> for its WebAssembly and injects inline styles it cannot nonce, so
/// <c>style-src</c> allows <c>'unsafe-inline'</c> and carries no nonce (a style nonce would make
/// the browser ignore <c>'unsafe-inline'</c> and block every one of Monaco's inline styles).
/// </summary>
public sealed class ContentSecurityPolicyMiddleware(RequestDelegate next, IOptions<SecurityHeadersOptions> options)
{
    /// <summary>HttpContext item key under which the per-request nonce is stored.</summary>
    public const string NonceItemKey = "__CspNonce";

    private readonly SecurityHeadersOptions options = options.Value;

    /// <summary>Invokes the middleware.</summary>
    public Task InvokeAsync(HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (!this.options.EnableContentSecurityPolicy || IsApiRequest(context) || IsApiDocsRequest(context))
        {
            return next(context);
        }

        var nonce = GenerateNonce();
        context.Items[NonceItemKey] = nonce;

        var connectSrc = "'self'";
        if (this.options.AdditionalConnectSources.Length > 0)
        {
            connectSrc = $"'self' {string.Join(' ', this.options.AdditionalConnectSources)}";
        }

        var csp =
            $"default-src 'self'; " +
            $"script-src 'self' 'nonce-{nonce}' 'wasm-unsafe-eval'; " +
            $"style-src 'self' 'unsafe-inline'; " +
            $"img-src 'self' data: blob:; " +
            $"font-src 'self' data:; " +
            $"connect-src {connectSrc}; " +
            $"worker-src 'self' blob:; " +
            $"frame-ancestors 'none'; " +
            $"base-uri 'self'; " +
            $"form-action 'self'; " +
            $"object-src 'none'";

        context.Response.Headers.ContentSecurityPolicy = csp;
        return next(context);
    }

    private static string GenerateNonce()
    {
        Span<byte> bytes = stackalloc byte[16];
        RandomNumberGenerator.Fill(bytes);
        return Convert.ToBase64String(bytes);
    }

    private static bool IsApiRequest(HttpContext context)
    {
        var path = context.Request.Path.Value;
        return !string.IsNullOrEmpty(path) && path.StartsWith("/api/", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Returns <see langword="true"/> for the Scalar UI / OpenAPI document routes, which are
    /// only mapped in Development and Test environments and require their own inline styles/scripts.
    /// </summary>
    private static bool IsApiDocsRequest(HttpContext context)
    {
        var path = context.Request.Path.Value;
        if (string.IsNullOrEmpty(path))
        {
            return false;
        }

        return path.StartsWith("/scalar", StringComparison.OrdinalIgnoreCase)
            || path.StartsWith("/openapi", StringComparison.OrdinalIgnoreCase);
    }
}
