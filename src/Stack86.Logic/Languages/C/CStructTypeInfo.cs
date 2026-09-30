namespace Stack86.Logic.Languages.C;

/// <summary>
/// A struct type with named fields and computed layout.
/// </summary>
/// <param name="name">The struct tag name.</param>
/// <param name="fields">The ordered list of fields.</param>
public sealed class CStructTypeInfo(string name, List<CStructField> fields) : CTypeInfo
{
    /// <summary>Gets the struct tag name.</summary>
    public string Name { get; } = name;

    /// <summary>Gets the ordered list of fields with their offsets.</summary>
    public IReadOnlyList<CStructField> Fields => fields;

    /// <inheritdoc />
    public override int SizeInBytes => this.ComputeSize();

    /// <inheritdoc />
    public override bool IsScalar => false;

    /// <summary>
    /// Finds a field by name and returns its offset within the struct.
    /// </summary>
    /// <param name="fieldName">The name of the field.</param>
    /// <param name="offset">The byte offset from the struct base.</param>
    /// <param name="type">The type of the field.</param>
    /// <returns><see langword="true"/> if the field was found.</returns>
    public bool TryGetField(string fieldName, out int offset, out CTypeInfo type)
    {
        var currentOffset = 0;
        foreach (var f in fields)
        {
            if (f.Name == fieldName)
            {
                offset = currentOffset;
                type = f.Type;
                return true;
            }

            currentOffset += f.Type.SizeInBytes;
        }

        offset = 0;
        type = CPrimitiveTypeInfo.Void;
        return false;
    }

    /// <summary>
    /// Replaces the field list in-place so that existing <see cref="CPointerTypeInfo"/>
    /// references that point to this struct remain valid after re-registration.
    /// </summary>
    /// <param name="newFields">The resolved fields to set.</param>
    internal void SetFields(List<CStructField> newFields)
    {
        fields.Clear();
        fields.AddRange(newFields);
    }

    private int ComputeSize()
    {
        var total = 0;
        foreach (var f in fields)
        {
            total += f.Type.SizeInBytes;
        }

        return total;
    }
}
