namespace Stack86.Logic.Test.Languages.C;

using System.Collections.Generic;
using Stack86.Common.Exceptions;
using Stack86.Logic.Languages.C;

/// <summary>
/// Test-only wrapper mirroring the success/failure shape the preprocessor tests expect.
/// A returned <see cref="CIncludePreprocessor.PreprocessResult"/> is treated as success;
/// a thrown <see cref="CompilationFailedException"/> is treated as failure.
/// </summary>
internal sealed class PreprocessOutcome
{
    private readonly CIncludePreprocessor.PreprocessResult? value;
    private readonly string? error;

    private PreprocessOutcome(CIncludePreprocessor.PreprocessResult? value, string? error)
    {
        this.value = value;
        this.error = error;
    }

    public bool IsSuccess => this.error is null;

    public bool IsFailure => this.error is not null;

    public CIncludePreprocessor.PreprocessResult Value =>
        this.value ?? throw new InvalidOperationException($"Preprocess failed: {this.error}");

    public string Error =>
        this.error ?? throw new InvalidOperationException("Preprocess succeeded; no error available.");

    public static PreprocessOutcome Run(IReadOnlyDictionary<string, string> files, string mainFile)
    {
        try
        {
            return new PreprocessOutcome(CIncludePreprocessor.Preprocess(files, mainFile), null);
        }
        catch (CompilationFailedException ex)
        {
            return new PreprocessOutcome(null, ex.Message);
        }
    }
}
