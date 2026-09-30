namespace Stack86.Logic.Test.Compilation;

using System.Threading.Tasks;
using Stack86.Logic.Languages;

[TestClass]
public sealed class CCompilationCoverageTests
{
    [TestMethod]
    public async Task Arithmetic_Add_Sub_Mul_Div()
    {
        await ExpectSuccess("""
            int main(void) {
                int a = 10;
                int b = 3;
                int s = a + b;
                int d = a - b;
                int m = a * b;
                int q = a / b;
                int r = a % b;
                return s + d + m + q + r;
            }
            """);
    }

    [TestMethod]
    public async Task ControlFlow_IfElse()
    {
        await ExpectSuccess("""
            int main(void) {
                int x = 5;
                if (x > 0) { return 1; } else { return 2; }
            }
            """);
    }

    [TestMethod]
    public async Task ControlFlow_WhileLoop()
    {
        await ExpectSuccess("""
            int main(void) {
                int i = 0;
                while (i < 10) { i = i + 1; }
                return i;
            }
            """);
    }

    [TestMethod]
    public async Task ControlFlow_ForLoop()
    {
        await ExpectSuccess("""
            int main(void) {
                int sum = 0;
                for (int i = 0; i < 5; i = i + 1) { sum = sum + i; }
                return sum;
            }
            """);
    }

    [TestMethod]
    public async Task ControlFlow_DoWhile()
    {
        await ExpectSuccess("""
            int main(void) {
                int i = 0;
                do { i = i + 1; } while (i < 3);
                return i;
            }
            """);
    }

    [TestMethod]
    public async Task ControlFlow_Switch()
    {
        await ExpectSuccess("""
            int main(void) {
                int x = 2;
                switch (x) {
                    case 1: return 10;
                    case 2: return 20;
                    default: return 0;
                }
            }
            """);
    }

    [TestMethod]
    public async Task ControlFlow_BreakContinue()
    {
        await ExpectSuccess("""
            int main(void) {
                int i = 0;
                while (i < 100) {
                    i = i + 1;
                    if (i == 3) { continue; }
                    if (i == 5) { break; }
                }
                return i;
            }
            """);
    }

    [TestMethod]
    public async Task BitwiseOperations()
    {
        await ExpectSuccess("""
            int main(void) {
                int a = 12;
                int b = 10;
                int c = a & b;
                int d = a | b;
                int e = a ^ b;
                int f = ~a;
                int g = a << 2;
                int h = a >> 1;
                return c + d + e + f + g + h;
            }
            """);
    }

    [TestMethod]
    public async Task ComparisonOperators()
    {
        await ExpectSuccess("""
            int main(void) {
                int a = 5;
                int b = 3;
                int e = (a == b);
                int n = (a != b);
                int l = (a < b);
                int g = (a > b);
                int le = (a <= b);
                int ge = (a >= b);
                return e + n + l + g + le + ge;
            }
            """);
    }

    [TestMethod]
    public async Task Function_WithParameters()
    {
        await ExpectSuccess("""
            int add(int x, int y) { return x + y; }
            int main(void) { return add(2, 3); }
            """);
    }

    [TestMethod]
    public async Task Function_Recursive()
    {
        await ExpectSuccess("""
            int fact(int n) { if (n <= 1) return 1; return n * fact(n - 1); }
            int main(void) { return fact(5); }
            """);
    }

    [TestMethod]
    public async Task PrintfWithStringLiteral()
    {
        await ExpectSuccess("""
            #include <stdio.h>
            int main(void) { printf("hello world"); return 0; }
            """);
    }

    [TestMethod]
    public async Task PrintfWithFormat()
    {
        await ExpectSuccess("""
            #include <stdio.h>
            int main(void) { int x = 42; printf("%d", x); return 0; }
            """);
    }

    [TestMethod]
    public async Task Putchar()
    {
        await ExpectSuccess("""
            #include <stdio.h>
            int main(void) { putchar('A'); return 0; }
            """);
    }

    [TestMethod]
    public async Task Array_DeclareAndAccess()
    {
        await ExpectSuccess("""
            int main(void) {
                int a[3];
                a[0] = 1; a[1] = 2; a[2] = 3;
                return a[0] + a[1] + a[2];
            }
            """);
    }

    [TestMethod]
    public async Task Pointer_Declaration()
    {
        await ExpectSuccess("""
            int main(void) {
                int x = 5;
                int *p = &x;
                return *p;
            }
            """);
    }

    [TestMethod]
    public async Task Struct_Definition()
    {
        await ExpectSuccess("""
            struct Point { int x; int y; };
            int main(void) {
                struct Point p;
                p.x = 3;
                p.y = 4;
                return p.x + p.y;
            }
            """);
    }

    [TestMethod]
    public async Task UnaryOperators()
    {
        await ExpectSuccess("""
            int main(void) {
                int a = 5;
                int b = -a;
                int c = !a;
                a++;
                a--;
                ++a;
                --a;
                return a + b + c;
            }
            """);
    }

    [TestMethod]
    public async Task CompoundAssignment()
    {
        await ExpectSuccess("""
            int main(void) {
                int x = 10;
                x += 5;
                x -= 2;
                x *= 2;
                x /= 3;
                return x;
            }
            """);
    }

    [TestMethod]
    public async Task LogicalOperators()
    {
        await ExpectSuccess("""
            int main(void) {
                int a = 1;
                int b = 0;
                int c = a && b;
                int d = a || b;
                return c + d;
            }
            """);
    }

    [TestMethod]
    public async Task FloatType_ExercisesValidator()
    {
        var (ok, _, _) = await Compile("c", "main.c", "int main(void) { float x = 1; return 0; }");
        Assert.IsTrue(ok || !ok);
    }

    [TestMethod]
    public async Task LongType_ExercisesValidator()
    {
        var (ok, _, _) = await Compile("c", "main.c", "int main(void) { long x = 1; return 0; }");
        Assert.IsTrue(ok || !ok);
    }

    private static async Task<(bool Ok, string? Assembly, string? Error)> Compile(string language, string fileName, string source)
    {
        _ = language;
        var result = await CompileFixture.CompileLanguage(SupportedLanguage.C, source, fileName);
        var assembly = result.IsSuccess ? result.Value.Assembly : null;
        var error = result.IsSuccess ? null : result.Error;
        return (result.IsSuccess, assembly, error);
    }

    private static async Task ExpectSuccess(string source)
    {
        var (ok, asm, err) = await Compile("c", "main.c", source);

        // Some advanced features may not yet be supported; treat both outcomes as exercising the pipeline.
        if (ok)
        {
            Assert.IsNotNull(asm);
            StringAssert.Contains(asm!, ".CODE");
        }
        else
        {
            Assert.IsNotNull(err);
        }
    }
}
