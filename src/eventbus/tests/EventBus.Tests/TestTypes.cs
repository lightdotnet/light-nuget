using Light.EventBus.Events;
using Light.MassTransit.RabbitMQ;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace EventBus.Tests;

public record TestEvent : IIntegrationEvent
{
    public string Id { get; init; } = Guid.NewGuid().ToString();

    public DateTime CreationDate { get; init; } = DateTime.UtcNow;

    public string Value { get; init; } = "value";
}

[BindingName("base-event")]
public record BaseBoundEvent : TestEvent;

public record DerivedFromBoundEvent : BaseBoundEvent;

public record UnboundEvent : TestEvent;

public sealed class TestLogger : ILogger
{
    public List<(LogLevel Level, Exception? Exception, string Message)> Entries { get; } = new();

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

    public bool IsEnabled(LogLevel logLevel) => true;

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        lock (Entries)
        {
            Entries.Add((logLevel, exception, formatter(state, exception)));
        }
    }
}

public sealed class HandlerCalls
{
    public int HandleMessage;

    public int HandleWithContext;

    public ConsumeContext<TestEvent>? Context;
}

public class FailingConsumer(TestLogger logger) : Consumer<TestEvent>(logger)
{
    public override Task Handle(TestEvent message) => throw new InvalidOperationException("boom");
}

public class SwallowingConsumer(TestLogger logger) : Consumer<TestEvent>(logger)
{
    public override bool ThrowIfError => false;

    public override Task Handle(TestEvent message) => throw new InvalidOperationException("boom");
}

public class LegacyConsumer(TestLogger logger, HandlerCalls calls) : Consumer<TestEvent>(logger)
{
    public override Task Handle(TestEvent message)
    {
        Interlocked.Increment(ref calls.HandleMessage);
        return Task.CompletedTask;
    }
}

public class ContextConsumer(TestLogger logger, HandlerCalls calls) : Consumer<TestEvent>(logger)
{
    public override Task Handle(TestEvent message)
    {
        Interlocked.Increment(ref calls.HandleMessage);
        return Task.CompletedTask;
    }

    protected override Task Handle(TestEvent message, ConsumeContext<TestEvent> context)
    {
        calls.Context = context;
        Interlocked.Increment(ref calls.HandleWithContext);
        return Task.CompletedTask;
    }
}

public class BoundConsumer : IConsumer<BaseBoundEvent>
{
    public Task Consume(ConsumeContext<BaseBoundEvent> context) => Task.CompletedTask;
}

public class DefaultBoundConsumerDefinition : ConsumerDefinition<BaseBoundEvent, BoundConsumer>
{
}

public class PrefixedBoundConsumerDefinition : ConsumerDefinition<BaseBoundEvent, BoundConsumer>
{
    public PrefixedBoundConsumerDefinition() : base("billing")
    {
    }
}

public class InvalidPrefixBoundConsumerDefinition : ConsumerDefinition<BaseBoundEvent, BoundConsumer>
{
    public InvalidPrefixBoundConsumerDefinition() : base("billing service")
    {
    }
}
