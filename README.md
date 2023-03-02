# Rebus.Fake

[![install from nuget](https://img.shields.io/nuget/v/Rebus.Fake.svg?style=flat-square)](https://www.nuget.org/packages/Rebus.Fake)

Transport, SubscriptionStorage and SagaStorage for [Rebus](https://github.com/rebus-org/Rebus) that don't actually send/receive
anything at all. Kind-of like `/dev/null` in Linux.

# Why?

I've found this useful for scenarios where I'm forced to inject a Rebus instance but I don't care about the functionality using
it at all. I also don't want messages that I know will never be consumed, getting collected in-memory.

E.g. an application intended for both online and offline use: when hosted in an online environment it communicates with a
number of external services using Rebus, but when it's hosted offline and those external services are not needed it is simple
to just inject Rebus with a FakeTransport and drop all messages that the application attempts to send.

*The official InMemory transport etc is better for testing. I do NOT recommend using FakeTransport for that.*

# How

```csharp
using var bus = Configure.With(...)
    .(...)
    .Transport(c => c.UseFakeTransportAsOneWayClient())
    .Start();
```

Or if the bus is expected to be bi-directional and subscribing to topics:

```csharp
using var bus = Configure.With(...)
    .(...)
    .Transport(c => c.UseFakeTransport("inputQueueName"))
    .Subscriptions(c => c.UseFakeSubscriptionStorage())
    .Start();
```
