using Light.EventBus.Abstractions;
using Light.MassTransit.RabbitMQ;
using MassTransit;
using MassTransit.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace EventBus.Tests;

public class ConsumerTests
{
    private static async Task<(ServiceProvider Provider, ITestHarness Harness)> StartHarness<TConsumer>(TestLogger logger, HandlerCalls calls)
        where TConsumer : class, IConsumer
    {
        var provider = new ServiceCollection()
            .AddSingleton(logger)
            .AddSingleton(calls)
            .AddScoped<IEventBus>(sp => new RabbitMQEventBus(sp.GetRequiredService<IPublishEndpoint>(), NullLogger<RabbitMQEventBus>.Instance))
            .AddMassTransitTestHarness(x => x.AddConsumer<TConsumer>())
            .BuildServiceProvider(true);

        var harness = provider.GetRequiredService<ITestHarness>();
        harness.TestInactivityTimeout = TimeSpan.FromSeconds(1);
        await harness.Start();

        return (provider, harness);
    }

    [Test]
    public async Task Consume_LogsException_AndFaults_WhenThrowIfErrorTrue()
    {
        var logger = new TestLogger();
        var (provider, harness) = await StartHarness<FailingConsumer>(logger, new HandlerCalls());
        await using var _ = provider;

        await harness.Bus.Publish(new TestEvent());

        Assert.That(await harness.GetConsumerHarness<FailingConsumer>().Consumed.Any<TestEvent>(x => x.Exception != null), Is.True);
        Assert.That(await harness.Published.Any<Fault<TestEvent>>(), Is.True);

        var error = logger.Entries.Single(x => x.Level == LogLevel.Error);
        Assert.That(error.Exception, Is.TypeOf<InvalidOperationException>());
    }

    [Test]
    public async Task Consume_LogsException_AndSwallows_WhenThrowIfErrorFalse()
    {
        var logger = new TestLogger();
        var (provider, harness) = await StartHarness<SwallowingConsumer>(logger, new HandlerCalls());
        await using var _ = provider;

        await harness.Bus.Publish(new TestEvent());

        var consumerHarness = harness.GetConsumerHarness<SwallowingConsumer>();
        Assert.That(await consumerHarness.Consumed.Any<TestEvent>(), Is.True);
        Assert.That(await consumerHarness.Consumed.Any<TestEvent>(x => x.Exception != null), Is.False);
        Assert.That(await harness.Published.Any<Fault<TestEvent>>(), Is.False);

        var error = logger.Entries.Single(x => x.Level == LogLevel.Error);
        Assert.That(error.Exception, Is.TypeOf<InvalidOperationException>());
    }

    [Test]
    public async Task Consume_CallsHandleMessage_WhenContextOverloadNotOverridden()
    {
        var calls = new HandlerCalls();
        var (provider, harness) = await StartHarness<LegacyConsumer>(new TestLogger(), calls);
        await using var _ = provider;

        await harness.Bus.Publish(new TestEvent());

        Assert.That(await harness.GetConsumerHarness<LegacyConsumer>().Consumed.Any<TestEvent>(), Is.True);
        Assert.That(calls.HandleMessage, Is.EqualTo(1));
    }

    [Test]
    public async Task Consume_CallsContextOverload_WhenOverridden()
    {
        var calls = new HandlerCalls();
        var (provider, harness) = await StartHarness<ContextConsumer>(new TestLogger(), calls);
        await using var _ = provider;

        var message = new TestEvent();
        await harness.Bus.Publish(message);

        Assert.That(await harness.GetConsumerHarness<ContextConsumer>().Consumed.Any<TestEvent>(), Is.True);
        Assert.Multiple(() =>
        {
            Assert.That(calls.HandleWithContext, Is.EqualTo(1));
            Assert.That(calls.HandleMessage, Is.EqualTo(0));
            Assert.That(calls.Context?.Message.Id, Is.EqualTo(message.Id));
        });
    }

    [Test]
    public async Task Consume_LogsPayloadOnlyAtDebug()
    {
        var logger = new TestLogger();
        var (provider, harness) = await StartHarness<LegacyConsumer>(logger, new HandlerCalls());
        await using var _ = provider;

        await harness.Bus.Publish(new TestEvent { Value = "secret-payload" });

        Assert.That(await harness.GetConsumerHarness<LegacyConsumer>().Consumed.Any<TestEvent>(), Is.True);
        Assert.That(logger.Entries.Where(x => x.Level > LogLevel.Debug).Any(x => x.Message.Contains("secret-payload")), Is.False);
        Assert.That(logger.Entries.Any(x => x.Level == LogLevel.Information), Is.True);
    }

    [Test]
    public async Task EventBus_Publish_PublishesMessage()
    {
        var (provider, harness) = await StartHarness<LegacyConsumer>(new TestLogger(), new HandlerCalls());
        await using var _ = provider;

        using (var scope = provider.CreateScope())
        {
            await scope.ServiceProvider.GetRequiredService<IEventBus>().Publish(new TestEvent());
        }

        Assert.That(await harness.Published.Any<TestEvent>(), Is.True);
    }

    [Test]
    public void EventBus_Publish_ThrowsForNullMessage()
    {
        var eventBus = new RabbitMQEventBus(null!, NullLogger<RabbitMQEventBus>.Instance);

        Assert.ThrowsAsync<ArgumentNullException>(() => eventBus.Publish<TestEvent>(null!));
    }
}
