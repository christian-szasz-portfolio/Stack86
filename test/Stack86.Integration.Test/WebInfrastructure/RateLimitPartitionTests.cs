namespace Stack86.Integration.Test.WebInfrastructure;

using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Stack86.Common.Security.Constants;
using Stack86.Common.Security.Options;
using Stack86.Web.Infrastructure.Security;

/// <summary>The allowance belongs to a caller, not to the site, which needs the forwarded address to work.</summary>
[TestClass]
public sealed class RateLimitPartitionTests
{
    private const string Path = "/ping";

    private const string CompilePath = "/api/compiler/compile";

    private const int Permits = 2;

    [TestMethod]
    public async Task OneCaller_PastItsAllowance_IsRefused()
    {
        // Arrange
        using var host = await StartAsync();
        using var client = host.GetTestClient();

        // Act
        await SpendAsync(client, "203.0.113.7", Permits);
        var refused = await GetAsync(client, "203.0.113.7");

        // Assert
        Assert.AreEqual(HttpStatusCode.TooManyRequests, refused.StatusCode);
        Assert.IsNotNull(refused.Headers.RetryAfter);
    }

    [TestMethod]
    public async Task AnotherCaller_IsUnaffectedByTheFirst()
    {
        // Arrange
        using var host = await StartAsync();
        using var client = host.GetTestClient();

        // Act: the first caller overspends.
        await SpendAsync(client, "203.0.113.7", Permits + 1);
        var second = await GetAsync(client, "198.51.100.4");

        // Assert
        Assert.AreEqual(HttpStatusCode.OK, second.StatusCode);
    }

    [TestMethod]
    public async Task CallersBeyondTheTrackedCap_ShareOneOverflowAllowance()
    {
        // Arrange: only one caller is tracked; every other collapses into a single overflow bucket.
        using var host = await StartAsync(new Dictionary<string, string?>
        {
            [$"{RateLimitOptions.SectionName}:MaxTrackedCallers"] = "1",
        });
        using var client = host.GetTestClient();

        // Act: the first caller takes the one tracked slot; two further callers share overflow.
        await GetAsync(client, "203.0.113.1");
        await SpendAsync(client, "198.51.100.2", Permits);
        var refused = await GetAsync(client, "198.51.100.3");

        // Assert: the third caller is refused on the second caller's spent overflow allowance.
        Assert.AreEqual(HttpStatusCode.TooManyRequests, refused.StatusCode);
    }

    [TestMethod]
    public async Task ManyCallers_MeetTheSharedCompileCeiling()
    {
        // Arrange: a low global compile ceiling and a high per-caller limit, so the shared ceiling
        // is what refuses — a flood spread over addresses still cannot exceed it.
        using var host = await StartAsync(new Dictionary<string, string?>
        {
            [$"{RateLimitOptions.SectionName}:CompileGlobalLimitPerMinute"] = "2",
            [$"{RateLimitOptions.SectionName}:CompileLimitPerMinute"] = "100",
            [$"{RateLimitOptions.SectionName}:CompileQueueLimit"] = "0",
        });
        using var client = host.GetTestClient();

        // Act: two compiles from one caller, a third from another — three against a ceiling of two.
        await GetAsync(client, "203.0.113.10", CompilePath);
        await GetAsync(client, "203.0.113.10", CompilePath);
        var refused = await GetAsync(client, "198.51.100.20", CompilePath);

        // Assert: the third is refused although its own caller has spent nothing.
        Assert.AreEqual(HttpStatusCode.TooManyRequests, refused.StatusCode);
    }

    [TestMethod]
    public async Task GeneralTraffic_IsNotBoundByTheCompileCeiling()
    {
        // Arrange: the compile ceiling is one, but the general endpoint must not feel it.
        using var host = await StartAsync(new Dictionary<string, string?>
        {
            [$"{RateLimitOptions.SectionName}:CompileGlobalLimitPerMinute"] = "1",
        });
        using var client = host.GetTestClient();

        // Act: a general request after the compile ceiling would already be spent.
        await GetAsync(client, "203.0.113.30", CompilePath);
        var general = await GetAsync(client, "203.0.113.30");

        // Assert
        Assert.AreEqual(HttpStatusCode.OK, general.StatusCode);
    }

    private static async Task SpendAsync(HttpClient client, string caller, int requests)
    {
        for (var sent = 0; sent < requests; sent++)
        {
            using var spent = await GetAsync(client, caller);
        }
    }

    private static async Task<HttpResponseMessage> GetAsync(HttpClient client, string caller, string path = Path)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, path);
        request.Headers.Add("X-Forwarded-For", caller);

        return await client.SendAsync(request);
    }

    /// <summary>A host carrying only the registration under test and one endpoint per policy.</summary>
    private static async Task<IHost> StartAsync(IDictionary<string, string?>? overrides = null)
    {
        var settings = new Dictionary<string, string?>
        {
            [$"{RateLimitOptions.SectionName}:Enabled"] = "true",
            [$"{RateLimitOptions.SectionName}:GeneralApiLimitPerMinute"] = Permits.ToString(),

            // No queue, so the third request is refused rather than held.
            [$"{RateLimitOptions.SectionName}:GeneralApiQueueLimit"] = "0",
        };

        if (overrides is not null)
        {
            foreach (var (key, value) in overrides)
            {
                settings[key] = value;
            }
        }

        var configuration = new ConfigurationBuilder().AddInMemoryCollection(settings).Build();

        return await new HostBuilder()
            .ConfigureWebHost(web => web
                .UseTestServer()
                .ConfigureServices(services =>
                {
                    services.AddRouting();
                    services.Configure<ForwardedHeadersOptions>(options =>
                    {
                        options.ForwardedHeaders = ForwardedHeaders.XForwardedFor;
                        options.KnownIPNetworks.Clear();
                        options.KnownProxies.Clear();
                    });
                    services.AddStack86RateLimiting(configuration);
                })
                .Configure(app =>
                {
                    app.UseForwardedHeaders();
                    app.UseRouting();
                    app.UseRateLimiter();
                    app.UseEndpoints(endpoints =>
                    {
                        endpoints.MapGet(Path, () => Results.Ok())
                            .RequireRateLimiting(RateLimitPolicies.GeneralApi);
                        endpoints.MapGet(CompilePath, () => Results.Ok())
                            .RequireRateLimiting(RateLimitPolicies.Compile);
                    });
                }))
            .StartAsync();
    }
}
