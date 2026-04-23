---
applyTo: "VirtualClient.Actions/**/*.cs"
description: "Pattern for developing new workload executors"
---

# Workload Development Guide

## New Executor Checklist

1. Create subfolder under `VirtualClient.Actions/` named after the workload
2. Create executor class inheriting `VirtualClientComponent`
3. Add `[SupportedPlatforms("linux-x64,linux-arm64,win-x64")]` attribute
4. Define constructor: `(IServiceCollection dependencies, IDictionary<string, IConvertible> parameters)`
5. Expose profile parameters as properties using `this.Parameters.GetValue<T>(nameof(...))`
6. Override `InitializeAsync` — locate package, set executable path
7. Override `ExecuteAsync` — run process, capture output, parse metrics, log telemetry
8. Override `Validate` — check required parameters, throw `WorkloadException`
9. Create a `MetricsParser` subclass (see MetricsParser.instructions.md)
10. Create profile JSON in `VirtualClient.Main/profiles/`
11. Add unit tests inheriting `MockFixture` in `.UnitTests` project
12. Add example output files in `Examples/` for parser tests

## Class Structure

```csharp
[SupportedPlatforms("linux-arm64,linux-x64,win-x64")]
public class MyWorkloadExecutor : VirtualClientComponent
{
    private IFileSystem fileSystem;
    private ISystemManagement systemManagement;

    public MyWorkloadExecutor(IServiceCollection dependencies, IDictionary<string, IConvertible> parameters)
        : base(dependencies, parameters)
    {
        this.fileSystem = dependencies.GetService<IFileSystem>();
        this.systemManagement = dependencies.GetService<ISystemManagement>();
    }

    public string CommandArguments
    {
        get { return this.Parameters.GetValue<string>(nameof(this.CommandArguments)); }
    }

    protected override async Task InitializeAsync(EventContext telemetryContext, CancellationToken ct)
    {
        DependencyPath package = await this.GetPackageAsync(this.PackageName, ct);
        this.ExecutablePath = this.Combine(package.Path, "bin", "my-tool");
    }

    protected override async Task ExecuteAsync(EventContext telemetryContext, CancellationToken ct)
    {
        using (IProcessProxy process = await this.ExecuteCommandAsync(
            this.ExecutablePath, this.CommandArguments, this.WorkingDirectory, telemetryContext, ct))
        {
            await this.LogProcessDetailsAsync(process, telemetryContext, "MyWorkload");
            process.ThrowIfWorkloadFailed();

            MyWorkloadParser parser = new MyWorkloadParser(process.StandardOutput.ToString());
            IList<Metric> metrics = parser.Parse();

            this.Logger.LogMetrics(
                "MyWorkload", this.MetricScenario ?? this.Scenario,
                process.StartTime, process.ExitTime,
                metrics, null, this.CommandArguments, this.Tags, telemetryContext);
        }
    }

    protected override void Validate()
    {
        base.Validate();
        this.ThrowIfParameterNotDefined(nameof(this.CommandArguments));
    }
}
```

## Process Execution

- Use `this.ExecuteCommandAsync()` or `ProcessManager.CreateProcess()`
- Always call `process.ThrowIfWorkloadFailed()` to check exit code
- Capture output via `process.StandardOutput.ToString()`

## Telemetry

- Log metrics via `this.Logger.LogMetrics(toolName, scenario, start, end, metrics, ...)`
- Use `EventContext` for structured telemetry throughout the lifecycle
- Log process details via `this.LogProcessDetailsAsync(process, context, toolName)`
