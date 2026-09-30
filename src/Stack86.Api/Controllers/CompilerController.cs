namespace Stack86.Api.Controllers;

using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.DependencyInjection;
using Stack86.Api.Controllers.Utility;
using Stack86.Api.Infrastructure.Compiler;
using Stack86.Api.Models.Compiler.Requests;
using Stack86.Api.Models.Compiler.Responses;
using Stack86.Common.Exceptions;
using Stack86.Common.Json;
using Stack86.Common.Security.Constants;
using Stack86.Logic.Compilation.Models;
using Stack86.Logic.Compilation.Providers;
using Stack86.Logic.Languages;

/// <summary>
/// Handles compilation requests — transforms high-level source code into 8086 assembly.
/// </summary>
[EnableRateLimiting(RateLimitPolicies.Compile)]
public sealed class CompilerController : ApiControllerBase
{
    private static readonly byte[] NdJsonNewLine = "\n"u8.ToArray();

    [Inject]
    public required CompilerProvider Compiler { private get; init; }

    [Inject]
    public required ICompilationQueue Queue { private get; init; }

    /// <summary>
    /// Compiles the provided source code for the specified language and returns the compilation result, including
    /// generated assembly, errors, and warnings.
    /// </summary>
    /// <param name="request">The compilation request containing the source code and language identifier.</param>
    /// <returns>A <see cref="CompileResponse"/> wrapped in HTTP 200 on success or HTTP 400 on failure.</returns>
    [HttpPost]
    [ActionName("compile")]
    [RequestSizeLimit(CompileRequest.MaxBodyBytes)]
    [ProducesResponseType<CompileResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<CompileResponse>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> CompileAsync([FromBody] CompileRequest request, CancellationToken cancellationToken = default)
    {
        var job = new CompilationJob
        {
            Language = request.Language,
            Files = request.Files,
            CancellationToken = cancellationToken,
        };

        if (!this.Queue.TryEnqueue(job))
        {
            return this.CompilerBusy();
        }

        CompilationResultDto result;
        try
        {
            result = await job.Completion.Task.WaitAsync(cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // Client disconnected before the job completed — nothing left to return.
            return new EmptyResult();
        }
        catch (CompilationFailedException ex)
        {
            return this.BadRequest(new CompileResponse
            {
                Errors = [new CompilationDiagnosticDto { Message = ex.Message, Line = 0, Column = 0 }],
            });
        }

        return this.Ok(MapToResponse(result));
    }

    /// <summary>
    /// Streams compilation progress as newline-delimited JSON. Each line is either a
    /// <c>{"type":"log","text":"..."}</c> event or a final
    /// <c>{"type":"result","assembly":"...","errors":[...],"warnings":[...]}</c> event.
    /// </summary>
    /// <param name="request">The compilation request.</param>
    /// <returns>An empty action result; the body has already been streamed.</returns>
    [HttpPost]
    [ActionName("compileStream")]
    [RequestSizeLimit(CompileRequest.MaxBodyBytes)]
    [ProducesResponseType<EmptyResult>(StatusCodes.Status200OK)]
    public async Task<IActionResult> CompileStreamAsync([FromBody] CompileRequest request, CancellationToken cancellationToken = default)
    {
        this.Response.ContentType = "application/x-ndjson";
        this.Response.Headers.CacheControl = "no-cache";
        this.Response.Headers["X-Accel-Buffering"] = "no";

        var bufferingFeature = this.HttpContext.Features.Get<IHttpResponseBodyFeature>();
        bufferingFeature?.DisableBuffering();

        var cancellation = cancellationToken;
        var body = this.Response.Body;

        if (!SupportedLanguage.TryFromValue(request.Language, out var language))
        {
            await WriteEventAsync(body, new { type = "log", text = $"[Error] Unsupported language: '{request.Language}'" }, cancellation);
            await WriteEventAsync(
                body,
                new
                {
                    type = "result",
                    assembly = (string?)null,
                    errors = new[] { new { message = $"Unsupported language: '{request.Language}'", line = 0, column = 0, file = (string?)null } },
                    warnings = Array.Empty<object>(),
                    consoleMessages = Array.Empty<string>(),
                },
                cancellation);
            return new EmptyResult();
        }

        if (!this.Queue.TryAcquireStreamSlot())
        {
            this.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
            this.Response.Headers.RetryAfter = "5";
            await WriteEventAsync(body, new { type = "log", text = "[Error] Server is busy. Please retry shortly." }, cancellation);
            await WriteEventAsync(
                body,
                new
                {
                    type = "result",
                    assembly = (string?)null,
                    errors = new[] { new { message = "Server is busy. Please retry shortly.", line = 0, column = 0, file = (string?)null } },
                    warnings = Array.Empty<object>(),
                    consoleMessages = Array.Empty<string>(),
                },
                cancellation);
            return new EmptyResult();
        }

        try
        {
            // Built only once a slot is held, so a saturated server does no compiler work,
            // and inside the try so a throwing pipeline cannot leak the slot.
            var stream = this.Compiler.CompileStreamAsync(language, request.Files, cancellation);

            await foreach (var evt in stream)
            {
                object payload = evt switch
                {
                    CompileStreamEvent.Log log => new { type = "log", text = log.Text },
                    CompileStreamEvent.Heartbeat => new { type = "heartbeat" },
                    CompileStreamEvent.Complete complete => new
                    {
                        type = "result",
                        assembly = complete.Result.Assembly,
                        errors = complete.Result.Errors.Select(e => new { e.Message, e.Line, e.Column, file = e.SourceFile }),
                        warnings = complete.Result.Warnings.Select(w => new { w.Message, w.Line, w.Column, file = w.SourceFile }),
                        consoleMessages = complete.Result.ConsoleMessages,
                    },
                    _ => throw new CompilerInvariantException($"Unknown compile-stream event type: {evt.GetType().Name}"),
                };

                await WriteEventAsync(body, payload, cancellation);
            }
        }
        catch (OperationCanceledException) when (cancellation.IsCancellationRequested)
        {
            // Client disconnected mid-stream — nothing left to send.
        }
        finally
        {
            this.Queue.ReleaseStreamSlot();
        }

        return new EmptyResult();
    }

    private static async Task WriteEventAsync(Stream body, object payload, CancellationToken ct)
    {
        await JsonSerializer.SerializeAsync(body, payload, payload.GetType(), Stack86JsonOptions.Default, ct);
        await body.WriteAsync(NdJsonNewLine, ct);
        await body.FlushAsync(ct);
    }

    private static CompileResponse MapToResponse(CompilationResultDto dto) => new()
    {
        Assembly = dto.Assembly,
        Errors = [.. dto.Errors.Select(e => new CompilationDiagnosticDto
        {
            Message = e.Message,
            Line = e.Line,
            Column = e.Column,
            File = e.SourceFile,
        })],
        Warnings = [.. dto.Warnings.Select(w => new CompilationDiagnosticDto
        {
            Message = w.Message,
            Line = w.Line,
            Column = w.Column,
            File = w.SourceFile,
        })],
        ConsoleMessages = dto.ConsoleMessages,
    };

    private ObjectResult CompilerBusy()
    {
        this.Response.Headers.RetryAfter = "5";
        return this.StatusCode(
            StatusCodes.Status503ServiceUnavailable,
            new
            {
                type = "https://tools.ietf.org/html/rfc7231#section-6.6.4",
                title = "Service Unavailable",
                status = StatusCodes.Status503ServiceUnavailable,
                detail = "The compiler is at capacity. Please retry shortly.",
            });
    }
}
