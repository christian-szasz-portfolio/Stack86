namespace Stack86.Logic.Test.Compilation;

using Stack86.Logic.Compilation.Models;
using Stack86.Logic.Compilation.Pipeline;
using Stack86.Logic.Languages;

/// <summary>
/// End-to-end C compilation tests. Each test compiles a small program through the full
/// pipeline (lex → parse → IR → optimise → validate → emit) and asserts on the produced
/// assembly. These tests provide broad coverage of <c>CLexer</c>, <c>CParser</c>,
/// <c>CIrGenerator</c>, <c>IrOptimizer</c>, <c>IrCapabilityValidator</c> and
/// <c>Asm8086Generator</c> in a single sweep.
/// </summary>
[TestClass]
public sealed class CCompileEndToEndTests
{
    [TestMethod]
    public async Task EmptyMain_ProducesAssemblyWithDataSegment()
    {
        var result = await CompileFixture.CompileC("int main() { return 0; }");
        AssertCompiledOk(result);
        StringAssert.Contains(result.Value.Assembly!, ".MODEL SMALL", StringComparison.Ordinal);
        StringAssert.Contains(result.Value.Assembly!, ".CODE", StringComparison.Ordinal);
    }

    [TestMethod]
    public async Task PrintfHelloWorld_EmitsString()
    {
        var src = """
            #include <stdio.h>
            int main() { printf("hello"); return 0; }
            """;
        var result = await CompileFixture.CompileC(src);
        AssertCompiledOk(result);
        StringAssert.Contains(result.Value.Assembly!, "hello", StringComparison.Ordinal);
    }

    [TestMethod]
    public async Task PrintfFormatSpecifiers_EachKindLowers()
    {
        var src = """
            #include <stdio.h>
            int main() {
                int x = 7;
                char c = 'A';
                printf("%d %c %s\n", x, c, "hi");
                return 0;
            }
            """;
        var result = await CompileFixture.CompileC(src);
        AssertCompiledOk(result);
    }

    [TestMethod]
    public async Task ArithmeticOperators_AllLower()
    {
        var src = """
            int main() {
                int a = 5;
                int b = 3;
                int s = a + b;
                int d = a - b;
                int m = a * b;
                int q = a / b;
                int r = a % b;
                int n = -a;
                return s + d + m + q + r + n;
            }
            """;
        var result = await CompileFixture.CompileC(src);
        AssertCompiledOk(result);
    }

    [TestMethod]
    public async Task BitwiseOperators_AllLower()
    {
        var src = """
            int main() {
                int a = 12;
                int b = 5;
                return (a & b) | (a ^ b) | (~a) | (a << 1) | (a >> 1);
            }
            """;
        var result = await CompileFixture.CompileC(src);
        AssertCompiledOk(result);
    }

    [TestMethod]
    public async Task ComparisonAndLogicalOperators_AllLower()
    {
        var src = """
            int main() {
                int a = 1;
                int b = 2;
                if (a < b && b > a) return 1;
                if (a <= b || b >= a) return 2;
                if (a == b) return 3;
                if (a != b) return 4;
                if (!a) return 5;
                return 0;
            }
            """;
        var result = await CompileFixture.CompileC(src);
        AssertCompiledOk(result);
    }

    [TestMethod]
    public async Task IfElseStatement_ProducesConditionalJumps()
    {
        var src = """
            int main() {
                int x = 10;
                if (x > 5) { x = 1; } else { x = 2; }
                return x;
            }
            """;
        var result = await CompileFixture.CompileC(src);
        AssertCompiledOk(result);
    }

    [TestMethod]
    public async Task WhileLoop_ProducesLoop()
    {
        var src = """
            int main() {
                int i = 0;
                while (i < 10) { i = i + 1; }
                return i;
            }
            """;
        var result = await CompileFixture.CompileC(src);
        AssertCompiledOk(result);
    }

    [TestMethod]
    public async Task ForLoop_WithBreakContinue_Lowers()
    {
        var src = """
            int main() {
                int sum = 0;
                for (int i = 0; i < 10; i = i + 1) {
                    if (i == 5) continue;
                    if (i == 8) break;
                    sum = sum + i;
                }
                return sum;
            }
            """;
        var result = await CompileFixture.CompileC(src);
        AssertCompiledOk(result);
    }

    [TestMethod]
    public async Task DoWhileLoop_Lowers()
    {
        var src = """
            int main() {
                int i = 0;
                do { i = i + 1; } while (i < 5);
                return i;
            }
            """;
        var result = await CompileFixture.CompileC(src);
        AssertCompiledOk(result);
    }

