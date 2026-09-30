namespace Stack86.Integration.Test.WebInfrastructure;

using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Stack86.Common.Security.Options;
using Stack86.Web.Infrastructure.Security;

[TestClass]
public sealed class SecurityHeadersMiddlewareTests
{
    [TestMethod]
    public async Task InvokeAsync_AlwaysSetsBaseHeaders()
    {
        var ctx = NewContext(isHttps: false);
        var sut = new SecurityHeadersMiddleware(_ => Task.CompletedTask, Options.Create(new SecurityHeadersOptions()));

        await sut.InvokeAsync(ctx);

        Assert.AreEqual("nosniff", ctx.Response.Headers.XContentTypeOptions.ToString());
        Assert.AreEqual("DENY", ctx.Response.Headers.XFrameOptions.ToString());
        Assert.AreEqual("strict-origin-when-cross-origin", ctx.Response.Headers["Referrer-Policy"].ToString());
        Assert.AreEqual("same-origin", ctx.Response.Headers["Cross-Origin-Opener-Policy"].ToString());
        Assert.AreEqual("same-origin", ctx.Response.Headers["Cross-Origin-Resource-Policy"].ToString());
        Assert.IsTrue(string.IsNullOrEmpty(ctx.Response.Headers.StrictTransportSecurity.ToString()));
    }

    [TestMethod]
    public async Task InvokeAsync_OverHttps_AddsHstsWhenEnabled()
    {
        var ctx = NewContext(isHttps: true);
        var sut = new SecurityHeadersMiddleware(_ => Task.CompletedTask, Options.Create(new SecurityHeadersOptions { EnableHsts = true, HstsMaxAgeSeconds = 1234 }));

        await sut.InvokeAsync(ctx);

        StringAssert.Contains(ctx.Response.Headers.StrictTransportSecurity.ToString(), "max-age=1234");
        StringAssert.Contains(ctx.Response.Headers.StrictTransportSecurity.ToString(), "includeSubDomains");
    }

    [TestMethod]
    public async Task InvokeAsync_OverHttps_OmitsHstsWhenDisabled()
    {
        var ctx = NewContext(isHttps: true);
        var sut = new SecurityHeadersMiddleware(_ => Task.CompletedTask, Options.Create(new SecurityHeadersOptions { EnableHsts = false }));

        await sut.InvokeAsync(ctx);

        Assert.IsTrue(string.IsNullOrEmpty(ctx.Response.Headers.StrictTransportSecurity.ToString()));
    }

    [TestMethod]
    public async Task InvokeAsync_NullContext_Throws()
    {
        var sut = new SecurityHeadersMiddleware(_ => Task.CompletedTask, Options.Create(new SecurityHeadersOptions()));
        await Assert.ThrowsExactlyAsync<ArgumentNullException>(async () => await sut.InvokeAsync(null!));
    }

    private static DefaultHttpContext NewContext(bool isHttps)
    {
        var ctx = new DefaultHttpContext();
        ctx.Request.Scheme = isHttps ? "https" : "http";
        return ctx;
    }
}
