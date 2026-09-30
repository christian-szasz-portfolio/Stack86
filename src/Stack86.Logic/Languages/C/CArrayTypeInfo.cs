namespace Stack86.Logic.Languages.C;

/// <summary>
/// A fixed-size array type: <c>T[N]</c>.
/// </summary>
/// <param name="element">The element type.</param>
/// <param name="length">The number of elements.</param>
public sealed class CArrayTypeInfo(CTypeInfo element, int length) : CTypeInfo
{
    /// <summary>Gets the element type.</summary>
    public CTypeInfo Element { get; } = element;

    /// <summary>Gets the number of elements in the array.</summary>
    public int Length { get; } = length;

    /// <inheritdoc />
    public override int SizeInBytes => this.Element.SizeInBytes * this.Length;

    /// <inheritdoc />
    public override bool IsScalar => false;
}
