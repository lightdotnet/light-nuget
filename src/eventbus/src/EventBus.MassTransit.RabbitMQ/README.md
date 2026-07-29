# Lightsoft.EventBus.MassTransit.RabbitMQ

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

## What's in this package

| Type | Namespace | Visibility | Purpose |
|---|---|---|---|
| `MassTransitRabbitMQServiceCollectionExtensions` | `Light.Extensions.DependencyInjection` | public static | `AddMassTransit(Action<MassTransitConfigurator>)` — the main entry point. Builds a `MassTransitConfigurator`, registers consumers (explicit + assembly-scanned module consumers), applies `SetKebabCaseEndpointNameFormatter()`, configures the RabbitMQ transport, and registers `IEventBus` → `RabbitMQEventBus` as scoped. |
| `MassTransitConfigurator` | `Light.MassTransit.RabbitMQ` | public | Configuration builder passed into `AddMassTransit`'s callback. `AddConsumer<TConsumer, TDefinition>()` registers an explicit consumer/definition pair; `AddConsumers(params Assembly[])` records assemblies to scan for `ModuleConsumer` types; `ConfigRabbitMQ(Action<RabbitMQConfigurator>)` configures the transport via a nested callback. |
| `RabbitMQConfigurator` | `Light.MassTransit.RabbitMQ` | public | Transport settings: `Host`, `Username`, `Password` (all non-nullable `string`, must be set). `Exclude<T>()` / `Exclude(Type)` mark a message type as excluded from auto topic/exchange creation on publish (`Publish(type, p => p.Exclude = true)`). |
| `RabbitMQEventBus` | `Light.MassTransit.RabbitMQ` | public | The `IEventBus` implementation registered by `AddMassTransit`. `Publish<T>` calls the injected `IPublishEndpoint.Publish` and logs `event_bus {id} published with data: {@Data}` via `ILogger<RabbitMQEventBus>`. |
| `Consumer<TMessage>` | `Light.MassTransit.RabbitMQ` | public abstract | Base class for MassTransit consumers of an `IIntegrationEvent`-derived message. Implements `IConsumer<TMessage>.Consume`: calls the abstract `Handle(TMessage)`, logs success (`event_bus {id} consumed data: {@Data}`) or failure (`event_bus {id} consumed data: {@Data} with error: {error}`). On failure, re-throws (`throw;`, preserving the original stack trace) only if the overridable `ThrowIfError` property (default `true`) is `true`; otherwise the exception is swallowed after logging. Takes an `ILogger` via constructor. |
| `ConsumerDefinition<TMessage, TConsumer>` | `Light.MassTransit.RabbitMQ` | public | Base `MassTransit.ConsumerDefinition<TConsumer>` that, in its constructor, sets `EndpointName` to `typeof(TMessage).GetBindingName()` when present — i.e. the consumer's endpoint/queue name follows the message's `[BindingName]` rather than MassTransit's default kebab-case type name. |
| `BusEntityBindingNameFormatter` | `Light.MassTransit.RabbitMQ` | public | `IEntityNameFormatter` decorator installed by `UseRabbitMQ`. `FormatEntityName<T>()` returns `typeof(T).GetBindingName()` if the type carries `[BindingName]`, otherwise delegates to the original formatter it wraps (MassTransit's default, kebab-case-by-type-name formatter after `SetKebabCaseEndpointNameFormatter()`). |
| `BindingNameExtensions.GetBindingName(this MemberInfo)` | `Light.MassTransit.RabbitMQ` | internal static | Reflection helper reading the `BindingNameAttribute` (from `Lightsoft.EventBus`) off a type/member and returning its `BindingName`, or `null` if absent. Used by both `BusEntityBindingNameFormatter` and `ConsumerDefinition<TMessage, TConsumer>`. |
| `ModuleConsumer` (and internal `IModuleConsumer`) | `Light.AspNetCore.Modularity` | public abstract (`ModuleConsumer`) / internal (`IModuleConsumer`) | Base class a consuming module derives from to register its MassTransit consumers without the host application needing to know about them individually. `AddConsumers(IBusRegistrationConfigurator)` is a no-op by default — override it to call `configurator.AddConsumer<TConsumer, TDefinition>()`. Discovered via assembly scanning (see `AddModuleConsumers` below). Declared under `AspNetCore\Modularity\ModuleConsumer.cs`, in the `Light.AspNetCore.Modularity` namespace — the same namespace name used by the unrelated `Modularity` package in `src/framework`, but there is no code dependency between the two; this is namespace-name overlap only. |

### Internal wiring (not part of the public API, but useful to know)

- `MassTransitRabbitMQServiceCollectionExtensions.UseRabbitMQ` (private) — configures the RabbitMQ host/credentials
  from `RabbitMQConfigurator`, calls `ConfigureEndpoints(busRegistrationContext)`, installs
  `BusEntityBindingNameFormatter` as the bus's entity-name formatter, excludes `IIntegrationEvent` itself from
  auto-publish topology (`Publish<IIntegrationEvent>(p => p.Exclude = true)`), and excludes every type passed to
  `RabbitMQConfigurator.Exclude`/`Exclude<T>()`.
- `MassTransitRabbitMQServiceCollectionExtensions.AddModuleConsumers` (private) — scans the assemblies passed to
  `MassTransitConfigurator.AddConsumers(params Assembly[])` for concrete, non-abstract classes assignable to
  `IModuleConsumer` (i.e. deriving from `ModuleConsumer`), instantiates each via `Activator.CreateInstance` (throwing
  a descriptive `InvalidOperationException` naming the offending type if it lacks a public parameterless
  constructor), and calls `AddConsumers` on each instance.

## Usage

### 1. Register MassTransit + RabbitMQ

```csharp
using Light.Extensions.DependencyInjection;
using System.Reflection;

var assembly = Assembly.GetExecutingAssembly();

builder.Services.AddMassTransit(x =>
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

### 2. Define an event and give it a binding name

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
MassTransit falls back to its default kebab-case-by-type-name convention.

### 3. Write a consumer

```csharp
using Light.MassTransit.RabbitMQ;
using MassTransit;

public class ColorChangedConsumer(ILogger<ColorChangedConsumer> logger)
    : Consumer<ColorChangedIntegrationEvent>(logger)
{
    public override bool ThrowIfError => false;

    public override async Task Handle(ColorChangedIntegrationEvent message)
    {
        logger.LogInformation("Color changed from {oldColor} to {newColor}", message.OldColor, message.NewColor);
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

### 4. Register the consumer via a module

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

`AddMassTransit`'s `x.AddConsumers(assembly)` call (step 1) discovers `SampleModuleConsumers` by scanning `assembly`
for `ModuleConsumer`-derived types and invokes `AddConsumers` on it — you don't need to register the consumer
explicitly at the call site.

Alternatively, register a consumer explicitly without a module, via `MassTransitConfigurator.AddConsumer<TConsumer, TDefinition>()`
inside the `AddMassTransit` callback.

### 5. Publish

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

## Notes

- **Every `[BindingName]`-attributed event type must use a distinct binding name.** The binding name determines the
  RabbitMQ entity/queue an event is published to and consumed from (`BusEntityBindingNameFormatter.FormatEntityName<T>`
  and `ConsumerDefinition<TMessage, TConsumer>`'s `EndpointName`). Two different event types sharing the same
  `[BindingName("...")]` value collide on the same entity — this was a real bug in `EventBusSample`
  (`ColorRemovedIntegrationEvent` originally reused `ColorChangedIntegrationEvent`'s `"color-value-changed"` instead
  of its own `"color-value-removed"`) and has since been fixed. Double-check binding names are unique per event type
  when adding new events.
- `Consumer<TMessage>.ThrowIfError` defaults to `true` (unhandled `Handle` exceptions are logged then re-thrown, so
  MassTransit's retry/error pipeline sees the fault). Override it to `false` (as the sample consumers do) to log and
  swallow instead.
- `RabbitMQConfigurator.Host`/`Username`/`Password` are declared `= null!` — they're required and not validated by
  this package; leaving one unset will surface as a MassTransit/RabbitMQ connection failure at bus start, not a
  clear configuration error from this package.
- `AddMassTransit` always excludes `IIntegrationEvent` itself from publish topology
  (`rabbitMqBusFactoryConfigurator.Publish<IIntegrationEvent>(p => p.Exclude = true)`), independent of anything
  passed to `RabbitMQConfigurator.Exclude`/`Exclude<T>()`. You typically still want to call `Exclude<T>()` for your
  own base/marker record types (e.g. an abstract `IntegrationEvent`/`EventBase` base record), since MassTransit
  otherwise creates topology for every type in the inheritance chain, not just `IIntegrationEvent`.
- `ModuleConsumer`/`IModuleConsumer` live under `Light.AspNetCore.Modularity` in this package
  (`AspNetCore\Modularity\ModuleConsumer.cs`) — the same namespace name as the unrelated app-composition
  `Modularity` package in `src/framework`. This overlap is intentional/known and was left as-is after an earlier
  attempt to rename it was reverted; there is no `ProjectReference` between the two packages, so it does not cause a
  build-time collision, but be aware of it if both packages are referenced by the same consuming project (a `using
  Light.AspNetCore.Modularity;` will pull in whichever assembly resolves the type you asked for).
