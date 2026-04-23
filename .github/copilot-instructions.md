# Copilot Instructions for VirtualClient

## Project Overview

VirtualClient is a cross-platform benchmarking and workload execution framework built by Microsoft. It provides a standardized, profile-driven way to run performance benchmarks, stress tests, and qualification workloads on systems (particularly Azure VMs) and collect structured metrics. It supports Windows and Linux on x64 and ARM64 architectures.

The application is a .NET CLI tool that reads declarative execution profiles (JSON or YAML), installs dependencies, runs workload executors, collects metrics via parsers, and optionally emits telemetry to Azure services.

## Tech Stack and Key Dependencies

<!-- All versions sourced from Directory.Packages.props and global.json -->
- **Runtime**: .NET 9 (SDK 9.0.301 defined in `global.json`, rollForward: feature)
- **Target platforms**: `linux-x64`, `linux-arm64`, `win-x64`, `win-arm64` (self-contained publish; see `build.sh` / `build.cmd`)
- **CLI parsing**: `System.CommandLine` 2.0.0-beta1
- **Serialization**: `Newtonsoft.Json` 13.0.3 (JSON), `YamlDotNet` 15.1.1 (YAML profiles)
- **Dependency injection**: `Microsoft.Extensions.DependencyInjection` 9.0.9
- **Logging**: `Serilog.Extensions.Logging` 9.0.2 + `Serilog.Sinks.File` 6.0.0, custom `EventContext` telemetry (`VirtualClient.Common/Telemetry/EventContext.cs`)
- **Azure integration**: `Azure.Storage.Blobs` 12.18.0, `Azure.Messaging.EventHubs` 5.11.5, `Azure.Security.KeyVault.*` 4.7.0, `Azure.Identity` 1.16.0
- **HTTP resilience**: `Polly` 8.5.0, `Microsoft.Extensions.Http.Polly` 9.0.9
- **SSH**: `SSH.NET` 2024.2.0
- **REST API**: ASP.NET Core (built-in API for client/server coordination; see `VirtualClient.Api/`)
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

<!-- See: VirtualClient.Contracts/VirtualClientComponent.cs -->
All workloads, dependencies, and monitors inherit from `VirtualClientComponent` (in `VirtualClient.Contracts/VirtualClientComponent.cs`). This abstract base class provides:

- **Constructor signature**: Always `(IServiceCollection dependencies, IDictionary<string, IConvertible> parameters = null)` — see line 85 of `VirtualClientComponent.cs`
- **Lifecycle methods** (called in order by the base `ExecuteAsync` at line 713):
  1. `InitializeAsync(EventContext, CancellationToken)` — virtual, setup logic (line 861)
  2. `Validate()` — virtual, parameter validation (line 929)
  3. `ExecuteAsync(EventContext, CancellationToken)` — **abstract**, main workload logic (line 856)
  4. `CleanupAsync(EventContext, CancellationToken)` — virtual, teardown (line 814)
- **Key properties**: `Platform`, `CpuArchitecture`, `Scenario`, `PackageName`, `Parameters`, `Logger`, `Dependencies`, `MetadataContract`
- Monitors extend `VirtualClientMonitorComponent` (in `VirtualClient.Contracts/VirtualClientMonitorComponent.cs`) which adds `MonitorFrequency`, `MonitorIterations`, `MonitorWarmupPeriod`, `MonitorStrategy`

### Profile-Driven Execution

Profiles are JSON or YAML files with three sections:
- **Dependencies**: Installers to run before workloads (package downloads, GPU drivers, disk setup)
- **Actions**: Workload executors to run
- **Monitors**: Background monitoring during execution

