# OpenFOAM Workload Profiles
The following profiles run customer-representative or benchmarking scenarios using the OpenFOAM workload.

* [Workload Details](./OpenFOAM.md)  
* [Workload Profile Metrics](./OpenFOAMMetrics.md)


-----------------------------------------------------------------------

### Preliminaries
OpenFOAM workload profiles will download workload packages from a Blob Store. In order to download the
workload packages, an account key to the Blob Store must be provided on the command line. See the 'Workload Packages'
documentation above for details on how that works.

-----------------------------------------------------------------------

### PERF-OPENFOAM.json
Runs the OpenFOAM workload which measures performance in terms of iterations per minute. 

* **Supported Platform/Architectures**
  * linux-x64
  * linux-arm64

* **Dependencies**  
  The following dependencies must be met to run this workload profile.

  * Workload package must exist in the 'packages' directory or connection information for the package store supplied on the command line (see 'Workload Packages' link above).

* **Scenarios**  
  The following scenarios are covered by this workload profile.
  
  * 'airFoil2D' simulation using simpleFoam solver.
  * 'elbow' simulation using icoFoam solver.
  * 'lockExchange' simulation using twoLiquidMixingFoamSolver.
  * 'motorBike' simulation using simpleFoam solver.
  * 'pitzDaily' simulation using simpleFoam solver.

* **Profile Parameters**  
  The following parameters can be optionally supplied on the command line to modify the behaviors of the workload. See the 'Usage Scenarios/Examples' above for examples on how to supply parameters to 
  Virtual Client profiles.

  | Parameter                 | Purpose                                                                         | Default Value |
  |---------------------------|---------------------------------------------------------------------------------|---------------|
  | Iterations                | Optional. Number of iterations for which particular simulation will be run.     | 500 |

* **Workload Runtimes**  
  The following timings represent the length of time required to run a single round of tests ran. These timings can be used to determine
  minimum required runtimes for the Virtual Client in order to get results. These are estimates based on the use of prescribed VM SKUs.
  It is practical to allow for 1 to 2 hours extra runtime to ensure the tests can complete full test runs.

  * Expected Runtime (2-core/vCPU VM) = 1 - 2 hours

* **Usage Examples**  
  The following section provides a few basic examples of how to use the workload profile. Additional usage examples can be found in the
  'Usage Scenarios/Examples' link at the top.


  ``` csharp
  ./VirtualClient --profile=PERF-OPENFOAM.json --system=Azure --timeout=1440 --packageStore="{BlobConnectionString|SAS Uri}"
  ./VirtualClient --profile=PERF-OPENFOAM.json --system=Azure --timeout=1440 --packageStore="{BlobConnectionString|SAS Uri}" --parameters=Iterations=10
  ./VirtualClient --profile=PERF-OPENFOAM.json --system=Azure --timeout=1440 --packageStore="{BlobConnectionString|SAS Uri}" --scenarios=airFoil2D,elbow,motorBike

  ```

-----------------------------------------------------------------------

### Resources
* [Azure VM Sizes](https://docs.microsoft.com/en-us/azure/virtual-machines/sizes)
* [Azure Managed Disks](https://azure.microsoft.com/en-us/pricing/details/managed-disks/)