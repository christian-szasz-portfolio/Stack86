namespace Stack86.Logic.Languages.C;

/// <summary>
/// A single token produced by the C lexer.
/// </summary>
public sealed record CToken
{
    public required CTokenKind Kind { get; init; }

    public required string Text { get; init; }

    public required int Line { get; init; }

    public required int Column { get; init; }

    /// <summary>Integer value for <see cref="CTokenKind.IntegerLiteral"/>.</summary>
    public int IntValue { get; init; }

    /// <summary>Char value for <see cref="CTokenKind.CharLiteral"/>.</summary>
    public char CharValue { get; init; }

    /// <summary>String value for <see cref="CTokenKind.StringLiteral"/> (unescaped).</summary>
    public string? StringValue { get; init; }
}
