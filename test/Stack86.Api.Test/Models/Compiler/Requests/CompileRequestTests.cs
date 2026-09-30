namespace Stack86.Api.Test.Models.Compiler.Requests;

using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using Stack86.Api.Models.Compiler.Requests;

[TestClass]
public sealed class CompileRequestTests
{
    [TestMethod]
    public void Validate_UnderTheByteLimit_ReturnsNoErrors()
    {
        // Arrange
        var request = new CompileRequest
        {
            Language = "c",
            Files = new Dictionary<string, string> { ["main.c"] = "int main(){return 0;}" },
        };

        // Act
        var results = Validate(request);

        // Assert
        Assert.IsEmpty(results);
    }

    [TestMethod]
    public void Validate_OverTheByteLimit_ReturnsAnErrorOnFiles()
    {
        // Arrange: one file alone over the 262,144-byte ceiling.
        var request = new CompileRequest
        {
            Language = "c",
            Files = new Dictionary<string, string> { ["main.c"] = new string('a', 262_145) },
        };

        // Act
        var results = Validate(request);

        // Assert
        Assert.HasCount(1, results);
        CollectionAssert.Contains(results[0].MemberNames.ToList(), nameof(CompileRequest.Files));
    }

    [TestMethod]
    public void Validate_OverTheByteLimitAcrossMultipleFiles_ReturnsAnError()
    {
        // Arrange: no single file is over the ceiling, but the sum is.
        var request = new CompileRequest
        {
            Language = "c",
            Files = new Dictionary<string, string>
            {
                ["a.c"] = new string('a', 150_000),
                ["b.c"] = new string('b', 150_000),
            },
        };

        // Act
        var results = Validate(request);

        // Assert
        Assert.HasCount(1, results);
    }

    private static List<ValidationResult> Validate(CompileRequest request)
    {
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(request, new ValidationContext(request), results, validateAllProperties: true);
        return results;
    }
}
