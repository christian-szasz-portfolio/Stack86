namespace Stack86.Api.Test.Controllers;

using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Stack86.Api.Controllers;
using Stack86.Api.Infrastructure.Compiler;
using Stack86.Api.Models.Compiler.Requests;

/// <summary>
/// The refusal paths. Both endpoints are allowed to say "no" when the compiler is saturated,
/// and both have to say it in a way a client can act on: a 503 with a Retry-After, and for the
/// stream a terminating result event rather than a silently truncated body.
/// </summary>
[TestClass]
public sealed class CompilerControllerCapacityTests : TestBase<CompilerController>
{
    [TestMethod]
    public async Task CompileAsync_WhenTheQueueIsFull_Returns503WithRetryAfter()
    {
        // Arrange
        this.GetMock<ICompilationQueue>()
            .Setup(q => q.TryEnqueue(It.IsAny<CompilationJob>()))
            .Returns(false);

        // Act
        var result = await this.Sut.CompileAsync(Request());

        // Assert
        var status = (ObjectResult)result;
        Assert.AreEqual(StatusCodes.Status503ServiceUnavailable, status.StatusCode);
        Assert.AreEqual("5", this.Sut.Response.Headers.RetryAfter.ToString());
    }

    [TestMethod]
    public async Task CompileAsync_WhenTheQueueIsFull_ExplainsWhyInAProblemDocument()
    {
        // Arrange
        this.GetMock<ICompilationQueue>()
            .Setup(q => q.TryEnqueue(It.IsAny<CompilationJob>()))
            .Returns(false);

        // Act
        var result = await this.Sut.CompileAsync(Request());

        // Assert
        var payload = ((ObjectResult)result).Value!.ToString()!;
        StringAssert.Contains(payload, "Service Unavailable");
    }

    [TestMethod]
    public async Task CompileAsync_WhenTheClientDisconnects_ReturnsNothingRatherThanAnError()
    {
        // Arrange: the job is accepted but never completed, and the caller goes away.
        using var caller = new CancellationTokenSource();
        this.GetMock<ICompilationQueue>()
            .Setup(q => q.TryEnqueue(It.IsAny<CompilationJob>()))
            .Callback<CompilationJob>(_ => caller.Cancel())
            .Returns(true);

        // Act
        var result = await this.Sut.CompileAsync(Request(), caller.Token);

        // Assert
        Assert.IsInstanceOfType<EmptyResult>(result);
    }

    [TestMethod]
    public async Task CompileStreamAsync_WhenNoStreamSlotIsFree_Returns503AndATerminatingResultEvent()
    {
        // Arrange
        var body = this.CaptureBody();
        this.GetMock<ICompilationQueue>()
            .Setup(q => q.TryAcquireStreamSlot())
            .Returns(false);

        // Act
        var result = await this.Sut.CompileStreamAsync(Request());

        // Assert
        Assert.IsInstanceOfType<EmptyResult>(result);
        Assert.AreEqual(StatusCodes.Status503ServiceUnavailable, this.Sut.Response.StatusCode);
        Assert.AreEqual("5", this.Sut.Response.Headers.RetryAfter.ToString());

        var payload = Encoding.UTF8.GetString(body.ToArray());
        StringAssert.Contains(payload, "Server is busy");
        StringAssert.Contains(payload, "\"type\":\"log\"");
        StringAssert.Contains(payload, "\"type\":\"result\"");
    }

    [TestMethod]
    public async Task CompileStreamAsync_WhenNoStreamSlotIsFree_DoesNotReleaseASlotItNeverTook()
    {
        // Arrange
        this.CaptureBody();
        var queue = this.GetMock<ICompilationQueue>();
        queue.Setup(q => q.TryAcquireStreamSlot()).Returns(false);

        // Act
        await this.Sut.CompileStreamAsync(Request());

        // Assert
        queue.Verify(q => q.ReleaseStreamSlot(), Times.Never);
    }

    [TestMethod]
    public async Task CompileStreamAsync_SetsTheHeadersAProxyNeedsToLeaveTheStreamAlone()
    {
        // Arrange
        this.CaptureBody();
        this.GetMock<ICompilationQueue>()
            .Setup(q => q.TryAcquireStreamSlot())
            .Returns(false);

        // Act
        await this.Sut.CompileStreamAsync(Request());

        // Assert
        Assert.AreEqual("application/x-ndjson", this.Sut.Response.ContentType);
        Assert.AreEqual("no-cache", this.Sut.Response.Headers.CacheControl.ToString());
        Assert.AreEqual("no", this.Sut.Response.Headers["X-Accel-Buffering"].ToString());
    }

    /// <inheritdoc />
    protected override CompilerController CreateSut()
    {
        var sut = base.CreateSut();
        sut.ControllerContext = ControllerContextFactory.Create();
        return sut;
    }

    private static CompileRequest Request() => new()
    {
        Language = "c",
        Files = new Dictionary<string, string> { ["main.c"] = "int main(){return 0;}" },
    };

    private MemoryStream CaptureBody()
    {
        var body = new MemoryStream();
        this.Sut.ControllerContext.HttpContext.Response.Body = body;
        return body;
    }
}
