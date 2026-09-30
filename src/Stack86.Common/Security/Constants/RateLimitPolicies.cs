namespace Stack86.Common.Security.Constants;

/// <summary>
/// Named rate limiting policies.
/// </summary>
public static class RateLimitPolicies
{
    /// <summary>Default rate limit for general API endpoints.</summary>
    public const string GeneralApi = "general-api";

    /// <summary>Stricter rate limit for the CPU-heavy compile endpoints.</summary>
    public const string Compile = "compile";
}
