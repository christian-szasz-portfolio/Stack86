namespace Stack86.Integration.Test.WebInfrastructure;

using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Stack86.Common.Exceptions;
using Stack86.Web.Infrastructure.Security;

[TestClass]
public sealed class ApiExceptionHandlerTests
{
    [TestMethod]
    public Task TryHandleAsync_Validation_WritesBadRequestProblem() =>
        Run(new ValidationException(new Dictionary<string, string[]> { ["Email"] = ["bad"] }), HttpStatusCode.BadRequest, "validation");

    [TestMethod]
    public Task TryHandleAsync_Unknown_WritesInternalServerError_ProductionDetailIsGeneric() =>
        Run(new InvalidOperationException("boom"), HttpStatusCode.InternalServerError, "An unexpected error occurred", "Production");

    [TestMethod]
    public async Task TryHandleAsync_Unknown_InDevelopment_DetailIncludesException()
    {
        var sut = new ApiExceptionHandler(new FakeEnv("Development"));
        var ctx = NewContext();

        var handled = await sut.TryHandleAsync(ctx, new InvalidOperationException("dev-boom"), CancellationToken.None);

        Assert.IsTrue(handled);
        var body = await ReadBody(ctx);
        StringAssert.Contains(body, "dev-boom");
    }

    [TestMethod]
    public async Task TryHandleAsync_NullContext_Throws()
    {
        var sut = new ApiExceptionHandler(new FakeEnv("Production"));
        await Assert.ThrowsExactlyAsync<ArgumentNullException>(async () => await sut.TryHandleAsync(null!, new Exception(), CancellationToken.None));
    }

    [TestMethod]
    public async Task TryHandleAsync_NullException_Throws()
    {
        var sut = new ApiExceptionHandler(new FakeEnv("Production"));
        await Assert.ThrowsExactlyAsync<ArgumentNullException>(async () => await sut.TryHandleAsync(NewContext(), null!, CancellationToken.None));
    }

    private static async Task Run(Exception exception, HttpStatusCode expected, string contains, string env = "Production")
    {
        var sut = new ApiExceptionHandler(new FakeEnv(env));
        var ctx = NewContext();

        var handled = await sut.TryHandleAsync(ctx, exception, CancellationToken.None);

        Assert.IsTrue(handled);
        Assert.AreEqual((int)expected, ctx.Response.StatusCode);
        Assert.AreEqual("application/problem+json", ctx.Response.ContentType);
        var body = await ReadBody(ctx);
        StringAssert.Contains(body.ToLowerInvariant(), contains.ToLowerInvariant());
        StringAssert.Contains(body, "traceId");
    }

    private static DefaultHttpContext NewContext()
    {
        var ctx = new DefaultHttpContext { TraceIdentifier = "trace-1" };
        ctx.Request.Path = "/api/test";
        ctx.Response.Body = new MemoryStream();
        return ctx;
    }

    private static async Task<string> ReadBody(HttpContext ctx)
    {
        ctx.Response.Body.Position = 0;
        using var reader = new StreamReader(ctx.Response.Body, Encoding.UTF8, leaveOpen: true);
        return await reader.ReadToEndAsync();
    }

    private sealed class FakeEnv(string env) : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = env;

        public string ApplicationName { get; set; } = "Test";

        public string ContentRootPath { get; set; } = ".";

        public IFileProvider ContentRootFileProvider { get; set; } = null!;
    }
}
