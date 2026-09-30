namespace Stack86.Logic.Pipeline.Ast;

/// <summary>
/// A function declaration with return type, name, parameters and body.
/// When <see cref="Body"/> is <c>null</c> the node represents a forward declaration (prototype).
/// </summary>
public sealed record FunctionDeclaration : DeclarationNode
{
    public required TypeNode ReturnType { get; init; }

    public required string Name { get; init; }

    public required IReadOnlyList<ParameterDeclaration> Parameters { get; init; }

    public required BlockStatement? Body { get; init; }
}
