namespace Stack86.Web.Infrastructure.Security;

using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

/// <summary>
/// Serves the SPA index.html with the per-request CSP nonce substituted into
/// <c>__CSP_NONCE__</c> placeholders. Used by both the default file route and the
/// SPA fallback so every navigation receives a fresh nonce that matches the
/// Content-Security-Policy header set by <see cref="ContentSecurityPolicyMiddleware"/>.
/// </summary>
public sealed class IndexHtmlNonceWriter
{
    private readonly string indexHtmlPath;
    private readonly string template;

    /// <summary>Initializes the writer by loading the index.html template from disk.</summary>
    public IndexHtmlNonceWriter(string webRootPath)
    {
        ArgumentNullException.ThrowIfNull(webRootPath);
        this.indexHtmlPath = Path.Combine(webRootPath, "index.html");
        this.template = File.Exists(this.indexHtmlPath)
            ? File.ReadAllText(this.indexHtmlPath, Encoding.UTF8)
            : string.Empty;
    }

    /// <summary>True if an index.html template was successfully loaded.</summary>
    public bool HasTemplate => !string.IsNullOrEmpty(this.template);

    /// <summary>Writes the index.html response with nonce substitution and no-cache headers.</summary>
    public async Task WriteAsync(HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var nonce = context.Items[ContentSecurityPolicyMiddleware.NonceItemKey] as string ?? string.Empty;
        var html = this.template.Replace("__CSP_NONCE__", nonce, StringComparison.Ordinal);

        var headers = context.Response.Headers;
        headers.CacheControl = "no-cache, no-store, must-revalidate";
        headers.Pragma = "no-cache";
        headers.Expires = "0";

        context.Response.ContentType = "text/html; charset=utf-8";
        await context.Response.WriteAsync(html, Encoding.UTF8, context.RequestAborted);
    }
}
