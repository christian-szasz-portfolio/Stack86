namespace Stack86.Logic.Test.Compilation;

using Stack86.Common.Exceptions;
using Stack86.Logic.Compilation.Models;

/// <summary>
/// Test-only wrapper that mirrors the success/failure shape the compile tests expect.
/// A returned DTO (including one that carries compile errors) is treated as a success;
/// a thrown <see cref="CompilationFailedException"/> is treated as a failure.
/// </summary>
internal sealed class CompileResult
{
    private readonly CompilationResultDto? value;
    private readonly string? error;

    private CompileResult(CompilationResultDto? value, string? error)
    {
        this.value = value;
        this.error = error;
    }

    public bool IsSuccess => this.error is null;

    public bool IsFailure => this.error is not null;

    public CompilationResultDto Value =>
        this.value ?? throw new InvalidOperationException($"Compilation failed: {this.error}");

    public string Error =>
        this.error ?? throw new InvalidOperationException("Compilation succeeded; no error available.");

    public static async Task<CompileResult> RunAsync(Func<Task<CompilationResultDto>> compile)
    {
        try
        {
            return new CompileResult(await compile().ConfigureAwait(false), null);
        }
        catch (CompilationFailedException ex)
        {
            return new CompileResult(null, ex.Message);
        }
    }
}
