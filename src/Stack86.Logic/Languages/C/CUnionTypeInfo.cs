namespace Stack86.Logic.Languages.C;

/// <summary>
/// A union type — all fields overlap at offset 0, size = max field size.
/// </summary>
/// <param name="name">The union tag name.</param>
/// <param name="fields">The ordered list of fields.</param>
public sealed class CUnionTypeInfo(string name, List<CStructField> fields) : CTypeInfo
{
    /// <summary>Gets the union tag name.</summary>
    public string Name { get; } = name;

    /// <summary>Gets the fields (all at offset 0).</summary>
    public IReadOnlyList<CStructField> Fields => fields;

    /// <inheritdoc />
    public override int SizeInBytes
    {
        get
        {
            var max = 0;
            foreach (var f in fields)
            {
                if (f.Type.SizeInBytes > max)
                {
                    max = f.Type.SizeInBytes;
                }
            }

            return max;
        }
    }

    /// <inheritdoc />
    public override bool IsScalar => false;

    /// <summary>
    /// Finds a field by name. All union fields are at offset 0.
    /// </summary>
    /// <param name="fieldName">The name of the field.</param>
    /// <param name="type">The type of the field.</param>
    /// <returns><see langword="true"/> if the field was found.</returns>
    public bool TryGetField(string fieldName, out CTypeInfo type)
    {
        foreach (var f in fields)
        {
            if (f.Name == fieldName)
            {
                type = f.Type;
                return true;
            }
        }

        type = CPrimitiveTypeInfo.Void;
        return false;
    }
}
