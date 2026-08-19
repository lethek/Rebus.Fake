# Changelog

Notable changes for consumers of the `Rebus.Fake` package. Prerelease `-ci.*` builds are published per commit and are
not listed here.

## 2.1.1 - 2026-08-19

- Package README and description rewritten: scope, when to use this instead of Rebus's InMemory
  transport or `FakeBus`, and deferred-message behaviour.
- No code changes; the assembly is identical to 2.1.0.

## 2.1.0 - 2026-08-18

- XML documentation now ships with the package, so all public types and members show IntelliSense.
- No API changes.

## 2.0.0 - 2025-09-23

- **Breaking:** requires Rebus 8.0.1 or later (was 6.0.0).
- **Breaking for subclasses:** `FakeTransport.SendOutgoingMessages` now takes `OutgoingTransportMessage`
  instead of `OutgoingMessage`, following Rebus 8.
- No other API changes.

## 1.1.0 - 2023-03-02

- Added `FakeSagaStorage` and `UseFakeSagaStorage()`. `Find` returns null, so every message starts a fresh saga.

## 1.0.0 - 2023-02-24

Initial release. Requires Rebus 6.0.0 or later, targets `netstandard2.0`, ships symbols and Source Link.

- `FakeTransport` - discards sent messages, never receives. Also implements `ITransportInspector`, reporting a
  queue length of 0.
- `FakeSubscriptionStorage` - accepts subscribe/unsubscribe, never returns subscribers. `IsCentralized` is true.
- `UseFakeTransport(inputQueueName)`, `UseFakeTransportAsOneWayClient()`, `UseFakeSubscriptionStorage()`.
