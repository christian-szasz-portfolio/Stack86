namespace Stack86.Api.Infrastructure.Compiler;

using System.Threading;
using System.Threading.Tasks;
using Stack86.Logic.Compilation.Models;

/// <summary>
/// A unit of work enqueued onto the <see cref="ICompilationQueue"/>. The producer (the controller)
/// awaits <see cref="Completion"/>; a background worker dispatches the compile command and sets the result.
/// </summary>
public sealed class CompilationJob
{
    /// <summary>Gets the source language identifier.</summary>
    public required string Language { get; init; }

    /// <summary>Gets the project files keyed by filename.</summary>
    public required IReadOnlyDictionary<string, string> Files { get; init; }

    /// <summary>Gets the request cancellation token (e.g. client disconnect).</summary>
    public required CancellationToken CancellationToken { get; init; }

    /// <summary>Gets the completion source signalled by the worker once the job has been processed.</summary>
    public TaskCompletionSource<CompilationResultDto> Completion { get; } =
        new(TaskCreationOptions.RunContinuationsAsynchronously);
}
