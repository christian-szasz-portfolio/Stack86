namespace Stack86.Api.Models.Compiler.Requests;

using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

/// <summary>
/// Request payload for the compile endpoint.
/// </summary>
public sealed record CompileRequest : IValidatableObject
{
    /// <summary>The largest body the compile endpoints read: the source ceiling plus room for the JSON around it.</summary>
    public const int MaxBodyBytes = 4 * MaxTotalSourceBytes;

    /// <summary>The ceiling only the external Tcc/TypeScript stages enforced before this; every
    /// language now gets the same bound before any compiler stage sees the source.</summary>
    private const int MaxTotalSourceBytes = 262_144;

    /// <summary>
    /// Gets the source language (e.g. "c", "cpp", "csharp", "javascript", "typescript", "rust", "java").
    /// </summary>
    [Required]
    public required string Language { get; init; }

    /// <summary>
    /// Gets a map of file names to their source content (e.g. { "main.c": "...", "utils.h": "..." }).
    /// </summary>
    [Required]
    [MinLength(1)]
    public required IReadOnlyDictionary<string, string> Files { get; init; }

    /// <inheritdoc/>
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        var totalBytes = 0;
        foreach (var content in this.Files.Values)
        {
            totalBytes += Encoding.UTF8.GetByteCount(content);
        }

        if (totalBytes > MaxTotalSourceBytes)
        {
            yield return new ValidationResult(
                $"Total source size ({totalBytes} bytes) exceeds the {MaxTotalSourceBytes}-byte limit.",
                [nameof(this.Files)]);
        }
    }
}
