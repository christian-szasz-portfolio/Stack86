namespace Stack86.Logic.Languages.C;

/// <summary>
/// A single member of a <see cref="CEnumTypeInfo"/>.
/// </summary>
/// <param name="name">The member name.</param>
/// <param name="value">The integer value.</param>
public sealed class CEnumMember(string name, int value)
{
    /// <summary>Gets the member name.</summary>
    public string Name { get; } = name;

    /// <summary>Gets the integer value.</summary>
    public int Value { get; } = value;
}
