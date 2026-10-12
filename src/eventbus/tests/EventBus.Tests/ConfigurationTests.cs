using Light.EventBus.Abstractions;
using Light.Extensions.DependencyInjection;
using Light.MassTransit.RabbitMQ;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace EventBus.Tests;

public class ConfigurationTests
{
    private static Action<MassTransitConfigurator> Configure(string? host, string? username, string? password) =>
        x => x.ConfigRabbitMQ(mq =>
        {
            mq.Host = host!;
            mq.Username = username!;
            mq.Password = password!;
        });

    [TestCase(null, "user", "pass", "Host")]
    [TestCase(" ", "user", "pass", "Host")]
    [TestCase("localhost", null, "pass", "Username")]
    [TestCase("localhost", "", "pass", "Username")]
    [TestCase("localhost", "user", null, "Password")]
    [TestCase("localhost", "user", "", "Password")]
    public void AddRabbitMQEventBus_Throws_WhenSettingMissing(string? host, string? username, string? password, string setting)
    {
        var services = new ServiceCollection();

        var ex = Assert.Throws<ArgumentException>(() => services.AddRabbitMQEventBus(Configure(host, username, password)));

        Assert.That(ex!.Message, Does.Contain("'" + setting + "'"));
    }

    [Test]
    public void AddRabbitMQEventBus_Throws_WhenActionNull()
    {
        var services = new ServiceCollection();

        Assert.Throws<ArgumentNullException>(() => services.AddRabbitMQEventBus(null!));
    }

    [Test]
    public void AddRabbitMQEventBus_RegistersEventBus_WhenValid()
    {
        var services = new ServiceCollection();

        services.AddRabbitMQEventBus(Configure("localhost", "guest", "guest"));

        Assert.That(services.Any(x => x.ServiceType == typeof(IEventBus) && x.ImplementationType == typeof(RabbitMQEventBus)), Is.True);
    }

    [Test]
    public void AddMassTransit_ForwardsToAddRabbitMQEventBus()
    {
        var services = new ServiceCollection();

        Assert.Throws<ArgumentException>(() => MassTransitRabbitMQServiceCollectionExtensions.AddMassTransit(services, Configure(null, "u", "p")));

        MassTransitRabbitMQServiceCollectionExtensions.AddMassTransit(services, Configure("localhost", "guest", "guest"));

        Assert.That(services.Any(x => x.ServiceType == typeof(IEventBus)), Is.True);
    }

    [Test]
    public void AddConsumers_AppendsAssemblies()
    {
        var first = typeof(ConfigurationTests).Assembly;
        var second = typeof(RabbitMQEventBus).Assembly;
        var third = typeof(Light.EventBus.Events.IIntegrationEvent).Assembly;

        var configurator = new MassTransitConfigurator();
        configurator.AddConsumers(first);
        configurator.AddConsumers(second, first);
        configurator.AddConsumers(third);

        Assert.That(configurator.FromAssemblies, Is.EqualTo(new Assembly[] { first, second, third }));
    }
}
