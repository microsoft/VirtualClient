---
slug: /workloads/ctstraffic-profiles
---

# ctsTraffic Profiles
The following profiles run customer-representative or benchmarking scenarios using the ctsNetwork workload.

* [Workload Details](/docs/workloads/network-suite)  
* [Client/Server Workloads](/docs/guides/client-server)
* [Profiling Monitors](/docs/developing/develop-profiling-monitor)

## Client/Server Topology Support
The Networking workload profiles ALL require a client/server topology in order to operate. This means that there must be 2 distinct systems in order
to run the workload. One of the systems operates in the 'Client' role. The other system operates in the 'Server' role. The Virtual Client running on
the client and server systems will synchronize with each other before running each individual workload. An environment layout file MUST be supplied
to each instance of the Virtual Client on the command line to describe the IP address/location of other Virtual Client instances. See the section below 
on 'Client/Server Topologies'.

[Environment Layouts](/docs/guides/client-server)

In the environment layout file provided to the Virtual Client, define the role of the client system/VM as "Client" and the role of the server system/VM as "Server".
The spelling of the roles must be exact. The IP addresses of the systems/VMs must be correct as well. The following example illustrates the
idea. The name of the client must match the name of the system or the value of the agent ID passed in on the command line.

``` bash
# Client role system
VirtualClient.exe --profile=PERF-NETWORK-CTSTRAFFIC.json --timeout=1440 --client-id=Client01 --layout=C:\any\path\to\layout.json

# Server role system
VirtualClient.exe --profile=PERF-NETWORK-CTSTRAFFIC.json --timeout=1440 --client-id=Server01 --layout=C:\any\path\to\layout.json

# Example contents of the 'layout.json' file:
{
    "clients": [
        {
            "name": "Client01",
            "role": "Client",
            "privateIPAddress": "10.1.0.1"
        },
        {
            "name": "Server01",
            "role": "Server",
            "privateIPAddress": "10.1.0.2"
        }
    ]
}
```

# PERF-NETWORK-CTSTRAFFIC.json
Runs the ctsTraffic workload on the system to evaluate network connection scalability, throughput, and reliability between client and server endpoints.

* **Supported Platform-Architectures**  
  * win-x64
  * win-arm64

* **Dependencies**  
  The dependencies defined in the 'Dependencies' section of the profile itself are required in order to run the   workload operations effectively.
  * Internet connection.
  * The IP addresses defined in the environment layout for the Client and Server systems must be correct.
  * The name of the Client and Server instances defined in the environment layout must match the agent/client IDs   supplied on the command line (e.g., `--client-id`) or must match the name of the system as defined by the   operating system itself.
  * The port used by the ctsTraffic workload (4444, as defined in the profile parameters) must NOT be used by   other applications or services on the systems in which they are running or different ports must be used.
  
  Note that the port used for the workload can be optionally modified on the command line (e.g.,   `--parameters="Port=4445"`).

  Additional information on components that exist within the 'Dependencies' section of the profile can be found in   the following locations:  
  [Installing Dependencies](/docs/category/dependencies/)

* **Profile Runtimes**  
See the 'Metadata' section of the profile for estimated runtimes. These timings represent the length of time required to run a single round of profile actions. These timings can be used to determine minimum required runtimes for the Virtual Client in order to get results.

* **Usage Examples**  
The following section provides a few basic examples of how to use the workload profile.

  ```bash
  # On the Client role system
  ./VirtualClient.exe --profile=PERF-NETWORK-CTSTRAFFIC.json --system=Demo --timeout=1440 --client-id=Client01   --layout="C:\path\to\layout.json"
  
  # On the Server role system
  ./VirtualClient.exe --profile=PERF-NETWORK-CTSTRAFFIC.json --system=Demo --timeout=1440 --client-id=Server01   --layout="C:\path\to\layout.json"
  
  # Client/server layouts can be defined on the command line explicitly as well (i.e. no file required).
  # Format = {client_id},{client_ip_address},{client_role};{server_id},{server_ip_address},{server_role}
  #
  # Client role system
  VirtualClient.exe --profile=PERF-NETWORK-CTSTRAFFIC.json --timeout=1440 --client-id=Client01 --layout="Client01,  10.1.0.1,Client;Server01,10.1.0.2,Server"
  
  # Server role system
  VirtualClient.exe --profile=PERF-NETWORK-CTSTRAFFIC.json --timeout=1440 --client-id=Server01 --layout="Client01,  10.1.0.1,Client;Server01,10.1.0.2,Server"
  ```