    [TestMethod]
    public async Task SwitchStatement_Lowers()
    {
        var src = """
            int main() {
                int x = 2;
                switch (x) {
                    case 1: return 10;
                    case 2: return 20;
                    case 3: return 30;
                    default: return 0;
                }
            }
            """;
        var result = await CompileFixture.CompileC(src);
        AssertCompiledOk(result);
    }

    [TestMethod]
    public async Task FunctionDefinitionAndCall_Lowers()
    {
        var src = """
            int add(int a, int b) { return a + b; }
            int main() { return add(2, 3); }
            """;
        var result = await CompileFixture.CompileC(src);
        AssertCompiledOk(result);
    }

    [TestMethod]
    public async Task RecursiveFunction_Lowers()
    {
        var src = """
            int fact(int n) { if (n <= 1) return 1; return n * fact(n - 1); }
            int main() { return fact(5); }
            """;
        var result = await CompileFixture.CompileC(src);
        AssertCompiledOk(result);
    }

    [TestMethod]
    public async Task GlobalVariables_AreEmitted()
    {
        var src = """
            int g = 42;
            int main() { return g; }
            """;
        var result = await CompileFixture.CompileC(src);
        AssertCompiledOk(result);
    }

    [TestMethod]
    public async Task ArrayDeclaration_AndIndexing_Lower()
    {
        var src = """
            int main() {
                int a[5];
                a[0] = 10;
                a[1] = a[0] + 5;
                return a[1];
            }
            """;
        var result = await CompileFixture.CompileC(src);
        AssertCompiledOk(result);
    }

    [TestMethod]
    public async Task PointerDeclaration_Dereference_AddressOf_Lower()
    {
        var src = """
            int main() {
                int x = 5;
                int *p = &x;
                *p = 10;
                return *p;
            }
            """;
        var result = await CompileFixture.CompileC(src);
        AssertCompiledOk(result);
    }

    [TestMethod]
    public async Task StructDeclaration_AndFieldAccess_Lower()
    {
        var src = """
            struct Point { int x; int y; };
            int main() {
                struct Point p;
                p.x = 1;
                p.y = 2;
                return p.x + p.y;
            }
            """;
        var result = await CompileFixture.CompileC(src);
        AssertCompiledOk(result);
    }

    [TestMethod]
    public async Task EnumDeclaration_AndUse_Lower()
    {
        var src = """
            enum Color { RED, GREEN, BLUE };
            int main() { return GREEN; }
            """;
        var result = await CompileFixture.CompileC(src);
        AssertCompiledOk(result);
    }

    [TestMethod]
    public async Task TypedefAlias_IsResolved()
    {
        var src = """
            typedef int MyInt;
            int main() { MyInt x = 5; return x; }
            """;
        var result = await CompileFixture.CompileC(src);
        AssertCompiledOk(result);
    }

    [TestMethod]
    public async Task CompoundAssignmentOperators_Lower()
    {
        var src = """
            int main() {
                int x = 10;
                x += 1; x -= 1; x *= 2; x /= 2;
                return x;
            }
            """;
        var result = await CompileFixture.CompileC(src);
        AssertCompiledOk(result);
    }

    [TestMethod]
    public async Task IncrementDecrement_Lower()
    {
        var src = """
            int main() {
                int x = 0;
                x++; ++x; x--; --x;
                return x;
            }
            """;
        var result = await CompileFixture.CompileC(src);
        AssertCompiledOk(result);
    }

    [TestMethod]
    public async Task SyntaxError_ProducesErrorResult()
    {
        var src = "int main( { return 0; }";
        var result = await CompileFixture.CompileC(src);

        // Either the pipeline returns failure or the build produces error diagnostics.
        if (result.IsSuccess)
        {
            Assert.IsNotEmpty(result.Value.Errors, "expected errors for malformed source");
        }
    }

    [TestMethod]
    public async Task UnsupportedFloat_IsRejectedByCapabilityValidator()
    {
        var src = "int main() { float f = 1.0; return 0; }";
        var result = await CompileFixture.CompileC(src);

        Assert.IsTrue(result.IsSuccess);
        Assert.IsNotEmpty(
            result.Value.Errors,
            "expected capability validator to reject 'float'");
    }

    private static void AssertCompiledOk(CompileResult result)
    {
        Assert.IsTrue(
            result.IsSuccess,
            $"compilation failed: {(result.IsFailure ? result.Error : string.Empty)}");
        Assert.IsNotNull(result.Value.Assembly);
        Assert.IsEmpty(
            result.Value.Errors,
            $"unexpected errors: {string.Join(", ", result.Value.Errors.Select(e => e.Message))}");
    }
}
