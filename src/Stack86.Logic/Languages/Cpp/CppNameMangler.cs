namespace Stack86.Logic.Languages.Cpp;

/// <summary>
/// Mangles C++ names to produce unique C identifiers for methods, overloads, and namespaces.
/// </summary>
public static class CppNameMangler
{
    /// <summary>
    /// Mangles a class method name: <c>ClassName_methodName</c>.
    /// </summary>
    public static string MangleMethod(string className, string methodName)
    {
        return $"{className}_{methodName}";
    }

    /// <summary>
    /// Mangles a constructor: <c>ClassName_init</c>.
    /// </summary>
    public static string MangleConstructor(string className)
    {
        return $"{className}_init";
    }

    /// <summary>
    /// Mangles a destructor: <c>ClassName_destroy</c>.
    /// </summary>
    public static string MangleDestructor(string className)
    {
        return $"{className}_destroy";
    }

    /// <summary>
    /// Mangles a namespaced name: <c>namespace_name</c>.
    /// </summary>
    public static string MangleNamespaced(string namespaceName, string name)
    {
        return $"{namespaceName}_{name}";
    }

    /// <summary>
    /// Produces a type abbreviation for overload mangling.
    /// </summary>
    public static string TypeAbbreviation(string typeName)
    {
        return typeName switch
        {
            "int" => "i",
            "char" => "c",
            "void" => "v",
            "bool" => "b",
            "short" => "s",
            "long" => "l",
            _ => typeName.Length > 0 ? typeName[..1].ToLowerInvariant() : "x",
        };
    }

    /// <summary>
    /// Mangles an overloaded function: <c>funcName_ic</c> (for int, char params).
    /// </summary>
    public static string MangleOverload(string funcName, IReadOnlyList<string> paramTypes)
    {
        if (paramTypes.Count == 0)
        {
            return funcName;
        }

        var suffix = string.Concat(paramTypes.Select(TypeAbbreviation));
        return $"{funcName}_{suffix}";
    }

    /// <summary>
    /// Mangles a template instantiation: <c>funcName_int</c> (for template&lt;T&gt; with T=int).
    /// </summary>
    public static string MangleTemplate(string funcName, IReadOnlyList<string> typeArgs)
    {
        if (typeArgs.Count == 0)
        {
            return funcName;
        }

        var suffix = string.Join("_", typeArgs);
        return $"{funcName}_{suffix}";
    }
}
