namespace Stack86.Logic.Languages.Cpp.Ast;

/// <summary>
/// A block of statements enclosed in braces.
/// </summary>
public sealed record CppBlockStatement : CppStatementNode
{
    /// <summary>Statements within the block.</summary>
    public required IReadOnlyList<CppStatementNode> Statements { get; init; }
}
