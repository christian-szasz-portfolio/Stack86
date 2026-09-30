namespace Stack86.Logic.Languages.C;

/// <summary>
/// A function pointer type: <c>int (*)(int, int)</c>.
/// </summary>
/// <param name="returnType">The return type.</param>
/// <param name="parameterTypes">The parameter types.</param>
public sealed class CFunctionPointerTypeInfo(CTypeInfo returnType, IReadOnlyList<CTypeInfo> parameterTypes) : CTypeInfo
{
    /// <summary>Gets the return type of the pointed-to function.</summary>
    public CTypeInfo ReturnType { get; } = returnType;

    /// <summary>Gets the parameter types of the pointed-to function.</summary>
    public IReadOnlyList<CTypeInfo> ParameterTypes { get; } = parameterTypes;

    /// <inheritdoc />
    public override int SizeInBytes => 2; // near function pointer
}
