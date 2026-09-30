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

    private static async Task SpendAsync(HttpClient client, string caller, int requests)
    {
        for (var sent = 0; sent < requests; sent++)
        {
            using var spent = await GetAsync(client, caller);
        }
    }

    private static async Task<HttpResponseMessage> GetAsync(HttpClient client, string caller)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, Path);
        request.Headers.Add("X-Forwarded-For", caller);

        return await client.SendAsync(request);
    }

    /// <summary>A host carrying only the registration under test and one endpoint using it.</summary>
    private static async Task<IHost> StartAsync()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                [$"{RateLimitOptions.SectionName}:Enabled"] = "true",
                [$"{RateLimitOptions.SectionName}:GeneralApiLimitPerMinute"] = Permits.ToString(),

                // No queue, so the third request is refused rather than held.
                [$"{RateLimitOptions.SectionName}:GeneralApiQueueLimit"] = "0",
            })
            .Build();

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
                    app.UseEndpoints(endpoints => endpoints
                        .MapGet(Path, () => Results.Ok())
                        .RequireRateLimiting(RateLimitPolicies.GeneralApi));
                }))
            .StartAsync();
    }
}
