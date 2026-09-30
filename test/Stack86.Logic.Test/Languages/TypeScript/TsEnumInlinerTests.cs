namespace Stack86.Logic.Test.Languages.TypeScript;

using Stack86.Logic.Languages.TypeScript;

/// <summary>
/// Direct unit tests for <see cref="TsEnumInliner"/>.
/// </summary>
[TestClass]
public sealed class TsEnumInlinerTests
{
    [TestMethod]
    public void InlineEnums_passthrough_when_no_enum_iife()
    {
        const string Js = "console.log(1);\n";
        Assert.AreEqual(Js, TsEnumInliner.InlineEnums(Js));
    }

    [TestMethod]
    public void InlineEnums_replaces_member_reference_with_value()
    {
        const string Js = "var Direction;\n(function (Direction) {\n    Direction[Direction[\"Up\"] = 0] = \"Up\";\n    Direction[Direction[\"Down\"] = 1] = \"Down\";\n})(Direction || (Direction = {}));\nconsole.log(Direction.Up);\nconsole.log(Direction.Down);\n";
        var result = TsEnumInliner.InlineEnums(Js);
        Assert.Contains("console.log(0)", result);
        Assert.Contains("console.log(1)", result);
        Assert.DoesNotContain("Direction.Up", result);
        Assert.DoesNotContain("Direction.Down", result);
        Assert.DoesNotContain("(function (Direction)", result);
    }

    [TestMethod]
    public void InlineEnums_handles_negative_values()
    {
        const string Js = "var E;\n(function (E) {\n    E[E[\"A\"] = -1] = \"A\";\n})(E || (E = {}));\nvar x = E.A;\n";
        var result = TsEnumInliner.InlineEnums(Js);
        Assert.Contains("var x = -1", result);
    }

    [TestMethod]
    public void InlineEnums_handles_multiple_enums()
    {
        const string Js =
            "var A;\n(function (A) {\n    A[A[\"X\"] = 0] = \"X\";\n})(A || (A = {}));\n" +
            "var B;\n(function (B) {\n    B[B[\"Y\"] = 7] = \"Y\";\n})(B || (B = {}));\n" +
            "console.log(A.X);\nconsole.log(B.Y);\n";
        var result = TsEnumInliner.InlineEnums(Js);
        Assert.Contains("console.log(0)", result);
        Assert.Contains("console.log(7)", result);
    }

    [TestMethod]
    public void InlineEnums_collapses_excess_blank_lines()
    {
        const string Js = "var E;\n(function (E) {\n    E[E[\"A\"] = 0] = \"A\";\n})(E || (E = {}));\n\n\n\nconsole.log(1);\n";
        var result = TsEnumInliner.InlineEnums(Js);
        Assert.DoesNotContain("\n\n\n", result);
    }
}
