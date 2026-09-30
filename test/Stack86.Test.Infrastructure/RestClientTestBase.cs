namespace Stack86.Test.Infrastructure;

using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

/// <summary>
/// Base class for API integration tests using a shared <see cref="WebApplicationFactory{TEntryPoint}"/>.
/// Each test gets its own <see cref="HttpClient"/>; the underlying test server is shared.
/// </summary>
public abstract class RestClientTestBase : IDisposable
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() },
    };

    private static readonly Lock FactoryLock = new();
    private static WebApplicationFactory<Program>? sharedFactory;
    private static HttpClient? warmupClient;
    private static bool serverStarted;

    private HttpClient? httpClient;
    private bool disposed;

    public required TestContext TestContext { get; init; }

    /// <summary>Gets the shared JSON serializer options used for request/response serialization.</summary>
    protected static JsonSerializerOptions SerializerOptions => JsonOptions;

    /// <summary>Gets the shared <see cref="WebApplicationFactory{TEntryPoint}"/> instance.</summary>
    protected static WebApplicationFactory<Program> Factory
    {
        get
        {
            EnsureServerStarted();
            return sharedFactory!;
        }
    }

    /// <summary>Gets a per-test <see cref="HttpClient"/> bound to the shared test server.</summary>
    protected HttpClient Client
    {
        get
        {
            this.httpClient ??= Factory.CreateClient(new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false,
            });
            return this.httpClient;
        }
    }

    [TestCleanup]
    public void Cleanup()
    {
        this.Dispose();
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        this.Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>Deserializes the response body as the specified type.</summary>
    protected static async Task<T?> ReadAsAsync<T>(HttpResponseMessage response)
    {
        ArgumentNullException.ThrowIfNull(response);
        var content = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<T>(content, JsonOptions);
    }

    protected async Task<HttpResponseMessage> GetRawAsync(string url) =>
        await this.Client.GetAsync(url, CancellationToken.None);

    protected async Task<HttpResponseMessage> PostRawAsync<TRequest>(string url, TRequest body)
    {
        var json = JsonSerializer.Serialize(body, JsonOptions);
        using var content = new StringContent(json, Encoding.UTF8, "application/json");
        return await this.Client.PostAsync(url, content, CancellationToken.None);
    }

    protected async Task<HttpResponseMessage> PostRawAsync(string url) =>
        await this.Client.PostAsync(url, null, CancellationToken.None);

    protected async Task<HttpResponseMessage> PutRawAsync<TRequest>(string url, TRequest body)
    {
        var json = JsonSerializer.Serialize(body, JsonOptions);
        using var content = new StringContent(json, Encoding.UTF8, "application/json");
        return await this.Client.PutAsync(url, content, CancellationToken.None);
    }

    protected async Task<HttpResponseMessage> PatchRawAsync<TRequest>(string url, TRequest body)
    {
        var json = JsonSerializer.Serialize(body, JsonOptions);
        using var content = new StringContent(json, Encoding.UTF8, "application/json");
        return await this.Client.PatchAsync(url, content, CancellationToken.None);
    }

    protected async Task<HttpResponseMessage> DeleteRawAsync(string url) =>
        await this.Client.DeleteAsync(url, CancellationToken.None);

    /// <summary>Sets the Bearer token on the client for authenticated requests.</summary>
    protected void SetBearerToken(string token)
    {
        this.Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
    }

    /// <summary>Clears the Bearer token from the client headers.</summary>
    protected void ClearBearerToken()
    {
        this.Client.DefaultRequestHeaders.Authorization = null;
    }

    protected virtual void Dispose(bool disposing)
    {
        if (this.disposed)
        {
            return;
        }

        // Intentionally do NOT dispose the per-test HttpClient: in current Microsoft.AspNetCore.Mvc.Testing
        // versions, disposing a client created via CreateClient also disposes the underlying TestServer
        // (which is shared across all tests in the run).
        _ = disposing;
        this.disposed = true;
    }

    private static void EnsureServerStarted()
    {
        if (serverStarted)
        {
            return;
        }

        lock (FactoryLock)
        {
            if (serverStarted)
            {
                return;
            }

            sharedFactory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Testing");
                builder.UseContentRoot(LocateWebProjectContentRoot());
            });

            // Force the server to start (runs migrations, seeds data) before any tests execute.
            // The warmup client is kept alive intentionally — disposing it tears down the
            // shared TestServer in current Microsoft.AspNetCore.Mvc.Testing versions.
            warmupClient = sharedFactory.CreateClient();

            serverStarted = true;
        }
    }

    private static string LocateWebProjectContentRoot()
    {
        var dir = new System.IO.DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            var candidate = System.IO.Path.Combine(dir.FullName, "src", "Stack86.Web");
            if (System.IO.Directory.Exists(candidate)
                && System.IO.File.Exists(System.IO.Path.Combine(candidate, "Stack86.Web.csproj")))
            {
                return candidate;
            }

            dir = dir.Parent;
        }

        throw new System.IO.DirectoryNotFoundException(
            "Unable to locate Stack86.Web project directory from the test assembly base directory.");
    }
}
