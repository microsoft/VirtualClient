# StressAppTest Workload Profiles
The following profiles run customer-representative or benchmarking scenarios using the StressAppTest workload.

* [Workload Details](./stressapptest.md)

## PERF-MEM-STRESSAPPTEST.json
Runs the StressAppTest workload for a specific period of time on the system. This profile is designed to allow the user to run the workload for
the purpose of evaluating the performance of the Memory over various periods of time while allowing the user to apply a longer-term stress
to the system if desired.

* **Supported Platform/Architectures**
  * linux-x64
  * linux-arm64

* **Supports Disconnected Scenarios**
  * Yes. When the StressAppTest package is included in 'packages' directory of the Virtual Client.
    * [Installing VC Packages](../../dependencies/0001-install-vc-packages.md).

* **Dependencies**
  The dependencies defined in the 'Dependencies' section of the profile itself are required in order to run the workload operations effectively.
  * Internet connection.

  Additional information on components that exist within the 'Dependencies' section of the profile can be found in the following locations:
  * [Installing Dependencies](https://microsoft.github.io/VirtualClient/docs/category/dependencies/)

* **Profile Parameters**
  The following parameters can be optionally supplied on the command line to modify the behaviors of the workload.

  | Parameter | Purpose | Acceptable Range | Default Value |
  |-----------|---------|------------------|---------------|
  | Duration | Time to run StressAppTest Memory Workload. Accepts seconds (e.g. 60) or TimeSpan (e.g. 00:01:00) | >0 | 60 seconds |
  | UseCpuStressfulMemoryCopy | Switch to toggle StressAppTest built-in CPU Stressful Memory Copy option (-W flag). If enabled (true), CPU Usage should increase | true/false | false |
  | MemoryInMB | Megabytes of RAM to test (-M flag). When not specified, stressapptest auto-detects (~94% of system memory) | >0 | not set (auto-detect) |
  | ThreadCount | Number of memory copy threads to run (-m flag). When not specified, stressapptest defaults to 2 threads per CPU | >0 | not set (auto-detect) |
  | CpuStressThreadCount | Number of CPU stress threads to run (-C flag). When not specified, no CPU stress threads are added | >=0 | not set (none) |

* **Profile Runtimes**  
  See the 'Metadata' section of the profile for estimated runtimes. These timings represent the length of time required to run a single round of profile 
  actions. These timings can be used to determine minimum required runtimes for the Virtual Client in order to get results. These are often estimates based on the
  number of system cores. 

* **Usage Examples**
  The following section provides a few basic examples of how to use the workload profile.

  ```bash
  # Execute the workload profile
  ./VirtualClient --profile=PERF-MEM-STRESSAPPTEST.json --system=Demo --timeout=90

  # Override the default parameters to run the workload for a longer period of time
  ./VirtualClient --profile=PERF-MEM-STRESSAPPTEST.json --system=Demo --timeout=90 --parameters="Duration=240"

  # Override the default parameters to change the "torture settings" when running the workload.
  ./VirtualClient --profile=PERF-MEM-STRESSAPPTEST.json --system=Demo --timeout=90 --parameters="Duration=240,,,UseCpuStressfulMemoryCopy=true"

  # Limit memory to 2048 MB with 8 memory threads and 4 CPU stress threads
  ./VirtualClient --profile=PERF-MEM-STRESSAPPTEST.json --system=Demo --timeout=90 --parameters="Duration=240,,,MemoryInMB=2048,,,ThreadCount=8,,,CpuStressThreadCount=4"
  ```
