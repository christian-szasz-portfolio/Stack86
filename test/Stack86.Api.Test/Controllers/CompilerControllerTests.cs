namespace Stack86.Api.Test.Controllers;

using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Stack86.Api.Controllers;
using Stack86.Api.Controllers.Utility;
using Stack86.Api.Infrastructure.Compiler;
using Stack86.Api.Models.Compiler.Requests;
using Stack86.Common.Exceptions;
using Stack86.Logic.Compilation.Models;
using Stack86.Test.Utility;

[TestClass]
public sealed class CompilerControllerTests : TestBase<CompilerController>
{
    [TestMethod]
    public async Task CompileAsync_WhenSuccess_ReturnsOkWithAssembly()
    {
        // Arrange
        var dto = new CompilationResultDto { Assembly = ".CODE\nMOV AX, 1\n" };
        this.GetMock<ICompilationQueue>()
            .Setup(q => q.TryEnqueue(It.IsAny<CompilationJob>()))
            .Callback<CompilationJob>(job => job.Completion.TrySetResult(dto))
            .Returns(true);

        // Act
        var result = await this.Sut.CompileAsync(new CompileRequest
        {
            Language = "c",
            Files = new Dictionary<string, string> { ["main.c"] = "int main(){return 0;}" },
        });

        // Assert
        Assert.IsInstanceOfType<OkObjectResult>(result);
    }

    [TestMethod]
    public async Task CompileAsync_WhenDispatcherFails_ReturnsBadRequestWithSingleError()
    {
        // Arrange
        this.GetMock<ICompilationQueue>()
            .Setup(q => q.TryEnqueue(It.IsAny<CompilationJob>()))
            .Callback<CompilationJob>(job => job.Completion.TrySetException(new CompilationFailedException("Unsupported language")))
            .Returns(true);

        // Act
        var result = await this.Sut.CompileAsync(new CompileRequest
        {
            Language = "fortran",
            Files = new Dictionary<string, string> { ["main.f"] = "PROGRAM HELLO" },
        });

        // Assert
        Assert.IsInstanceOfType<BadRequestObjectResult>(result);
    }

    [TestMethod]
    public async Task CompileAsync_WhenQueueIsFull_ReturnsServiceUnavailable()
    {
        // Arrange
        this.GetMock<ICompilationQueue>()
            .Setup(q => q.TryEnqueue(It.IsAny<CompilationJob>()))
            .Returns(false);

        // Act
        var result = await this.Sut.CompileAsync(new CompileRequest
        {
            Language = "c",
            Files = new Dictionary<string, string> { ["main.c"] = "int main(){return 0;}" },
        });

        // Assert
        var objectResult = Assert.IsInstanceOfType<ObjectResult>(result);
        Assert.AreEqual(StatusCodes.Status503ServiceUnavailable, objectResult.StatusCode);
    }

    protected override CompilerController CreateSut()
    {
        var sut = base.CreateSut();
        sut.ControllerContext = ControllerContextFactory.Create();
        return sut;
    }
}
