namespace Stack86.Api.Test.Controllers;

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Stack86.Api.Controllers;
using Stack86.Api.Infrastructure.Compiler;
using Stack86.Api.Models.Compiler.Requests;
using Stack86.Logic.Compilation.Models;

[TestClass]
public sealed class CompilerControllerExtraTests : TestBase<CompilerController>
{
    [TestMethod]
    public async Task CompileAsync_PopulatesErrorsAndWarnings()
    {
        var dto = new CompilationResultDto
        {
            Assembly = ".CODE",
            Errors = [new DiagnosticDto { Message = "err", Line = 1, Column = 2, SourceFile = "main.c" }],
            Warnings = [new DiagnosticDto { Message = "warn", Line = 3, Column = 4, SourceFile = "main.c" }],
            ConsoleMessages = ["[Build Log] starting"],
        };
        this.GetMock<ICompilationQueue>()
            .Setup(q => q.TryEnqueue(It.IsAny<CompilationJob>()))
            .Callback<CompilationJob>(job => job.Completion.TrySetResult(dto))
            .Returns(true);

        var result = await this.Sut.CompileAsync(new CompileRequest
        {
            Language = "c",
            Files = new Dictionary<string, string> { ["main.c"] = "int main(){return 0;}" },
        });

        var ok = (OkObjectResult)result;
        var response = (Stack86.Api.Models.Compiler.Responses.CompileResponse)ok.Value!;
        Assert.HasCount(1, response.Errors);
        Assert.HasCount(1, response.Warnings);
        Assert.AreEqual("err", response.Errors[0].Message);
        Assert.AreEqual("main.c", response.Warnings[0].File);
    }

    [TestMethod]
    public async Task CompileStreamAsync_UnsupportedLanguage_WritesErrorAndResult()
    {
        var http = new DefaultHttpContext();
        var body = new MemoryStream();
        http.Response.Body = body;
        this.Sut.ControllerContext = new ControllerContext { HttpContext = http };

        var result = await this.Sut.CompileStreamAsync(new CompileRequest
        {
            Language = "fortran",
            Files = new Dictionary<string, string> { ["main.f"] = "PROGRAM" },
        });

        Assert.IsInstanceOfType<EmptyResult>(result);
        Assert.AreEqual("application/x-ndjson", http.Response.ContentType);
        var payload = Encoding.UTF8.GetString(body.ToArray());
        StringAssert.Contains(payload, "Unsupported language");
        StringAssert.Contains(payload, "\"type\":\"log\"");
        StringAssert.Contains(payload, "\"type\":\"result\"");
    }

    protected override CompilerController CreateSut()
    {
        var sut = base.CreateSut();
        sut.ControllerContext = ControllerContextFactory.Create();
        return sut;
    }
}
