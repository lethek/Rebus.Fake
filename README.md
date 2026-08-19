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

An application sometimes has to hand out an `IBus` whose messages will never be consumed. Rebus.Fake discards those
messages at the transport, so nothing accumulates and nothing has to be drained.

**Example use case:** An application intended for both online and offline use. When hosted in an online environment it communicates with external services using Rebus, but when hosted offline and those external services are not needed, it's simpler to inject Rebus with fake components and drop all messages that the application attempts to send.

### Why not one of the built-in options?

| Option | Why it doesn't fit |
|--------|--------------------|
| [InMemory transport](https://github.com/rebus-org/Rebus/wiki/Transport) | Messages are queued in an `InMemNetwork`. With nothing consuming them they accumulate for the lifetime of the process. |
| `Rebus.TestHelpers.FakeBus` | Records every operation as events for tests to assert on, growing unboundedly in a long-running process. It also replaces the whole bus, so your Rebus configuration is never exercised. |
| Hand-written no-op `IBus` | `IBus` is a wide interface (`Send`, `SendLocal`, `Publish`, `Subscribe`, `Defer`, `Reply`, plus everything under `Advanced`) and it grows between Rebus versions. |

Replacing `ITransport` instead keeps Rebus itself real. Messages are still serialized, routing is still resolved, and
headers are still applied, so configuration mistakes still surface. Only delivery is discarded.

### When to use the persistence fakes

Rebus does not ignore missing subscription or saga storage. Its defaults are `DisabledSubscriptionStorage` and
`DisabledSagaStorage`, which throw the moment you use them:

> A subscription storage has not been configured. Please configure a subscription storage with the .Subscriptions(...) configurer

`FakeSubscriptionStorage` and `FakeSagaStorage` replace that exception with silence. That suits `Subscribe<T>()` or a saga
running in offline mode, but it also hides a genuine misconfiguration. **Only register them if your code calls into
subscriptions or sagas in the fake configuration** - otherwise leave Rebus's throwing defaults in place.

`FakeTransport` involves no such trade, since Rebus has no throwing default transport.

### Is this useful for testing?

For testing messaging itself, no. Use the [InMemory transport](https://github.com/rebus-org/Rebus/wiki/Transport) to run
handlers, or `Rebus.TestHelpers.FakeBus` to assert which messages were sent. Rebus.Fake discards everything and records
nothing, leaving nothing to assert against.

It does fit one case: booting a real application host where messaging is not under test and the bus should exist but stay
inert. Swapping only the transport leaves the application's Rebus configuration intact, with no delivery, no accumulation,
and no leakage between tests through a shared `InMemNetwork`.

### Scope

Deferred messages need no extra configuration. `bus.Defer(...)` stamps headers and sends like any other message, so the
fake transport discards it; Rebus consults `ITimeoutManager` only when a deferred message is *received*. Registering a
fake timeout manager would make things worse, because Rebus skips its due-messages background poller only for the
disabled default.

Rebus's data bus is **not** covered. `IDataBus.OpenRead` and `GetMetadata` must return what was written, which a
discarding implementation can only do by handing back empty streams that look like real payloads. Rebus's
`DisabledDataBus` throws instead.

## Components

Rebus.Fake provides three no-op implementations:

| Component | Description |
|-----------|-------------|
| **FakeTransport** | Silently discards all sent messages, never receives anything |
| **FakeSubscriptionStorage** | Accepts subscribe/unsubscribe operations but never returns subscribers |
| **FakeSagaStorage** | Accepts saga operations but never persists or retrieves saga data |

`FakeTransport` is the one you always want. The other two are only needed if your code calls into subscriptions or sagas
while faked out - see [When to use the persistence fakes](#when-to-use-the-persistence-fakes).

## Usage

**Prefer the one-way client unless you need an input queue.** `UseFakeTransportAsOneWayClient()` sets Rebus's
`NumberOfWorkers` to 0, so no workers start. `UseFakeTransport(inputQueueName)` starts workers that poll a transport
returning `null` forever. They back off, so the cost is small, but a send-only configuration gains nothing from them.

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

No timeout manager configuration is needed - deferring sends like any other message, so the transport discards it.
`Defer` resolves a destination through routing like `Send`; `DeferLocal` targets the bus's own input queue.

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
