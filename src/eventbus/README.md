[← Back to main README](https://github.com/lightdotnet/light-nuget#readme)

# EventBus

Two NuGet packages: a transport-agnostic `IEventBus` contract, and a MassTransit + RabbitMQ implementation of it.
See `samples/EventBusSample` and `samples/EventBusConsumer` for runnable end-to-end examples (publisher + consumer,
consumer-only) referencing these packages.

---

## Lightsoft.EventBus

Contracts package for EventBus. Defines the abstractions that an integration-event bus implementation (e.g.
`Lightsoft.EventBus.MassTransit.RabbitMQ`) implements and that event-producing/consuming code depends on, without
pulling in any concrete messaging technology.

- **NuGet package id / assembly name:** `Lightsoft.EventBus` (no explicit `PackageId`, so it defaults to `AssemblyName`)
- **Root namespace:** `Light.EventBus` — types live under `Light.EventBus.Abstractions` and `Light.EventBus.Events`
- **Target framework:** netstandard2.0
- **Dependencies:** none — no `PackageReference`s and no `ProjectReference`s. This is a leaf project with zero dependencies.

### What's in this package

| Type | Namespace | Purpose |
|---|---|---|
| `IEventBus` | `Light.EventBus.Abstractions` | The bus contract: `Task Publish<T>(T message, CancellationToken cancellationToken = default) where T : IIntegrationEvent`. Implementations (e.g. `RabbitMQEventBus`) publish `message` to the underlying transport. |
| `IIntegrationEvent` | `Light.EventBus.Events` | Contract every published/consumed event must implement: `string Id { get; }` and `DateTime CreationDate { get; }`. |
| `BindingNameAttribute` | `Light.EventBus.Events` | `[AttributeUsage(AttributeTargets.Class \| AttributeTargets.Struct, Inherited = false)]` attribute taking a single `string bindingName` constructor argument (null/empty/whitespace throws `ArgumentException`), exposed as `BindingName { get; }`. Applied to an event type to control the name of the RabbitMQ entity/queue it binds to (interpreted by `Lightsoft.EventBus.MassTransit.RabbitMQ`; this package only defines the attribute). **Not inherited:** a type deriving from an attributed event does not get its binding name — declare `[BindingName]` on each concrete event type. |

### Usage

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
[Lightsoft.EventBus.MassTransit.RabbitMQ](#lightsofteventbusmasstransitrabbitmq) below.

### Notes

- This package intentionally has no dependency on MassTransit, RabbitMQ, or any other transport — application code
  and domain/event-contract assemblies can reference it without pulling in messaging infrastructure.
- `IIntegrationEvent.Id` and `CreationDate` are plain getter-only properties with no attached validation or default —
  implementations are responsible for populating them (typically in the event's constructor, as the samples do).

---

## Lightsoft.EventBus.MassTransit.RabbitMQ

MassTransit + RabbitMQ implementation of the `Lightsoft.EventBus` contracts: registers MassTransit with a RabbitMQ
transport, publishes `IIntegrationEvent`s through `IEventBus`, and provides base classes for writing consumers and
for modules to register their own consumers.

- **NuGet package id / assembly name:** `Lightsoft.EventBus.MassTransit.RabbitMQ`
- **Root namespace (MSBuild `RootNamespace`):** `Light` — note this is only the MSBuild default-namespace setting;
  every file in this project declares its namespace explicitly (see table below), so `RootNamespace` has no
  practical effect here.
- **Target framework:** netstandard2.1, `<Nullable>enable</Nullable>`
- **References:**
  - `ProjectReference` → `..\EventBus\EventBus.csproj` (`Lightsoft.EventBus` — `IEventBus`, `IIntegrationEvent`, `BindingNameAttribute`)
  - `PackageReference` → `MassTransit.RabbitMQ` (`Version="8.*"`)

### What's in this package

| Type | Namespace | Visibility | Purpose |
|---|---|---|---|
| `MassTransitRabbitMQServiceCollectionExtensions` | `Light.Extensions.DependencyInjection` | public static | `AddRabbitMQEventBus(Action<MassTransitConfigurator>)` — the main entry point. Builds a `MassTransitConfigurator`, validates `Host`/`Username`/`Password` (throws `ArgumentException` naming the missing setting), registers consumers (explicit + assembly-scanned module consumers), applies `SetKebabCaseEndpointNameFormatter()`, configures the RabbitMQ transport, and registers `IEventBus` → `RabbitMQEventBus` as scoped. `AddMassTransit(Action<MassTransitConfigurator>)` is kept for compatibility and simply forwards to `AddRabbitMQEventBus`; prefer the new name because `AddMassTransit` overlaps MassTransit's own `AddMassTransit(Action<IBusRegistrationConfigurator>)`, and which one a call binds to depends on the lambda body. |
| `MassTransitConfigurator` | `Light.MassTransit.RabbitMQ` | public | Configuration builder passed into `AddRabbitMQEventBus`'s callback. `AddConsumer<TConsumer, TDefinition>()` registers an explicit consumer/definition pair; `AddConsumers(params Assembly[])` adds assemblies to scan for `ModuleConsumer` types (repeated calls append, duplicates ignored); `ConfigRabbitMQ(Action<RabbitMQConfigurator>)` configures the transport via a nested callback. |
| `RabbitMQConfigurator` | `Light.MassTransit.RabbitMQ` | public | Transport settings: `Host`, `Username`, `Password` (all non-nullable `string`, required — validated at registration). `Exclude<T>()` / `Exclude(Type)` mark a message type as excluded from auto topic/exchange creation on publish (`Publish(type, p => p.Exclude = true)`). |
| `RabbitMQEventBus` | `Light.MassTransit.RabbitMQ` | public | The `IEventBus` implementation registered by `AddRabbitMQEventBus`. `Publish<T>` throws `ArgumentNullException` for a null message, calls the injected `IPublishEndpoint.Publish`, logs `event_bus {id} published` at Information and the full payload (`{@Data}`) only at Debug. |
| `Consumer<TMessage>` | `Light.MassTransit.RabbitMQ` | public abstract | Base class for MassTransit consumers of an `IIntegrationEvent`-derived message (protected constructor taking an `ILogger`). Implements `IConsumer<TMessage>.Consume`: calls `protected virtual Handle(TMessage, ConsumeContext<TMessage>)`, which by default forwards to the abstract `Handle(TMessage)` — override the context overload to get the `CancellationToken`, headers, etc. Logs success (`event_bus {id} consumed`, Information) or failure (`LogError` with the exception); payloads are logged only at Debug. On failure, re-throws (`throw;`) only if the overridable `ThrowIfError` property (default `true`) is `true`; otherwise the exception is swallowed after logging and **the message is acknowledged** — see the retry note below. |
| `ConsumerDefinition<TMessage, TConsumer>` | `Light.MassTransit.RabbitMQ` | public abstract | Base `MassTransit.ConsumerDefinition<TConsumer>` that, in its constructor, sets `EndpointName` to `typeof(TMessage).GetBindingName()` when present — i.e. the consumer's endpoint/queue name follows the message's `[BindingName]` rather than MassTransit's default kebab-case type name. `protected ConsumerDefinition(string? endpointNamePrefix)` makes it `{prefix}-{bindingName}` (see the competing-consumers note below). |
| `BusEntityBindingNameFormatter` | `Light.MassTransit.RabbitMQ` | public | `IEntityNameFormatter` decorator installed by `UseRabbitMQ`. `FormatEntityName<T>()` returns `typeof(T).GetBindingName()` if the type carries `[BindingName]`, otherwise delegates to the original formatter it wraps (MassTransit's default, kebab-case-by-type-name formatter after `SetKebabCaseEndpointNameFormatter()`). |
| `BindingNameExtensions.GetBindingName(this MemberInfo)` | `Light.MassTransit.RabbitMQ` | internal static | Reflection helper reading the `BindingNameAttribute` (from `Lightsoft.EventBus`) off a type/member and returning its `BindingName`, or `null` if absent. Used by both `BusEntityBindingNameFormatter` and `ConsumerDefinition<TMessage, TConsumer>`. |
| `ModuleConsumer` (and internal `IModuleConsumer`) | `Light.AspNetCore.Modularity` | public abstract (`ModuleConsumer`) / internal (`IModuleConsumer`) | Base class a consuming module derives from to register its MassTransit consumers without the host application needing to know about them individually. `AddConsumers(IBusRegistrationConfigurator)` is a no-op by default — override it to call `configurator.AddConsumer<TConsumer, TDefinition>()`. Discovered via assembly scanning (see `AddModuleConsumers` below). Declared under `AspNetCore\Modularity\ModuleConsumer.cs`, in the `Light.AspNetCore.Modularity` namespace — the same namespace name used by the unrelated `Modularity` package in `src/framework`, but there is no code dependency between the two; this is namespace-name overlap only. |

#### Internal wiring (not part of the public API, but useful to know)

- `MassTransitRabbitMQServiceCollectionExtensions.UseRabbitMQ` (private) — configures the RabbitMQ host/credentials
  from `RabbitMQConfigurator`, installs `BusEntityBindingNameFormatter` as the bus's entity-name formatter, excludes
  `IIntegrationEvent` itself from auto-publish topology (`Publish<IIntegrationEvent>(p => p.Exclude = true)`),
  excludes every type passed to `RabbitMQConfigurator.Exclude`/`Exclude<T>()`, and only then calls
  `ConfigureEndpoints(busRegistrationContext)`, so receive endpoints are bound using that topology.
- `MassTransitRabbitMQServiceCollectionExtensions.AddModuleConsumers` (private) — scans the assemblies passed to
  `MassTransitConfigurator.AddConsumers(params Assembly[])` for concrete, non-abstract classes assignable to
  `IModuleConsumer` (i.e. deriving from `ModuleConsumer`; if an assembly throws `ReflectionTypeLoadException`, the
  types that did load are still scanned), instantiates each via `Activator.CreateInstance` (throwing
  a descriptive `InvalidOperationException` naming the offending type if it lacks a public parameterless
  constructor), and calls `AddConsumers` on each instance.

### Usage

#### 1. Register MassTransit + RabbitMQ

```csharp
using Light.Extensions.DependencyInjection;
using System.Reflection;

var assembly = Assembly.GetExecutingAssembly();

builder.Services.AddRabbitMQEventBus(x =>
{
    // Scan `assembly` for ModuleConsumer-derived types and register their consumers.
    x.AddConsumers(assembly);

    x.ConfigRabbitMQ(mq =>
    {
        mq.Host = builder.Configuration["RabbitMQ:Host"]!;
        mq.Username = builder.Configuration["RabbitMQ:Username"]!;
        mq.Password = builder.Configuration["RabbitMQ:Password"]!;

        // Prevent MassTransit from auto-creating topology for base/marker event types.
        mq.Exclude<IntegrationEvent>();
        mq.Exclude<EventBase>();
    });
});
```

This registers `IEventBus` → `RabbitMQEventBus` as a scoped service, in addition to configuring MassTransit itself.
A missing `Host`, `Username` or `Password` throws `ArgumentException` here, at registration time.

`builder.Services.AddMassTransit(x => ...)` with the same callback still works (it forwards to
`AddRabbitMQEventBus`), but it shares its name with MassTransit's own `AddMassTransit`; new code should use
`AddRabbitMQEventBus`. Whether to mark the old name `[Obsolete]` is left for a future release.

#### 2. Define an event and give it a binding name

```csharp
using Light.EventBus.Events;

[BindingName("color-value-changed")]
public record ColorChangedIntegrationEvent : EventBase
{
    public string OldColor { get; set; } = null!;

    public string NewColor { get; set; } = null!;

    public DateTime ChangeOn { get; set; }
}
```

`[BindingName("...")]` controls the name of the RabbitMQ entity (exchange) this event publishes to, and — via
`ConsumerDefinition<TMessage, TConsumer>` — the endpoint/queue name of any consumer bound to it. Without it,
MassTransit falls back to its default kebab-case-by-type-name convention. The attribute is not inherited: a record
deriving from `ColorChangedIntegrationEvent` would use the default convention unless it declares its own
`[BindingName]`.

#### 3. Write a consumer

```csharp
using Light.MassTransit.RabbitMQ;
using MassTransit;

public class ColorChangedConsumer(ILogger<ColorChangedConsumer> logger)
    : Consumer<ColorChangedIntegrationEvent>(logger)
{
    // ThrowIfError stays true (the default) so the retry policy below actually applies.

    public override Task Handle(ColorChangedIntegrationEvent message)
    {
        logger.LogInformation("Color changed from {oldColor} to {newColor}", message.OldColor, message.NewColor);
        return Task.CompletedTask;
    }
}

internal class ColorChangedConsumerDefinition
    : ConsumerDefinition<ColorChangedIntegrationEvent, ColorChangedConsumer>
{
    public ColorChangedConsumerDefinition()
    {
        ConcurrentMessageLimit = 10;
    }

    protected override void ConfigureConsumer(
        IReceiveEndpointConfigurator configurator,
        IConsumerConfigurator<ColorChangedConsumer> consumerConfigurator,
        IRegistrationContext context)
    {
        configurator.UseMessageRetry(r => r.Intervals(100, 200, 5000, 8000, 10000));
        configurator.UseInMemoryOutbox(context);
    }
}
```

To access the `ConsumeContext` (e.g. `context.CancellationToken`, headers), override the protected
`Handle(TMessage message, ConsumeContext<TMessage> context)` overload instead — `Consume` calls it, and by default it
forwards to `Handle(TMessage)`. `Handle(TMessage)` is still abstract and must be implemented even when unused (see
`ColorRemovedConsumer` in the sample):

```csharp
public class ColorRemovedConsumer(ILogger<ColorRemovedConsumer> logger)
    : Consumer<ColorRemovedIntegrationEvent>(logger)
{
    public override Task Handle(ColorRemovedIntegrationEvent message) =>
        throw new NotSupportedException("Handle(message, context) is used instead.");

    protected override async Task Handle(
        ColorRemovedIntegrationEvent message,
        ConsumeContext<ColorRemovedIntegrationEvent> context)
    {
        await Task.Delay(100, context.CancellationToken);
    }
}
```

#### 4. Register the consumer via a module

```csharp
using Light.AspNetCore.Modularity;
using MassTransit;

public class SampleModuleConsumers : ModuleConsumer
{
    public override void AddConsumers(IBusRegistrationConfigurator configurator)
    {
        configurator.AddConsumer<ColorChangedConsumer, ColorChangedConsumerDefinition>();
    }
}
```

`AddRabbitMQEventBus`'s `x.AddConsumers(assembly)` call (step 1) discovers `SampleModuleConsumers` by scanning `assembly`
for `ModuleConsumer`-derived types and invokes `AddConsumers` on it — you don't need to register the consumer
explicitly at the call site.

Alternatively, register a consumer explicitly without a module, via `MassTransitConfigurator.AddConsumer<TConsumer, TDefinition>()`
inside the `AddRabbitMQEventBus` callback.

#### 5. Publish

```csharp
using Light.EventBus.Abstractions;

app.MapGet("/changed", async (Color oldColor, Color newColor, IEventBus eventBus) =>
{
    var evt = new ColorChangedIntegrationEvent
    {
        OldColor = oldColor.ToString(),
        NewColor = newColor.ToString(),
        ChangeOn = DateTime.Now,
    };

    await eventBus.Publish(evt);
});
```

### Notes

- **Every `[BindingName]`-attributed event type must use a distinct binding name.** The binding name determines the
  RabbitMQ entity/queue an event is published to and consumed from (`BusEntityBindingNameFormatter.FormatEntityName<T>`
  and `ConsumerDefinition<TMessage, TConsumer>`'s `EndpointName`). Two different event types sharing the same
  `[BindingName("...")]` value collide on the same entity — this was a real bug in `EventBusSample`
  (`ColorRemovedIntegrationEvent` originally reused `ColorChangedIntegrationEvent`'s `"color-value-changed"` instead
  of its own `"color-value-removed"`) and has since been fixed. Double-check binding names are unique per event type
  when adding new events.
- **Consumers of the same event in different services compete for one queue.** `ConsumerDefinition<TMessage, TConsumer>`
  names the receive endpoint (queue) after the event's binding name, so every service consuming e.g.
  `color-value-changed` through such a definition binds to the same `color-value-changed` queue and each message is
  delivered to only one of those services (load-balancing), not to all of them. That is right for scaled-out
  instances of one service, but wrong when different services each need the event. Give each service its own queue
  with the prefix constructor, e.g. `public MyDefinition() : base("billing") { }` → queue `billing-color-value-changed`
  (or set `EndpointName` yourself in the derived constructor). The parameterless constructor keeps the previous
  naming.
- **`ThrowIfError` and retries.** `Consumer<TMessage>.ThrowIfError` defaults to `true`: unhandled `Handle` exceptions
  are logged then re-thrown, so MassTransit's retry/error pipeline sees the fault (`UseMessageRetry`, redelivery, and
  finally the `_error` queue). If you override it to `false`, the exception is logged and swallowed and the message is
  **acknowledged as consumed** — any `UseMessageRetry`/redelivery configured for that consumer never runs and the
  message does not reach the `_error` queue. Don't combine `ThrowIfError => false` with a retry policy expecting it to
  apply (the sample's `ColorRemovedConsumer` uses `false` and therefore configures no retry).
- `RabbitMQConfigurator.Host`/`Username`/`Password` are required. `AddRabbitMQEventBus` (and `AddMassTransit`)
  validate them after running your callback and throw `ArgumentException` naming the missing setting.
- Logging: `RabbitMQEventBus` and `Consumer<TMessage>` log only the event `Id` at Information/Error; full payloads
  (`{@Data}`) are logged at Debug, so event contents (which may contain personal data) stay out of production logs
  unless Debug is enabled for these categories.
- `AddRabbitMQEventBus` always excludes `IIntegrationEvent` itself from publish topology
  (`rabbitMqBusFactoryConfigurator.Publish<IIntegrationEvent>(p => p.Exclude = true)`), independent of anything
  passed to `RabbitMQConfigurator.Exclude`/`Exclude<T>()`. You typically still want to call `Exclude<T>()` for your
  own base/marker record types (e.g. an abstract `IntegrationEvent`/`EventBase` base record), since MassTransit
  otherwise creates topology for every type in the inheritance chain, not just `IIntegrationEvent`.
- `ModuleConsumer`/`IModuleConsumer` live under `Light.AspNetCore.Modularity` in this package
  (`AspNetCore\Modularity\ModuleConsumer.cs`) — the same namespace name as the unrelated app-composition
  `Modularity` package in `src/framework`. There is no `ProjectReference` between the two packages, so it does not
  cause a build-time collision, but be aware of it if both packages are referenced by the same consuming project (a
  `using Light.AspNetCore.Modularity;` will pull in whichever assembly resolves the type you asked for).
