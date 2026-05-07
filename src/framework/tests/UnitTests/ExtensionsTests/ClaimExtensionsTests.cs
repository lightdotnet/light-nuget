using NUnit.Framework;
using System.Security.Claims;

namespace UnitTests.ExtensionsTests;

public class ClaimExtensionsTests
{
    [Test]
    public void Add_ShouldAddClaim_WhenValueIsNotNullOrEmpty()
    {
        // Arrange
        var claims = new List<Claim>();
        string key = "key";
        string value = "value";

        // Act
        claims.Add(key, value);

        // Assert
        LightAssert.ShouldBe(claims.Count, 1);
        LightAssert.ShouldBe(key, claims[0].Type);
        LightAssert.ShouldBe(value, claims[0].Value);
    }

    [Test]
    public void Add_ShouldNotAddClaim_WhenValueIsNullOrEmpty()
    {
        // Arrange
        var claims = new List<Claim>();
        string key = "key";
        string? value = null;

        // Act
        claims.Add(key, value);

        // Assert
        LightAssert.ShouldBe(claims.Count, 0);
    }
}