namespace Stack86.Logic.Test.Languages.C;

using System.Collections.Generic;
using Stack86.Logic.Languages.C;

/// <summary>
/// Direct unit tests for <see cref="CIncludePreprocessor"/>.
/// </summary>
[TestClass]
public sealed class CIncludePreprocessorTests
{
    [TestMethod]
    public void Preprocess_missing_main_returns_failure()
    {
        var files = new Dictionary<string, string>();
        var result = PreprocessOutcome.Run(files, "main.c");
        Assert.IsTrue(result.IsFailure);
        Assert.Contains("Main file", result.Error);
    }

    [TestMethod]
    public void Preprocess_single_file_no_includes()
    {
        var files = new Dictionary<string, string>
        {
            ["main.c"] = "int main() { return 0; }",
        };
        var result = PreprocessOutcome.Run(files, "main.c");
        Assert.IsTrue(result.IsSuccess);
        Assert.Contains("int main", result.Value.MergedSource);
    }

    [TestMethod]
    public void Preprocess_resolves_quoted_include()
    {
        var files = new Dictionary<string, string>
        {
            ["main.c"] = "#include \"util.h\"\nint main() { return foo(); }",
            ["util.h"] = "int foo() { return 7; }",
        };
        var result = PreprocessOutcome.Run(files, "main.c");
        Assert.IsTrue(result.IsSuccess);
        Assert.Contains("int foo()", result.Value.MergedSource);
    }

    [TestMethod]
    public void Preprocess_missing_include_returns_failure()
    {
        var files = new Dictionary<string, string>
        {
            ["main.c"] = "#include \"missing.h\"\nint main() { return 0; }",
        };
        var result = PreprocessOutcome.Run(files, "main.c");
        Assert.IsTrue(result.IsFailure);
        Assert.Contains("Include file not found", result.Error);
    }

    [TestMethod]
    public void Preprocess_collects_system_headers()
    {
        var files = new Dictionary<string, string>
        {
            ["main.c"] = "#include <stdio.h>\n#include <stdlib.h>\nint main() { return 0; }",
        };
        var result = PreprocessOutcome.Run(files, "main.c");
        Assert.IsTrue(result.IsSuccess);
        Assert.Contains("stdio.h", result.Value.SystemHeaders);
        Assert.Contains("stdlib.h", result.Value.SystemHeaders);
    }

    [TestMethod]
    public void Preprocess_expands_object_macro()
    {
        var files = new Dictionary<string, string>
        {
            ["main.c"] = "#define N 42\nint main() { return N; }",
        };
        var result = PreprocessOutcome.Run(files, "main.c");
        Assert.IsTrue(result.IsSuccess);
        Assert.Contains("return 42", result.Value.MergedSource);
    }

    [TestMethod]
    public void Preprocess_chained_macros_expand_recursively()
    {
        var files = new Dictionary<string, string>
        {
            ["main.c"] = "#define A 7\n#define B A\nint main() { return B; }",
        };
        var result = PreprocessOutcome.Run(files, "main.c");
        Assert.IsTrue(result.IsSuccess);
        Assert.Contains("return 7", result.Value.MergedSource);
    }

    [TestMethod]
    public void Preprocess_include_once()
    {
        var files = new Dictionary<string, string>
        {
            ["main.c"] = "#include \"util.h\"\n#include \"util.h\"\nint main() { return 0; }",
            ["util.h"] = "int x = 1;",
        };
        var result = PreprocessOutcome.Run(files, "main.c");
        Assert.IsTrue(result.IsSuccess);
        var count = result.Value.MergedSource.Split("int x = 1;").Length - 1;
        Assert.AreEqual(1, count);
    }

    [TestMethod]
    public void Preprocess_merges_extra_c_files()
    {
        var files = new Dictionary<string, string>
        {
            ["main.c"] = "int main() { return helper(); }",
            ["helper.c"] = "int helper() { return 99; }",
        };
        var result = PreprocessOutcome.Run(files, "main.c");
        Assert.IsTrue(result.IsSuccess);
        Assert.Contains("int helper()", result.Value.MergedSource);
    }

    [TestMethod]
    public void MapLine_returns_mapping_for_valid_line()
    {
        var files = new Dictionary<string, string>
        {
            ["main.c"] = "int x = 1;\nint y = 2;",
        };
        var result = PreprocessOutcome.Run(files, "main.c");
        Assert.IsTrue(result.IsSuccess);
        var mapping = CIncludePreprocessor.MapLine(result.Value.LineMappings, 1);
        Assert.IsNotNull(mapping);
        Assert.AreEqual("main.c", mapping.File);
        Assert.AreEqual(1, mapping.OriginalLine);
    }

    [TestMethod]
    public void MapLine_out_of_range_returns_null()
    {
        var files = new Dictionary<string, string>
        {
            ["main.c"] = "int x;",
        };
        var result = PreprocessOutcome.Run(files, "main.c");
        Assert.IsTrue(result.IsSuccess);
        Assert.IsNull(CIncludePreprocessor.MapLine(result.Value.LineMappings, 0));
        Assert.IsNull(CIncludePreprocessor.MapLine(result.Value.LineMappings, 999));
    }
}
