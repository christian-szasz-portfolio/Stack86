namespace Stack86.Common.Validation;

using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Stack86.Common.Exceptions;

/// <summary>
/// Converts MVC <see cref="ModelStateDictionary"/> validation errors
/// into a <see cref="ValidationException"/> so the global exception handler emits a uniform problem-details body.
/// </summary>
public sealed class ValidationActionFilter : IActionFilter
{
    /// <inheritdoc/>
    public void OnActionExecuting(ActionExecutingContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        if (context.ModelState.IsValid)
        {
            return;
        }

        var errors = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase);
        foreach (var entry in context.ModelState)
        {
            if (entry.Value is null || entry.Value.Errors.Count == 0)
            {
                continue;
            }

            var messages = entry.Value.Errors
                .Select(e => string.IsNullOrWhiteSpace(e.ErrorMessage) ? "Invalid value." : e.ErrorMessage)
                .ToArray();

            errors[entry.Key] = messages;
        }

        throw new ValidationException(errors);
    }

    /// <inheritdoc/>
    public void OnActionExecuted(ActionExecutedContext context)
    {
    }
}
