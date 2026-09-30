namespace Stack86.Logic.Test.Languages;

using Stack86.Logic.Languages.C;
using Stack86.Logic.Languages.CSharp;
using Stack86.Logic.Languages.JavaScript;
using Stack86.Logic.Languages.TypeScript;
using Stack86.Logic.Pipeline.X86Conversion;

[TestClass]
public sealed class CapabilityValidatorTests
{
    private static readonly X8086CapabilityProfile Profile = new();

    [TestMethod]
    public void CSharpValidator_FlagsDecimal()
    {
        var v = new CSharpCapabilityValidator(Profile);
        Assert.IsGreaterThanOrEqualTo(1, v.Validate("decimal d = 1m;").Count);
    }

    [TestMethod]
    public void JsValidator_FlagsBigInt()
    {
        var v = new JsCapabilityValidator(Profile);
        Assert.IsGreaterThanOrEqualTo(1, v.Validate("const x = BigInt(1);").Count);
    }

    [TestMethod]
    public void TsValidator_FlagsBigInt()
    {
        var v = new TsCapabilityValidator(Profile);
        Assert.IsGreaterThanOrEqualTo(1, v.Validate("const x = BigInt(1);").Count);
    }

    [TestMethod]
    public void CValidator_AcceptsFunctionPointerDeclarator()
    {
        var v = new CCapabilityValidator(Profile);
        Assert.HasCount(0, v.Validate("int (*fp)(int, int);"));
    }

    [TestMethod]
    public void CValidator_AcceptsPlainFunctionPrototype()
    {
        var v = new CCapabilityValidator(Profile);
        Assert.HasCount(0, v.Validate("int add(int, int);"));
    }

    [TestMethod]
    public void CValidator_FlagsC11TypeKeywords()
    {
        var v = new CCapabilityValidator(Profile);
        Assert.IsGreaterThanOrEqualTo(1, v.Validate("_Bool b = 1;").Count);
        Assert.IsGreaterThanOrEqualTo(1, v.Validate("_Complex c;").Count);
        Assert.IsGreaterThanOrEqualTo(1, v.Validate("_Atomic int a;").Count);
    }

    [TestMethod]
    public void CValidator_IgnoresKeywordsInMultiLineBlockComment()
    {
        var v = new CCapabilityValidator(Profile);
        var source = "/* this comment mentions\n float and double and const\n across lines */\nint main() { return 0; }";
        Assert.HasCount(0, v.Validate(source));
    }

    [TestMethod]
    public void CValidator_FlagsKeywordAfterBlockCommentCloses()
    {
        var v = new CCapabilityValidator(Profile);
        var source = "/* comment\n spanning */ float x;";
        Assert.IsGreaterThanOrEqualTo(1, v.Validate(source).Count);
    }
}
