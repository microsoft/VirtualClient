# NAS Parallel Workload Profiles
The following profiles run customer-representative or benchmarking scenarios using the NAS Parallel toolset.

* [Workload Details](./nasparallel.md)
* [Client/Server Workloads](../../guides/0020-client-server.md)

## Client/Server Topology Support
NAS Parallel workload profiles support running the workload on both a single system as well as in an multi-system, client/server topology. This means that the workload supports
operation on a single system or on N number of distinct systems. The client/server topology is typically used when it is desirable to include a network component in the
overall performance evaluation. In a client/server topology, one system operates in the 'Client' role making calls to the system operating in the 'Server' role. 
The Virtual Client instances running on the client and server systems will synchronize with each other before running the workload. In order to support a client/server topology,
an environment layout file MUST be supplied to each instance of the Virtual Client on the command line to describe the IP address/location of other Virtual Client instances. An
environment layout file is not required for the single system topology.

The Virtual Client running on the client and server systems will synchronize with each other before running each individual workload. An environment layout
file MUST be supplied to each instance of the Virtual Client on the command line to describe the IP address/location of other Virtual Client instances.

[Environment Layouts](../../guides/0020-client-server.md)

In the environment layout file provided to the Virtual Client, define the role of the client system/VM as "Client" and the role of the server system(s)/VM(s) as "Server".
The spelling of the roles must be exact. The IP addresses of the systems/VMs must be correct as well. The following example illustrates the
idea. The name of the client must match the name of the system or the value of the agent ID passed in on the command line.

For different benchmarks with NAS Parallel we have various recommendation on number of nodes as mentioned below.
* BT, SP benchmarks  
  A square number of processes (1, 4, 9, ...).

* LU benchmark  
  2D (n1 * n2) process grid where n1/2 <= n2 <= n1.

* CG, FT, IS, MG benchmarks  
  a power-of-two number of processes (1, 2, 4, ...).

* EP benchmark  
  No special requirements.

* DC,UA benchmarks  
  Run only on single machine.

* DT benchmark  
  Minimum of 5 machines required.

```bash
# Single System (environment layout not required)
./VirtualClient --profile=PERF-HPC-NASPARALLELBENCH.json --system=Demo --timeout=1440

# Multi-System
# On the Client role system (the controller)
./VirtualClient --profile=PERF-HPC-NASPARALLELBENCH.json --system=Demo --timeout=1440 --clientId=Client01 --layoutPath=/any/path/to/layout.json

# On Server role system #1
./VirtualClient --profile=PERF-HPC-NASPARALLELBENCH.json --system=Demo --timeout=1440 --clientId=Server01 --layoutPath=/any/path/to/layout.json

# On Server role system #2
./VirtualClient --profile=PERF-HPC-NASPARALLELBENCH.json --system=Demo --timeout=1440 --clientId=Server02 --layoutPath=/any/path/to/layout.json

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
        },
        {
            "name": "Server02",
            "role": "Server",
            "privateIPAddress": "10.1.0.3"
        }
    ]
}
```

## SSH Requirements
OpenMPI sends messages over port 22 - as well as expects to send messages without having to supply a key or passsword. A secure and safe way is to register an SSH identity with the
client machine. Here is an example [blog post](https://linuxize.com/post/how-to-setup-passwordless-ssh-login/) on how to do this. Although the basic steps are:
- On client, store a private-public key pair under ~/.ssh/id_rsa and ~/.ssh/id_rsa.pub
- On server, append the id_rsa.pub generated under ~/.ssh/authorized_keys
- On client, store server fingprints in ~/.ssh/known_hosts 
- Last when running the profile, supply the username whos .ssh directory contains all of the files just created/edited. 

## PERF-HPC-NASPARALLELBENCH.json
Runs a set of HPC workloads using NAS Parallel Benchmarks to the parallel computing performance. This profile is designed to test both single and 
multiple nodes performance.

* [Workload Profile](https://github.com/microsoft/VirtualClient/blob/main/src/VirtualClient/VirtualClient.Main/profiles/PERF-HPC-NASPARALLELBENCH.json) 

* **Supported Platform/Architectures**
  * linux-x64
  * linux-arm64

* **Supported Operating Systems**
  * Ubuntu 18
  * Ubuntu 20
  * Ubuntu 22

* **Supports Disconnected Scenarios**  
  * No. Internet connection required.

* **Dependencies**  
  The dependencies defined in the 'Dependencies' section of the profile itself are required in order to run the workload operations effectively. 
  * Internet connection.
  * For multi-system scenarios, communications over SSH port 22 must be allowed.
  * The IP addresses defined in the environment layout (see above) for the Client and Server systems must be correct.
  * The name of the Client and Server instances defined in the environment layout must match the agent/client IDs supplied on the command line (e.g. --agentId)
    or must match the name of the system as defined by the operating system itself.
    
  Additional information on individual components that exist within the 'Dependencies' section of the profile can be found in the following locations:
  * [Installing Dependencies](https://microsoft.github.io/VirtualClient/docs/category/dependencies/).

* **Profile Parameters**  
  The following parameters can be optionally supplied on the command line to modify the behaviors of the workload.

  | Parameter                 | Purpose                                                                         | Default Value |
  |---------------------------|---------------------------------------------------------------------------------|---------------|
  | Username                  | Required. See 'SSH Requirements' above                                          | No default, must be supplied |

* **Profile Runtimes**  
  The following timings represent the length of time required to run a single round of profile actions. These timings can be used to determine
  minimum required runtimes for the Virtual Client in order to get results. These are estimates based on the number of system cores.

  * (2-cores/vCPUs) = 4 - 5 hours

* **Usage Examples**  
  The following section provides a few basic examples of how to use the workload profile.

  ``` bash
  # When running on a single system (environment layout not required)
  ./VirtualClient --profile=PERF-HPC-NASPARALLELBENCH.json --system=Demo --timeout=1440 --parameters="Username=testuser" --packageStore="{BlobConnectionString|SAS Uri}"

   # When running in a client/server environment
  ./VirtualClient --profile=PERF-HPC-NASPARALLELBENCH.json --system=Demo --timeout=1440 --clientId=Client01 --parameters="Username=testuser" --layoutPath="/any/path/to/layout.json" --packageStore="{BlobConnectionString|SAS Uri}"
  ./VirtualClient --profile=PERF-HPC-NASPARALLELBENCH.json --system=Demo --timeout=1440 --clientId=Server01 --parameters="Username=testuser" --layoutPath="/any/path/to/layout.json" --packageStore="{BlobConnectionString|SAS Uri}"

  # When running in a client/server environment with additional systems
  ./VirtualClient --profile=PERF-HPC-NASPARALLELBENCH.json --system=Demo --timeout=1440 --clientId=Client01 --parameters="Username=testuser" --layoutPath="/any/path/to/layout.json" --packageStore="{BlobConnectionString|SAS Uri}"
  ./VirtualClient --profile=PERF-HPC-NASPARALLELBENCH.json --system=Demo --timeout=1440 --clientId=Server01 --parameters="Username=testuser" --layoutPath="/any/path/to/layout.json" --packageStore="{BlobConnectionString|SAS Uri}"
  ./VirtualClient --profile=PERF-HPC-NASPARALLELBENCH.json --system=Demo --timeout=1440 --clientId=Server02 --parameters="Username=testuser" --layoutPath="/any/path/to/layout.json" --packageStore="{BlobConnectionString|SAS Uri}"
  ```
