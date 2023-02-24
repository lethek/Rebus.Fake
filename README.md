# Rebus.Fake

[![install from nuget](https://img.shields.io/nuget/v/Rebus.Fake.svg?style=flat-square)](https://www.nuget.org/packages/Rebus.Fake)

Transport and SubscriptionStorage for [Rebus](https://github.com/rebus-org/Rebus) that doesn't actually send/receive anything
at all. Kind-of like `/dev/null` in Linux.

Note: I haven't really figured out how to unit-test this properly yet. I'm not very familiar with Rebus internals and this
project was just quickly thrown together from a refactoring of several other projects that I've extracted my FakeTransport and
FakeSubscriptionStorage classes from. What few tests are currently in this project do not have any assertions and are more
testing the fact that nothing errors right now.

# Why?

The official InMemory transport etc is better for testing. I do NOT recommend using FakeTransport for that.

I've found this useful for scenarios where I'm forced to inject a Rebus instance but I don't care about the functionality using
it at all.

E.g. an application intended for both online and offline use: when hosted in an online environment it communicates with a
number of external services using Rebus, but when it's hosted offline and those external services are not needed it is simple
to just inject Rebus with a FakeTransport and ignore any messages which the application attempts to send.

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
    .Transport(c => c.UseFakeTransport())
    .Subscriptions(c => c.UseFakeSubscriptionStorage())
	.Start();
```
