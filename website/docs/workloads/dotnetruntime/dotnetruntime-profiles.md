# DotNetRuntime Workload Profiles
The following profiles run customer-representative or benchmarking scenarios using the .NET Runtime workload.  

* [Getting Started](https://microsoft.github.io/VirtualClient/)
* [Workload Details](./dotnetruntime.md)  

## PERF-CPU-DOTNETRUNTIME.json
Runs a CPU-intensive workload using the DotNetRuntime toolset to test the performance of the CPU in processing transactions to the database(warehouse).
This profile is designed by the Intel team as part of Cloud R1 Workload to identify general/broad regressions when compared against a baseline.

* [Workload Profile](https://msazure.visualstudio.com/One/_git/CRC-AIR-Workloads?path=/src/VirtualClient/CRC.VirtualClient.Packaging/profiles/PERF-CPU-DOTNETRUNTIME.json)  

* **Supported Platform/Architectures**
  * win-x64
  * win-arm64

* **Supported Operating Systems**
  * Windows

* **Dependencies**  
  The dependencies defined in the 'Dependencies' section of the profile itself are required in order to run the workload operations effectively.
  * Internet connection.

  Additional information on components that exist within the 'Dependencies' section of the profile can be found in the following locations:
  * [Installing Dependencies](https://microsoft.github.io/VirtualClient/docs/category/dependencies/)

* **Profile Parameters**  
  The following parameters can be optionally supplied on the command line to modify the behaviors of the workload.

  | Parameter                 | Purpose                                                                         | Default value |
  |---------------------------|---------------------------------------------------------------------------------|---------------|
  | NumberOfJvmInstances      | Optional. This specifies the number of JVM(Java Virtual Machine) application instances to run. | 1 
  | NumberOfWarehouses        | Optional. This specifies the number of warehouses that we can have. | 8 
  | RampUpSeconds             | Optional. This specifies the time in seconds that the workload will ramp up/warm up. | 60 
  | MeasurementSeconds        | Optional. This specifies the time in seconds that the workload will run. | 3600 
  | FixedThroughput           | Optional. This specifies the target throughput. The workload will try to target the specified throughput. | 16000 |

* **Profile Runtimes**  
  The following timings represent the length of time required to run a single round of profile actions. These timings can be used to determine
  minimum required runtimes for the Virtual Client in order to get results. These are estimates based on the number of system cores.
  This particular workload runtime is affected directly by the value of the 'MeasurementSeconds' parameter of the  profile. The larger the value,
  the longer the runtime.

  * Expected Runtime = 1 hour

* **Usage Examples**  
  The following section provides a few basic examples of how to use the workload profile.

  ``` bash
  # Execute the workload profile
  VirtualClient.exe --profile=PERF-CPU-DOTNETRUNTIME.json --system=Juno --timeout=1440 --packageStore="{BlobConnectionString|SAS Uri}"

  # Increase the scale factors.
  VirtualClient.exe --profile=PERF-CPU-DOTNETRUNTIME.json --system=Juno --timeout=1440 --packageStore="{BlobConnectionString|SAS Uri}" --parameters=NumberOfJvmInstances=2
  VirtualClient.exe --profile=PERF-CPU-DOTNETRUNTIME.json --system=Juno --timeout=1440 --packageStore="{BlobConnectionString|SAS Uri}" --parameters=NumberOfJvmInstances=2,,,NumberOfWarehouses=16
  ```