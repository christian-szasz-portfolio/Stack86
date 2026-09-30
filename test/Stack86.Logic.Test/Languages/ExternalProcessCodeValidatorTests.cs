namespace Stack86.Logic.Test.Languages;

using System.Collections.Generic;
using System.Reflection;
using Stack86.Logic.Compilation.Models;
using Stack86.Logic.Languages;
using Stack86.Logic.Languages.JavaScript;
using Stack86.Logic.Pipeline.Ir;

/// <summary>
/// Unit tests for the JavaScript <see cref="ExternalProcessCodeValidator"/> implementation
/// covering the paths that do not require the real toolchain: the oversized-source guard, the
/// graceful missing-binary fallback, and diagnostic parsing. Real <c>node</c> invocations are
/// intentionally not exercised here so the suite runs without node installed.
/// </summary>
[TestClass]
public sealed class ExternalProcessCodeValidatorTests
{
    [TestMethod]
    public async Task Validate_SourceLargerThanMaxSize_ReturnsSingleSizeError()
    {
        var validator = new JsExternalValidator(new NodeCheckSettings { MaxSourceSizeBytes = 16 });

        var diags = await validator.ValidateAsync(new string('x', 17));

        Assert.HasCount(1, diags);
        Assert.AreEqual(DiagnosticSeverity.Error, diags[0].Severity);
        Assert.Contains("maximum size", diags[0].Message);
    }

    [TestMethod]
    public async Task Validate_BoundarySourceExactlyAtMax_DoesNotTriggerSizeError()
    {
        var validator = new JsExternalValidator(new NodeCheckSettings
        {
            MaxSourceSizeBytes = 32,
            ExecutablePath = MissingTool("node"),
        });

        var diags = await validator.ValidateAsync(new string('x', 32));

        Assert.HasCount(1, diags);
        Assert.AreNotEqual("Source code exceeds maximum size of 32 bytes.", diags[0].Message);
    }

    [TestMethod]
    public async Task Validate_NodeBinaryMissing_ReturnsWarning()
    {
        var validator = new JsExternalValidator(new NodeCheckSettings { ExecutablePath = MissingTool("node") });

        var diags = await validator.ValidateAsync("console.log('hi');");

        Assert.HasCount(1, diags);
        Assert.AreEqual(DiagnosticSeverity.Warning, diags[0].Severity);
        Assert.Contains("node", diags[0].Message);
    }

    [TestMethod]
    public void Language_MapsToExpectedValue()
    {
        Assert.AreEqual(SupportedLanguage.JavaScript, new JsExternalValidator(new NodeCheckSettings()).Language);
    }

    [TestMethod]
    public void ParseDiagnostics_ZeroExit_ReturnsEmpty()
    {
        var validator = new JsExternalValidator(new NodeCheckSettings());
        Assert.IsEmpty(InvokeParse(validator, string.Empty, string.Empty, 0));
    }

    [TestMethod]
    public void ParseDiagnostics_NonZeroExitNoOutput_ReturnsGenericError()
    {
        var validator = new JsExternalValidator(new NodeCheckSettings());
        var diags = InvokeParse(validator, string.Empty, string.Empty, 1);
        Assert.HasCount(1, diags);
        Assert.AreEqual(DiagnosticSeverity.Error, diags[0].Severity);
        Assert.Contains("node", diags[0].Message);
    }

    private static string MissingTool(string name) =>
        Path.Combine(Path.GetTempPath(), $"missing_{name}_{Guid.NewGuid():N}");

    private static IReadOnlyList<IrDiagnostic> InvokeParse(
        ExternalProcessCodeValidator validator,
        string stdout,
        string stderr,
        int exitCode)
    {
        var method = typeof(ExternalProcessCodeValidator).GetMethod(
            "ParseDiagnostics",
            BindingFlags.NonPublic | BindingFlags.Instance)!;
        return (IReadOnlyList<IrDiagnostic>)method.Invoke(validator, [stdout, stderr, exitCode])!;
    }
}
