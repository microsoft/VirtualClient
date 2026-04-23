# Copilot Instructions for VirtualClient

## Project Overview

VirtualClient is a cross-platform benchmarking framework by Microsoft. It runs profile-driven performance
benchmarks, stress tests, and qualification workloads on systems (particularly Azure VMs), collecting
structured metrics. Supports Windows and Linux on x64 and ARM64.

## Tech Stack

- **Runtime**: .NET 9 (SDK 9.0.301, `global.json`)
- **Platforms**: `linux-x64`, `linux-arm64`, `win-x64`, `win-arm64`
- **Serialization**: `Newtonsoft.Json` 13.0.3, `YamlDotNet` 15.1.1
- **DI**: `Microsoft.Extensions.DependencyInjection` 9.0.9
- **Logging**: `Serilog.Extensions.Logging` 9.0.2 (NOT `Serilog`)
- **Testing**: NUnit 3.13.2, Moq 4.18.2, AutoFixture 4.18.1
- **Code quality**: StyleCop.Analyzers 1.1.118, AsyncFixer 1.6.0
- **Package versions**: Centrally managed via `Directory.Packages.props`

## Repository Structure

```
src/VirtualClient/
├── VirtualClient.Main/           # CLI entry point, profiles/
├── VirtualClient.Contracts/      # Base classes (VirtualClientComponent), Metric, Parser/
├── VirtualClient.Core/           # ProfileExecutor, PackageManager, SystemManagement
├── VirtualClient.Common/         # IProcessProxy, ConcurrentBuffer, Telemetry/EventContext
├── VirtualClient.Api/            # REST API for client/server coordination
├── VirtualClient.Actions/        # ~40+ workload executors (OpenSSL/, FIO/, DiskSpd/, ...)
├── VirtualClient.Dependencies/   # Prerequisite installers
├── VirtualClient.Monitors/       # Background monitors
├── VirtualClient.TestFramework/  # MockFixture, InMemoryProcess, test doubles
└── VirtualClient.*.UnitTests/    # Unit test projects
```

## Architecture Patterns

### Component Model

All workloads, dependencies, monitors inherit `VirtualClientComponent`.
Constructor: `(IServiceCollection, IDictionary<string, IConvertible>)`.
Lifecycle: `IsSupported` → `InitializeAsync` → `Validate` → `ExecuteAsync` → `CleanupAsync`.

### Process Execution

External binaries run through `IProcessProxy` (wraps `System.Diagnostics.Process`).
Output captured via `ConcurrentBuffer`. Tests use `InMemoryProcess`.

## Build and Test

```bash
# Build
./build.sh                    # or: dotnet build src/VirtualClient/VirtualClient.sln -c Debug

# Test
./build-test.sh               # or: dotnet test <project>.csproj -c Debug --filter "Category=Unit"

# Publish
dotnet publish src/VirtualClient/VirtualClient.Main/VirtualClient.Main.csproj -r linux-x64 -c Release --self-contained
```

Version is read from the `VERSION` file. Override with `VCBuildVersion` env var.
