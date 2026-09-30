namespace Stack86.Integration.Test.WebInfrastructure;

using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using Stack86.Test.Infrastructure;
using Stack86.Web.Infrastructure;

/// <summary>What a host asks before sending traffic. Both answers are cheap on purpose.</summary>
[TestClass]
public sealed class HealthEndpointTests : RestClientTestBase
{
    [TestMethod]
    public async Task Liveness_SaysTheProcessIsUp()
    {
        // Act
        using var response = await this.Client.GetAsync(HealthEndpoints.LivenessPath);

        // Assert
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        Assert.AreEqual("alive", await StatusOfAsync(response));
    }

    [TestMethod]
    public async Task Readiness_SaysTheInstanceCanTakeTraffic()
    {
        // Act
        using var response = await this.Client.GetAsync(HealthEndpoints.ReadinessPath);

        // Assert
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        Assert.AreEqual("ready", await StatusOfAsync(response));
    }

    /// <summary>The queue's depth rides along, so a probe's log shows it.</summary>
    [TestMethod]
    public async Task Readiness_ReportsWhatIsWaitingToCompile()
    {
        // Act
        using var response = await this.Client.GetAsync(HealthEndpoints.ReadinessPath);
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

        // Assert
        Assert.IsTrue(document.RootElement.TryGetProperty("queued", out var queued));
        Assert.IsGreaterThanOrEqualTo(0, queued.GetInt32());
    }

    private static async Task<string?> StatusOfAsync(System.Net.Http.HttpResponseMessage response)
    {
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

        return document.RootElement.GetProperty("status").GetString();
    }
}
