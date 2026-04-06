# Copilot Instructions for VirtualClient

## Build, Test, and Lint

```bash
# Build the solution (builds AnyCPU with Debug, then publishes per-platform with Release)
build.cmd                          # all platforms
build.cmd --win-x64                # single platform
build.cmd --linux-x64 --linux-arm64  # multiple platforms

# Run all unit + functional tests
build-test.cmd

# Run a single test project
dotnet test -c Debug src\VirtualClient\VirtualClient.Actions.UnitTests\VirtualClient.Actions.UnitTests.csproj --no-restore --no-build --filter "(Category=Unit)" --logger "console;verbosity=normal"

# Run a single test by name
dotnet test -c Debug src\VirtualClient\VirtualClient.Actions.UnitTests\VirtualClient.Actions.UnitTests.csproj --no-restore --no-build --filter "FullyQualifiedName~CoreMarkExecutorTests.CoreMarkExecutorExecutesTheExpectedCommandInLinux"

# Build NuGet packages (run after build.cmd)
build-packages.cmd
build-packages.cmd --suffix beta

# Clean build output
clean.cmd
```

The solution must build before running tests (`build.cmd` then `build-test.cmd`). The solution is built with **Debug** configuration to support extensions debugging. Publishing uses **Release**. StyleCop, AsyncFixer, and Roslyn analyzers are enforced at build time — warnings are treated as errors.

Test categories are `Unit` and `Functional`. The test filter in CI is `(Category=Unit|Category=Functional)`.

## Architecture

### Project Dependency Graph

```
VirtualClient.Main (Entry point, self-contained EXE)
├── VirtualClient.Actions      — 50+ workload executors (benchmarks)
├── VirtualClient.Dependencies — Package/tool installers
├── VirtualClient.Monitors     — System monitors (GPU, perf counters, etc.)
├── VirtualClient.Api          — REST API (ASP.NET Core) for state/heartbeat/events
└── VirtualClient.Core         — Runtime: package/state/process/blob managers
    ├── VirtualClient.Contracts — Base classes, interfaces, data contracts
    │   └── VirtualClient.Common — Extensions, telemetry primitives, Azure SDK wrappers
    └── VirtualClient.Common
```

### Component Model

All actions, monitors, and dependencies inherit from `VirtualClientComponent`. The runtime discovers components via reflection — no manual registration needed.

**Lifecycle methods** (override these):

1. `IsSupported()` — Check platform support (optional; also driven by `[SupportedPlatforms]` attribute)
2. `InitializeAsync(EventContext, CancellationToken)` — Download packages, set up state
3. `Validate()` — Verify parameters/preconditions (optional)
4. `ExecuteAsync(EventContext, CancellationToken)` — Run the workload, capture metrics
5. `CleanupAsync(EventContext, CancellationToken)` — Tear down resources (optional)

### Execution Profiles

Profiles are JSON files in `src/VirtualClient/VirtualClient.Main/profiles/` with three sections:

- **Dependencies** — Run first; install packages/tools from blob storage
- **Actions** — Workload executors to run
- **Monitors** — Background system monitors

Parameters support JPath references (`"$.Parameters.ProfilingEnabled"`) and environment variable substitution (`"{Environment:VAR_NAME}"`).

Profile naming convention: `PERF-<CATEGORY>-<WORKLOAD>.json` (e.g., `PERF-CPU-COREMARK.json`, `PERF-IO-FIO.json`).

### Client/Server Workloads

Some workloads (e.g., HammerDB, network benchmarks) use a multi-VM client/server topology. This requires a `layout.json` file specifying IP addresses and roles:

```json
{
    "clients": [
        { "name": "client-vm", "role": "Client", "privateIPAddress": "10.1.0.11" },
        { "name": "server-vm", "role": "Server", "privateIPAddress": "10.1.0.18" }
    ]
}
```

Pass `--layout-path=/path/to/layout.json` when running VC on each VM.

## Key Conventions

### Implementing a New Workload

1. Create a class in `VirtualClient.Actions` inheriting `VirtualClientComponent`
2. Add `[SupportedPlatforms("linux-x64,win-x64")]` attribute
3. Expose profile parameters as properties using `this.Parameters.GetValue<T>(nameof(Property))`
4. Execute workloads via `this.ExecuteCommandAsync(exe, args, workingDir, telemetryContext, cancellationToken)`
5. Parse output into `IList<Metric>` and log via `this.Logger.LogMetrics(...)`
6. Create a matching profile JSON in `VirtualClient.Main/profiles/`
7. Write unit tests in a matching `VirtualClient.Actions.UnitTests/<WorkloadName>/` directory

### Test Patterns

