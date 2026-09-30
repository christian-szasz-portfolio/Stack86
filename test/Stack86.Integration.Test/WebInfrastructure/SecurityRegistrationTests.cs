namespace Stack86.Integration.Test.WebInfrastructure;

using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.RateLimiting;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Stack86.Common.Security.Options;
using Stack86.Web.Infrastructure.Security;

/// <summary>
/// Both registrations read configuration and hand back a container, so what is asserted is what
/// a caller can actually observe: the guards, the bound options, and the rejection behaviour a
/// throttled client would meet. Policy names live in an internal map and are left to the
/// endpoint tests rather than reached for by reflection.
/// </summary>
[TestClass]
public sealed class SecurityRegistrationTests
{
    [TestMethod]
    public void AddStack86RateLimiting_WhenServicesAreNull_Throws()
    {
        // Act + Assert
        Assert.ThrowsExactly<ArgumentNullException>(
            () => RateLimitingExtensions.AddStack86RateLimiting(null!, Configuration()));
    }

    [TestMethod]
    public void AddStack86RateLimiting_WhenConfigurationIsNull_Throws()
    {
        // Act + Assert
        Assert.ThrowsExactly<ArgumentNullException>(
            () => new ServiceCollection().AddStack86RateLimiting(null!));
    }

    [TestMethod]
    public void AddStack86RateLimiting_WhenEnabled_TakesTheConfiguredRejectionCode()
    {
        // Arrange + Act
        var options = Limiter(enabled: true, rejectionStatusCode: 503);

        // Assert
        Assert.AreEqual(503, options.RejectionStatusCode);
    }

    [TestMethod]
    public void AddStack86RateLimiting_WhenEnabled_InstallsARejectionHandler()
    {
        // Arrange + Act
        var options = Limiter(enabled: true);

        // Assert
        Assert.IsNotNull(options.OnRejected);
    }

    [TestMethod]
    public void AddStack86RateLimiting_WhenDisabled_LeavesNoRejectionHandler()
    {
        // Arrange: the disabled path registers no-op limiters, so nothing is ever rejected.
        var options = Limiter(enabled: false);

        // Assert
        Assert.IsNull(options.OnRejected);
    }

    [TestMethod]
    public async Task OnRejected_WritesAProblemDocumentAndARetryAfterHeader()
    {
        // Arrange
        var options = Limiter(enabled: true, rejectionStatusCode: 429);
        var context = new DefaultHttpContext();
        using var body = new MemoryStream();
        context.Response.Body = body;

        // Act
        await options.OnRejected!(
            new OnRejectedContext { HttpContext = context, Lease = new NoMetadataLease() },
            CancellationToken.None);

        // Assert
        Assert.AreEqual(429, context.Response.StatusCode);
        Assert.AreEqual("60", context.Response.Headers.RetryAfter.ToString());

        body.Position = 0;
        using var document = JsonDocument.Parse(body);
        Assert.AreEqual("Too Many Requests", document.RootElement.GetProperty("title").GetString());
        Assert.AreEqual(429, document.RootElement.GetProperty("status").GetInt32());
    }

    [TestMethod]
    public void AddStack86Security_WhenServicesAreNull_Throws()
    {
        // Act + Assert
        Assert.ThrowsExactly<ArgumentNullException>(
            () => SecurityExtensions.AddStack86Security(null!, Configuration()));
    }

    [TestMethod]
    public void AddStack86Security_WhenConfigurationIsNull_Throws()
    {
        // Act + Assert
        Assert.ThrowsExactly<ArgumentNullException>(
            () => new ServiceCollection().AddStack86Security(null!));
    }

    [TestMethod]
    public void AddStack86Security_BindsCorsAndSecurityHeaderOptions()
    {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                [$"{CorsOptions.SectionName}:AllowedOrigins:0"] = "https://localhost:1234",
                [$"{SecurityHeadersOptions.SectionName}:EnableHsts"] = "false",
                [$"{SecurityHeadersOptions.SectionName}:HstsMaxAgeSeconds"] = "600",
            })
            .Build();

        var services = new ServiceCollection();

        // Act
        services.AddStack86Security(configuration);

        // Assert
        using var provider = services.BuildServiceProvider();
        var cors = provider.GetRequiredService<IOptions<CorsOptions>>().Value;
        var headers = provider.GetRequiredService<IOptions<SecurityHeadersOptions>>().Value;

        CollectionAssert.Contains(cors.AllowedOrigins, "https://localhost:1234");
        Assert.IsFalse(headers.EnableHsts);
        Assert.AreEqual(600, headers.HstsMaxAgeSeconds);
    }

    [TestMethod]
    public void AddStack86Security_WhenTheSectionsAreAbsent_FallsBackToDefaults()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddStack86Security(new ConfigurationBuilder().Build());

        // Assert
        using var provider = services.BuildServiceProvider();
        var headers = provider.GetRequiredService<IOptions<SecurityHeadersOptions>>().Value;

        Assert.IsTrue(headers.EnableHsts);
        Assert.IsTrue(headers.EnableContentSecurityPolicy);
    }

    [TestMethod]
    public void AddStack86Security_ReturnsTheSameCollection_SoItChains()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        var returned = services.AddStack86Security(Configuration());

        // Assert
        Assert.AreSame(services, returned);
    }

    private static RateLimiterOptions Limiter(bool enabled, int rejectionStatusCode = 429)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddStack86RateLimiting(Configuration(enabled, rejectionStatusCode));

        using var provider = services.BuildServiceProvider();
        return provider.GetRequiredService<IOptions<RateLimiterOptions>>().Value;
    }

    private static IConfiguration Configuration(bool enabled = true, int rejectionStatusCode = 429) =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                [$"{RateLimitOptions.SectionName}:Enabled"] = enabled.ToString(),
                [$"{RateLimitOptions.SectionName}:RejectionStatusCode"] = rejectionStatusCode.ToString(),
            })
            .Build();

    /// <summary>A lease that reports no retry metadata, which is the fallback path.</summary>
    private sealed class NoMetadataLease : RateLimitLease
    {
        public override bool IsAcquired => false;

        public override IEnumerable<string> MetadataNames => [];

        public override bool TryGetMetadata(string metadataName, out object? metadata)
        {
            metadata = null;
            return false;
        }
    }
}
