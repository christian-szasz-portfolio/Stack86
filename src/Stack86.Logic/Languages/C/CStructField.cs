namespace Stack86.Logic.Languages.C;

/// <summary>
/// A single field within a <see cref="CStructTypeInfo"/> or <see cref="CUnionTypeInfo"/>.
/// </summary>
/// <param name="name">The field name.</param>
/// <param name="type">The field type.</param>
public sealed class CStructField(string name, CTypeInfo type)
{
    /// <summary>Gets the field name.</summary>
    public string Name { get; } = name;

    /// <summary>Gets the field type.</summary>
    public CTypeInfo Type { get; } = type;
}
