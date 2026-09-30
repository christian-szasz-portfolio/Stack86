namespace Stack86.Integration.Test.WebInfrastructure;

using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Stack86.Web.Infrastructure.Security;

[TestClass]
public sealed class IndexHtmlNonceWriterTests
{
    [TestMethod]
    public void HasTemplate_FalseWhenIndexHtmlMissing()
    {
        var dir = CreateTempDir();
        try
        {
            var sut = new IndexHtmlNonceWriter(dir);
            Assert.IsFalse(sut.HasTemplate);
        }
        finally
        {
            Directory.Delete(dir, true);
        }
    }

    [TestMethod]
    public void HasTemplate_TrueWhenIndexHtmlExists()
    {
        var dir = CreateTempDir();
        try
        {
            File.WriteAllText(Path.Combine(dir, "index.html"), "<html nonce='__CSP_NONCE__'></html>", Encoding.UTF8);
            var sut = new IndexHtmlNonceWriter(dir);
            Assert.IsTrue(sut.HasTemplate);
        }
        finally
        {
            Directory.Delete(dir, true);
        }
    }

    [TestMethod]
    public async Task WriteAsync_SubstitutesNonceAndSetsHeaders()
    {
        var dir = CreateTempDir();
        try
        {
            File.WriteAllText(Path.Combine(dir, "index.html"), "<script nonce='__CSP_NONCE__'></script>", Encoding.UTF8);
            var sut = new IndexHtmlNonceWriter(dir);
            var ctx = new DefaultHttpContext();
            ctx.Items[ContentSecurityPolicyMiddleware.NonceItemKey] = "abc123";
            using var body = new MemoryStream();
            ctx.Response.Body = body;

            await sut.WriteAsync(ctx);

            var output = Encoding.UTF8.GetString(body.ToArray());
            StringAssert.Contains(output, "abc123");
            Assert.AreEqual("text/html; charset=utf-8", ctx.Response.ContentType);
            StringAssert.Contains(ctx.Response.Headers.CacheControl.ToString(), "no-cache");
            Assert.AreEqual("no-cache", ctx.Response.Headers.Pragma.ToString());
            Assert.AreEqual("0", ctx.Response.Headers.Expires.ToString());
        }
        finally
        {
            Directory.Delete(dir, true);
        }
    }

    [TestMethod]
    public async Task WriteAsync_WithoutNonceItem_WritesEmptyNonce()
    {
        var dir = CreateTempDir();
        try
        {
            File.WriteAllText(Path.Combine(dir, "index.html"), "x[__CSP_NONCE__]y", Encoding.UTF8);
            var sut = new IndexHtmlNonceWriter(dir);
            var ctx = new DefaultHttpContext();
            using var body = new MemoryStream();
            ctx.Response.Body = body;

            await sut.WriteAsync(ctx);

            Assert.AreEqual("x[]y", Encoding.UTF8.GetString(body.ToArray()));
        }
        finally
        {
            Directory.Delete(dir, true);
        }
    }

    [TestMethod]
    public async Task WriteAsync_NullContext_Throws()
    {
        var dir = CreateTempDir();
        try
        {
            var sut = new IndexHtmlNonceWriter(dir);
            await Assert.ThrowsExactlyAsync<ArgumentNullException>(async () => await sut.WriteAsync(null!));
        }
        finally
        {
            Directory.Delete(dir, true);
        }
    }

    [TestMethod]
    public void Constructor_NullPath_Throws()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() => _ = new IndexHtmlNonceWriter(null!));
    }

    private static string CreateTempDir()
    {
        var dir = Path.Combine(Path.GetTempPath(), "stack86-tests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        return dir;
    }
}
