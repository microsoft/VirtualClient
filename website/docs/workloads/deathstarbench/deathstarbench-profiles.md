# DeathStarBench Workload Profiles
The following profiles run customer-representative or benchmarking scenarios using the DeathStarBench workload.  

* [Workload Details](./deathstarbench.md)  
* [Client/Server Workloads](../../guides/0020-client-server.md)

## Client/Server Topology Support
DeathStarBench workload profiles support running the workload on both a single system as well as in a client/server topology. This means that the workload supports
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
./VirtualClient --profile=PERF-NETWORK-DEATHSTARBENCH.json --system=Juno --timeout=1440

# Multi-System
# On Client Role System...
./VirtualClient --profile=PERF-NETWORK-DEATHSTARBENCH.json --system=Juno --timeout=1440 --clientId=Client01 --layoutPath=/any/path/to/layout.json

# On Server Role System...
./VirtualClient --profile=PERF-NETWORK-DEATHSTARBENCH.json --system=Juno --timeout=1440 --clientId=Server01 --layoutPath=/any/path/to/layout.json

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

## PERF-NETWORK-DEATHSTARBENCH.json
Runs a Network intensive workload using the DeathStarBench toolset to test the Network performance in processing HTTP load to 
the server between applications in a Docker swarm environment.

* [Workload Profile](https://github.com/microsoft/VirtualClient/blob/main/src/VirtualClient/VirtualClient.Main/profiles/PERF-NETWORK-DEATHSTARBENCH.json) 

* **Supported Platform/Architectures**
  * linux-x64

* **Supported Operating Systems**
   * Ubuntu

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

  :::caution
 > The value for parameter 'ConnectionCount' should be greater than or equals to the value for parameter 'ThreadCount'.
  :::

  | Parameter                 | Purpose                                                                         | Default value |
  |---------------------------|---------------------------------------------------------------------------------|---------------|
  | ThreadCount          | Optional. This specifies the number of threads to be created to send http load for each workload scenario. | 20
  | ConnectionCount        | Optional. This specifies the number of connections to be created to send http load for each workload scenario that we can have. | 1000
  | Duration                   | Optional. This specifies the time for which the http load will be sent. | 300s
  | RequestPerSec          | Optional. This specifies the constant throughput load. | 1000 
  | GraphType           | Optional. This specifies the type of graph to be used in **socialNetwork** scenario. | socfb-Reed98 [Allowed values : socfb-Reed98, ego-twitter] |

* **Profile Runtimes**  
  See the 'Metadata' section of the profile for estimated runtimes. These timings represent the length of time required to run a single round of profile 
  actions. These timings can be used to determine minimum required runtimes for the Virtual Client in order to get results. These are often estimates based on the
  number of system cores. 

* **Usage Examples**  
  The following section provides a few basic examples of how to use the workload profile. Additional usage examples can be found in the
  'Usage Scenarios/Examples' link at the top.

  ``` bash
  # When running on a single system (environment layout not required)
  ./VirtualClient --profile=PERF-NETWORK-DEATHSTARBENCH.json --system=Demo --timeout=1440 --packageStore="{BlobConnectionString|SAS Uri}"

  # Override the profile default parameters
  ./VirtualClient --profile=PERF-NETWORK-DEATHSTARBENCH.json --system=Demo --timeout=1440 --packageStore="{BlobConnectionString|SAS Uri}" --parameters="Duration=60s,,,ThreadCount=2,,,ConnectionCount=100"

   # When running in a client/server environment
  ./VirtualClient --profile=PERF-NETWORK-DEATHSTARBENCH.json --system=Demo --timeout=1440 --clientId=Client01 --packageStore="{BlobConnectionString|SAS Uri}" --layoutPath="/any/path/to/layout.json"
  ./VirtualClient --profile=PERF-NETWORK-DEATHSTARBENCH.json --system=Demo --timeout=1440 --clientId=Server01 --packageStore="{BlobConnectionString|SAS Uri}" --layoutPath="/any/path/to/layout.json"
  ```
