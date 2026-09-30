namespace Stack86.Common.Security.Options;

/// <summary>
/// Security response header configuration.
/// </summary>
public sealed class SecurityHeadersOptions
{
    /// <summary>Configuration section name.</summary>
    public const string SectionName = "SecurityHeaders";

    /// <summary>Gets or sets a value indicating whether the HSTS header is enabled (production only by default).</summary>
    public bool EnableHsts { get; set; } = true;

    /// <summary>Gets or sets the HSTS max-age value in seconds.</summary>
    public int HstsMaxAgeSeconds { get; set; } = 31_536_000;

    /// <summary>Gets or sets a value indicating whether the Content-Security-Policy header is enabled for non-API routes.</summary>
    public bool EnableContentSecurityPolicy { get; set; } = true;

    /// <summary>Gets or sets additional CSP connect-src origins (e.g., wss://host).</summary>
    public string[] AdditionalConnectSources { get; set; } = [];
}
