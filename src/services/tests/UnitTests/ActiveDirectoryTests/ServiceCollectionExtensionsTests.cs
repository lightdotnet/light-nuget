using Light.ActiveDirectory.Interfaces;
using Light.ActiveDirectory.Services;
using Light.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

namespace UnitTests.ActiveDirectoryTests;

public class ServiceCollectionExtensionsTests
{
    [Test]
    public void AddActiveDirectory_RegistersFakeActiveDirectoryService()
    {
        var services = new ServiceCollection();
        services.AddActiveDirectory();
        var provider = services.BuildServiceProvider();

        var resolved = provider.GetRequiredService<IActiveDirectoryService>();

        Assert.That(resolved, Is.TypeOf<FakeActiveDirectoryService>());
    }

    [Test]
    public void AddActiveDirectory_RegistrationIsTransient()
    {
        var services = new ServiceCollection();
        services.AddActiveDirectory();
        var provider = services.BuildServiceProvider();

        var first = provider.GetRequiredService<IActiveDirectoryService>();
        var second = provider.GetRequiredService<IActiveDirectoryService>();

        Assert.That(ReferenceEquals(first, second), Is.False);
    }
}
