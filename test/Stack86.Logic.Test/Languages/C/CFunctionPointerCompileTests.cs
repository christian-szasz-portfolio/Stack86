namespace Stack86.Logic.Test.Languages.C;

using Stack86.Logic.Test.Compilation;

/// <summary>
/// End-to-end compile coverage for C function pointers: declaring a function-pointer
/// variable, assigning a function's address to it, and invoking the function through the
/// pointer. The generated assembly is expected to load the function label into a register
/// and call it indirectly via <c>CALL AX</c>.
/// </summary>
[TestClass]
public sealed class CFunctionPointerCompileTests
{
    private const string AddViaPointer =
        "#include <stdio.h>\n" +
        "int add(int a, int b) { return a + b; }\n" +
        "int main() { int (*fp)(int, int) = add; printf(\"%d\", fp(2, 3)); return 0; }";

    private const string NoArgViaPointer =
        "#include <stdio.h>\n" +
        "int answer() { return 42; }\n" +
        "int main() { int (*fp)() = answer; printf(\"%d\", fp()); return 0; }";

    [TestMethod]
    public async Task FunctionPointer_CompilesAndCallsIndirectly()
    {
        var result = await CompileFixture.CompileC(AddViaPointer);

        Assert.IsTrue(result.IsSuccess, result.IsFailure ? result.Error : string.Empty);
        var asm = result.Value.Assembly!;
        Assert.IsTrue(asm.Contains("MOV AX, add", StringComparison.Ordinal), "expected the function address to be loaded");
        Assert.IsTrue(asm.Contains("CALL AX", StringComparison.Ordinal), "expected an indirect call through the pointer");
    }

    [TestMethod]
    public async Task FunctionPointer_NoArgsCompiles()
    {
        var result = await CompileFixture.CompileC(NoArgViaPointer);

        Assert.IsTrue(result.IsSuccess, result.IsFailure ? result.Error : string.Empty);
        var asm = result.Value.Assembly!;
        Assert.IsTrue(asm.Contains("MOV AX, answer", StringComparison.Ordinal), "expected the function address to be loaded");
        Assert.IsTrue(asm.Contains("CALL AX", StringComparison.Ordinal), "expected an indirect call through the pointer");
    }
}
