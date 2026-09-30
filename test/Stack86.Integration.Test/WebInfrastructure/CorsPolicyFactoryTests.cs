namespace Stack86.Integration.Test.WebInfrastructure;

using System.Linq;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Stack86.Web.Infrastructure.Security;
using CorsOptions = Stack86.Common.Security.Options.CorsOptions;

[TestClass]
public sealed class CorsPolicyFactoryTests
{
    [TestMethod]
    public void Configure_NoOriginsConfigured_DeniesEveryOrigin()
    {
        // Arrange
        var builder = new CorsPolicyBuilder();

        // Act
        CorsPolicyFactory.Configure(builder, new CorsOptions { AllowedOrigins = [] });
        var policy = builder.Build();

        // Assert
        Assert.IsFalse(policy.IsOriginAllowed("https://localhost:1234"));
        Assert.IsFalse(policy.IsOriginAllowed("https://evil.example"));
    }

    [TestMethod]
    public void Configure_ConfiguredOrigin_AllowsOnlyThatOrigin()
    {
        // Arrange
        var builder = new CorsPolicyBuilder();
        var options = new CorsOptions
        {
            AllowedOrigins = ["https://localhost:1234"],
            AllowedMethods = ["POST", "OPTIONS"],
            AllowedHeaders = ["Content-Type"],
        };

        // Act
        CorsPolicyFactory.Configure(builder, options);
        var policy = builder.Build();

        // Assert
        Assert.IsTrue(policy.IsOriginAllowed("https://localhost:1234"));
        Assert.IsFalse(policy.IsOriginAllowed("https://evil.example"));
        CollectionAssert.Contains(policy.Methods.ToList(), "POST");
        CollectionAssert.Contains(policy.Headers.ToList(), "Content-Type");
    }

    /// <summary>This app has no sign-in, so no configuration can open the policy to cookies.</summary>
    [TestMethod]
    public void Configure_AnyOrigin_NeverSupportsCredentials()
    {
        // Arrange
        var builder = new CorsPolicyBuilder();
        var options = new CorsOptions
        {
            AllowedOrigins = ["https://localhost:1234"],
        };

        // Act
        CorsPolicyFactory.Configure(builder, options);
        var policy = builder.Build();

        // Assert
        Assert.IsFalse(policy.SupportsCredentials);
    }
}
