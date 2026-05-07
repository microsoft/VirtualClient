# NAS Parallel Workload Profiles
The following profiles run customer-representative or benchmarking scenarios using the NAS Parallel toolset.

* [Workload Details](./nasparallel.md)
* [Client/Server Workloads](../../guides/0020-client-server.md)

## Client/Server Topology Support
NAS Parallel workload profiles support running the workload on both a single system as well as in a multi-system, client/server topology. This means that the workload supports
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
  2D (n1 * n2) process grid where  n1/2 {'<='} n2 {'<='} n1 .

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
./VirtualClient --profile=PERF-HPC-NASPARALLELBENCH.json --system=Demo --timeout=1440 --client-id=Client01 --layout=/any/path/to/layout.json

# On Server role system #1
./VirtualClient --profile=PERF-HPC-NASPARALLELBENCH.json --system=Demo --timeout=1440 --client-id=Server01 --layout=/any/path/to/layout.json

# On Server role system #2
./VirtualClient --profile=PERF-HPC-NASPARALLELBENCH.json --system=Demo --timeout=1440 --client-id=Server02 --layout=/any/path/to/layout.json

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
OpenMPI sends messages over port 22 - as well as expects to send messages without having to supply a key or password. A secure and safe way is to register an SSH identity with the
client machine. Here is an example [blog post](https://linuxize.com/post/how-to-setup-passwordless-ssh-login/) on how to do this. Although the basic steps are:
- On client, store a private-public key pair under ~/.ssh/id_rsa and ~/.ssh/id_rsa.pub
- On server, append the id_rsa.pub generated under ~/.ssh/authorized_keys
- On client, store server fingerprints in ~/.ssh/known_hosts 
- Last when running the profile, supply the username whose .ssh directory contains all of the files just created/edited. 

## Resource Requirements
NPB defines problem classes that control the size of the workload. Classes range from **S** (smallest) to **F** (largest). The default class used in this
profile is **C** (configurable via the `Benchmarkclass` parameter). Larger classes require significantly more memory and compute time.

The table below provides estimated **minimum memory required per node** for each class. These estimates are driven by the most memory-intensive benchmarks
in the profile (FT and BT/SP/LU). Lighter benchmarks such as EP, IS, and CG require substantially less memory.

| Class | Size Category | Estimated Min. Memory per Node | Typical Use Case |
|-------|---------------|-------------------------------|------------------|
| S | Sample | < 200 MB | Quick smoke testing and validation |
| W | Workstation | < 1 GB | Basic workstation testing |
| A | Small | ~1 GB | Small-scale testing |
| B | Medium | ~4 GB | Standard testing |
| **C** | **Large (default)** | **~8 GB** | **Performance benchmarking** |
| D | Extra Large | ~128 GB | Large-system benchmarking |
| E | Huge | ~1 TB | Supercomputer-scale testing |
| F | Extreme | ~8 TB | Extreme-scale testing |

### How to Estimate Memory
The most memory-intensive benchmark in the profile is **FT** (3D Fast Fourier Transform), which stores complex double-precision arrays. A simple formula to
estimate FT memory is:

```
Memory ≈ 3 × grid_x × grid_y × grid_z × 16 bytes
```

For example, FT at Class C uses a 512 × 512 × 512 grid:
```
3 × 512 × 512 × 512 × 16 bytes ≈ 6.4 GB
```

The **BT**, **SP**, and **LU** benchmarks (3D fluid dynamics solvers) use the formula:
```
Memory ≈ 5 × grid_size³ × 8 bytes × 4 (working copies)
```

For example, BT at Class C uses a 162 × 162 × 162 grid:
```
5 × 162³ × 8 × 4 ≈ 680 MB
```

As a rule of thumb, memory requirements increase by roughly **8x between consecutive classes** because grid dimensions double in each spatial direction (2³ = 8).

For the full table of problem sizes and grid dimensions for each benchmark and class, see the
[NPB Problem Sizes](https://www.nas.nasa.gov/software/npb_problem_sizes.html) reference on the NASA website.

## PERF-HPC-NASPARALLELBENCH.json
Runs a set of HPC workloads using NAS Parallel Benchmarks to the parallel computing performance. This profile is designed to test both single and 
multiple nodes performance.

* [Workload Profile](https://github.com/microsoft/VirtualClient/blob/main/src/VirtualClient/VirtualClient.Main/profiles/PERF-HPC-NASPARALLELBENCH.json) 

* **Supported Platform/Architectures**
  * linux-x64
  * linux-arm64

* **Supports Disconnected Scenarios**  
  * No. Internet connection required.

* **Dependencies**  
  The dependencies defined in the 'Dependencies' section of the profile itself are required in order to run the workload operations effectively. 
  * Internet connection.
  * For multi-system scenarios, communications over SSH port 22 must be allowed.
  * The IP addresses defined in the environment layout (see above) for the Client and Server systems must be correct.
  * The name of the Client and Server instances defined in the environment layout must match the agent/client IDs supplied on the command line (e.g. --client-id)
    or must match the name of the system as defined by the operating system itself.
    
  Additional information on individual components that exist within the 'Dependencies' section of the profile can be found in the following locations:
  * [Installing Dependencies](https://microsoft.github.io/VirtualClient/docs/category/dependencies/).

* **Profile Parameters**  
  The following parameters can be optionally supplied on the command line to modify the behaviors of the workload.

  | Parameter                 | Purpose                                                                         | Default Value |
  |---------------------------|---------------------------------------------------------------------------------|---------------|
  | Username                  | Required. See 'SSH Requirements' above                                          | No default, must be supplied |
  | Benchmarkclass            | Optional. The NPB problem class size (S, W, A, B, C, D, E, F). Larger classes require more memory and compute time. See [Resource Requirements](#resource-requirements). | C |
  | ThreadCount               | Optional. The number of threads for OpenMP parallelism (OMP_NUM_THREADS). On systems with 2 or fewer cores, this is automatically set to 1. | Logical core count - 2 |

  :::note
  The **DC** (Data Cube) benchmark is pinned to class **B** in the profile regardless of the `Benchmarkclass` parameter
  because DC only supports classes S, W, A, and B.
  :::
= 8).

For the full table of problem sizes and grid dimensions for each benchmark and class, see the
[NPB Problem Sizes](https://www.nas.nasa.gov/software/npb_problem_sizes.html) reference on the NASA website.

## PERF-HPC-NASPARALLELBENCH.json
Runs a set of HPC workloads using NAS Parallel Benchmarks to the parallel computing performance. This profile is designed to test both single and 
multiple nodes performance.

* [Workload Profile](https://github.com/microsoft/VirtualClient/blob/main/src/VirtualClient/VirtualClient.Main/profiles/PERF-HPC-NASPARALLELBENCH.json) 

* **Supported Platform/Architectures**
  * linux-x64
  * linux-arm64

* **Supports Disconnected Scenarios**  
  * No. Internet connection required.

* **Dependencies**  
  The dependencies defined in the 'Dependencies' section of the profile itself are required in order to run the workload operations effectively. 
  * Internet connection.
  * For multi-system scenarios, communications over SSH port 22 must be allowed.
  * The IP addresses defined in the environment layout (see above) for the Client and Server systems must be correct.
  * The name of the Client and Server instances defined in the environment layout must match the agent/client IDs supplied on the command line (e.g. --client-id)
    or must match the name of the system as defined by the operating system itself.
    
  Additional information on individual components that exist within the 'Dependencies' section of the profile can be found in the following locations:
  * [Installing Dependencies](https://microsoft.github.io/VirtualClient/docs/category/dependencies/).

* **Profile Parameters**  
  The following parameters can be optionally supplied on the command line to modify the behaviors of the workload.

  | Parameter                 | Purpose                                                                         | Default Value |
  |---------------------------|---------------------------------------------------------------------------------|---------------|
  | Username                  | Required. See 'SSH Requirements' above                                          | No default, must be supplied |
  | ThreadCount               | Optional. The number of threads for OpenMP parallelism (OMP_NUM_THREADS). On systems with 2 or fewer cores, this is automatically set to 1. | Logical core count - 2 |

* **Profile Runtimes**  
  See the 'Metadata' section of the profile for estimated runtimes. These timings represent the length of time required to run a single round of profile 
  actions. These timings can be used to determine minimum required runtimes for the Virtual Client in order to get results. These are often estimates based on the
  number of system cores. 

* **Usage Examples**  
  The following section provides a few basic examples of how to use the workload profile.

  ``` bash
  # When running on a single system (environment layout not required)
  ./VirtualClient --profile=PERF-HPC-NASPARALLELBENCH.json --system=Demo --timeout=1440 --parameters="Username=testuser"

   # When running in a client/server environment
  ./VirtualClient --profile=PERF-HPC-NASPARALLELBENCH.json --system=Demo --timeout=1440 --client-id=Client01 --parameters="Username=testuser" --layout="/any/path/to/layout.json"
  ./VirtualClient --profile=PERF-HPC-NASPARALLELBENCH.json --system=Demo --timeout=1440 --client-id=Server01 --parameters="Username=testuser" --layout="/any/path/to/layout.json"

  # When running in a client/server environment with additional systems
  ./VirtualClient --profile=PERF-HPC-NASPARALLELBENCH.json --system=Demo --timeout=1440 --client-id=Client01 --parameters="Username=testuser" --layout="/any/path/to/layout.json"
  ./VirtualClient --profile=PERF-HPC-NASPARALLELBENCH.json --system=Demo --timeout=1440 --client-id=Server01 --parameters="Username=testuser" --layout="/any/path/to/layout.json"
  ./VirtualClient --profile=PERF-HPC-NASPARALLELBENCH.json --system=Demo --timeout=1440 --client-id=Server02 --parameters="Username=testuser" --layout="/any/path/to/layout.json"
  ```
