namespace Stack86.Api.Test.Controllers;

using System;
using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Mvc;
using Stack86.Api.Controllers;
using Stack86.Api.Models.Compiler.Requests;

/// <summary>The bound in front of the model binder: a body nothing will accept is never read.</summary>
[TestClass]
public sealed class CompilerControllerLimitsTests
{
    [TestMethod]
    [DataRow(nameof(CompilerController.CompileAsync))]
    [DataRow(nameof(CompilerController.CompileStreamAsync))]
    public void Action_ReadsNoMoreThanTheCompileBodyLimit(string action)
    {
        // Arrange
        MethodInfo method = typeof(CompilerController)
            .GetMethods(BindingFlags.Public | BindingFlags.Instance)
            .Single(candidate => candidate.Name == action);

        // Act: the attribute keeps the figure to itself, so read what it was given.
        CustomAttributeData? limit = method
            .GetCustomAttributesData()
            .SingleOrDefault(data => data.AttributeType == typeof(RequestSizeLimitAttribute));

        // Assert
        Assert.IsNotNull(limit, $"{action} reads a body of any size the platform allows.");
        Assert.AreEqual((long)CompileRequest.MaxBodyBytes, limit.ConstructorArguments[0].Value);
    }

    /// <summary>The headroom is deliberate, so a valid request is never refused by the limit.</summary>
    [TestMethod]
    public void BodyLimit_LeavesRoomForJsonAroundTheSource()
    {
        Assert.IsGreaterThan(262_144, CompileRequest.MaxBodyBytes);
        Assert.IsLessThan(30_000_000, CompileRequest.MaxBodyBytes);
    }
}
