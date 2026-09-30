namespace Stack86.Common.Exceptions;

using System.Collections.Generic;

/// <summary>
/// Thrown when input validation fails. Maps to HTTP 400 with a ValidationProblemDetails body.
/// </summary>
public sealed class ValidationException(IReadOnlyDictionary<string, string[]> errors)
    : Exception("One or more validation errors occurred.")
{
    /// <summary>Initializes a new instance of the <see cref="ValidationException"/> class with a single field error.</summary>
    public ValidationException(string field, string error)
        : this(new Dictionary<string, string[]> { [field] = [error] })
    {
    }

    /// <summary>Gets the field-keyed validation errors.</summary>
    public IReadOnlyDictionary<string, string[]> Errors { get; } = errors;
}
