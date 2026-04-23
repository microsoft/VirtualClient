---
applyTo: "VirtualClient.Actions/**/*.cs"
description: "Pattern for developing multi-VM client/server/reverseProxy workloads"
---

# Client/Server Workload Development

## Overview

For network and database workloads, VirtualClient supports multi-role execution where separate
instances run as client and server, coordinating via the built-in REST API.

## Key Components

- `EnvironmentLayout` — defines the topology of instances (`ClientInstance` objects with Name, Role, IPAddress)
- `IApiClientManager` — creates API clients for inter-VM communication
- `ClientRole.Client` / `ClientRole.Server` — role constants
- `ServerCancellationSource` — `CancellationTokenSource` to signal server shutdown

## Role Determination

Roles come from the `EnvironmentLayout` loaded into DI. The component checks its role:

```csharp
// Defined in profile Parameters: "Role": "Client" or "Role": "Server"
// Accessed via this.IsInRole(ClientRole.Client) or this.IsInRole(ClientRole.Server)
```

## Executor Pattern

```csharp
[SupportedPlatforms("linux-x64,linux-arm64")]
public class MyWorkloadExecutor : VirtualClientComponent
{
    protected IApiClientManager ApiClientManager { get; }
    protected IApiClient ServerApiClient { get; set; }
    protected CancellationTokenSource ServerCancellationSource { get; set; }

    public MyWorkloadExecutor(IServiceCollection dependencies, IDictionary<string, IConvertible> parameters)
        : base(dependencies, parameters)
    {
        this.ApiClientManager = dependencies.GetService<IApiClientManager>();
    }

    // Define supported roles
    public List<string> SupportedRoles = new List<string> { ClientRole.Client, ClientRole.Server };

    protected override async Task ExecuteAsync(EventContext context, CancellationToken ct)
    {
        if (this.IsInRole(ClientRole.Server))
        {
            await this.ExecuteServerAsync(context, ct);
        }
        else
        {
            await this.ExecuteClientAsync(context, ct);
        }
    }
}
```

## State Synchronization

Client and server coordinate via the REST API (`VirtualClient.Api/`):

- Server publishes state indicating readiness
- Client polls for server state before starting workload
- Use `Polly` retry policies for resilience against transient failures
- `IApiClient` provides `GetStateAsync`/`CreateStateAsync` for state exchange

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

- [ ] Define `SupportedRoles` with `ClientRole.Client` and/or `ClientRole.Server`
- [ ] Resolve `IApiClientManager` from dependencies
- [ ] Use role checks (`this.IsInRole(...)`) to branch execution logic
- [ ] Implement state synchronization via REST API
- [ ] Use `Polly` retry policies for cross-VM communication
- [ ] Handle `ServerCancellationSource` for clean server shutdown
- [ ] Test both client and server code paths independently
