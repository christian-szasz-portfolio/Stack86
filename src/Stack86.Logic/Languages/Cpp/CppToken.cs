namespace Stack86.Logic.Languages.Cpp;

/// <summary>
/// A single token produced by the C++ lexer.
/// </summary>
public sealed record CppToken
{
    /// <summary>The kind of this token.</summary>
    public required CppTokenKind Kind { get; init; }

    /// <summary>The source text of this token.</summary>
    public required string Text { get; init; }

    /// <summary>1-based line number where this token starts.</summary>
    public required int Line { get; init; }

    /// <summary>1-based column number where this token starts.</summary>
    public required int Column { get; init; }

    /// <summary>Integer value for <see cref="CppTokenKind.IntegerLiteral"/>.</summary>
    public int IntValue { get; init; }

    /// <summary>Char value for <see cref="CppTokenKind.CharLiteral"/>.</summary>
    public char CharValue { get; init; }

    /// <summary>String value for <see cref="CppTokenKind.StringLiteral"/> (unescaped).</summary>
    public string? StringValue { get; init; }
}
