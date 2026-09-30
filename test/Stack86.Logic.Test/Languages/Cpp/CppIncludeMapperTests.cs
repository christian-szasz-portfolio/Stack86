namespace Stack86.Logic.Test.Languages.Cpp;

using Stack86.Logic.Languages.Cpp;

/// <summary>
/// Direct unit tests for <see cref="CppIncludeMapper"/>.
/// </summary>
[TestClass]
public sealed class CppIncludeMapperTests
{
    [TestMethod]
    [DataRow("iostream", "stdio.h")]
    [DataRow("cstdio", "stdio.h")]
    [DataRow("cstdlib", "stdlib.h")]
    [DataRow("cstring", "string.h")]
    [DataRow("string", "string.h")]
    public void MapInclude_known_returns_mapped(string input, string expected)
    {
        Assert.AreEqual(expected, CppIncludeMapper.MapInclude(input));
    }

    [TestMethod]
    [DataRow("cmath")]
    [DataRow("cassert")]
    [DataRow("climits")]
    [DataRow("cctype")]
    [DataRow("nonexistent_header")]
    public void MapInclude_null_or_unknown_returns_null(string input)
    {
        Assert.IsNull(CppIncludeMapper.MapInclude(input));
    }

    [TestMethod]
    public void TransformIncludes_replaces_iostream_with_stdio()
    {
        const string Source = "#include <iostream>\nint main() { return 0; }";
        var result = CppIncludeMapper.TransformIncludes(Source);
        Assert.Contains("#include <stdio.h>", result);
        Assert.DoesNotContain("iostream", result);
    }

    [TestMethod]
    public void TransformIncludes_deduplicates()
    {
        const string Source = "#include <iostream>\n#include <cstdio>\nint main() { return 0; }";
        var result = CppIncludeMapper.TransformIncludes(Source);
        var count = result.Split("#include <stdio.h>").Length - 1;
        Assert.AreEqual(1, count);
    }

    [TestMethod]
    public void TransformIncludes_drops_cmath()
    {
        const string Source = "#include <cmath>\nint main() { return 0; }";
        var result = CppIncludeMapper.TransformIncludes(Source);
        Assert.DoesNotContain("cmath", result);
        Assert.DoesNotContain("#include", result);
    }

    [TestMethod]
    public void TransformIncludes_preserves_unknown()
    {
        const string Source = "#include <foo.h>\nint main() { return 0; }";
        var result = CppIncludeMapper.TransformIncludes(Source);
        Assert.DoesNotContain("#include <foo.h>", result);
    }

    [TestMethod]
    public void TransformIncludes_passes_through_quoted_includes_when_unknown()
    {
        const string Source = "#include \"local.h\"\nint main() { return 0; }";
        var result = CppIncludeMapper.TransformIncludes(Source);
        Assert.Contains("int main", result);
    }

    [TestMethod]
    public void TransformIncludes_passes_through_non_include_lines()
    {
        const string Source = "// hello\nint main() { return 0; }";
        var result = CppIncludeMapper.TransformIncludes(Source);
        Assert.Contains("// hello", result);
        Assert.Contains("int main", result);
    }

    [TestMethod]
    public void ExtractCIncludes_returns_dedup_list_with_iostream_and_cstdio()
    {
        const string Source = "#include <iostream>\n#include <cstdio>\n#include <cstring>\n";
        var includes = CppIncludeMapper.ExtractCIncludes(Source);
        Assert.HasCount(2, includes);
        Assert.Contains("#include <stdio.h>", includes);
        Assert.Contains("#include <string.h>", includes);
    }

    [TestMethod]
    public void ExtractCIncludes_skips_unmapped()
    {
        const string Source = "#include <cmath>\n";
        var includes = CppIncludeMapper.ExtractCIncludes(Source);
        Assert.IsEmpty(includes);
    }

    [TestMethod]
    public void ExtractCIncludes_handles_quoted_form()
    {
        const string Source = "#include \"iostream\"\n";
        var includes = CppIncludeMapper.ExtractCIncludes(Source);
        Assert.HasCount(1, includes);
        Assert.AreEqual("#include <stdio.h>", includes[0]);
    }

    [TestMethod]
    public void ExtractCIncludes_skips_malformed()
    {
        const string Source = "#include\nint main() {}";
        var includes = CppIncludeMapper.ExtractCIncludes(Source);
        Assert.IsEmpty(includes);
    }
}
