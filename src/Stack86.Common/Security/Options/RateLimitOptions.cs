namespace Stack86.Common.Security.Options;

/// <summary>
/// Rate-limiting configuration. Bound from the <c>Security:RateLimiting</c> configuration section.
/// </summary>
public sealed class RateLimitOptions
{
    /// <summary>Configuration section name for binding.</summary>
    public const string SectionName = "Security:RateLimiting";

    /// <summary>Gets or sets a value indicating whether rate limiting is enabled.</summary>
    public bool Enabled { get; set; } = true;

    /// <summary>Gets or sets the general API requests limit per minute.</summary>
    public int GeneralApiLimitPerMinute { get; set; } = 100;

    /// <summary>Gets or sets the compile endpoint limit per minute (CPU-heavy, kept stricter).</summary>
    public int CompileLimitPerMinute { get; set; } = 20;

    /// <summary>
    /// Gets or sets one shared ceiling on compile requests across every caller, per minute. A flood
    /// spread over many addresses stays under each per-caller limit but together meets this one, so
    /// the server's total compile load is bounded even under a distributed attack.
    /// </summary>
    public int CompileGlobalLimitPerMinute { get; set; } = 120;

    /// <summary>
    /// Gets or sets how many distinct callers the per-caller limiters track. Once reached, further
    /// addresses share one overflow partition, so a flood of unique addresses cannot grow the
    /// limiter's memory without bound.
    /// </summary>
    public int MaxTrackedCallers { get; set; } = 20_000;

    /// <summary>Gets or sets the queue limit for general API requests.</summary>
    public int GeneralApiQueueLimit { get; set; } = 2;

    /// <summary>Gets or sets the queue limit for compile requests.</summary>
    public int CompileQueueLimit { get; set; } = 2;

    /// <summary>Gets or sets the HTTP status code returned when the rate limit is exceeded.</summary>
    public int RejectionStatusCode { get; set; } = 429;
}
