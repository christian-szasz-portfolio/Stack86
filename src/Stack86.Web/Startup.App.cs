namespace Stack86.Web;

using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.FileProviders;
using Scalar.AspNetCore;
using Stack86.Web.Infrastructure;
using Stack86.Web.Infrastructure.Security;

/// <summary>
/// Middleware pipeline configuration.
/// </summary>
public static partial class Startup
{
    /// <summary>
    /// Configures the HTTP request pipeline.
    /// </summary>
    public static WebApplication ConfigureApp(this WebApplication app)
    {
        // First, so everything after it sees the caller rather than the ingress.
        app.UseForwardedHeaders();

        var isDevOrTest = app.Environment.IsDevelopment() || app.Environment.IsEnvironment("Test");
        if (app.Environment.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
        }
        else
        {
            app.UseExceptionHandler();
        }

        if (isDevOrTest)
        {
            app.MapOpenApi();
            app.MapScalarApiReference();
        }

        app.UseMiddleware<SecurityHeadersMiddleware>();
        app.UseMiddleware<ContentSecurityPolicyMiddleware>();

        app.UseCors("AllowFrontend");
        app.UseStaticFiles();

        app.UseRateLimiter();

        app.MapControllers();

        // No rate-limiting policy, so a probe never spends a caller's allowance.
        app.MapStack86Health(app.Configuration.GetSection(HealthEndpoints.WakeOriginsKey).Get<string[]>() ?? []);

        app.ConfigureStaticAssets();

        return app;
    }

    /// <summary>
    /// Sets appropriate cache headers for static files.
    /// HTML files: no-cache (always revalidate to get latest app shell).
    /// Hashed assets (JS/CSS): immutable with long max-age (1 year).
    /// Other assets: short cache with revalidation.
    /// </summary>
    internal static void SetCacheHeaders(StaticFileResponseContext context)
    {
        var path = context.File.Name;
        var headers = context.Context.Response.Headers;

        if (path.EndsWith(".html", StringComparison.OrdinalIgnoreCase))
        {
            headers.CacheControl = "no-cache, no-store, must-revalidate";
            headers.Pragma = "no-cache";
            headers.Expires = "0";
        }
        else if (IsHashedAsset(path))
        {
            headers.CacheControl = "public, max-age=31536000, immutable";
        }
        else
        {
            headers.CacheControl = "public, max-age=86400, must-revalidate";
        }
    }

    /// <summary>
    /// Determines if a file is a hashed asset (has content hash in filename).
    /// Vite production builds use pattern: name-HASH.ext (e.g., main-ABC123.js).
    /// </summary>
    internal static bool IsHashedAsset(string fileName)
    {
        var extension = Path.GetExtension(fileName);
        if (!extension.Equals(".js", StringComparison.OrdinalIgnoreCase) &&
            !extension.Equals(".css", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var nameWithoutExtension = Path.GetFileNameWithoutExtension(fileName);
        var lastDash = nameWithoutExtension.LastIndexOf('-');

        if (lastDash > 0 && lastDash < nameWithoutExtension.Length - 6)
        {
            var potentialHash = nameWithoutExtension[(lastDash + 1)..];
            return potentialHash.Length >= 6 && potentialHash.All(char.IsLetterOrDigit);
        }

        return false;
    }

    private static WebApplication ConfigureStaticAssets(this WebApplication app)
    {
        var webRootPath = app.Environment.WebRootPath ?? string.Empty;

        if (!File.Exists(Path.Combine(webRootPath, "index.html")))
        {
            return app;
        }

        var clientFileProvider = new PhysicalFileProvider(webRootPath);
        var indexWriter = new IndexHtmlNonceWriter(webRootPath);

        // Static assets (JS/CSS/images) — index.html itself is intentionally excluded so that
        // the nonce-substituting writer always handles it.
        app.UseStaticFiles(new StaticFileOptions
        {
            FileProvider = clientFileProvider,
            OnPrepareResponse = SetCacheHeaders,
            ServeUnknownFileTypes = false,
        });

        if (indexWriter.HasTemplate)
        {
            // Root + SPA fallback both render index.html with a per-request CSP nonce.
            app.MapGet("/", indexWriter.WriteAsync);
            app.MapFallback(indexWriter.WriteAsync);
        }
        else
        {
            app.MapFallbackToFile("index.html", new StaticFileOptions
            {
                FileProvider = clientFileProvider,
                OnPrepareResponse = SetCacheHeaders,
            });
        }

        return app;
    }
}
