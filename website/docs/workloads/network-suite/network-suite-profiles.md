# Network Workload Suite Profiles
The following profiles run customer-representative or benchmarking scenarios using the Azure Networking benchmark
workload suite.

* [Workload Details](./network-suite.md)  
* [Workload Profile Metrics](./network-suite-metrics.md)  


-----------------------------------------------------------------------

### Preliminaries
The profiles below require the ability to download workload packages and dependencies from a package store. In order to download the workload packages, connection information 
must be supplied on the command line. See the 'Workload Packages' documentation above for details on how that works.

Note that The virtual machines should have accelerated networking enabled. The systems will be configured for busy polling as well as for additional TCP ephemeral port options. The following settings 
are used to configure the systems. These settings were provided to the team by partner teams during the initial onboarding of the Networking workload and have been additionally confirmed by 
the Azure Edge and Platform Network Performance team.

:::danger
The systems are rebooted on first run after having these settings applied. The Virtual Client must be restarted to continue the execution of the Network workload. For testing purposes, this behaviors
can be disabled by supplying the 'ConfigureNetwork=false' parameter on the command line as noted in the examples below. For production operation, these system level settings should be applied.
:::


```
# On Windows systems
# ------------------------------------------------------------------
PowerShell.exe Set-NetTCPSetting -AutoReusePortRangeStartPort 10000 -AutoReusePortRangeNumberOfPorts 50000"

# On Linux systems
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

### Topology Requirements (Client/Server)
The Networking workload profiles ALL require a client/server topology in order to operate. This means that there must be 2 distinct systems in order
to run the workload. One of the systems operates in the 'Client' role. The other system operates in the 'Server' role. The Virtual Client running on
the client and server systems will synchronize with each other before running each individual workload. An environment layout file MUST be supplied
to each instance of the Virtual Client on the command line to describe the IP address/location of other Virtual Client instances. See the section below 
on 'Client/Server Topologies'.

[Environment Layouts](../../guides/0020-client-server.md)

In the environment layout file provided to the Virtual Client, define the role of the client system/VM as "Client" and the role of the server system/VM as "Server".
The spelling of the roles must be exact. The IP addresses of the systems/VMs must be correct as well. The following example illustrates the
idea. The name of the client must match the name of the system or the value of the agent ID passed in on the command line.



```csharp
// Client role system
VirtualClient.exe --profile=PERF-NETWORK.json --system=Azure --timeout=1440 --agentId=AnyVM01 --layoutPath=C:\any\path\to\layout.json

// Server role system
VirtualClient.exe --profile=PERF-NETWORK.json --system=Azure --timeout=1440 --agentId=AnyVM02 --layoutPath=C:\any\path\to\layout.json

// Example contents of the 'layout.json' file below:
```

```json
{
    "clients": [
        {
            "name": "AnyVM01",
            "role": "Client",
            "privateIPAddress": "10.1.0.1"
        },
        {
            "name": "AnyVM02",
            "role": "Server",
            "privateIPAddress": "10.1.0.2"
        }
    ]
}
```

### Client/Server Topologies
The Network suite of workloads are a client/server scenario whereby clients make TCP and UDP requests to a server host. As such this workload
is only valid when ran in some form of client/server topology. The requirement is that there must be exactly 2 systems in order to create a proper
client/server interaction. In Azure Cloud environments, these 2 systems may be 2 virtual machines that run on the same physical node/blade. This scenario
will test performance that includes network performance aspects through a Hyper-V virtual switch. These 2 systems may also be 2 virtual machines that
run on different physical nodes/blades. This scenario will test performance through the physical network of a data center rack or cluster. Data center
racks have a top-of-rack network/T1 switch (TOR). Data center racks are connected by T2 network switches. The default production scenario targets the
physical network systems and typically SameCluster/T2 network switches.

The Virtual Client itself does not create these topologies or the virtual machines etc... within them. This is a feature expected of the user or
the automation running the Virtual Client application. Once the environment is setup, it is easy to provide topology/layout information to the Virtual Client so that each
instance running on a given system knows about all of the other instances and additionally knows what its role to play in the client/server workload
execution process is.

See [Environment Layouts](../../guides/0020-client-server.md) for more information.

-----------------------------------------------------------------------

### Resources
* [Azure VM Sizes](https://docs.microsoft.com/en-us/azure/virtual-machines/sizes)
* [Azure Managed Disks](https://azure.microsoft.com/en-us/pricing/details/managed-disks/)