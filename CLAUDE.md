# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

Rebus.Fake is a NuGet package that provides "no-op" implementations of Rebus components (Transport, SubscriptionStorage, SagaStorage) that silently discard all operations. This is useful for scenarios where a Rebus instance must be injected but messaging functionality is not needed (e.g., offline mode in applications designed for both online and offline use).

**Important**: This is NOT intended for testing. Use Rebus's official InMemory transport for tests. FakeTransport is specifically for production scenarios where messages should be discarded.

## Solution Structure

```
src/Rebus.Fake/          # Main library (targets netstandard2.0)
├── Config/              # Extension methods for Rebus configuration
├── Transport/Fake/      # FakeTransport implementation
└── Persistence/Fake/    # FakeSubscriptionStorage and FakeSagaStorage

tests/Rebus.Fake.Tests/  # xUnit tests (targets net8.0 and net9.0)
```

## Build Commands

```bash
# Restore dependencies
dotnet restore

# Build the solution
dotnet build -c Release

# Run all tests
dotnet test -c Release

# Run a specific test
dotnet test --filter "FullyQualifiedName~FakeTransportTests.BusUsingFakeTransport_Send_DoesNotDeliverMessages"

# Pack NuGet package
dotnet pack -c Release -p:PackageOutputPath="./artifacts/"
```

## Architecture

### Core Components

**FakeTransport** (`Transport/Fake/FakeTransport.cs`):
- Inherits from `AbstractRebusTransport`
- Implements `ITransport` and `ITransportInspector`
- `CreateQueue()` is a no-op
- `Receive()` always returns null (no messages)
- `SendOutgoingMessages()` silently discards all messages
- Reports queue length as 0

**FakeSubscriptionStorage** (`Persistence/Fake/FakeSubscriptionStorage.cs`):
- Implements `ISubscriptionStorage`
- `GetSubscriberAddresses()` always returns empty list
- `RegisterSubscriber()` and `UnregisterSubscriber()` are no-ops
- `IsCentralized` is true

**FakeSagaStorage** (`Persistence/Fake/FakeSagaStorage.cs`):
- Implements `ISagaStorage`
- `Find()` always returns null (no sagas found)
- `Insert()`, `Update()`, and `Delete()` are no-ops

### Configuration Extensions

All configuration is done through extension methods in `Config/StandardConfigurerExtensions.cs`:

- `UseFakeTransport(inputQueueName)` - Configures bidirectional bus with fake transport
- `UseFakeTransportAsOneWayClient()` - Configures one-way client (send-only)
- `UseFakeSubscriptionStorage()` - Registers fake subscription storage
- `UseFakeSagaStorage()` - Registers fake saga storage

## Versioning

- Uses GitVersion for automatic versioning (see `GitVersion.yml`)
- Version numbers are determined from git tags and branch names
- CI/CD publishes to NuGet on pushes to main/master or version tags

## Testing

Tests use:
- **xUnit** as the test framework
- **Hypothesist.Rebus** for testing observable behavior (verifying messages are NOT received)
- Tests verify that operations don't throw exceptions and that messages are properly discarded

Test pattern: Tests create hypotheses that exactly 0 messages are received within a short timeout (0.5s), then send messages and verify the hypothesis holds.

## CI/CD

GitHub Actions workflow (`.github/workflows/dotnet.yml`):
1. Runs on pushes to main/master or version tags
2. Uses GitVersion to determine package version
3. Builds with .NET 8.x and 9.x
4. Runs tests (excluding IntegrationTests filter)
5. Packs and publishes to NuGet

## Dependencies

- **Rebus**: Version 8.0.1 or higher (main dependency)
- Uses C# latest language version with nullable reference types enabled
- Targets netstandard2.0 for broad compatibility
