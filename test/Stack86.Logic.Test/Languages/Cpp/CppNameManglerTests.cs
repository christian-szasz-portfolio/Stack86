namespace Stack86.Logic.Test.Languages.Cpp;

using Stack86.Logic.Languages.Cpp;

/// <summary>
/// Direct unit tests for <see cref="CppNameMangler"/>.
/// </summary>
[TestClass]
public sealed class CppNameManglerTests
{
    [TestMethod]
    public void MangleMethod_combines_class_and_method()
    {
        Assert.AreEqual("Box_get", CppNameMangler.MangleMethod("Box", "get"));
    }

    [TestMethod]
    public void MangleConstructor_appends_init()
    {
        Assert.AreEqual("Pt_init", CppNameMangler.MangleConstructor("Pt"));
    }

    [TestMethod]
    public void MangleDestructor_appends_destroy()
    {
        Assert.AreEqual("Pt_destroy", CppNameMangler.MangleDestructor("Pt"));
    }

    [TestMethod]
    public void MangleNamespaced_combines_namespace_and_name()
    {
        Assert.AreEqual("ns_func", CppNameMangler.MangleNamespaced("ns", "func"));
    }

    [TestMethod]
    [DataRow("int", "i")]
    [DataRow("char", "c")]
    [DataRow("void", "v")]
    [DataRow("bool", "b")]
    [DataRow("short", "s")]
    [DataRow("long", "l")]
    [DataRow("MyType", "m")]
    [DataRow("", "x")]
    public void TypeAbbreviation_maps_known_and_unknown(string typeName, string expected)
    {
        Assert.AreEqual(expected, CppNameMangler.TypeAbbreviation(typeName));
    }

    [TestMethod]
    public void MangleOverload_with_no_params_returns_funcName()
    {
        Assert.AreEqual("foo", CppNameMangler.MangleOverload("foo", []));
    }

    [TestMethod]
    public void MangleOverload_concatenates_abbreviations()
    {
        Assert.AreEqual("f_ic", CppNameMangler.MangleOverload("f", ["int", "char"]));
    }

    [TestMethod]
    public void MangleOverload_three_params()
    {
        Assert.AreEqual("g_ibv", CppNameMangler.MangleOverload("g", ["int", "bool", "void"]));
    }

    [TestMethod]
    public void MangleTemplate_with_no_args_returns_funcName()
    {
        Assert.AreEqual("foo", CppNameMangler.MangleTemplate("foo", []));
    }

    [TestMethod]
    public void MangleTemplate_joins_args_with_underscore()
    {
        Assert.AreEqual("max_int_int", CppNameMangler.MangleTemplate("max", ["int", "int"]));
    }
}
