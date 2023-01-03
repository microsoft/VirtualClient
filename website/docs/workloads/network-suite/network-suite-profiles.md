# Network Workload Suite Profiles
The following profiles run customer-representative or benchmarking scenarios using the suite of network workloads (CPS, NTttcp, Latte and SockPerf).

* [Workload Details](./network-suite.md)  
* [Client/Server Workloads](../../guides/0020-client-server.md)
* [Profiling Monitors](../../developing/0050-develop-profiling-monitor.md)

## Client/Server Topology Support
The Networking workload profiles ALL require a client/server topology in order to operate. This means that there must be 2 distinct systems in order
to run the workload. One of the systems operates in the 'Client' role. The other system operates in the 'Server' role. The Virtual Client running on
the client and server systems will synchronize with each other before running each individual workload. An environment layout file MUST be supplied
to each instance of the Virtual Client on the command line to describe the IP address/location of other Virtual Client instances. See the section below 
on 'Client/Server Topologies'.

[Environment Layouts](../../guides/0020-client-server.md)

In the environment layout file provided to the Virtual Client, define the role of the client system/VM as "Client" and the role of the server system/VM as "Server".
The spelling of the roles must be exact. The IP addresses of the systems/VMs must be correct as well. The following example illustrates the
idea. The name of the client must match the name of the system or the value of the agent ID passed in on the command line.

``` bash
# Client role system
VirtualClient.exe --profile=PERF-NETWORK.json --system=Demo --timeout=1440 --clientId=Client01 --layoutPath=C:\any\path\to\layout.json

# Server role system
VirtualClient.exe --profile=PERF-NETWORK.json --system=Demo --timeout=1440 --clientId=Server01 --layoutPath=C:\any\path\to\layout.json

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

## PERF-NETWORK.json
Runs the suite of workloads on the system to evaluate the average and peak performance of the network hardware and stack. On Linux systems, the CPS, NTttcp and SockPerf workloads
are used. On Windows systems the CPS, NTttcp and Latte workloads are used.

:::danger
*By default, the systems have settings applied to ensure the OS is configured for full network performance. These settings require the systems to be rebooted in order to be applied.
Unless otherwise directed, be aware that the systems will be rebooted on first run. This behavior can be excluded by using the 'ConfigureNetwork' parameter described below but should
not be modified in real performance measurement scenarios.*
:::

``` bash
# Settings applied on Windows systems
# ------------------------------------------------------------------
PowerShell.exe Set-NetTCPSetting -AutoReusePortRangeStartPort 10000 -AutoReusePortRangeNumberOfPorts 50000"

# Settings applied on Linux systems
# ------------------------------------------------------------------
# 1) The following changes are made to the '/etc/security/limits.conf' file:
*   soft    nofile  1048575
*   hard    nofile  1048575

