namespace Stack86.Logic.Pipeline.CodeGen;

using Stack86.Logic.Pipeline.Ir;

/// <summary>
/// Contract for a backend that transforms an <see cref="IrProgram"/> into assembly text.
/// </summary>
public interface ICodeGenerator
{
    /// <summary>
    /// Generates target assembly from the given IR program.
    /// </summary>
    /// <exception cref="Stack86.Common.Exceptions.CompilationFailedException">Thrown when generation fails.</exception>
    string Generate(IrProgram program);
}
