namespace Stack86.Integration.Test.WebInfrastructure;

using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Stack86.Common.Security.Options;
using Stack86.Web.Infrastructure.Security;

[TestClass]
public sealed class ContentSecurityPolicyMiddlewareTests
{
    [TestMethod]
    public async Task InvokeAsync_DisabledOption_DoesNotSetHeader()
    {
        var ctx = NewContext("/index.html");
        var sut = new ContentSecurityPolicyMiddleware(_ => Task.CompletedTask, Options.Create(new SecurityHeadersOptions { EnableContentSecurityPolicy = false }));

        await sut.InvokeAsync(ctx);

        Assert.IsTrue(string.IsNullOrEmpty(ctx.Response.Headers.ContentSecurityPolicy.ToString()));
        Assert.IsFalse(ctx.Items.ContainsKey(ContentSecurityPolicyMiddleware.NonceItemKey));
    }

    [TestMethod]
    public async Task InvokeAsync_ApiPath_Skips()
    {
        var ctx = NewContext("/api/auth/login");
        var sut = new ContentSecurityPolicyMiddleware(_ => Task.CompletedTask, Options.Create(new SecurityHeadersOptions()));

        await sut.InvokeAsync(ctx);

        Assert.IsTrue(string.IsNullOrEmpty(ctx.Response.Headers.ContentSecurityPolicy.ToString()));
    }

    [TestMethod]
    public async Task InvokeAsync_ScalarPath_Skips()
    {
        var ctx = NewContext("/scalar/v1");
        var sut = new ContentSecurityPolicyMiddleware(_ => Task.CompletedTask, Options.Create(new SecurityHeadersOptions()));

        await sut.InvokeAsync(ctx);

        Assert.IsTrue(string.IsNullOrEmpty(ctx.Response.Headers.ContentSecurityPolicy.ToString()));
    }

    [TestMethod]
    public async Task InvokeAsync_OpenApiPath_Skips()
    {
        var ctx = NewContext("/openapi/v1.json");
        var sut = new ContentSecurityPolicyMiddleware(_ => Task.CompletedTask, Options.Create(new SecurityHeadersOptions()));

        await sut.InvokeAsync(ctx);

        Assert.IsTrue(string.IsNullOrEmpty(ctx.Response.Headers.ContentSecurityPolicy.ToString()));
    }

    [TestMethod]
    public async Task InvokeAsync_HtmlPath_SetsCspWithNonce()
    {
        var ctx = NewContext("/");
        var sut = new ContentSecurityPolicyMiddleware(_ => Task.CompletedTask, Options.Create(new SecurityHeadersOptions()));

        await sut.InvokeAsync(ctx);

        var csp = ctx.Response.Headers.ContentSecurityPolicy.ToString();
        StringAssert.Contains(csp, "default-src 'self'");
        StringAssert.Contains(csp, "wasm-unsafe-eval");
        StringAssert.Contains(csp, "frame-ancestors 'none'");
        Assert.IsNotNull(ctx.Items[ContentSecurityPolicyMiddleware.NonceItemKey]);
    }

    [TestMethod]
    public async Task InvokeAsync_AdditionalConnectSources_AppendedToCsp()
    {
        var ctx = NewContext("/");
        var sut = new ContentSecurityPolicyMiddleware(
            _ => Task.CompletedTask,
            Options.Create(new SecurityHeadersOptions { AdditionalConnectSources = ["wss://example.test", "https://api.test"] }));

        await sut.InvokeAsync(ctx);

        var csp = ctx.Response.Headers.ContentSecurityPolicy.ToString();
        StringAssert.Contains(csp, "wss://example.test");
        StringAssert.Contains(csp, "https://api.test");
    }

    [TestMethod]
    public async Task InvokeAsync_NullContext_Throws()
    {
        var sut = new ContentSecurityPolicyMiddleware(_ => Task.CompletedTask, Options.Create(new SecurityHeadersOptions()));
        await Assert.ThrowsExactlyAsync<ArgumentNullException>(async () => await sut.InvokeAsync(null!));
    }

    private static DefaultHttpContext NewContext(string path)
    {
        var ctx = new DefaultHttpContext();
        ctx.Request.Path = path;
        return ctx;
    }
}