Each section element has a `Type` (C# class name resolved at runtime) and `Parameters` dictionary. Parameter references use `$.Parameters.Name` syntax. Expression placeholders use `{PropertyName}` syntax within command arguments.

<!-- Taken verbatim from src/VirtualClient/VirtualClient.Main/profiles/PERF-CPU-OPENSSL.json -->
Example profile structure (abbreviated from `PERF-CPU-OPENSSL.json`):
```json
{
    "Description": "OpenSSL CPU Performance Workload",
    "Metadata": {
        "RecommendedMinimumExecutionTime": "01:00:00",
        "SupportedPlatforms": "linux-x64,linux-arm64,win-x64",
        "SupportedOperatingSystems": "AzureLinux,CentOS,Debian,RedHat,Suse,Ubuntu,Windows"
    },
    "Parameters": {
        "Duration": "00:01:40"
    },
    "Actions": [
        {
            "Type": "OpenSslExecutor",
            "Parameters": {
                "Scenario": "MD5",
                "MetricScenario": "md5",
                "CommandArguments": "speed -elapsed -seconds {Duration.TotalSeconds} md5",
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

<!-- See: VirtualClient.Main/CommandBase.cs lines 767–898 -->
Services are registered in `CommandBase.InitializeDependencies()` (`VirtualClient.Main/CommandBase.cs:767`) and passed as `IServiceCollection` to every component. Components resolve services via extension methods:
```csharp
// Direct resolution (e.g. OpenSslExecutor.cs:48–49)
this.fileSystem = dependencies.GetService<IFileSystem>();
this.systemManagement = dependencies.GetService<ISystemManagement>();

// Safe resolution (e.g. VirtualClientComponent.cs:118)
if (dependencies.TryGetService<EnvironmentLayout>(out EnvironmentLayout layout)) { ... }
```

Key registered services (from `CommandBase.cs:851–868`): `PlatformSpecifics`, `IApiManager`, `IApiClientManager`, `IConfiguration`, `IDiskManager`, `IExpressionEvaluator`, `IEnumerable<IBlobManager>`, `IFileSystem`, `IFirewallManager`, `IPackageManager`, `IProfileManager`, `IStateManager`, `ISystemInfo`, `ISystemManagement`, `ProcessManager`, `ILogger`.

### Process Execution

<!-- See: VirtualClient.Common/IProcessProxy.cs, VirtualClient.Common/ProcessProxy.cs, VirtualClient.Common/ConcurrentBuffer.cs -->
External workload binaries are executed through the `IProcessProxy` abstraction (`VirtualClient.Common/IProcessProxy.cs`), which wraps `System.Diagnostics.Process`. Output is captured via `ConcurrentBuffer` (`VirtualClient.Common/ConcurrentBuffer.cs`) for both `StandardOutput` and `StandardError` (see `ProcessProxy.cs:40–41`). The `ProcessManager` creates process proxies. In tests, `InMemoryProcess` (`VirtualClient.TestFramework/InMemoryProcess.cs`) is used as a test double.

### Output Parsing and Metrics

Each workload has a parser that extracts structured `Metric` objects from raw benchmark output:
<!-- See: VirtualClient.Contracts/Parser/MetricsParser.cs, VirtualClient.Contracts/Parser/TextParser.cs -->
- Parsers inherit from `MetricsParser` (`VirtualClient.Contracts/Parser/MetricsParser.cs:15`) which extends `TextParser<IList<Metric>>` (`VirtualClient.Contracts/Parser/TextParser.cs:12`)
- Override `Parse()` (required, abstract at `TextParser.cs:48`) and optionally `Preprocess()` for text normalization (`TextParser.cs:53`)
- Use regex patterns defined as `private static readonly Regex` (e.g., `DiskSpdMetricsParser.cs:24–34`)
- Use `TextParsingExtensions.Sectionize()` to split output into logical sections (`VirtualClient.Contracts/Parser/TextParsingExtensions.cs:92`)
- Return `IList<Metric>` where each `Metric` has: `Name`, `Value`, `Unit`, `Relativity`, `Tags`, `Metadata` (see `VirtualClient.Contracts/Metric.cs`)

<!-- See actual usage in OpenSslExecutor.cs:225–234 -->
Metrics are logged via (from `OpenSslExecutor.cs:225`):
```csharp
this.Logger.LogMetrics(
    "OpenSSL",
    this.MetricScenario ?? this.Scenario,
    workloadProcess.StartTime,
    workloadProcess.ExitTime,
    metrics,
    null,
    commandArguments,
    this.Tags,
    telemetryContext);
```

### Client/Server Architecture

<!-- See: VirtualClient.Actions/Network/NetworkingWorkload/NetworkingWorkloadExecutor.cs, VirtualClient.Contracts/EnvironmentLayout.cs -->
For network and database workloads, VirtualClient supports multi-role execution:
- One instance runs as **server**, another as **client** (e.g., `NetworkingWorkloadExecutor.cs`)
- They coordinate via the built-in REST API (`VirtualClient.Api/` project) for state synchronization and heartbeat
- Components use `Polly` retry policies for resilience (e.g., `NetworkingWorkloadExecutor.cs:49`)
- The `EnvironmentLayout` (`VirtualClient.Contracts/EnvironmentLayout.cs`) defines the topology of instances

### Error Handling

<!-- See: VirtualClient.Contracts/Exceptions.cs, VirtualClient.Contracts/Enumerations.cs -->
Custom exception hierarchy rooted at `VirtualClientException` (`Exceptions.cs:12`):
- `ApiException` (`Exceptions.cs:78`) — API communication failures
- `ComponentException` (`Exceptions.cs:137`) — general component failures
- `MonitorException` (`Exceptions.cs:196`) — monitor failures
- `WorkloadResultsException` (`Exceptions.cs:373`) — parsing failures, missing results
- `DependencyException` (`Exceptions.cs:432`) — dependency resolution failures
- `ProcessException` (`Exceptions.cs:491`) — process execution failures
- `StartupException` (`Exceptions.cs:549`) — startup/initialization failures

All exceptions carry an `ErrorReason` enum value (`Enumerations.cs:37`). Error reasons ≥500 are fatal (e.g., `ProfileNotFound = 500`); 400–499 are potentially transient (e.g., `InvalidResults = 400`). See the comment at `Enumerations.cs:115` and `Enumerations.cs:153`.

## Coding Standards and Conventions

### File Header
<!-- Observed in every .cs file, e.g. OpenSslExecutor.cs:1–2, VirtualClientComponent.cs:1–2, Metric.cs:1–2 -->
Every `.cs` file starts with:
```csharp
// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
```

### Namespace and Using Style
<!-- See: OpenSslExecutor.cs:4–21, VirtualClientComponent.cs:4–21, CoreMarkExecutor.cs:4–20 -->
- **Using statements go inside the namespace block** (not at file top)
- Ordering: `System.*` → `Microsoft.*` → `Newtonsoft.*` → `VirtualClient.*`
- Namespace matches folder structure: `VirtualClient.Actions`, `VirtualClient.Contracts`, etc.

```csharp
// From OpenSslExecutor.cs:4–21
namespace VirtualClient.Actions
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO.Abstractions;
    using System.Runtime.InteropServices;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Logging;
    using VirtualClient.Common;
    using VirtualClient.Common.Extensions;
    using VirtualClient.Common.Platform;
    using VirtualClient.Common.Telemetry;
    using VirtualClient.Contracts;
    using VirtualClient.Contracts.Metadata;
```

### Naming Conventions
<!-- See examples cited inline -->
- **Classes**: PascalCase, suffixed by role (e.g., `OpenSslExecutor` in `OpenSSL/OpenSslExecutor.cs`, `DiskSpdMetricsParser` in `DiskSpd/DiskSpdMetricsParser.cs`, `CoreMarkExecutor` in `CoreMark/CoreMarkExecutor.cs`)
- **Properties**: PascalCase (e.g., `CommandLine`, `MetricScenario`, `MonitorEnabled`)
- **Private fields**: camelCase, no prefix for instance fields; `const` fields use PascalCase
  ```csharp
  // From OpenSslExecutor.cs:37–38
  private IFileSystem fileSystem;
  private ISystemManagement systemManagement;
  // From CoreMarkExecutor.cs:28–29
  private const string CoreMarkOutputFile1 = "run1.log";
  private const string CoreMarkOutputFile2 = "run2.log";
  ```
- **Parameters dictionary keys**: PascalCase, accessed case-insensitively via `StringComparer.OrdinalIgnoreCase` (see `VirtualClientComponent.cs:92`)
- **Async methods**: Suffixed with `Async` (`ExecuteAsync`, `InitializeAsync`, `CleanupAsync`)
- **Test classes**: `{ComponentName}Tests` (e.g., `FioExecutorTests` in `VirtualClient.Actions.UnitTests/FIO/FioExecutorTests.cs:24`)
- **Test methods**: Descriptive with underscores for scenario separation (e.g., `FioExecutorSelectsTheExpectedDisksForTest_RemoteDiskScenario` at `FioExecutorTests.cs:80`)

### Property Pattern for Profile Parameters
<!-- See: OpenSslExecutor.cs:55–61, CoreMarkExecutor.cs:49–55 -->
Properties that read from the `Parameters` dictionary follow this pattern:
```csharp
// From OpenSslExecutor.cs:55–61
public string CommandArguments
{
    get
    {
        return this.Parameters.GetValue<string>(nameof(OpenSslExecutor.CommandArguments));
    }
}

// With default value (from CoreMarkExecutor.cs:49–55):
public string CompilerName
{
    get
    {
        return this.Parameters.GetValue<string>(nameof(this.CompilerName), string.Empty);
    }
}
```

### XML Documentation
<!-- See: OpenSslExecutor.cs:23–33, 40–44, VirtualClientComponent.cs:78–84 -->
All public members have XML doc comments using `<summary>`, `<param>`, `<returns>`, `<remarks>`, and `<inheritdoc />` tags:
```csharp
// From OpenSslExecutor.cs:40–44
/// <summary>
/// Constructor
/// </summary>
/// <param name="dependencies">Provides required dependencies to the component.</param>
/// <param name="parameters">Parameters defined in the profile or supplied on the command line.</param>
```

### Platform Support Attribute
<!-- See: VirtualClient.Common/Platform/SupportedPlatformsAttribute.cs; usage in OpenSslExecutor.cs:34, CoreMarkExecutor.cs:25 -->
Executors declare supported platforms via a class-level attribute:
```csharp
// From OpenSslExecutor.cs:34–35
[SupportedPlatforms("linux-arm64,linux-x64,win-x64")]
public class OpenSslExecutor : VirtualClientComponent
```

### Code Quality
<!-- See: src/VirtualClient/.editorconfig, src/VirtualClient/CodeQuality.targets -->
- **StyleCop.Analyzers** enforces style rules (suppressed: SA1204 static element ordering — `.editorconfig:3–4`)
- **AsyncFixer** validates async patterns (suppressed: AZCA1002 async method naming — `.editorconfig:6–7`)
- Central package version management (`Directory.Packages.props`) prevents version drift across projects

## Executor Implementation Checklist

When adding a new workload executor (pattern observed in `OpenSSL/OpenSslExecutor.cs`, `CoreMark/CoreMarkExecutor.cs`, `DiskSpd/DiskSpdExecutor.cs`):

1. Create a subfolder under `VirtualClient.Actions/` named after the workload
2. Create an executor class inheriting `VirtualClientComponent` (`VirtualClient.Contracts/VirtualClientComponent.cs`)
3. Add `[SupportedPlatforms("...")]` attribute (`VirtualClient.Common/Platform/SupportedPlatformsAttribute.cs`)
4. Define constructor with `(IServiceCollection dependencies, IDictionary<string, IConvertible> parameters)`
5. Expose profile parameters as properties reading from `this.Parameters` (e.g., `OpenSslExecutor.cs:55–61`)
6. Override `InitializeAsync` for setup (locate package, set executable path)
7. Override `ExecuteAsync` for the main workload logic (execute process, capture output, parse, log metrics)
8. Optionally override `CleanupAsync` and `Validate`
9. Create a `MetricsParser` subclass to parse workload output into `IList<Metric>` (e.g., `DiskSpd/DiskSpdMetricsParser.cs`)
10. Create an execution profile JSON in `VirtualClient.Main/profiles/` (e.g., `PERF-CPU-OPENSSL.json`)
11. Add unit tests inheriting from `MockFixture` in the corresponding `.UnitTests` project (e.g., `FIO/FioExecutorTests.cs`)
12. Add example output files under `Examples/` for parser tests

## Testing Philosophy and Patterns

### Framework
<!-- See: Directory.Packages.props for versions; FioExecutorTests.cs:22–23 for annotations -->
- **NUnit 3** with `[TestFixture]`, `[Test]`, `[SetUp]`, `[OneTimeSetUp]` attributes (e.g., `FioExecutorTests.cs:22–23`)
- **Moq** for mocking interfaces (e.g., `FioExecutorTests.cs:57`)
- **AutoFixture** via `MockFixture` base class for test data generation (`MockFixture.cs:35` extends `Fixture`)
- Tests are categorized: `[Category("Unit")]` (e.g., `FioExecutorTests.cs:23`) or `[Category("Functional")]`

### MockFixture Base Class
<!-- See: VirtualClient.TestFramework/MockFixture.cs -->
Test classes inherit from `MockFixture` (in `VirtualClient.TestFramework/MockFixture.cs:35`), which provides:
- Pre-configured mock services: `ApiClient` (line 87), `DiskManager` (line 141), `FileSystem` (line 156), `File` (line 161), `Directory` (line 146), `ProcessManager` (line 235)
- `Setup(PlatformID platform, Architecture architecture = ...)` method to configure platform-specific behavior (line 466)
- `MockFixture.ReadFile(...)` to load example output from `Examples/` directory (line 278)
- `InMemoryProcess` (`InMemoryProcess.cs:20`), `InMemoryFile` (`InMemoryFile.cs:17`), `InMemoryDirectory` (`InMemoryDirectory.cs:13`) test doubles

### Test Structure Pattern
<!-- Taken from VirtualClient.Actions.UnitTests/FIO/FioExecutorTests.cs:22–77 -->
```csharp
// From FioExecutorTests.cs
[TestFixture]
[Category("Unit")]
public class FioExecutorTests : MockFixture
{
    private IDictionary<string, IConvertible> profileParameters;
    private string mockResults;

    [OneTimeSetUp]
    public void SetupFixture()
    {
        this.mockResults = MockFixture.ReadFile(MockFixture.ExamplesDirectory, "FIO", "Results_FIO.json");
    }

    [SetUp]
    public void SetupTest()
    {
        this.Setup(PlatformID.Unix);
        // ...
        this.ProcessManager.OnCreateProcess = (command, arguments, workingDir) =>
        {
            return new InMemoryProcess
            {
                OnHasExited = () => true,
                ExitCode = 0,
                StartInfo = new ProcessStartInfo
                {
                    FileName = command,
                    Arguments = arguments,
                    WorkingDirectory = workingDir
                },
                StandardOutput = new ConcurrentBuffer(new StringBuilder(this.mockResults))
            };
        };
    }

    [Test]
    public void FioExecutorSelectsTheExpectedDisksForTest_RemoteDiskScenario()
    {
        // Arrange, Act, Assert
    }
}
```

### Parser Tests
<!-- See: VirtualClient.Actions.UnitTests/ for parser test examples; example outputs in TestResources/ directories -->
Parser tests load real example output (stored in `Examples/` or `TestResources/` under test projects), run the parser, and assert against expected metric names, values, and units. This ensures parsers remain correct as output formats evolve.

## Build and Test Commands

### Build
<!-- See: build.sh and build.cmd at repo root -->
```bash
# Linux — builds solution (Debug) then publishes self-contained for all platforms (Release)
./build.sh

# Build for specific platform only (see build.sh for all options)
./build.sh --linux-x64
./build.sh --win-x64 --linux-arm64

# Windows
build.cmd
build.cmd --win-x64
```

The build first compiles the solution in Debug configuration (for extension debugging — see comment in `build.sh:124–126` and `build.cmd:69–74`), then publishes runtime-specific self-contained binaries in Release.

### Test
<!-- See: build-test.sh and build-test.cmd at repo root -->
```bash
# Linux — runs Unit tests only (build-test.sh:65 filters Category=Unit)
./build-test.sh

# Windows — runs Unit + Functional tests (build-test.cmd:23 filters Category=Unit|Category=Functional)
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
<!-- See: VERSION file at repo root; build.sh:104–106 and build.cmd:42 -->
Build version is read from the `VERSION` file at the repo root. Override with the `VCBuildVersion` environment variable.

## PR Review Guidelines

When reviewing pull requests, flag items below as **Required Fix** (will break the build, crash at runtime, or violate a hard architectural constraint) or **Suggestion** (inconsistency with conventions that won't break anything but should be addressed).

### Required Fixes (flag these — they break things)

1. **Component must inherit `VirtualClientComponent`.**
   Any class referenced by a profile `"Type"` field is resolved via `ComponentTypeCache` and instantiated by `ComponentFactory.CreateComponent` using `Activator.CreateInstance` — which casts the result to `VirtualClientComponent`. A class that does not inherit this base type will throw `InvalidCastException` → `StartupException` at runtime.
   <!-- See: ComponentFactory.cs:73–77, ComponentTypeCache.cs:20, ProfileExecutor.cs:697–718 -->

2. **Component constructor must be `(IServiceCollection, IDictionary<string, IConvertible>)`.**
   `ComponentFactory` calls `Activator.CreateInstance(componentType, dependencies, effectiveParameters)` which requires exactly this two-parameter constructor signature. A missing or differently-typed constructor throws `MissingMethodException` → `StartupException`.
   <!-- See: ComponentFactory.cs:130, ComponentFactory.cs:79–85, VirtualClientComponent.cs:85 -->

3. **Assembly containing new components must have `[assembly: VirtualClientComponentAssembly]`.**
   `ComponentTypeCache.IsComponentAssembly()` checks for this attribute; assemblies without it are skipped during type discovery. A new executor in an un-attributed assembly will cause `TypeLoadException` at profile load time.
   <!-- See: ComponentTypeCache.cs:50–53, VirtualClient.Actions/AssemblyInfo.cs:4, ProfileExecutor.cs:712–718 -->

4. **Profile `"Type"` value must exactly match the C# class name.**
   The `ComponentTypeCache.TryGetComponentType` lookup matches on type name. A typo or wrong name throws `TypeLoadException` → `StartupException` with the message "does not exist or does not inherit from VirtualClientComponent".
   <!-- See: ProfileExecutor.cs:697, ComponentFactory.cs:47–49, PERF-CPU-OPENSSL.json "Type": "OpenSslExecutor" -->

5. **Parser must extend `MetricsParser` (which extends `TextParser<IList<Metric>>`) and implement `Parse()`.**
   `Parse()` is abstract in `TextParser<T>` — failing to override it is a compile error. Using a return type other than `IList<Metric>` will break the `LogMetrics` call which iterates over the result.
   <!-- See: TextParser.cs:48, MetricsParser.cs:15, VirtualClientLoggingExtensions.cs:645–662, OpenSslExecutor.cs:222–234 -->

6. **NuGet package versions must be in `Directory.Packages.props`, not in individual `.csproj` files.**
   The repo uses central package management. Adding a `Version=` attribute in a `.csproj` `<PackageReference>` will cause a build error (`NU1008`) because it conflicts with centrally-managed versions.
   <!-- See: Directory.Packages.props, VirtualClient.Actions.csproj:11–12 (no Version attribute) -->

7. **`using` statements must be inside the `namespace` block.**
   StyleCop.Analyzers (enabled repo-wide) enforces `SA1200` — `using` directives outside a namespace cause build warnings treated as errors in CI.
   <!-- See: OpenSslExecutor.cs:4–21, VirtualClientComponent.cs:4–21, .editorconfig:1–7 -->

8. **Exception types must use the project's exception hierarchy.**
   Throwing raw `Exception` or `InvalidOperationException` instead of the established types (`WorkloadException`, `DependencyException`, `ProcessException`, `MonitorException`, etc.) breaks the error-handling pipeline that routes on `ErrorReason`. The `Validate()` pattern in existing executors consistently throws `WorkloadException` with `ErrorReason.InvalidProfileDefinition`.
   <!-- See: Exceptions.cs:12–549, FioExecutor.cs:841–867 (Validate throws WorkloadException), Enumerations.cs:37 -->

9. **Test classes must have `[TestFixture]` and `[Category("Unit")]` (or `"Functional"`).**
   The build scripts filter tests by category (`--filter "Category=Unit"`). Tests without a `[Category]` attribute are silently skipped by the CI pipeline and will never run.
   <!-- See: FioExecutorTests.cs:22–23, build-test.sh:65, build-test.cmd:23 -->

10. **Copyright header must be present on every `.cs` file.**
    StyleCop rule `SA1633` requires the standard two-line header. Missing it will fail the StyleCop check.
    <!-- See: OpenSslExecutor.cs:1–2, VirtualClientComponent.cs:1–2 -->

### Suggestions (flag these — won't break but inconsistent)

1. **Prefer `this.Parameters.GetValue<T>(nameof(...))` for profile parameters.**
   All existing executors expose parameters as properties backed by `this.Parameters.GetValue<T>()` using `nameof()` for the key. Direct dictionary access (`this.Parameters["Key"]`) works but is inconsistent with the established pattern and loses the case-insensitive, typed lookup.
   <!-- See: OpenSslExecutor.cs:55–61, CoreMarkExecutor.cs:49–55 -->

2. **Add `[SupportedPlatforms("...")]` attribute to executor classes.**
   While not strictly required (the base class handles missing attributes gracefully), every existing executor uses this attribute. Omitting it means the workload will attempt to run on all platforms, which is rarely correct.
   <!-- See: SupportedPlatformsAttribute.cs:13, OpenSslExecutor.cs:34, CoreMarkExecutor.cs:25 -->

3. **Test classes should inherit `MockFixture`, not create mocks from scratch.**
   `MockFixture` pre-configures all the standard service mocks (`FileSystem`, `DiskManager`, `ProcessManager`, etc.) and provides helpers like `ReadFile()` and `Setup(PlatformID)`. Manually recreating these mocks is verbose and diverges from the pattern used by all existing tests.
   <!-- See: MockFixture.cs:35, FioExecutorTests.cs:24 -->

4. **Using-statement ordering: `System.*` → `Microsoft.*` → `Newtonsoft.*` → `VirtualClient.*`.**
   StyleCop `SA1208`/`SA1210` enforce alphabetical ordering within groups. While reordering won't break the build (it's a warning, not an error), it's inconsistent with every file in the codebase.
   <!-- See: OpenSslExecutor.cs:6–21, VirtualClientComponent.cs:6–21 -->

5. **Private instance fields should use camelCase with no prefix.**
   The codebase uses `private IFileSystem fileSystem;` not `_fileSystem` or `m_fileSystem`. Using a different convention won't break anything but creates visual inconsistency.
   <!-- See: OpenSslExecutor.cs:37–38, CoreMarkExecutor.cs:28–29 -->

6. **XML doc comments on all public members.**
   Existing code consistently documents every public method, property, and constructor with `<summary>`, `<param>`, and `<returns>` tags. Missing docs won't fail the build (XML doc warnings are not currently treated as errors) but diverges from project norms.
   <!-- See: OpenSslExecutor.cs:23–44, VirtualClientComponent.cs:78–84 -->

7. **Parser tests should load real example output from `Examples/` directories.**
   Parser tests in the codebase use `MockFixture.ReadFile(MockFixture.ExamplesDirectory, ...)` to load actual benchmark output. Inline test strings work but don't verify against real-world output formats.
   <!-- See: FioExecutorTests.cs:35, MockFixture.cs:50–52 -->

8. **`Validate()` should check required parameters and throw `WorkloadException` with `ErrorReason.InvalidProfileDefinition`.**
   This is the established validation pattern. While not enforced by the compiler, skipping validation means invalid profiles fail later with obscure errors instead of clear messages at startup.
   <!-- See: FioExecutor.cs:841–867, StressNgExecutor.cs:122, DiskSpdExecutor.cs:644 -->

9. **Async methods should be suffixed with `Async`.**
   The `AZCA1002` analyzer rule is suppressed (`.editorconfig:6–7`), so this won't break the build. However, every existing method follows the `Async` suffix convention (`ExecuteAsync`, `InitializeAsync`, `CleanupAsync`).
   <!-- See: VirtualClientComponent.cs:713,856,861,814 -->
