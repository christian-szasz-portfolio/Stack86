namespace Stack86.Web.Infrastructure.Security;

using System.Net;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;
using Serilog;
using Stack86.Common.Exceptions;
using Stack86.Common.Json;

/// <summary>
/// Global API exception handler. Maps domain exceptions to HTTP problem-details responses with a traceId.
/// </summary>
public sealed class ApiExceptionHandler(
    IHostEnvironment environment) : IExceptionHandler
{
    /// <inheritdoc/>
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(httpContext);
        ArgumentNullException.ThrowIfNull(exception);

        var traceId = httpContext.TraceIdentifier;

        switch (exception)
        {
            case ValidationException validation:
                Log.Information(validation, "Validation failure {TraceId}", traceId);
                await WriteValidation(httpContext, validation, traceId, cancellationToken).ConfigureAwait(false);
                return true;

            default:
                Log.Error(exception, "Unhandled exception {TraceId}", traceId);
                var detail = environment.IsDevelopment() ? exception.ToString() : "An unexpected error occurred.";
                await WriteProblem(httpContext, HttpStatusCode.InternalServerError, "Internal server error", detail, traceId, cancellationToken).ConfigureAwait(false);
                return true;
        }
    }

    private static Task WriteProblem(HttpContext context, HttpStatusCode status, string title, string detail, string traceId, CancellationToken ct)
    {
        var problem = new ProblemDetails
        {
            Status = (int)status,
            Title = title,
            Detail = detail,
            Type = $"https://httpstatuses.io/{(int)status}",
            Instance = context.Request.Path,
        };
        problem.Extensions["traceId"] = traceId;

        context.Response.StatusCode = (int)status;
        context.Response.ContentType = "application/problem+json";
        return JsonSerializer.SerializeAsync(context.Response.Body, problem, problem.GetType(), Stack86JsonOptions.Default, ct);
    }

    private static Task WriteValidation(HttpContext context, ValidationException ex, string traceId, CancellationToken ct)
    {
        var problem = new ValidationProblemDetails(ex.Errors.ToDictionary(kv => kv.Key, kv => kv.Value))
        {
            Status = (int)HttpStatusCode.BadRequest,
            Title = "One or more validation errors occurred.",
            Type = "https://httpstatuses.io/400",
            Instance = context.Request.Path,
        };
        problem.Extensions["traceId"] = traceId;

        context.Response.StatusCode = StatusCodes.Status400BadRequest;
        context.Response.ContentType = "application/problem+json";
        return JsonSerializer.SerializeAsync(context.Response.Body, problem, problem.GetType(), Stack86JsonOptions.Default, ct);
    }
}
