namespace Stack86.Common.Security.Options;

/// <summary>
/// CORS configuration.
/// </summary>
public sealed class CorsOptions
{
    /// <summary>Configuration section name.</summary>
    public const string SectionName = "Cors";

    /// <summary>Gets or sets the allowed origin URLs.</summary>
    public string[] AllowedOrigins { get; set; } = [];

    /// <summary>Gets or sets the allowed HTTP methods.</summary>
    public string[] AllowedMethods { get; set; } = ["POST", "OPTIONS"];

    /// <summary>Gets or sets the allowed request headers.</summary>
    public string[] AllowedHeaders { get; set; } = ["Content-Type"];

    // No AllowCredentials setting: this app has no sign-in and sets no cookies.
}
