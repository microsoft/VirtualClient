# Copilot Instructions for VirtualClient

## Project Overview

VirtualClient is a cross-platform benchmarking and workload execution framework built by Microsoft. It provides a standardized, profile-driven way to run performance benchmarks, stress tests, and qualification workloads on systems (particularly Azure VMs) and collect structured metrics. It supports Windows and Linux on x64 and ARM64 architectures.

The application is a .NET CLI tool that reads declarative execution profiles (JSON or YAML), installs dependencies, runs workload executors, collects metrics via parsers, and optionally emits telemetry to Azure services.

## Tech Stack and Key Dependencies

- **Runtime**: .NET 9 (SDK 9.0.301 defined in `global.json`, rollForward: feature)
- **Target platforms**: `linux-x64`, `linux-arm64`, `win-x64`, `win-arm64` (self-contained publish)
- **CLI parsing**: `System.CommandLine` (2.0.0-beta1)
- **Serialization**: `Newtonsoft.Json` 13.0.3 (JSON), `YamlDotNet` 15.1.1 (YAML profiles)
- **Dependency injection**: `Microsoft.Extensions.DependencyInjection` 9.0.9
- **Logging**: `Serilog` 9.0.2 + `Serilog.Sinks.File` 6.0.0, custom `EventContext` telemetry
- **Azure integration**: `Azure.Storage.Blobs` 12.18.0, `Azure.Messaging.EventHubs` 5.11.5, `Azure.Security.KeyVault.*` 4.7.0, `Azure.Identity` 1.16.0
- **HTTP resilience**: `Polly` 8.5.0, `Microsoft.Extensions.Http.Polly` 9.0.9
- **SSH**: `SSH.NET` 2024.2.0
- **REST API**: ASP.NET Core (built-in API for client/server coordination)
- **File system abstraction**: `System.IO.Abstractions` 22.0.14
- **Testing**: `NUnit` 3.13.2, `Moq` 4.18.2, `AutoFixture` 4.18.1
- **Code quality**: `StyleCop.Analyzers` 1.1.118, `AsyncFixer` 1.6.0
- **Package versions**: Centrally managed via `Directory.Packages.props` at the repo root

## Repository Structure

```
/
├── src/VirtualClient/
│   ├── VirtualClient.sln                    # Single solution file
│   ├── VirtualClient.Main/                  # Entry point: CLI, command parsing, profiles
│   │   ├── Program.cs                       # Application entry point
│   │   ├── CommandLineParser.cs             # CLI command definitions
│   │   ├── profiles/                        # All built-in execution profiles (JSON/YAML)
│   │   └── ...
│   ├── VirtualClient.Contracts/             # Domain model and base classes
│   │   ├── VirtualClientComponent.cs        # THE base class for all components
│   │   ├── VirtualClientMonitorComponent.cs # Base for monitors
│   │   ├── ExecutionProfile.cs              # Profile model
│   │   ├── Metric.cs                        # Metric data model
│   │   ├── Exceptions.cs                    # Exception hierarchy
│   │   ├── Parser/                          # TextParser<T> and MetricsParser base classes
│   │   └── Extensibility/                   # Data point types for telemetry
│   ├── VirtualClient.Core/                  # Orchestration engine
│   │   ├── ProfileExecutor.cs               # Runs profiles
│   │   ├── PackageManager.cs                # Package download/install
│   │   ├── SystemManagement.cs              # OS abstractions (disk, firewall, SSH)
│   │   ├── Components/                      # Built-in execution components
│   │   └── Logging/                         # Telemetry providers
│   ├── VirtualClient.Common/                # Low-level utilities
│   │   ├── IProcessProxy.cs                 # Process execution abstraction
│   │   ├── ProcessProxy.cs                  # Process wrapper with ConcurrentBuffer
│   │   ├── Platform/                        # SupportedPlatformsAttribute, etc.
│   │   ├── Telemetry/                       # EventContext
│   │   └── Rest/                            # HTTP client utilities
│   ├── VirtualClient.Api/                   # REST API controllers (client/server coordination)
│   ├── VirtualClient.Actions/               # ~40+ workload executors
│   │   ├── OpenSSL/                         # Each subfolder = one workload
│   │   ├── FIO/
│   │   ├── CoreMark/
│   │   ├── DiskSpd/
│   │   ├── Network/NetworkingWorkload/      # Client/server network benchmarks
│   │   └── ...
│   ├── VirtualClient.Dependencies/          # Prerequisite installers (GPU drivers, compilers, disks)
│   ├── VirtualClient.Monitors/              # Background monitors (perf counters, GPU stats)
│   ├── VirtualClient.TestFramework/         # Shared test infrastructure
│   ├── VirtualClient.*.UnitTests/           # Unit test projects
│   ├── VirtualClient.*.FunctionalTests/     # Functional test projects
│   ├── VirtualClient.Examples/              # Extension examples
│   └── VirtualClient.Packaging/             # NuGet packaging
├── website/                                 # Docusaurus documentation site
├── build.cmd / build.sh                     # Build scripts
├── build-test.cmd / build-test.sh           # Test scripts
├── Directory.Packages.props                 # Central NuGet version management
├── global.json                              # .NET SDK version pin
└── VERSION                                  # Build version
```

