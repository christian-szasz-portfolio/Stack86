namespace Stack86.Logic.Test.Languages.C;

using Stack86.Logic.Test.Compilation;

/// <summary>
/// End-to-end compile coverage for advanced C features that previously lacked tests:
/// unions (including overlapping storage), multi-dimensional arrays, nested structs, and
/// function prototypes / forward declarations with unnamed parameters. Each program is
/// asserted to emit working 8086 assembly.
/// </summary>
[TestClass]
public sealed class CAdvancedFeatureCompileTests
{
    [TestMethod]
    [DataRow("#include <stdio.h>\nunion U { int i; char c; };\nint main() { union U u; u.i = 65; printf(\"%d\", u.i); return 0; }", "union_int")]
    [DataRow("#include <stdio.h>\nunion U { int i; char c; };\nint main() { union U u; u.c = 'A'; printf(\"%c\", u.c); return 0; }", "union_char")]
    [DataRow("#include <stdio.h>\nunion U { int a; int b; };\nint main() { union U u; u.a = 7; printf(\"%d\", u.b); return 0; }", "union_overlap")]
    [DataRow("#include <stdio.h>\nint main() { int m[2][3]; m[0][0] = 1; m[1][2] = 6; printf(\"%d %d\", m[0][0], m[1][2]); return 0; }", "multidim_2d")]
    [DataRow("#include <stdio.h>\nint main() { int m[2][2]; for (int i = 0; i < 2; i++) for (int j = 0; j < 2; j++) m[i][j] = i * 2 + j; printf(\"%d\", m[1][1]); return 0; }", "multidim_loop")]
    [DataRow("#include <stdio.h>\nstruct Inner { int v; };\nstruct Outer { struct Inner in; int w; };\nint main() { struct Outer o; o.in.v = 5; o.w = 9; printf(\"%d %d\", o.in.v, o.w); return 0; }", "nested_struct")]
    [DataRow("#include <stdio.h>\nstruct Inner { int x; int y; };\nstruct Outer { struct Inner a; struct Inner b; };\nint main() { struct Outer o; o.a.x = 1; o.b.y = 4; printf(\"%d %d\", o.a.x, o.b.y); return 0; }", "nested_struct_two")]
    [DataRow("#include <stdio.h>\nint fwd(int);\nint main() { printf(\"%d\", fwd(5)); return 0; }\nint fwd(int n) { return n * n; }", "forward_decl")]
    [DataRow("#include <stdio.h>\nint combine(int, int);\nint main() { printf(\"%d\", combine(3, 4)); return 0; }\nint combine(int a, int b) { return a * 10 + b; }", "forward_decl_two_unnamed")]
    [DataRow("#include <stdio.h>\nvoid greet(void);\nint main() { greet(); return 0; }\nvoid greet(void) { printf(\"hi\"); }", "forward_decl_void")]
    public async Task CProgram_Compiles(string source, string label)
    {
        var result = await CompileFixture.CompileC(source);

        Assert.IsTrue(result.IsSuccess, $"{label}: {(result.IsFailure ? result.Error : string.Empty)}");
        Assert.IsTrue(result.Value.Assembly!.Contains(".CODE", StringComparison.Ordinal), $"{label}: missing .CODE");
    }
}