- **Framework**: NUnit 3 + Moq + AutoFixture
- **Base class**: Test fixtures inherit from `MockFixture` (provides `IFileSystem`, `IPackageManager`, `ProcessManager`, `ISystemManagement`, etc. pre-mocked)
- **Example output files**: Store in `src/VirtualClient/TestResources/` and read via `MockFixture.ReadFile(MockFixture.ExamplesDirectory, "WorkloadName", "example-output.txt")`
- **Process mocking**: Use `this.ProcessManager.OnCreateProcess = (cmd, args, wd) => { /* assert args */ return this.Process; };`
- **Platform testing**: Call `this.Setup(PlatformID.Unix)` or `this.Setup(PlatformID.Win32NT)` in test setup

### Dependency Package Installation

Workload binaries/scripts are packaged as zip files in Azure Blob Storage. In profiles, use:

```json
{
    "Type": "DependencyPackageInstallation",
    "Parameters": {
        "Scenario": "InstallMyWorkloadPackage",
        "BlobContainer": "packages",
        "BlobName": "myworkload.1.0.0.zip",
        "PackageName": "myworkload",
        "Extract": true
    }
}
```

### Telemetry and Logging

All operations are wrapped with `EventContext` for correlation. Use:

- `this.Logger.LogMessage("Component.Operation", LogLevel.Information, telemetryContext)` for traces
- `this.Logger.LogMetrics("ToolName", metricName, value, unit, categorization, telemetryContext)` for workload results

### Versioning

The repo uses semantic versioning from the `VERSION` file at repo root (currently `3.0.5`). Override with `VCBuildVersion` environment variable. Central package management is enforced — all NuGet versions are in `Directory.Packages.props`.

## Development Workflow

### Fixing Source Code vs. Script Issues

Issues will either require a **source code change** (C# in the VirtualClient solution) or a **script/package change** (workload scripts in blob storage).

**For source code changes**: Edit, build, test, then deploy to VMs for validation.

**For script/package changes**: Compress the updated files and upload to the VC packages blob store (`virtualclientinternal` storage account, `packages` container).

- **Direct endpoint**: `https://virtualclientinternal.blob.core.windows.net/packages`
- **Azure Portal**: [packages container](https://ms.portal.azure.com/#view/Microsoft_Azure_Storage/ContainerMenuBlade/~/overview/storageAccountId/%2Fsubscriptions%2F94f4f5c5-3526-4f0d-83e5-2e7946a41b75%2FresourceGroups%2Fvirtualclient%2Fproviders%2FMicrosoft.Storage%2FstorageAccounts%2Fvirtualclientinternal/path/packages/etag/%220x8DB982C9A0ACF93%22/defaultEncryptionScope/%24account-encryption-key/denyEncryptionScopeOverride~/false/defaultId//publicAccessVal/None) The package version must match the version referenced in the VC profile's `DependencyPackageInstallation` `BlobName`. If major script changes are needed from the previous commit, consult on incrementing the package version.

### Testing on VMs

After local unit/functional tests pass, validate on Azure VMs using the scripts in `~/OneDrive - Microsoft/Documents/create-vc-vms/`:

```powershell
Import-Module -Name "C:\Users\evellanoweth\OneDrive - Microsoft\Documents\create-vc-vms\newVCvm.psm1" -Force

# Single-VM workflow
New-VC-VM -vmName "my-test" -alias "evellanoweth" -vmSize "Standard_D2s_v5"
Build-VC                          # clean + build + package
Copy-Local-Item -vmName "my-test" -alias "evellanoweth" -itemPath "$vcPath\out\packages\VirtualClient.linux-x64.3.0.5.nupkg"
Extract-VC -vmName "my-test" -alias "evellanoweth"
Run-VC -vmName "my-test" -alias "evellanoweth" -vcArguments "--profile=PERF-WORKLOAD.json --packages=<blob-url> --verbose"

# Client/Server workflow — create both VMs, copy VC + layout.json to each, run with --layout-path
```

Key VC runtime flags: `--profile`, `--packages` (blob store URL with managed identity), `--event-hub`, `--layout-path`, `--logger=csv`, `--verbose`, `--debug`, `-i=<iterations>`.

### Debugging Failures with Kusto

When a VM run fails, check metrics and traces in Azure Data Explorer:

- **Cluster**: `azurecrcworkloads.westus2.kusto.windows.net`
- **Metrics**: `WorkloadPerformance.Metrics_Dev01` — one row per metric data point (latency, throughput, IOPS)
- **Traces**: `WorkloadDiagnostics.Traces_Dev01` — diagnostic logs, errors, stack traces

```kql
// Get error traces for a run
Traces_Dev01
| where Timestamp > ago(1d)
| where SeverityLevel >= 3
| where ProfileName == "PERF-MY-WORKLOAD"
| order by Timestamp asc

// Check if workload produced metrics
Metrics_Dev01
| where Timestamp > ago(1d)
| where MetricName !in ("Succeeded", "Failed")
| summarize count() by ToolName, MetricName
```

For PPE/production environments, swap the `_Dev01` suffix to `_PPE01`. The Juno cluster (`azurecrc.westus2.kusto.windows.net`) has experiment scheduling and failure data in `JunoIngestion` and `JunoStaging` databases.
