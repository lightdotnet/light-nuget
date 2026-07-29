using Light.Extensions.DependencyInjection;
using Light.Graph;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Graph;
using NUnit.Framework;

namespace UnitTests.GraphTests;

public class ServiceCollectionExtensionsTests
{
    // Fake, non-functional credentials: constructing ClientSecretCredential/GraphServiceClient
    // does not itself perform any network/token request, so this is safe without a real Azure AD tenant.
    private static void ConfigureFakeOptions(Light.Graph.Infrastructure.GraphOptions options)
    {
        options.TenantId = "00000000-0000-0000-0000-000000000000";
        options.ClientId = "00000000-0000-0000-0000-000000000000";
        options.ClientSecret = "fake-secret";
    }

    [Test]
    public void AddMicrosoftGraph_Registers_GraphServiceClient_As_Singleton()
    {
        var services = new ServiceCollection();
        services.AddMicrosoftGraph(ConfigureFakeOptions);
        var provider = services.BuildServiceProvider();

        var first = provider.GetRequiredService<GraphServiceClient>();
        var second = provider.GetRequiredService<GraphServiceClient>();

        Assert.That(ReferenceEquals(first, second), Is.True);
    }

    [Test]
    public void AddMicrosoftGraph_Registers_IGraphMailService_As_Scoped()
    {
        var services = new ServiceCollection();
        services.AddMicrosoftGraph(ConfigureFakeOptions);
        var provider = services.BuildServiceProvider();

        using (var scope = provider.CreateScope())
        {
            var first = scope.ServiceProvider.GetRequiredService<IGraphMailService>();
            var second = scope.ServiceProvider.GetRequiredService<IGraphMailService>();
            Assert.That(ReferenceEquals(first, second), Is.True);
        }

        IGraphMailService acrossScopes1, acrossScopes2;
        using (var scope1 = provider.CreateScope())
            acrossScopes1 = scope1.ServiceProvider.GetRequiredService<IGraphMailService>();
        using (var scope2 = provider.CreateScope())
            acrossScopes2 = scope2.ServiceProvider.GetRequiredService<IGraphMailService>();

        Assert.That(ReferenceEquals(acrossScopes1, acrossScopes2), Is.False);
    }

    [Test]
    public void AddMicrosoftGraph_Registers_IGraphTeams_As_Transient()
    {
        var services = new ServiceCollection();
        services.AddMicrosoftGraph(ConfigureFakeOptions);
        var provider = services.BuildServiceProvider();

        var first = provider.GetRequiredService<IGraphTeams>();
        var second = provider.GetRequiredService<IGraphTeams>();

        Assert.That(ReferenceEquals(first, second), Is.False);
    }

    [Test]
    public void AddMicrosoftGraph_InvokesConfigureAction_Once()
    {
        var services = new ServiceCollection();
        var invocationCount = 0;

        services.AddMicrosoftGraph(options =>
        {
            invocationCount++;
            ConfigureFakeOptions(options);
        });

        invocationCount.ShouldBe(1);
    }

    [Test]
    public void AddMicrosoftGraph_ReturnsSameServiceCollection()
    {
        var services = new ServiceCollection();

        var result = services.AddMicrosoftGraph(ConfigureFakeOptions);

        Assert.That(ReferenceEquals(result, services), Is.True);
    }
}
