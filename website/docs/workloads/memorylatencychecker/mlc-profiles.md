---
slug: /workloads/mlc-profiles
---

# Memory Latency Checker (MLC) Workload Profiles
The following profiles run customer-representative and benchmarking scenarios using the Intel Memory Latency Checker(MLC) workload.

* [Getting Started](https://microsoft.github.io/VirtualClient/)
* [Workload Details](./mlc.md)  

## PERF-MEM-LATENCY.json
Runs the MemoryLatencyChecker workload to measure memory latency and bandwidths against increasing load.

* [Workload Profile](https://github.com/microsoft/VirtualClient/blob/main/src/VirtualClient/VirtualClient.Main/profiles/PERF-MEM-LATENCY.json)  

* **Supported Platform/Architectures**
  * linux-x64
  * win-x64

* **Supported Operating Systems**
  * Azure Linux 3, 4
  * Ubuntu 20.04, 22.04, 24.04
  * RedHat 8, 9
  * Windows 10, 11
  * Windows Server 2019, 2022, 2025

* **Dependencies**  
  The dependencies defined in the 'Dependencies' section of the profile itself are required in order to run the workload operations effectively.
  * Internet connection.

  Additional information on components that exist within the 'Dependencies' section of the profile can be found in the following locations:
  * [Installing Dependencies](https://microsoft.github.io/VirtualClient/docs/category/dependencies/)

* **Profile Parameters**
  The following parameters can be optionally supplied on the command line to modify the behaviors of the workload.

  | Parameter           | Purpose | Default Value |
  |---------------------|---------|---------------|
  | CommandArguments    | Optional. A set of arguments to pass to the MLC toolset. See the [MLC Documentation](https://www.intel.com/content/www/us/en/developer/articles/tool/intelr-memory-latency-checker.html) for additional information. | |

* **Profile Runtimes**
  The following timings represent the length of time required to run a single round of profile actions. These timings can be used to determine
  minimum required runtimes for the Virtual Client in order to get results. These are estimates based on the number of system cores.

  * (4-cores/vCPUs) = 5 minutes
  * (16-cores/vCPUs) = 20 minutes

* **Usage Examples**
  The following section provides a few basic examples of how to use the workload profile.

  ``` csharp
  # Execute the workload profile
  VirtualClient.exe --profile=PERF-MEM-LATENCY.json --system=Demo --timeout=1440

  # Override the default MLC command line used
  VirtualClient.exe --profile=PERF-MEM-LATENCY.json --system=Demo --timeout=01.00:00:00 --parameters="CommandLineArguments=--peak_injection_bandwidth"
  ```