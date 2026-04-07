# OpenFOAM Workload Profiles
The following profiles run customer-representative or benchmarking scenarios using the OpenFOAM workload.

* [Workload Details](./openfoam.md)  

## PERF-OPENFOAM.json
Runs the OpenFOAM workload which measures performance in terms of iterations per minute. 

* [Workload Profile](https://github.com/microsoft/VirtualClient/blob/main/src/VirtualClient/VirtualClient.Main/profiles/PERF-OPENFOAM.json) 

* **Supported Platform/Architectures**
  * linux-x64
  * linux-arm64

* **Dependencies**  
  The dependencies defined in the 'Dependencies' section of the profile itself are required in order to run the workload operations effectively.
  * Internet connection.

  Additional information on components that exist within the 'Dependencies' section of the profile can be found in the following locations:
  * [Installing Dependencies](https://microsoft.github.io/VirtualClient/docs/category/dependencies/)

* **Scenarios**  
  The following scenarios are covered by this workload profile.
  
  * 'airFoil2D' simulation using simpleFoam solver.
  * 'elbow' simulation using icoFoam solver.
  * 'lockExchange' simulation using twoLiquidMixingFoamSolver.
  * 'motorBike' simulation using simpleFoam solver (on linux-x64 systems only).
  * 'pitzDaily' simulation using simpleFoam solver.

* **Profile Parameters**  
  The following parameters can be optionally supplied on the command line to modify the behaviors of the workload.

  | Parameter                 | Purpose                                                                         | Default Value |
  |---------------------------|---------------------------------------------------------------------------------|---------------|
  | Iterations                | Optional. Number of iterations for which particular simulation will be run.     | 500 |

* **Profile Runtimes**  
  See the 'Metadata' section of the profile for estimated runtimes. These timings represent the length of time required to run a single round of profile 
  actions. These timings can be used to determine minimum required runtimes for the Virtual Client in order to get results. These are often estimates based on the
  number of system cores. 

* **Usage Examples**  
  The following section provides a few basic examples of how to use the workload profile.

  ``` bash
  ./VirtualClient --profile=PERF-OPENFOAM.json 
  ./VirtualClient --profile=PERF-OPENFOAM.json --parameters=Iterations=10
  ./VirtualClient --profile=PERF-OPENFOAM.json --scenarios=airFoil2D,elbow,motorBike

  ```