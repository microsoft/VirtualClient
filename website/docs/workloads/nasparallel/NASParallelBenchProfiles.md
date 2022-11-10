# NAS Parallel Workload Profiles
The following profiles run customer-representative or benchmarking scenarios using the NAS Parallel toolset.

* [Workload Details](./NASParallelBench.md)
* [Workload Metrics](./NASParallelBenchs.md)
* [Workload Packages](./DependencyPackages.md)
* [Usage Scenarios/Examples](./UsageScenarios.md)

-----------------------------------------------------------------------

### Preliminaries

The profiles below require the ability to download workload packages and dependencies from a package store. In order to download the workload packages, connection information must be supplied on the command line. See the 'Workload Packages' documentation above for details on how that works.

-----------------------------------------------------------------------

### Topology Requirements 
The NAS Parallel workload profiles can run both on single and multiple nodes within same subnet to run the workload. One system operates in the 'Client' role making calls to zero or more servers. zero or more systems operate in the 'Server' role. (1-N relation where N can be [0,1,2...])

The Virtual Client running on the client and server systems will synchronize with each other before running each individual workload. An environment layout
file MUST be supplied to each instance of the Virtual Client on the command line to describe the IP address/location of other Virtual Client instances. See 
the section below on 'Client/Server Topologies'.

[Environment Layouts](./EnvironmentLayouts.md)

In the environment layout file provided to the Virtual Client, define the role of the client system/VM as "Client" and the role of the server system(s)/VM(s) as "Server".
The spelling of the roles must be exact. The IP addresses of the systems/VMs must be correct as well. The following example illustrates the
idea. The name of the client must match the name of the system or the value of the agent ID passed in on the command line.

For different benchmarks with NAS Parallel we have various recommendation on number of nodes as mentioned below.
* BT, SP         - a square number of processes (1, 4, 9, ...)
* LU             - 2D (n1 * n2) process grid where n1/2 <= n2 <= n1
* CG, FT, IS, MG - a power-of-two number of processes (1, 2, 4, ...)
* EP         - no special requirement
* DC,UA          - Run only on single machine.
* DT             - Minimum 5 machines required.

So for the whole profile 1,4,16... are the recommended numbers.


```bash
// Client role system (the controller)
./VirtualClient --profile=PERF-HPC-NASPARALLELBENCH.json --system=Azure --timeout=1440 --agentId=AnyVM01 --layoutPath=/any/path/to/layout.json

// Server role system #1
./VirtualClient --profile=PERF-HPC-NASPARALLELBENCH.json --system=Azure --timeout=1440 --agentId=AnyVM02 --layoutPath=/any/path/to/layout.json

// Server role system #2
./VirtualClient --profile=PERF-HPC-NASPARALLELBENCH.json --system=Azure --timeout=1440 --agentId=AnyVM03 --layoutPath=/any/path/to/layout.json

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
        },
        {
            "name": "AnyVM03",
            "role": "Server",
            "privateIPAddress": "10.1.0.3"
        }
    ]
}
```

-----------------------------------------------------------------------

### PERF-HPC-NASPARALLELBENCH.json
Runs a set of HPC workloads using NAS Parallel Benchmarks to the parallel computing performance.
This profile is designed to test both single and multiple nodes performance.

* **Supported Platform/Architectures**
  * linux-x64
  * linux-arm64

* **Dependencies**  
  The following dependencies must be met to run this workload profile.

  * Workload package must exist in the 'packages' directory or connection information for the package store supplied on the command line (see 'Workload Packages' link above).
  * The IP addresses defined in the environment layout (see above) for the Client and Server systems must be correct.
  * The name of the Client and Server instances defined in the environment layout must match the agent/client IDs supplied on the command line (e.g. --agentId)
    or must match the name of the system as defined by the operating system itself.

* **Profile Parameters**  
  The following parameters can be optionally supplied on the command line to modify the behaviors of the workload. See the 'Usage Scenarios/Examples' above for examples on how to supply parameters to 
  Virtual Client profiles.

  | Parameter                 | Purpose                                                                         | Default Value |
  |---------------------------|---------------------------------------------------------------------------------|---------------|
  | Username                  | Required. See [SSH Requirements](#SSH-Requirements) | NULL |


* **Recommended Configurations**  
  
* **Usage Examples**  
  The following section provides a few basic examples of how to use the workload profile. Additional usage examples can be found in the
  'Usage Scenarios/Examples' link at the top.


  ``` csharp
  ./VirtualClient --profile=PERF-HPC-NASPARALLELBENCH.json --system=Azure --timeout=1440 --packageStore="{BlobConnectionString|SAS Uri}" --parameters="Username=virtualcli..." --layoutPath="/any/path/to/layout.json"
  ./VirtualClient --profile=PPERF-HPC-NASPARALLELBENCH.json --system=Azure --timeout=1440 --packageStore="{BlobConnectionString|SAS Uri}" --parameters="Username=virtualcli..."
  ```

#### SSH Requirements
OpenMPI sends messages over port 22 - as well as expects to send messages without having to supply a key or passsword. A secure and safe way is to register an SSH identity with the
client machine. Here is an example [blog post](https://linuxize.com/post/how-to-setup-passwordless-ssh-login/) on how to do this. Although the basic steps are:
- On client, store a private-public key pair under ~/.ssh/id_rsa and ~/.ssh/id_rsa.pub
- On server, append the id_rsa.pub generated under ~/.ssh/authorized_keys
- On client, store server fingprints in ~/.ssh/known_hosts 
- Last when running the profile, supply the username whos .ssh directory contains all of the files just created/edited. 

-----------------------------------------------------------------------

### Client/Server Topologies
The NAS Parallel workload can be a client/server(It supports single machine scenario too) scenario whereby a controller/host makes SSH requests to target clients to distribute work. As such this workload
is only valid when ran in some form of client/server topology. The requirement is that there must be at least 2 systems in order to create a proper
client/server interaction. In Azure Cloud environments, these 2 systems may be 2 virtual machines that run on the same physical node/blade. This scenario
will test performance that includes network performance aspects through a Hyper-V virtual switch. These 2 systems may also be 2 virtual machines that
run on different physical nodes/blades. This scenario will test performance through the physical network of a data center rack or cluster. Data center
racks have a top-of-rack network/T1 switch (TOR). Data center racks are connected by T2 network switches. The default production scenario targets the
physical network systems and typically SameCluster/T2 network switches.

The Virtual Client itself does not create these topologies or the virtual machines etc... within them. This is a feature expected of the user or
the automation running the Virtual Client application. For example, at Azure, it has engineering system that is capable of creating advanced topologies into which the
Virtual Client is deployed. Once the environment is setup, it is easy to provide topology/layout information to the Virtual Client so that each
instance running on a given system knows about all of the other instances and additionally knows what its role to play in the client/server workload
execution process is.

See [Environment Layouts](./EnvironmentLayouts.md) for more information.
