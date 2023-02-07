# Memcached Workload Profiles
The following profiles run customer-representative or benchmarking scenarios using the Memtier workload against a
Memcached server.

* [Workload Details](./memcached.md)  
* [Client/Server Workloads](../../guides/0020-client-server.md)

## Client/Server Topology Support
Memcached workload profiles support running the workload on both a single system as well as in a client/server topology. This means that the workload supports
operation on a single system or on 2 distinct systems. The client/server topology is typically used when it is desirable to include a network component in the
overall performance evaluation. In a client/server topology, one system operates in the 'Client' role making calls to the system operating in the 'Server' role. 
The Virtual Client instances running on the client and server systems will synchronize with each other before running the workload. In order to support a client/server topology,
an environment layout file MUST be supplied to each instance of the Virtual Client on the command line to describe the IP address/location of other Virtual Client instances. An
environment layout file is not required for the single system topology.

* [Environment Layouts](../../guides/0020-client-server.md)

In the environment layout file provided to the Virtual Client, define the role of the client system/VM as "Client" and the role of the server system(s)/VM(s) as "Server".
The spelling of the roles must be exact. The IP addresses of the systems/VMs must be correct as well. The following example illustrates the
idea. The name of the client must match the name of the system or the value of the agent ID passed in on the command line.

``` bash
# Single System (environment layout not required)
./VirtualClient --profile=PERF-MEMCACHED.json --system=Demo --timeout=1440

# Multi-System
# On Client role system...
./VirtualClient --profile=PERF-MEMCACHED.json --system=Demo --timeout=1440 --clientId=Client01 --layoutPath=/any/path/to/layout.json

# On Server role system...
./VirtualClient --profile=PERF-MEMCACHED.json --system=Demo --timeout=1440 --clientId=Server01 --layoutPath=/any/path/to/layout.json

# Example contents of the 'layout.json' file:
{
    "clients": [
        {
            "name": "Client01",
            "role": "Client",
            "ipAddress": "10.1.0.1"
        },
        {
            "name": "Server01",
            "role": "Server",
            "ipAddress": "10.1.0.2"
        }
    ]
}
```

## PERF-MEMCACHED.json
Runs the Memtier workload against to generate various network traffic patterns against a Memcached server.

* [Workload Profile](https://github.com/microsoft/VirtualClient/blob/main/src/VirtualClient/VirtualClient.Main/profiles/PERF-MEMCACHED.json) 

* **Supported Platform/Architectures**
  * linux-x64
  * linux-arm64

* **Supports Disconnected Scenarios**  
  * No. Internet connection required.

* **Dependencies**  
  The dependencies defined in the 'Dependencies' section of the profile itself are required in order to run the workload operations effectively.
  * Internet connection.
  * The IP addresses defined in the environment layout (see above) for the Client and Server systems must be correct.
  * The name of the Client and Server instances defined in the environment layout must match the agent/client IDs supplied on the command line (e.g. --agentId)
    or must match the name of the system as defined by the operating system itself.

  Additional information on components that exist within the 'Dependencies' section of the profile can be found in the following locations:
  * [Installing Dependencies](https://microsoft.github.io/VirtualClient/docs/category/dependencies/)

* **Profile Parameters**  
  The following parameters can be optionally supplied on the command line to modify the behaviors of the workload.

  | Parameter                 | Purpose                                                                         | Default Value |
  |---------------------------|---------------------------------------------------------------------------------|---------------|
  | Username                  | Required for multi-system scenarios. Defines the user to use for SSH connections on the Server role system.  |  |
  | ServerItemMemoryMB        | Optional. The amount of memory (in megabytes) to be created/staged for use on the server-side by the Memcached server. Allows for scaling.  | 1024 (megabytes) |

* **Profile Runtimes**  
  See the 'Metadata' section of the profile for estimated runtimes. These timings represent the length of time required to run a single round of profile 
  actions. These timings can be used to determine minimum required runtimes for the Virtual Client in order to get results. These are often estimates based on the
  number of system cores. 

* **Usage Examples**  
  The following section provides a few basic examples of how to use the workload profile.

  ``` bash
  # When running on a single system (environment layout not required)
  ./VirtualClient --profile=PERF-MEMCACHED.json --system=Demo --timeout=250 --packageStore="{BlobConnectionString|SAS Uri}"

  # When running in a client/server environment
  ./VirtualClient --profile=PERF-MEMCACHED.json --system=Demo --timeout=1440 --clientId=Client01 --parameters="Username=testuser" --layoutPath="/any/path/to/layout.json" --packageStore="{BlobConnectionString|SAS Uri}"
  ./VirtualClient --profile=PERF-MEMCACHED.json --system=Demo --timeout=1440 --clientId=Server01 --parameters="Username=testuser" --layoutPath="/any/path/to/layout.json" --packageStore="{BlobConnectionString|SAS Uri}"
  ```