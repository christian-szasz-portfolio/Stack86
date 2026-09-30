namespace Stack86.Logic.Languages.Cpp.Ast;

/// <summary>
/// Access modifier for class members.
/// </summary>
public enum CppAccessModifier
{
    /// <summary>Public access.</summary>
    Public,

    /// <summary>Private access (default for class).</summary>
    Private,

    /// <summary>Protected access.</summary>
    Protected,
}
