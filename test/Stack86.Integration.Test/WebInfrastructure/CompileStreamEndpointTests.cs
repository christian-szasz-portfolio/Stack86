namespace Stack86.Integration.Test.WebInfrastructure;

using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;

/// <summary>
/// The streaming endpoint is what the compiler workbench actually talks to, and its contract is
/// the shape of the newline-delimited body: a run of log events followed by exactly one result
/// event. Driven over real HTTP, because the framing is the thing being asserted.
/// </summary>
[TestClass]
public sealed class CompileStreamEndpointTests : RestClientTestBase
{
    [TestMethod]
    public async Task CompileStream_ForValidSource_EndsWithASingleResultEvent()
    {
        // Act
        var events = await this.StreamAsync("c", "int main() { return 0; }");

        // Assert
        Assert.IsGreaterThan(0, events.Count);
        Assert.AreEqual("result", Kind(events[^1]));
        Assert.AreEqual(1, events.Count(e => Kind(e) == "result"));
    }

    [TestMethod]
    public async Task CompileStream_ForValidSource_NarratesTheStagesBeforeTheResult()
    {
        // Act
        var events = await this.StreamAsync("c", "int main() { return 0; }");

        // Assert
        Assert.IsGreaterThan(0, events.Count(e => Kind(e) == "log"));
    }

    [TestMethod]
    public async Task CompileStream_ForValidSource_CarriesTheAssemblyOnTheResult()
    {
        // Act
        var events = await this.StreamAsync("c", "int main() { return 0; }");

        // Assert
        var result = events.Last(e => Kind(e) == "result");
        var assembly = result.GetProperty("assembly").GetString();

        Assert.IsNotNull(assembly);
        StringAssert.Contains(assembly, ".CODE");
    }

    [TestMethod]
    public async Task CompileStream_ForBrokenSource_ReportsErrorsRatherThanFailingTheRequest()
    {
        // Act
        var events = await this.StreamAsync("c", "int main( { syntax error");

        // Assert
        var result = events.Last(e => Kind(e) == "result");
        Assert.IsGreaterThan(0, result.GetProperty("errors").GetArrayLength());
    }

    [TestMethod]
    public async Task CompileStream_ForAnUnknownLanguage_StillTerminatesTheStreamProperly()
    {
        // Act
        var events = await this.StreamAsync("fortran", "PROGRAM MAIN");

        // Assert
        var result = events.Last(e => Kind(e) == "result");
        Assert.AreEqual("result", Kind(events[^1]));
        Assert.IsGreaterThan(0, result.GetProperty("errors").GetArrayLength());
    }

    [TestMethod]
    public async Task Compile_ForValidSource_ReturnsAssembly()
    {
        // Act
        var response = await this.PostRawAsync(
            "/api/Compiler/compile",
            new { language = "c", files = new Dictionary<string, string> { ["main.c"] = "int main() { return 0; }" } });

        // Assert
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        StringAssert.Contains(body, ".CODE");
    }

    private static string? Kind(JsonElement element) =>
        element.TryGetProperty("type", out var type) ? type.GetString() : null;

    private async Task<List<JsonElement>> StreamAsync(string language, string source)
    {
        var response = await this.PostRawAsync(
            "/api/Compiler/compileStream",
            new { language, files = new Dictionary<string, string> { ["main.c"] = source } });

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadAsStringAsync();
        return [.. body
            .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(line => JsonDocument.Parse(line).RootElement.Clone())];
    }
}
