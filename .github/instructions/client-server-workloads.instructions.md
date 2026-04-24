---
applyTo: "VirtualClient.Actions/**/*.cs"
description: "Pattern for developing multi-VM client/server/reverseProxy workloads"
---

# Client/Server Workload Development

For network and database workloads, VirtualClient supports multi-role execution where separate
instances run as client and server, coordinating via the built-in REST API.

## Key Components

- `EnvironmentLayout` — topology of instances (`ClientInstance` with Name, Role, IPAddress)
- `IApiClientManager` — creates API clients for inter-VM communication
- `ClientRole.Client` / `ClientRole.Server` / `ClientRole.ReverseProxy` — role constants
- `this.SetServerOnline(bool)` — extension method to signal server readiness
- `serverApiClient.PollForHeartbeatAsync(timeout, ct)` / `PollForServerOnlineAsync(timeout, ct)`

## Base Executor Pattern

See `VirtualClient.Actions/Examples/ClientServer/ExampleClientServerExecutor.cs` for the canonical
implementation. The base class resolves dependencies and defines supported roles in the constructor:

```csharp
[SupportedPlatforms("linux-x64,linux-arm64")]
public class MyWorkloadExecutor : VirtualClientComponent
{
    public MyWorkloadExecutor(IServiceCollection dependencies, IDictionary<string, IConvertible> parameters = null)
        : base(dependencies, parameters)
    {
        this.SystemManagement = dependencies.GetService<ISystemManagement>();
        this.ApiClientManager = dependencies.GetService<IApiClientManager>();
        this.FileSystem = this.SystemManagement.FileSystem;
        this.PackageManager = this.SystemManagement.PackageManager;
        this.ProcessManager = this.SystemManagement.ProcessManager;
        this.StateManager = this.SystemManagement.StateManager;

        // Set the base class property — do NOT declare a new field
        this.SupportedRoles = new List<string> { ClientRole.Client, ClientRole.Server };
    }
}
```

## Client-Side Sync Flow

Clients poll the server before starting the workload (see `ExampleClientExecutor.cs`):

```csharp
IApiClient serverApiClient = this.ApiClientManager.GetOrCreateApiClient(server.Name, server);
await serverApiClient.PollForHeartbeatAsync(this.PollingTimeout, cancellationToken);
await serverApiClient.PollForServerOnlineAsync(TimeSpan.FromSeconds(30), cancellationToken);
// Server confirmed online — execute workload
```

## Server-Side Signal Flow

Servers signal readiness after starting (see `ExampleServerExecutor.cs`):

```csharp
this.SetServerOnline(true);   // Signal to clients
await webHostProcess.WaitForExitAsync(cancellationToken);
// In finally block:
this.SetServerOnline(false);  // Always signal offline before exiting
```

## Validation

Override `Validate()` to check layout and roles:
```csharp
protected override void Validate()
{
    base.Validate();
    this.ThrowIfLayoutNotDefined();
}
```

## Profile Structure

```json
{
    "Actions": [
        { "Type": "MyServerExecutor", "Parameters": { "Role": "Server", "Port": 5000 } },
        { "Type": "MyClientExecutor", "Parameters": { "Role": "Client", "ServerPort": "$.Parameters.Port" } }
    ]
}
```

## Checklist

- [ ] Set `this.SupportedRoles` in constructor (use base class property, not a new field)
- [ ] Resolve `IApiClientManager` and `ISystemManagement` from dependencies
- [ ] Use `this.IsInRole(ClientRole.Client/Server)` to branch execution
- [ ] Server calls `this.SetServerOnline(true/false)` for handshake
- [ ] Client calls `PollForHeartbeatAsync` then `PollForServerOnlineAsync`
- [ ] Use `Polly` retry policies for cross-VM communication resilience
- [ ] Test both client and server code paths independently
