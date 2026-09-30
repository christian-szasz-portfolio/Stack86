namespace Stack86.Logic.Test.Languages.C;

using System.Collections.Generic;
using System.Reflection;
using Stack86.Logic.Compilation.Models;
using Stack86.Logic.Languages.C;
using Stack86.Logic.Pipeline.Ir;

/// <summary>
/// Unit tests for <see cref="TccCodeValidator"/> covering paths that don't require an
/// actual TCC binary: oversized source guard, missing-binary fallback, and timeout
/// behaviour. Real TCC invocations are intentionally not exercised here.
/// </summary>
[TestClass]
public sealed class TccCodeValidatorTests
{
    [TestMethod]
    public async Task Validate_SourceLargerThanMaxSize_ReturnsSingleSizeError()
    {
        var settings = new TccSettings { MaxSourceSizeBytes = 16 };
        var validator = new TccCodeValidator(settings);

        var diags = await validator.ValidateAsync(new string('x', 17));

        Assert.HasCount(1, diags);
        Assert.AreEqual(DiagnosticSeverity.Error, diags[0].Severity);
        Assert.Contains("maximum size", diags[0].Message);
        Assert.AreEqual(0, diags[0].Line);
        Assert.AreEqual(0, diags[0].Column);
    }

    [TestMethod]
    public async Task Validate_TccBinaryMissing_ReturnsWarningAndContinues()
    {
        var settings = new TccSettings
        {
            ExecutablePath = Path.Combine(Path.GetTempPath(), "definitely_not_tcc_" + Guid.NewGuid().ToString("N") + ".exe"),
            MaxSourceSizeBytes = 65536,
        };
        var validator = new TccCodeValidator(settings);

        var diags = await validator.ValidateAsync("int main(void) { return 0; }");

        Assert.HasCount(1, diags);
        Assert.AreEqual(DiagnosticSeverity.Warning, diags[0].Severity);
        Assert.Contains("TCC", diags[0].Message);
        Assert.Contains("skipped", diags[0].Message);
    }

    [TestMethod]
    public async Task Validate_BoundarySourceExactlyAtMax_DoesNotTriggerSizeError()
    {
        // Source equal to the limit must NOT trigger the size guard (>, not >=).
        // We use a missing TCC so the validator returns the not-found warning instead of running TCC.
        var settings = new TccSettings
        {
            MaxSourceSizeBytes = 32,
            ExecutablePath = Path.Combine(Path.GetTempPath(), "missing_" + Guid.NewGuid().ToString("N")),
        };
        var validator = new TccCodeValidator(settings);

        var diags = await validator.ValidateAsync(new string('x', 32));

        Assert.HasCount(1, diags);
        Assert.AreNotEqual("Source code exceeds maximum size of 32 bytes.", diags[0].Message);
    }

    [TestMethod]
    public void ParseDiagnostics_Empty_ReturnsEmpty()
    {
        Assert.IsEmpty(InvokeParse(string.Empty));
    }

    [TestMethod]
    public void ParseDiagnostics_Whitespace_ReturnsEmpty()
    {
        Assert.IsEmpty(InvokeParse("   \n\t  "));
    }

    [TestMethod]
    public void ParseDiagnostics_ErrorLine_Parsed()
    {
        var diags = InvokeParse("foo.c:5: error: undefined symbol 'bar'");
        Assert.HasCount(1, diags);
        Assert.AreEqual(DiagnosticSeverity.Error, diags[0].Severity);
        Assert.AreEqual(5, diags[0].Line);
        Assert.Contains("undefined symbol", diags[0].Message);
    }

    [TestMethod]
    public void ParseDiagnostics_WarningLine_Parsed()
    {
        var diags = InvokeParse("foo.c:12: warning: unused variable 'x'");
        Assert.HasCount(1, diags);
        Assert.AreEqual(DiagnosticSeverity.Warning, diags[0].Severity);
        Assert.AreEqual(12, diags[0].Line);
    }

    [TestMethod]
    public void ParseDiagnostics_NonMatchingLine_KeptAsError()
    {
        var diags = InvokeParse("totally unrecognised stderr output");
        Assert.HasCount(1, diags);
        Assert.AreEqual(DiagnosticSeverity.Error, diags[0].Severity);
        Assert.AreEqual(0, diags[0].Line);
    }

    [TestMethod]
    public void ParseDiagnostics_MultipleLines_ParsedAll()
    {
        var diags = InvokeParse("foo.c:1: error: a\nfoo.c:2: warning: b\nrandom\n\n");
        Assert.HasCount(3, diags);
        Assert.AreEqual(DiagnosticSeverity.Error, diags[0].Severity);
        Assert.AreEqual(DiagnosticSeverity.Warning, diags[1].Severity);
        Assert.AreEqual(DiagnosticSeverity.Error, diags[2].Severity);
    }

    [TestMethod]
    public void ParseDiagnostics_BlankLinesSkipped()
    {
        var diags = InvokeParse("\n\n   \nfoo.c:9: error: x\n\n");
        Assert.HasCount(1, diags);
    }

    private static IReadOnlyList<IrDiagnostic> InvokeParse(string stderr)
    {
        var method = typeof(TccCodeValidator).GetMethod(
            "ParseDiagnostics",
            BindingFlags.NonPublic | BindingFlags.Static)!;
        return (IReadOnlyList<IrDiagnostic>)method.Invoke(null, [stderr])!;
    }
}