## Architecture Patterns

### Component Model

All workloads, dependencies, and monitors inherit from `VirtualClientComponent` (in `VirtualClient.Contracts`). This abstract base class provides:

- **Constructor signature**: Always `(IServiceCollection dependencies, IDictionary<string, IConvertible> parameters = null)`
- **Lifecycle methods** (called in order by the base `ExecuteAsync`):
  1. `InitializeAsync(EventContext, CancellationToken)` — virtual, setup logic
  2. `Validate()` — virtual, parameter validation
  3. `ExecuteAsync(EventContext, CancellationToken)` — **abstract**, main workload logic
  4. `CleanupAsync(EventContext, CancellationToken)` — virtual, teardown
- **Key properties**: `Platform`, `CpuArchitecture`, `Scenario`, `PackageName`, `Parameters`, `Logger`, `Dependencies`, `MetadataContract`
- Monitors extend `VirtualClientMonitorComponent` which adds `MonitorFrequency`, `MonitorIterations`, `MonitorWarmupPeriod`, `MonitorStrategy`

### Profile-Driven Execution

Profiles are JSON or YAML files with three sections:
- **Dependencies**: Installers to run before workloads (package downloads, GPU drivers, disk setup)
- **Actions**: Workload executors to run
- **Monitors**: Background monitoring during execution

