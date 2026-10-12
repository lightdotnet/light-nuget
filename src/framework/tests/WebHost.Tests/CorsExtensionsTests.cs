using Light.AspNetCore.Cors;
using Microsoft.AspNetCore.Cors.Infrastructure;

namespace WebHost.Tests;

public class CorsExtensionsTests
{
    [Test]
    public void AllowOrigins_ValidOrigins_AddsPolicyWithCredentials()
    {
        var options = new CorsOptions();

        options.AllowOrigins("web", "https://a.example.com", "https://b.example.com");

        var policy = options.GetPolicy("web");
        Assert.That(policy, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(policy!.Origins, Is.EquivalentTo(new[] { "https://a.example.com", "https://b.example.com" }));
            Assert.That(policy.SupportsCredentials, Is.True);
        });
    }

    [Test]
    public void AllowOrigins_Empty_Throws()
    {
        var options = new CorsOptions();

        Assert.Throws<ArgumentException>(() => options.AllowOrigins("web"));
    }

    [TestCase("*")]
    [TestCase("https://*.example.com")]
    [TestCase("")]
    [TestCase("  ")]
    public void AllowOrigins_InvalidOrigin_Throws(string origin)
    {
        var options = new CorsOptions();

        Assert.Throws<ArgumentException>(() => options.AllowOrigins("web", "https://ok.example.com", origin));
    }
}
