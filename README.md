# Rebus.Fake

[![install from nuget](https://img.shields.io/nuget/v/Rebus.Fake.svg?style=flat-square)](https://www.nuget.org/packages/Rebus.Fake)

No-op implementations of Rebus Transport, SubscriptionStorage, and SagaStorage that silently discard all operations. Kind-of like `/dev/null` in Linux.

## Installation

```bash
dotnet add package Rebus.Fake
```

Or via NuGet Package Manager:
```
Install-Package Rebus.Fake
```

## Why?

Sometimes an application has to hand out an `IBus`, but the messages sent through it are never going to be consumed.
Rebus.Fake makes those messages disappear at the transport, so nothing accumulates and nothing has to be drained.

**Example use case:** An application intended for both online and offline use. When hosted in an online environment it communicates with external services using Rebus, but when hosted offline and those external services are not needed, it's simpler to inject Rebus with fake components and drop all messages that the application attempts to send.

### Why not one of the built-in options?

| Option | Why it doesn't fit |
|--------|--------------------|
| [InMemory transport](https://github.com/rebus-org/Rebus/wiki/Transport) | Messages are queued in an `InMemNetwork`. With nothing consuming them they accumulate for the lifetime of the process. |
| `Rebus.TestHelpers.FakeBus` | Records every operation as events so tests can assert on them. That recording grows unboundedly in a long-running process, and it replaces the whole bus, so your application's real Rebus configuration is never exercised. |
| Hand-written no-op `IBus` | `IBus` is a wide interface (`Send`, `SendLocal`, `Publish`, `Subscribe`, `Defer`, `Reply`, plus everything under `Advanced`) and it grows between Rebus versions. |

Replacing `ITransport` instead keeps Rebus itself real. Messages are still serialized, routing is still resolved, and
headers are still applied, so configuration mistakes still surface. Only delivery is discarded.

### What the persistence fakes are actually for

This is worth being explicit about, because it is a trade rather than a free win.

Rebus does not silently ignore missing subscription or saga storage. Its defaults are `DisabledSubscriptionStorage` and
`DisabledSagaStorage`, which throw a descriptive exception the moment you use them:

> A subscription storage has not been configured. Please configure a subscription storage with the .Subscriptions(...) configurer

`FakeSubscriptionStorage` and `FakeSagaStorage` replace that deliberate failure with silence. That is what you want when
`Subscribe<T>()` or a saga runs in offline mode and should do nothing. It also means a genuine misconfiguration no longer
announces itself. **Only register them if your code actually calls into subscriptions or sagas in the fake configuration** -
if it doesn't, leave Rebus's throwing defaults in place and keep the safety net.

`FakeTransport` carries no such trade, because Rebus has no throwing default transport to begin with.

### Is this useful for testing?

For testing messaging itself, no. Use the [InMemory transport](https://github.com/rebus-org/Rebus/wiki/Transport) when you
want handlers to actually run, or `Rebus.TestHelpers.FakeBus` when you want to assert which messages were sent. Rebus.Fake
discards everything and records nothing, so there is nothing to assert against.

There is one case where it fits: booting a real application host where messaging is not what's under test and the bus
should exist but stay inert. Swapping only the transport leaves the application's own Rebus configuration intact, while
guaranteeing no delivery, no accumulation, and no leakage between tests through a shared `InMemNetwork`.

### Scope

Deferred messages need no extra configuration. `bus.Defer(...)` stamps headers and sends like any other message, so the
fake transport discards it; Rebus's `ITimeoutManager` is only consulted when a deferred message is *received*. Registering
a fake timeout manager would be actively worse, because Rebus skips its due-messages background poller only for the
disabled default.

Rebus's data bus is **not** covered. `IDataBus.OpenRead` and `GetMetadata` promise to return what was written, and a
discarding implementation cannot honour that without handing back empty streams that look like real payloads. Rebus's
`DisabledDataBus` throws instead, which is the more honest failure.

## Components

Rebus.Fake provides three no-op implementations:

| Component | Description |
|-----------|-------------|
| **FakeTransport** | Silently discards all sent messages, never receives anything |
| **FakeSubscriptionStorage** | Accepts subscribe/unsubscribe operations but never returns subscribers |
| **FakeSagaStorage** | Accepts saga operations but never persists or retrieves saga data |

`FakeTransport` is the one you always want. The other two are only needed if your code calls into subscriptions or sagas
while faked out - see [What the persistence fakes are actually for](#what-the-persistence-fakes-are-actually-for).

## Usage

**Prefer the one-way client unless you need an input queue.** `UseFakeTransportAsOneWayClient()` sets Rebus's
`NumberOfWorkers` to 0, so nothing is started. `UseFakeTransport(inputQueueName)` starts workers which poll a transport
that returns `null` forever. They back off, so the cost is small, but for a send-only offline configuration it buys
nothing.

### One-way Client (Send Only)

```csharp
using Rebus.Activation;
using Rebus.Config;
using Rebus.Routing.TypeBased;

using var activator = new BuiltinHandlerActivator();

using var bus = Configure.With(activator)
    .Transport(t => t.UseFakeTransportAsOneWayClient())
    .Routing(r => r.TypeBased().Map<MyMessage>("dummy-queue"))
    .Start();

await bus.Send(new MyMessage());
```

### Bidirectional with Pub/Sub

```csharp
using Rebus.Activation;
using Rebus.Config;

using var activator = new BuiltinHandlerActivator();

using var bus = Configure.With(activator)
    .Transport(t => t.UseFakeTransport("inputQueueName"))
    .Subscriptions(s => s.UseFakeSubscriptionStorage())
    .Start();

await bus.Subscribe<MyEvent>();
await bus.Publish(new MyEvent());
```

### All Components (with Sagas)

```csharp
using Rebus.Activation;
using Rebus.Config;

using var activator = new BuiltinHandlerActivator();

using var bus = Configure.With(activator)
    .Transport(t => t.UseFakeTransport("inputQueueName"))
    .Subscriptions(s => s.UseFakeSubscriptionStorage())
    .Sagas(s => s.UseFakeSagaStorage())
    .Start();
```

### Deferred Messages

No timeout manager configuration is needed - deferring sends like any other message, so it is discarded by the transport.
Note that `Defer` resolves a destination through routing just like `Send`, whereas `DeferLocal` targets the bus's own
input queue.

```csharp
using var bus = Configure.With(activator)
    .Transport(t => t.UseFakeTransport("inputQueueName"))
    .Routing(r => r.TypeBased().Map<MyMessage>("dummy-queue"))
    .Start();

await bus.Defer(TimeSpan.FromMinutes(5), new MyMessage());
await bus.DeferLocal(TimeSpan.FromMinutes(5), new MyMessage());
```

## License

MIT