Each section element has a `Type` (C# class name resolved at runtime) and `Parameters` dictionary. Parameter references use `$.Parameters.Name` syntax. Expression placeholders use `{PropertyName}` syntax within command arguments.

Example profile structure:
```json
{
    "Description": "OpenSSL CPU Performance Workload",
    "Metadata": {
        "RecommendedMinimumExecutionTime": "01:00:00",
        "SupportedPlatforms": "linux-x64,linux-arm64,win-x64"
    },
    "Parameters": {
        "Duration": "00:01:40"
    },
    "Actions": [
        {
            "Type": "OpenSslExecutor",
            "Parameters": {
                "Scenario": "SHA256",
                "CommandArguments": "speed -elapsed -seconds {Duration.TotalSeconds} sha256",
                "Duration": "$.Parameters.Duration",
                "PackageName": "openssl",
                "Tags": "CPU,OpenSSL,Cryptography"
            }
        }
    ],
    "Dependencies": [
        {
            "Type": "DependencyPackageInstallation",
            "Parameters": {
                "Scenario": "InstallOpenSSLPackage",
                "BlobContainer": "packages",
                "BlobName": "openssl.3.0.0.zip",
                "PackageName": "openssl",
                "Extract": true
            }
        }
    ]
}
```

### Dependency Injection

Services are registered in `CommandBase.InitializeDependencies()` and passed as `IServiceCollection` to every component. Components resolve services via extension methods:
```csharp
// Direct resolution
this.fileSystem = dependencies.GetService<IFileSystem>();
this.systemManagement = dependencies.GetService<ISystemManagement>();

// Safe resolution
if (dependencies.TryGetService<EnvironmentLayout>(out EnvironmentLayout layout)) { ... }
```

Key registered services: `ISystemManagement`, `IFileSystem`, `IDiskManager`, `IFirewallManager`, `IPackageManager`, `IProfileManager`, `IApiClientManager`, `IExpressionEvaluator`, `IEnumerable<IBlobManager>`, `PlatformSpecifics`, `ILogger`.

### Process Execution

External workload binaries are executed through the `IProcessProxy` abstraction (wraps `System.Diagnostics.Process`). Output is captured via `ConcurrentBuffer` for both stdout and stderr. The `ISystemManagement.ProcessManager` creates process proxies. In tests, `InMemoryProcess` is used as a test double.

### Output Parsing and Metrics

Each workload has a parser that extracts structured `Metric` objects from raw benchmark output:
- Parsers inherit from `MetricsParser` (which extends `TextParser<IList<Metric>>`)
- Override `Parse()` (required) and optionally `Preprocess()` for text normalization
- Use regex patterns (defined as `private static readonly Regex`) to extract data
- Use `TextParsingExtensions.Sectionize()` to split output into logical sections
- Return `IList<Metric>` where each `Metric` has: `Name`, `Value`, `Unit`, `Relativity`, `Tags`, `Metadata`

Metrics are logged via:
```csharp
this.Logger.LogMetrics("ToolName", scenario, startTime, endTime, metrics, relatedContext: telemetryContext);
```

### Client/Server Architecture

For network and database workloads, VirtualClient supports multi-role execution:
- One instance runs as **server**, another as **client**
- They coordinate via the built-in REST API (`VirtualClient.Api` project) for state synchronization and heartbeat
- Components use `Polly` retry policies for resilience
- The `EnvironmentLayout` defines the topology of instances

### Error Handling

Custom exception hierarchy rooted at `VirtualClientException`:
- `WorkloadResultsException` — parsing failures, missing results
- `MonitorException` — monitor failures
- `ApiException` — API communication failures
- `ComponentException` — general component failures
- `StartupException`, `DependencyException`, `ProcessException`

All exceptions carry an `ErrorReason` enum value (e.g., `WorkloadResultsParsingFailed`, `DependencyNotFound`, `WorkloadFailed`). Error reasons ≥500 are fatal; 400–499 are potentially transient.

## Coding Standards and Conventions

### File Header
Every `.cs` file starts with:
```csharp
// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
```

### Namespace and Using Style
- **Using statements go inside the namespace block** (not at file top)
- Ordering: `System.*` → `Microsoft.*` → `Newtonsoft.*` → `VirtualClient.*`
- Namespace matches folder structure: `VirtualClient.Actions`, `VirtualClient.Contracts`, etc.

```csharp
namespace VirtualClient.Actions
{
    using System;
    using System.Collections.Generic;
    using Microsoft.Extensions.DependencyInjection;
    using VirtualClient.Common;
    using VirtualClient.Contracts;
```

### Naming Conventions
- **Classes**: PascalCase, suffixed by role (`OpenSslExecutor`, `DiskSpdMetricsParser`, `CoreMarkExecutor`)
- **Properties**: PascalCase (`CommandLine`, `MetricScenario`, `MonitorEnabled`)
- **Private fields**: camelCase, no prefix for instance fields, `const` fields use PascalCase
  ```csharp
  private IFileSystem fileSystem;
  private ISystemManagement systemManagement;
  private const string CoreMarkOutputFile1 = "run1.log";
  ```
- **Parameters dictionary keys**: PascalCase, accessed case-insensitively via `StringComparer.OrdinalIgnoreCase`
- **Async methods**: Suffixed with `Async` (`ExecuteAsync`, `InitializeAsync`, `CleanupAsync`)
- **Test classes**: `{ComponentName}Tests` (e.g., `FioExecutorTests`, `CoreMarkExecutorTests`)
- **Test methods**: Descriptive with underscores for scenario separation: `FioExecutorSelectsTheExpectedDisksForTest_RemoteDiskScenario`

### Property Pattern for Profile Parameters
Properties that read from the `Parameters` dictionary follow this pattern:
```csharp
public string CommandArguments
{
    get
    {
        return this.Parameters.GetValue<string>(nameof(this.CommandArguments));
    }
}

// With default value:
public string CompilerName
{
    get
    {
        return this.Parameters.GetValue<string>(nameof(this.CompilerName), string.Empty);
    }
}
```

### XML Documentation
All public members have XML doc comments using `<summary>`, `<param>`, `<returns>`, `<remarks>`, and `<inheritdoc />` tags:
```csharp
/// <summary>
/// Executes the OpenSSL workload.
/// </summary>
/// <param name="dependencies">Provides required dependencies to the component.</param>
/// <param name="parameters">Parameters defined in the profile or supplied on the command line.</param>
```

### Platform Support Attribute
Executors declare supported platforms via a class-level attribute:
```csharp
[SupportedPlatforms("linux-arm64,linux-x64,win-x64")]
public class OpenSslExecutor : VirtualClientComponent
```

### Code Quality
- **StyleCop.Analyzers** enforces style rules (suppressed: SA1204 static element ordering)
- **AsyncFixer** validates async patterns (suppressed: AZCA1002 async method naming)
- Central package version management prevents version drift across projects

## Executor Implementation Checklist

When adding a new workload executor:

1. Create a subfolder under `VirtualClient.Actions/` named after the workload
2. Create an executor class inheriting `VirtualClientComponent`
3. Add `[SupportedPlatforms("...")]` attribute
4. Define constructor with `(IServiceCollection dependencies, IDictionary<string, IConvertible> parameters)`
5. Expose profile parameters as properties reading from `this.Parameters`
6. Override `InitializeAsync` for setup (locate package, set executable path)
7. Override `ExecuteAsync` for the main workload logic (execute process, capture output, parse, log metrics)
8. Optionally override `CleanupAsync` and `Validate`
9. Create a `MetricsParser` subclass to parse workload output into `IList<Metric>`
10. Create an execution profile JSON in `VirtualClient.Main/profiles/`
11. Add unit tests inheriting from `MockFixture` in the corresponding `.UnitTests` project
12. Add example output files under `TestResources/` for parser tests

## Testing Philosophy and Patterns

### Framework
- **NUnit 3** with `[TestFixture]`, `[Test]`, `[SetUp]`, `[OneTimeSetUp]` attributes
- **Moq** for mocking interfaces
- **AutoFixture** via `MockFixture` base class for test data generation
- Tests are categorized: `[Category("Unit")]` or `[Category("Functional")]`

### MockFixture Base Class
Test classes inherit from `MockFixture` (in `VirtualClient.TestFramework`), which provides:
- Pre-configured mock services: `ApiClient`, `DiskManager`, `FileSystem`, `File`, `Directory`, `ProcessManager`
- `Setup(PlatformID)` method to configure platform-specific behavior
- `MockFixture.ReadFile(...)` to load example output from `TestResources/Examples/`
- `InMemoryProcess`, `InMemoryFile`, `InMemoryDirectory` test doubles

### Test Structure Pattern
```csharp
[TestFixture]
[Category("Unit")]
public class MyExecutorTests : MockFixture
{
    private IDictionary<string, IConvertible> profileParameters;
    private string mockResults;

    [OneTimeSetUp]
    public void SetupFixture()
    {
        // Load example output files (one-time)
        this.mockResults = MockFixture.ReadFile(MockFixture.ExamplesDirectory, "MyWorkload", "Results.json");
    }

    [SetUp]
    public void SetupTest()
    {
        this.Setup(PlatformID.Unix);
        // Configure mocks for each test
        this.ProcessManager.OnCreateProcess = (command, arguments, workingDir) =>
        {
            return new InMemoryProcess
            {
                OnHasExited = () => true,
                ExitCode = 0,
                StartInfo = new ProcessStartInfo { FileName = command, Arguments = arguments },
                StandardOutput = new ConcurrentBuffer(new StringBuilder(this.mockResults))
            };
        };
    }

    [Test]
    public async Task MyExecutorExecutesTheExpectedCommandOnLinux()
    {
        // Arrange, Act, Assert
    }
}
```

### Parser Tests
Parser tests load real example output (stored in `TestResources/`), run the parser, and assert against expected metric names, values, and units. This ensures parsers remain correct as output formats evolve.

## Build and Test Commands

### Build
```bash
# Linux — builds solution (Debug) then publishes self-contained for all platforms (Release)
./build.sh

# Build for specific platform only
./build.sh --linux-x64
./build.sh --win-x64 --linux-arm64

# Windows
build.cmd
build.cmd --win-x64
```

The build first compiles the solution in Debug configuration (for extension debugging), then publishes runtime-specific self-contained binaries in Release.

### Test
```bash
# Linux — runs all Unit tests
./build-test.sh

# Windows — runs Unit + Functional tests
build-test.cmd
```

Tests are discovered from `*Tests.csproj` files and filtered by `Category=Unit` (Linux) or `Category=Unit|Category=Functional` (Windows).

### Direct dotnet commands
```bash
# Build solution only
dotnet build src/VirtualClient/VirtualClient.sln -c Debug

# Run a specific test project
dotnet test src/VirtualClient/VirtualClient.Actions.UnitTests/VirtualClient.Actions.UnitTests.csproj -c Debug --filter "Category=Unit"

# Publish for a specific runtime
dotnet publish src/VirtualClient/VirtualClient.Main/VirtualClient.Main.csproj -r linux-x64 -c Release --self-contained
```

### Version
Build version is read from the `VERSION` file at the repo root. Override with the `VCBuildVersion` environment variable.
