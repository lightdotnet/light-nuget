# Lightsoft.EventBus

Contracts package for EventBus. Defines the abstractions that an integration-event bus implementation (e.g.
`Lightsoft.EventBus.MassTransit.RabbitMQ`) implements and that event-producing/consuming code depends on, without
pulling in any concrete messaging technology.

- **NuGet package id / assembly name:** `Lightsoft.EventBus` (no explicit `PackageId`, so it defaults to `AssemblyName`)
- **Root namespace:** `Light.EventBus` — types live under `Light.EventBus.Abstractions` and `Light.EventBus.Events`
- **Target framework:** netstandard2.0
- **Dependencies:** none — no `PackageReference`s and no `ProjectReference`s. This is a leaf project with zero dependencies.

## What's in this package

| Type | Namespace | Purpose |
|---|---|---|
| `IEventBus` | `Light.EventBus.Abstractions` | The bus contract: `Task Publish<T>(T message, CancellationToken cancellationToken = default) where T : IIntegrationEvent`. Implementations (e.g. `RabbitMQEventBus`) publish `message` to the underlying transport. |
| `IIntegrationEvent` | `Light.EventBus.Events` | Contract every published/consumed event must implement: `string Id { get; }` and `DateTime CreationDate { get; }`. |
| `BindingNameAttribute` | `Light.EventBus.Events` | `[AttributeUsage(AttributeTargets.Class \| AttributeTargets.Struct)]` attribute taking a single `string bindingName` constructor argument, exposed as `BindingName { get; }`. Applied to an event type to control the name of the RabbitMQ entity/queue it binds to (interpreted by `Lightsoft.EventBus.MassTransit.RabbitMQ`; this package only defines the attribute). |

## Usage

This package has no runtime behavior of its own — it only defines the shapes above. Implement `IIntegrationEvent` on
your event types and depend on `IEventBus` to publish them:

```csharp
using Light.EventBus.Events;

public record OrderPlacedIntegrationEvent : IIntegrationEvent
{
    public string Id { get; init; } = Guid.NewGuid().ToString();

    public DateTime CreationDate { get; init; } = DateTime.UtcNow;

    public string OrderNumber { get; init; } = null!;
}
```

```csharp
using Light.EventBus.Abstractions;

public class OrderService(IEventBus eventBus)
{
    public Task PlaceOrder(string orderNumber, CancellationToken cancellationToken) =>
        eventBus.Publish(new OrderPlacedIntegrationEvent { OrderNumber = orderNumber }, cancellationToken);
}
```

Register a concrete `IEventBus` implementation via a transport-specific package — see
[`Lightsoft.EventBus.MassTransit.RabbitMQ`](../EventBus.MassTransit.RabbitMQ/README.md).

## Notes

- This package intentionally has no dependency on MassTransit, RabbitMQ, or any other transport — application code
  and domain/event-contract assemblies can reference it without pulling in messaging infrastructure.
- `IIntegrationEvent.Id` and `CreationDate` are plain getter-only properties with no attached validation or default —
  implementations are responsible for populating them (typically in the event's constructor, as the samples do).
