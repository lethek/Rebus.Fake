# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

Rebus.Fake is a NuGet package that provides "no-op" implementations of Rebus components (Transport, SubscriptionStorage, SagaStorage) that silently discard all operations. This is useful for scenarios where a Rebus instance must be injected but messaging functionality is not needed (e.g., offline mode in applications designed for both online and offline use).

**Important**: This is not a testing tool. Use Rebus's official InMemory transport to run handlers, or `Rebus.TestHelpers.FakeBus` to assert which messages were sent. Rebus.Fake targets production scenarios where messages should be discarded; the one testing case it suits is a host test where the bus must exist but stay inert. See the README for the full comparison.

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

# Run a specific test (TUnit uses --treenode-filter; VSTest's --filter is not supported)
dotnet test --treenode-filter "/*/*/FakeTransportTests/*"

# Run with coverage (one report per target framework, under bin/<tfm>/TestResults)
dotnet test -c Release --coverage --coverage-output-format cobertura

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

### Scope Boundaries

Do not add fakes for these without re-checking the reasoning in README.md:

- **`ITimeoutManager`**: not needed. `bus.Defer` stamps headers and sends, so `FakeTransport` discards it; the timeout manager is only consulted by `HandleDeferredMessagesStep`, an *incoming* pipeline step. Registering a fake would also start Rebus's due-messages background poller, which it skips only when the timeout manager is the `DisabledTimeoutManager` default.
- **`IDataBus`**: excluded. `OpenRead`/`GetMetadata` must return what was written, which a discarding implementation can only fake by returning empty streams that look like real payloads. Rebus's `DisabledDataBus` throws instead.
- **`FakeSubscriptionStorage` / `FakeSagaStorage`**: these replace Rebus's throwing `DisabledSubscriptionStorage` and `DisabledSagaStorage` defaults, so registering them silences a real diagnostic. Use them only when the application calls into subscriptions or sagas while faked out.

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
- **TUnit** as the test framework, running on Microsoft.Testing.Platform rather than VSTest
- **Hypothesist.Rebus** for testing observable behavior (verifying messages are NOT received)
- Tests verify that operations don't throw exceptions and that messages are properly discarded

TUnit specifics worth knowing before editing tests:
- The test project is an `Exe` (`OutputType`), and must not reference `Microsoft.NET.Test.Sdk` or `coverlet.collector` - both break test discovery.
- `global.json` opts `dotnet test` into MTP mode. Without it, the .NET 10 SDK fails with "Testing with VSTest target is no longer supported".
- `[Test]` replaces `[Fact]`; assertions are awaited: `await Assert.That(actual).IsEqualTo(expected)`. A test containing an assertion must be `async Task`.
- `--filter` is silently rejected and reports "Zero tests ran". Use `--treenode-filter "/*/*/Class/Test"`.
- Coverage comes from `Microsoft.Testing.Extensions.CodeCoverage` via `--coverage`, not coverlet. It counts compiler-generated closure classes, so lambdas registered but never resolved show as uncovered - which is how the missing `ITransportInspector` test was found.
- Tests run in parallel by default. Each test builds its own bus and `InMemNetwork`, so they stay isolated; anything sharing state needs `[NotInParallel]`.

Test pattern: Tests create hypotheses that exactly 0 messages are received within a short timeout (0.5s), then send messages and verify the hypothesis holds.

`Exactly(n)` cannot complete early, since ruling out an `n+1`th message means waiting out the whole timebox. Tests asserting that something *did* happen use `AtLeast(n)` instead, which completes as soon as the target is reached - see `BusUsingFakeSagaStorage_DoesNotPersistSagaBetweenMessages`. Bound every wait so a broken test fails rather than hangs.

Tests that assert a fake replaces a throwing Rebus default (subscriptions, sagas) should be checked by temporarily swapping in the real implementation and confirming the test fails. Several of these pass vacuously otherwise.

## CI/CD

GitHub Actions workflow (`.github/workflows/dotnet.yml`):
1. Runs on pushes to main/master, version tags, and pull requests against main/master
2. Uses GitVersion to determine package version
3. Builds with .NET 8.x and 10.x
4. Runs tests with `--coverage --coverage-output-format cobertura`
5. Merges both frameworks' Cobertura reports with ReportGenerator, writes the result to the job summary, and comments it on pull requests
6. Packs, uploads the packages as build artifacts, and publishes to NuGet

CI deliberately does **not** pass `--coverage-output`. A single path makes both target frameworks write to the same file, so whichever finishes last silently overwrites the other - which would hide per-framework differences the moment the library multi-targets or gains `#if` blocks. Left to the default, each framework writes into its own `bin/<tfm>/TestResults`, and ReportGenerator merges them (the summary then reports `MultiReport (2x Cobertura)`). Note that `--coverage-output` also rejects a directory path, throwing `DirectoryNotFoundException`.

Coverage steps use `if: ${{ !cancelled() }}` so a failing test run still reports coverage. The PR comment uses `gh pr comment --edit-last --create-if-none` to update a single comment rather than adding one per push; it cannot post on pull requests from forks, which only get a read-only token, so that step is `continue-on-error`.

Publishing uses [NuGet Trusted Publishing](https://learn.microsoft.com/en-us/nuget/nuget-org/trusted-publishing) rather than a long-lived API key. The `NuGet/login` step exchanges a GitHub OIDC token (hence `id-token: write`) for an API key valid for 1 hour, so it must stay immediately before the push step. The only secret involved is `NUGET_USER`, the nuget.org profile name. The matching trusted publishing policy is registered on nuget.org against repository owner `lethek`, repository `Rebus.Fake`, and workflow file `dotnet.yml`; renaming the workflow file breaks publishing until the policy is updated.

Publishing is controlled by the `publishEnabled` variable in the workflow's `env` block; set it to `'false'` to skip the login and push steps. Those steps also require a `push` event, so pull requests can never publish, and they skip entirely when the `NUGET_USER` secret is absent, which keeps a fork or a copy of this workflow green without publishing configured. `NUGET_USER` is read into a job-level `env` entry because the `secrets` context is not available in the workflow-level `env` block.

## Dependencies

- **Rebus**: Version 8.0.1 or higher (main dependency)
- Uses C# latest language version with nullable reference types enabled
- Targets netstandard2.0 for broad compatibility
