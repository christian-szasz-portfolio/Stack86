namespace Stack86.Logic.Pipeline.Ir;

/// <summary>
/// Validates an <see cref="IrProgram"/> against the target architecture capabilities.
/// Runs after IR generation and before code generation as a safety net to catch
/// any unsupported constructs that slipped past the source-level validators.
/// </summary>
public interface IIrValidator
{
    /// <summary>
    /// Validates the given IR program and returns diagnostics for any violations.
    /// </summary>
    /// <param name="program">The IR program to validate.</param>
    /// <returns>Diagnostics for any target-capability violations; empty if the program is valid.</returns>
    IReadOnlyList<IrDiagnostic> Validate(IrProgram program);
}
