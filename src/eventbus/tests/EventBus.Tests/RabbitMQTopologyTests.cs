using Light.AspNetCore.Modularity;
using Light.EventBus.Events;
using Light.Extensions.DependencyInjection;
using Light.MassTransit.RabbitMQ;
using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Reflection;

namespace EventBus.Tests;

/// <summary>
/// Exercises the RabbitMQ bus configuration built by <c>AddRabbitMQEventBus</c> without a broker: the bus is created
/// (which runs the <c>UsingRabbitMq</c> callback and builds the topology) but never started, so nothing connects.
/// </summary>
public class RabbitMQTopologyTests
{
    private static ServiceProvider BuildProvider(Action<MassTransitConfigurator> configure, ILoggerFactory? loggerFactory = null)
    {
        var services = new ServiceCollection();

        if (loggerFactory != null)
        {
            services.AddSingleton(loggerFactory);
        }

        services.AddRabbitMQEventBus(x =>
        {
            configure(x);
            x.ConfigRabbitMQ(mq =>
            {
                mq.Host = "localhost";
                mq.Username = "guest";
                mq.Password = "guest";
            });
        });

        return services.BuildServiceProvider();
    }

    [Test]
    public async Task Bus_UsesBindingName_AsEntityName()
    {
        await using var provider = BuildProvider(_ => { });

        var bus = provider.GetRequiredService<IBusControl>();

        Assert.That(bus.Topology.Message<BaseBoundEvent>().EntityName, Is.EqualTo("base-event"));
        // not inherited: falls back to MassTransit's default formatter
        Assert.That(bus.Topology.Message<DerivedFromBoundEvent>().EntityName, Is.Not.EqualTo("base-event"));
    }

    [Test]
    public async Task Bus_ExcludesIntegrationEvent_AndConfiguredTypes_FromPublishTopology()
    {
        await using var provider = BuildProvider(x => x.ConfigRabbitMQ(mq => mq.Exclude<TestEvent>()));

        var publishTopology = provider.GetRequiredService<IBusControl>().Topology.PublishTopology;

        Assert.That(publishTopology.GetMessageTopology<IIntegrationEvent>().Exclude, Is.True);
        Assert.That(publishTopology.GetMessageTopology<TestEvent>().Exclude, Is.True);
        Assert.That(publishTopology.GetMessageTopology<BaseBoundEvent>().Exclude, Is.False);
    }

    [Test]
    public void AddRabbitMQEventBus_RegistersConsumers_FromConfiguratorAndModules()
    {
        var services = new ServiceCollection();

        services.AddRabbitMQEventBus(x =>
        {
            x.AddConsumer<BoundConsumer, DefaultBoundConsumerDefinition>();
            x.AddConsumers(new FakeAssembly(typeof(TestModuleConsumers)));
            x.ConfigRabbitMQ(mq =>
            {
                mq.Host = "localhost";
                mq.Username = "guest";
                mq.Password = "guest";
            });
        });

        Assert.That(services.Any(x => x.ServiceType == typeof(BoundConsumer)), Is.True);
        Assert.That(services.Any(x => x.ServiceType == typeof(UnboundConsumer)), Is.True);
    }

    [Test]
    public void GetLoadableTypes_ReturnsLoadedTypes_AndRecordsError_OnReflectionTypeLoadException()
    {
        var assembly = new FakeAssembly(typeof(TestModuleConsumers), throwLoadError: true);
        var errors = new List<(Assembly Assembly, ReflectionTypeLoadException Exception)>();

        var types = MassTransitRabbitMQServiceCollectionExtensions.GetLoadableTypes(assembly, errors).ToList();

        Assert.That(types, Is.EqualTo(new[] { typeof(TestModuleConsumers) }));
        Assert.That(errors, Has.Count.EqualTo(1));
        Assert.That(errors[0].Assembly, Is.SameAs(assembly));
    }

    [Test]
    public async Task AddRabbitMQEventBus_LogsWarning_AndStillRegistersLoadedModules_WhenTypesFailToLoad()
    {
        var logger = new TestLogger();
        var services = new ServiceCollection();
        services.AddSingleton<ILoggerFactory>(new TestLoggerFactory(logger));

        services.AddRabbitMQEventBus(x =>
        {
            x.AddConsumers(new FakeAssembly(typeof(TestModuleConsumers), throwLoadError: true));
            x.ConfigRabbitMQ(mq =>
            {
                mq.Host = "localhost";
                mq.Username = "guest";
                mq.Password = "guest";
            });
        });

        Assert.That(services.Any(x => x.ServiceType == typeof(UnboundConsumer)), Is.True);

        await using var provider = services.BuildServiceProvider();
        provider.GetRequiredService<IBusControl>();

        var warning = logger.Entries.Single(x => x.Level == LogLevel.Warning && x.Message.Contains("could not be loaded"));
        Assert.That(warning.Exception, Is.TypeOf<ReflectionTypeLoadException>());
        Assert.That(warning.Message, Does.Contain("missing dependency"));
    }

    [Test]
    public void ConsumerDefinition_IgnoresPrefix_WhenMessageHasNoBindingName()
    {
        var definition = new PrefixedUnboundConsumerDefinition();

        Assert.That(((IConsumerDefinition)definition).GetEndpointName(KebabCaseEndpointNameFormatter.Instance),
            Is.EqualTo(KebabCaseEndpointNameFormatter.Instance.Consumer<UnboundConsumer>()));
    }
}

public class UnboundConsumer : IConsumer<UnboundEvent>
{
    public Task Consume(ConsumeContext<UnboundEvent> context) => Task.CompletedTask;
}

public class PrefixedUnboundConsumerDefinition : ConsumerDefinition<UnboundEvent, UnboundConsumer>
{
    public PrefixedUnboundConsumerDefinition() : base("billing")
    {
    }
}

public class TestModuleConsumers : ModuleConsumer
{
    public override void AddConsumers(IBusRegistrationConfigurator configurator)
    {
        configurator.AddConsumer<UnboundConsumer>();
    }
}

/// <summary>Assembly stub whose <see cref="GetTypes"/> returns the given types, or fails like a partially loadable assembly.</summary>
public sealed class FakeAssembly(Type loadedType, bool throwLoadError = false) : Assembly
{
    public override string FullName => "FakeAssembly, Version=1.0.0.0";

    public override Type[] GetTypes()
    {
        if (throwLoadError)
        {
            throw new ReflectionTypeLoadException(
                new[] { loadedType, null },
                new Exception?[] { null, new FileNotFoundException("missing dependency") });
        }

        return new[] { loadedType };
    }
}

public sealed class TestLoggerFactory(TestLogger logger) : ILoggerFactory
{
    public void AddProvider(ILoggerProvider provider)
    {
    }

    public ILogger CreateLogger(string categoryName) => logger;

    public void Dispose()
    {
    }
}
