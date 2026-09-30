namespace Stack86.Logic.Languages.C;

/// <summary>
/// An enum type — always equivalent to <c>int</c> (2 bytes).
/// </summary>
/// <param name="name">The enum tag name.</param>
/// <param name="members">The named constant members with their integer values.</param>
public sealed class CEnumTypeInfo(string name, IReadOnlyList<CEnumMember> members) : CTypeInfo
{
    /// <summary>Gets the enum tag name.</summary>
    public string Name { get; } = name;

    /// <summary>Gets the named constant members.</summary>
    public IReadOnlyList<CEnumMember> Members { get; } = members;

    /// <inheritdoc />
    public override int SizeInBytes => 2;

    /// <summary>
    /// Looks up a member value by name.
    /// </summary>
    /// <param name="memberName">The member name.</param>
    /// <param name="value">The integer value if found.</param>
    /// <returns><see langword="true"/> if the member was found.</returns>
    public bool TryGetValue(string memberName, out int value)
    {
        foreach (var member in this.Members)
        {
            if (member.Name == memberName)
            {
                value = member.Value;
                return true;
            }
        }

        value = 0;
        return false;
    }
}
