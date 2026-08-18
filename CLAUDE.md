# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

Rebus.Fake is a NuGet package that provides "no-op" implementations of Rebus components (Transport, SubscriptionStorage, SagaStorage) that silently discard all operations. This is useful for scenarios where a Rebus instance must be injected but messaging functionality is not needed (e.g., offline mode in applications designed for both online and offline use).

**Important**: This is NOT intended for testing. Use Rebus's official InMemory transport for tests. FakeTransport is specifically for production scenarios where messages should be discarded.

## Solution Structure

The solution uses the XML-based `.slnx` format (`Rebus.Fake.slnx`), which requires SDK 9.0.200 or newer.

```
src/Rebus.Fake/          # Main library (targets netstandard2.0)
├── Config/              # Extension methods for Rebus configuration
├── Transport/Fake/      # FakeTransport implementation
└── Persistence/Fake/    # FakeSubscriptionStorage and FakeSagaStorage

tests/Rebus.Fake.Tests/  # xUnit tests (targets net8.0 and net10.0)
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

### API Documentation

The library sets `GenerateDocumentationFile` with `WarningsAsErrors=CS1591`, so every publicly visible type and member must carry an XML doc comment. Adding an undocumented public API fails the build.

### Configuration Extensions

All configuration is done through extension methods in `Config/StandardConfigurerExtensions.cs`:

- `UseFakeTransport(inputQueueName)` - Configures bidirectional bus with fake transport
- `UseFakeTransportAsOneWayClient()` - Configures one-way client (send-only)
- `UseFakeSubscriptionStorage()` - Registers fake subscription storage
- `UseFakeSagaStorage()` - Registers fake saga storage

## Versioning

- Uses GitVersion 6 for automatic versioning (see `GitVersion.yml`)
- Version numbers are determined from git tags and branch names
- The package version comes from GitVersion's `semVer` output (e.g. `2.0.1-ci.7`). GitVersion 6 removed the `NuGetVersion` variable that earlier versions used
- `mode: ContinuousDelivery` is what GitVersion 5 called `ContinuousDeployment`; the modes were renamed in v6, and v6's `ContinuousDeployment` produces stable (non-prerelease) versions instead
- CI/CD publishes to NuGet on pushes to main/master or version tags

## Testing

Tests use:
- **xUnit** as the test framework
- **Hypothesist.Rebus** for testing observable behavior (verifying messages are NOT received)
- Tests verify that operations don't throw exceptions and that messages are properly discarded

Test pattern: Tests create hypotheses that exactly 0 messages are received within a short timeout (0.5s), then send messages and verify the hypothesis holds.

## CI/CD

GitHub Actions workflow (`.github/workflows/dotnet.yml`):
1. Runs on pushes to main/master, version tags, and pull requests against main/master
2. Uses GitVersion to determine package version
3. Builds with .NET 8.x and 10.x
4. Runs tests (excluding IntegrationTests filter)
5. Packs, uploads the packages as build artifacts, and publishes to NuGet

Publishing uses [NuGet Trusted Publishing](https://learn.microsoft.com/en-us/nuget/nuget-org/trusted-publishing) rather than a long-lived API key. The `NuGet/login` step exchanges a GitHub OIDC token (hence `id-token: write`) for an API key valid for 1 hour, so it must stay immediately before the push step. The only secret involved is `NUGET_USER`, the nuget.org profile name. The matching trusted publishing policy is registered on nuget.org against repository owner `lethek`, repository `Rebus.Fake`, and workflow file `dotnet.yml`; renaming the workflow file breaks publishing until the policy is updated.

Publishing is controlled by the `publishEnabled` variable in the workflow's `env` block. While it is `'false'` the push step is skipped; set it to `'true'` to publish. The push step additionally only runs for `push` events, so pull requests can never publish.

## Dependencies

- **Rebus**: Version 8.0.1 or higher (main dependency)
- Uses C# latest language version with nullable reference types enabled
- Targets netstandard2.0 for broad compatibility
