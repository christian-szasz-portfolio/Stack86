namespace Stack86.Logic.Test.Languages.TypeScript;

using System.Reflection;
using Stack86.Common.Exceptions;
using Stack86.Logic.Languages.TypeScript;
using Stack86.Logic.Pipeline.Ir;

/// <summary>
/// Direct unit tests for <see cref="NodeTypeScriptTranspiler"/> covering
/// the oversize-source path, missing-Node path, and diagnostic parsing.
/// The DI graph stubs out this class for E2E tests, so it has 0% coverage
/// without these dedicated unit tests.
/// </summary>
[TestClass]
public sealed class NodeTypeScriptTranspilerTests
{
    [TestMethod]
    public async Task Transpile_OversizeSource_Throws()
    {
        var settings = new TsTranspilerSettings { MaxSourceSizeBytes = 16 };
        var sut = new NodeTypeScriptTranspiler(settings);

        var ex = await Assert.ThrowsExactlyAsync<ExternalToolException>(
            () => sut.TranspileAsync(new string('x', 100)));

        Assert.IsTrue(ex.Message.Contains("exceeds maximum size", StringComparison.Ordinal));
    }

    [TestMethod]
    public async Task Transpile_NodeMissing_Throws()
    {
        var settings = new TsTranspilerSettings
        {
            NodePath = Path.Combine(Path.GetTempPath(), $"missing_node_{Guid.NewGuid():N}.exe"),
        };
        var sut = new NodeTypeScriptTranspiler(settings);

        var ex = await Assert.ThrowsExactlyAsync<ExternalToolException>(
            () => sut.TranspileAsync("let x = 1;"));

        Assert.IsTrue(ex.Message.Contains("Node.js", StringComparison.Ordinal));
    }

    [TestMethod]
    public void ParseDiagnostics_EmptyInput_ReturnsEmpty()
    {
        var diagnostics = InvokeParseDiagnostics(string.Empty);

        Assert.IsEmpty(diagnostics);
    }

    [TestMethod]
    public void ParseDiagnostics_WhitespaceInput_ReturnsEmpty()
    {
        var diagnostics = InvokeParseDiagnostics("   \n  \n");

        Assert.IsEmpty(diagnostics);
    }

    [TestMethod]
    public void ParseDiagnostics_ParsesErrorLine()
    {
        var diagnostics = InvokeParseDiagnostics("file.ts:42: error: Unexpected token");

        Assert.HasCount(1, diagnostics);
        Assert.AreEqual(DiagnosticSeverity.Error, diagnostics[0].Severity);
        Assert.AreEqual(42, diagnostics[0].Line);
        Assert.AreEqual(1, diagnostics[0].Column);
        Assert.AreEqual("Unexpected token", diagnostics[0].Message);
    }

    [TestMethod]
    public void ParseDiagnostics_ParsesWarningLine()
    {
        var diagnostics = InvokeParseDiagnostics("file.ts:7: warning: Unused variable");

        Assert.HasCount(1, diagnostics);
        Assert.AreEqual(DiagnosticSeverity.Warning, diagnostics[0].Severity);
        Assert.AreEqual(7, diagnostics[0].Line);
    }

    [TestMethod]
    public void ParseDiagnostics_NonMatchingLine_RecordsAsError()
    {
        var diagnostics = InvokeParseDiagnostics("Random failure occurred");

        Assert.HasCount(1, diagnostics);
        Assert.AreEqual(DiagnosticSeverity.Error, diagnostics[0].Severity);
        Assert.AreEqual("Random failure occurred", diagnostics[0].Message);
        Assert.AreEqual(0, diagnostics[0].Line);
    }

    [TestMethod]
    public void ParseDiagnostics_MultipleLines_ParsesEach()
    {
        var input = "file.ts:1: error: Bad syntax\nfile.ts:5: warning: Hint\nGeneric failure";
        var diagnostics = InvokeParseDiagnostics(input);

        Assert.HasCount(3, diagnostics);
        Assert.AreEqual(DiagnosticSeverity.Error, diagnostics[0].Severity);
        Assert.AreEqual(DiagnosticSeverity.Warning, diagnostics[1].Severity);
        Assert.AreEqual(DiagnosticSeverity.Error, diagnostics[2].Severity);
    }

    [TestMethod]
    public void ParseDiagnostics_SkipsBlankLines()
    {
        var input = "\n\nfile.ts:1: error: x\n\n";
        var diagnostics = InvokeParseDiagnostics(input);

        Assert.HasCount(1, diagnostics);
    }

    [TestMethod]
    public void ParseDiagnostics_WhitespaceLineBetweenEntries_IsSkipped()
    {
        // Force the loop to encounter a whitespace-only line whose trimmed length is 0
        // (cannot be eliminated by the IsNullOrWhiteSpace early return because real
        // entries are present).
        var diagnostics = InvokeParseDiagnostics("file.ts:1: error: x\n   \nfile.ts:2: error: y");

        Assert.HasCount(2, diagnostics);
    }

    [TestMethod]
    public void ParseErrorMessage_Empty_ReturnsUnknown()
    {
        Assert.AreEqual("Unknown error", InvokeParseErrorMessage(string.Empty));
    }

    [TestMethod]
    public void ParseErrorMessage_Whitespace_ReturnsUnknown()
    {
        Assert.AreEqual("Unknown error", InvokeParseErrorMessage("   \n   "));
    }

    [TestMethod]
    public void ParseErrorMessage_FirstLine_ReturnsTrimmed()
    {
        Assert.AreEqual("first failure", InvokeParseErrorMessage("  first failure  \nsecond line"));
    }

    private static string InvokeParseErrorMessage(string stderr)
    {
        var method = typeof(NodeTypeScriptTranspiler).GetMethod(
            "ParseErrorMessage",
            BindingFlags.NonPublic | BindingFlags.Static);
        Assert.IsNotNull(method);
        var result = method.Invoke(null, new object[] { stderr });
        Assert.IsNotNull(result);
        return (string)result;
    }

    private static IReadOnlyList<IrDiagnostic> InvokeParseDiagnostics(string stderr)
    {
        var method = typeof(NodeTypeScriptTranspiler).GetMethod(
            "ParseDiagnostics",
            BindingFlags.NonPublic | BindingFlags.Static);
        Assert.IsNotNull(method);
        var result = method.Invoke(null, new object[] { stderr });
        Assert.IsNotNull(result);
        return (IReadOnlyList<IrDiagnostic>)result;
    }
}
