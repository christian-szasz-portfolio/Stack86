namespace Stack86.Logic.Pipeline.Ast;

/// <summary>
/// A continue statement that jumps to the next iteration of the nearest enclosing loop.
/// </summary>
public sealed record ContinueStatement : StatementNode;
