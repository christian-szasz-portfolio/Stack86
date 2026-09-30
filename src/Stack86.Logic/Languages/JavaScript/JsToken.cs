namespace Stack86.Logic.Languages.JavaScript;

/// <summary>
/// A single token produced by the JavaScript lexer.
/// </summary>
public sealed record JsToken
{
    /// <summary>The kind of this token.</summary>
    public required JsTokenKind Kind { get; init; }

    /// <summary>The raw source text of the token.</summary>
    public required string Text { get; init; }

    /// <summary>1-based source line where the token begins.</summary>
    public required int Line { get; init; }

    /// <summary>1-based source column where the token begins.</summary>
    public required int Column { get; init; }

    /// <summary>Integer value for <see cref="JsTokenKind.IntegerLiteral"/>.</summary>
    public int IntValue { get; init; }

    /// <summary>String value for string and template literal tokens (unescaped).</summary>
    public string? StringValue { get; init; }
}
