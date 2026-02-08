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

I've found this useful for scenarios where I'm forced to inject a Rebus instance but I don't care about the functionality using
it at all. I also don't want messages that I know will never be consumed, getting collected in-memory.

**Example use case:** An application intended for both online and offline use. When hosted in an online environment it communicates with external services using Rebus, but when hosted offline and those external services are not needed, it's simpler to inject Rebus with fake components and drop all messages that the application attempts to send.

**Important:** The official core [InMemory transport](https://github.com/rebus-org/Rebus/wiki/Transport) is better for testing. I do **NOT** recommend using Rebus.Fake for unit/integration tests. Use Rebus.Fake for production scenarios where messages should be discarded.

## Components

Rebus.Fake provides three no-op implementations:

| Component | Description |
|-----------|-------------|
| **FakeTransport** | Silently discards all sent messages, never receives anything |
| **FakeSubscriptionStorage** | Accepts subscribe/unsubscribe operations but never returns subscribers |
| **FakeSagaStorage** | Accepts saga operations but never persists or retrieves saga data |

## Usage

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

## License

MIT
