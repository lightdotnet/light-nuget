using Light.Serilog;
using Microsoft.Extensions.Configuration;
using NUnit.Framework;

namespace UnitTests.SerilogTests;

public class SerilogOptionsExtensionsTests
{
    private static IConfiguration BuildConfig(Dictionary<string, string?> values) =>
        new ConfigurationBuilder().AddInMemoryCollection(values).Build();

    [Test]
    public void GetWriteTo_ExactNameMatch_ReturnsEntry()
    {
        var config = BuildConfig(new Dictionary<string, string?>
        {
            ["Serilog:WriteTo:0:Name"] = "FileAsync",
        });

        var entry = SerilogOptionsExtensions.GetWriteTo(config, "FileAsync");

        Assert.That(entry, Is.Not.Null);
        entry!.Name.ShouldBe("FileAsync");
    }

    [Test]
    public void GetWriteTo_NameWithTrailingCharacter_DoesNotMatch()
    {
        // Regression, mirrors a real config bug: samples/WebApi/appsettings.json configures the sink
        // as "ElasticsearchAsync1", which does not exact-match "ElasticsearchAsync" and silently
        // never activates.
        var config = BuildConfig(new Dictionary<string, string?>
        {
            ["Serilog:WriteTo:0:Name"] = "ElasticsearchAsync1",
        });

        var entry = SerilogOptionsExtensions.GetWriteTo(config, "ElasticsearchAsync");

        Assert.That(entry, Is.Null);
    }

    [Test]
    public void GetWriteTo_IsCaseSensitive()
    {
        var config = BuildConfig(new Dictionary<string, string?>
        {
            ["Serilog:WriteTo:0:Name"] = "FileAsync",
        });

        var entry = SerilogOptionsExtensions.GetWriteTo(config, "fileasync");

        Assert.That(entry, Is.Null);
    }

    [Test]
    public void GetWriteTo_NoWriteToSection_ReturnsNull_DoesNotThrow()
    {
        var config = BuildConfig([]);

        WriteToOptions? entry = null;
        Assert.DoesNotThrow(() => entry = SerilogOptionsExtensions.GetWriteTo(config, "FileAsync"));
        Assert.That(entry, Is.Null);
    }

    [Test]
    public void GetWriteTo_EntryWithoutArgs_ArgsIsNull()
    {
        var config = BuildConfig(new Dictionary<string, string?>
        {
            ["Serilog:WriteTo:0:Name"] = "FileAsync",
        });

        var entry = SerilogOptionsExtensions.GetWriteTo(config, "FileAsync");

        Assert.That(entry, Is.Not.Null);
        Assert.That(entry!.Args, Is.Null);
    }

    [Test]
    public void GetWriteTo_MultipleEntriesWithSameName_ReturnsFirstMatch()
    {
        var config = BuildConfig(new Dictionary<string, string?>
        {
            ["Serilog:WriteTo:0:Name"] = "FileAsync",
            ["Serilog:WriteTo:0:Args:path"] = "first.log",
            ["Serilog:WriteTo:1:Name"] = "FileAsync",
            ["Serilog:WriteTo:1:Args:path"] = "second.log",
        });

        var entry = SerilogOptionsExtensions.GetWriteTo(config, "FileAsync");

        Assert.That(entry!.Args!["path"], Is.EqualTo("first.log"));
    }
}
