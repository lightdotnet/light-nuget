using Light.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Json;
using Microsoft.Extensions.Hosting;

namespace WebHost.Tests;

public class JsonConfigurationLocationTests
{
    private string _root = null!;

    [SetUp]
    public void SetUp()
    {
        _root = Path.Combine(Path.GetTempPath(), "webhost-tests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(_root, "cfg1"));
        Directory.CreateDirectory(Path.Combine(_root, "cfg2"));

        File.WriteAllText(Path.Combine(_root, "appsettings.json"), """{ "Key": "root" }""");
        File.WriteAllText(Path.Combine(_root, "cfg1", "b.json"), """{ "B": "1", "Key": "cfg1" }""");
        File.WriteAllText(Path.Combine(_root, "cfg1", "a.json"), """{ "A": "1" }""");
        File.WriteAllText(Path.Combine(_root, "cfg2", "c.json"), """{ "C": "2" }""");
    }

    [TearDown]
    public void TearDown()
    {
        try { Directory.Delete(_root, recursive: true); } catch { /* best effort */ }
    }

    [Test]
    public void MultiplePaths_RelativeToContentRoot_RootFilesAddedOnce()
    {
        var builder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings
        {
            ContentRootPath = _root,
            EnvironmentName = "Test",
        });

        var before = builder.Configuration.Sources.Count;

        builder.LoadConfigurationFrom(["cfg1", "cfg2"]);

        var added = builder.Configuration.Sources.Skip(before).ToList();
        var jsonPaths = added.OfType<JsonConfigurationSource>().Select(s => Path.GetFileName(s.Path)).ToList();

        Assert.Multiple(() =>
        {
            // folder files in deterministic order, then root appsettings once, then env vars once
            Assert.That(jsonPaths, Is.EqualTo(new[] { "a.json", "b.json", "c.json", "appsettings.json", "appsettings.Test.json" }));
            Assert.That(added.Count(s => s is not JsonConfigurationSource), Is.EqualTo(1));
            Assert.That(builder.Configuration["A"], Is.EqualTo("1"));
            Assert.That(builder.Configuration["C"], Is.EqualTo("2"));
            Assert.That(builder.Configuration["Key"], Is.EqualTo("root"));
        });
    }
}