# 2) The following changes are made to the '/etc/rc.local' file:
#!/bin/sh
sysctl -w net.ipv4.tcp_tw_reuse=1 # TIME_WAIT work-around
sysctl -w net.ipv4.ip_local_port_range=\"10000 60000\"  # ephemeral ports increased
iptables -t raw -I OUTPUT -j NOTRACK  # disable connection tracking
iptables -t raw -I PREROUTING -j NOTRACK  # disable connection tracking
sysctl -w net.core.busy_poll=50
sysctl -w net.core.busy_read=50
```

* [Workload Profile](https://github.com/microsoft/VirtualClient/blob/main/src/VirtualClient/VirtualClient.Main/profiles/PERF-NETWORK.json) 

* **Supported Platform/Architectures**
  * linux-x64
  * linux-arm64
  * win-x64
  * win-arm64

* **Supported Operating Systems**
  * Ubuntu 18
  * Ubuntu 20
  * Ubuntu 22
  * Windows 10
  * Windows 11
  * Windows Server 2016
  * Windows Server 2019

* **Supports Disconnected Scenarios**  
  * No. Internet connection required.

* **Dependencies**  
  The dependencies defined in the 'Dependencies' section of the profile itself are required in order to run the workload operations effectively.
  * Internet connection.
  * The IP addresses defined in the environment layout (see above) for the Client and Server systems must be correct.
  * The name of the Client and Server instances defined in the environment layout must match the agent/client IDs supplied on the command line (e.g. --agentId)
    or must match the name of the system as defined by the operating system itself.
  * The ports used for each of the network suite workloads (defined in the profile parameters) must NOT be used by other applications or services on the
    systems in which they are running or different ports must be used.

  Additional information on components that exist within the 'Dependencies' section of the profile can be found in the following locations:
  * [Installing Dependencies](https://microsoft.github.io/VirtualClient/docs/category/dependencies/)

* **Scenarios**  
  The following scenarios are covered by this workload profile.

  * CPS. Connection establishment speed and reliability, 16 concurrent connections.
  * NTttcp. Network communications throughput and bandwidth with the following scenarios.
    * TCP protocol, 4K network buffer, 1 thread
    * TCP protocol, 64K network buffer, 1 thread
    * TCP protocol, 256K network buffer, 1 thread
    * TCP protocol, 4K network buffer, 32 threads
    * TCP protocol, 64K network buffer, 32 thread
    * TCP protocol, 256K network buffer, 32 thread
    * TCP protocol, 4K network buffer, 256 thread
    * TCP protocol, 64K network buffer, 256 thread
    * TCP protocol, 256K network buffer, 256 thread
    * TCP protocol, 256K network buffer, 256 thread
    * UDP protocol, 1400 byte network buffer, 1 thread
    * UDP protocol, 1400 byte network buffer, 32 threads
  * Latte (on Windows). Network communication latencies.
    * TCP protocol latencies
    * UDP protocol latencies
  * SockPerf (on Linux). Network communication latencies.
    * TCP protocol, ping-pong scenario, max messages/sec, 64K message size
    * UDP protocol, ping-pong scenario, max messages/sec, 64K message size
    * TCP protocol, under load scenario, max messages/sec, 64K message size
    * UDP protocol, under load scenario, max messages/sec, 64K message size

* **Profile Parameters**  
  The following parameters can be optionally supplied on the command line to modify the behaviors of the workload.

  | Parameter                 | Purpose                                                           | Default Value |
  |---------------------------|-------------------------------------------------------------------|---------------|
  | ConfigureNetwork          | Optional. True to configure the system network settings for peak performance (will reboot system). False to make no changes to the system network settings.               | True (will reboot system)  |
  | EnableBusyPoll            | Optional. True to configure busy poll in the system network settings for Linux systems. False to make no changes. This setting depends upon 'ConfigureNetwork' = true.    | True        |
  | DisableFirewall           | Optional. True to disable the firewall on Linux systems. False to make no changes. This setting depends upon 'ConfigureNetwork' = true.  | True        |
  | CpsPort                   | Optional. The starting port on which connections will be established between client and server when running the CPS workload. The CPS workload will use connections on additional ports starting with this port for each of the number of connections (e.g. 16) defined in the component/action. | 7201 |
  | LattePort                 | Optional. The starting port on which connections will be established between client and server when running the Latte workload. The Latte workload will use connections on additional ports starting with this port. | 6100 |
  | NTttcpPort                | Optional. The starting port on which connections will be established between client and server when running the NTttcp workload. The NTttcp workload will use connections on additional ports starting with this port. | 5500 |
  | SockPerfPort              | Optional. The starting port on which connections will be established between client and server when running the SockPerf workload. The SockPerf workload will use connections on additional ports starting with this port. | 8201 |
  | ProfilingEnabled          | Optional. True if background profiling should be enabled while the workloads are running. False if not. When profiling is enabled, any number of profiles containing profiler monitors can be used to run the profiler toolsets within in the background. Set the documentation at the top for additional information on profiler monitors. | false |
  | ProfilingMode             | Optional. Defines the profiling mode (Interval or OnDemand). In 'Interval' mode, the profilers will run in the background constantly and independent of the workload(s). In 'OnDemand' mode, the profilers will be signaled by the workload(s) and will run ONLY while they are running. | None |

* **Profile Runtimes**  
  The following timings represent the length of time required to run a single round of profile actions. These timings can be used to determine
  minimum required runtimes for the Virtual Client in order to get results. These are estimates based on the number of system cores.

  * (2-cores/vCPUs) = 2 hours.

* **Usage Examples**  
  The following section provides a few basic examples of how to use the workload profile.

  ``` bash
  # On the Client role system
  ./VirtualClient --profile=PERF-NETWORK.json --system=Demo --timeout=1440 --clientId=Client01 --layoutPath="/any/path/to/layout.json" --packageStore="{BlobConnectionString|SAS Uri}"

  # On the Server role system
  ./VirtualClient --profile=PERF-NETWORK.json --system=Demo --timeout=1440 --clientId=Server01 --layoutPath="/any/path/to/layout.json" --packageStore="{BlobConnectionString|SAS Uri}"
  ```