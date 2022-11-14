# Graph500 Workload Profiles
The following profiles run customer-representative or benchmarking scenarios using the Graph500 workload.  

* [Workload Details](./graph500.md)  
* [Workload Profile Metrics](./graph500-metrics.md)  


-----------------------------------------------------------------------

### Preliminaries
The profiles below require the ability to download workload packages and dependencies from a package store. In order to download the workload packages, connection information 
must be supplied on the command line. See the 'Workload Packages' documentation above for details on how that works.

-----------------------------------------------------------------------

### PERF-GRAPH500.json
Runs a data-intensive workload using the Graph500 toolset to test the performance of underlying hardware.
This profile is designed to identify general/broad regressions when compared against a baseline by validating the time taken to create a graph, perform 
BFS(Breadth First Search) and SSSP(Single Source Shortest Path)

* **Supported Platform/Architectures**
  * linux-x64
  * linux-arm64

* **Dependencies**  
  The following dependencies must be met to run this workload profile.

  * Workload package must exist in the 'packages' directory or connection information for the package store supplied on the command line (see 'Workload Packages' link above).

* **Profile Parameters**  
  The following parameters can be optionally supplied on the command line to change this default scale factor of the workload. See the 'Usage Scenarios/Examples' above for 
  examples on how to supply parameters to Virtual Client profiles.

  | Parameter                 | Purpose                                                                         | Default Value |
  |---------------------------|---------------------------------------------------------------------------------|---------------|
  | EdgeFactor                | Optional.EdgeFactor is ratio of the graph’s edge count to its vertex count (i.e., half the average degree of a vertex in the graph). EdgeFactor helps in determining the number of edges for the graph that this workload creates.  Number of edges = EdgeFactor * Number of vertices.  **Note:** Number of vertices is calculated from the following <b>Scale</b> parameter.| 16 |
  | Scale                     | Optional.Scale is logarithm base two of the number of vertices. Scale is used in determining Number of Vertices for the graph that this workload creates.  Scale = log<sub>2</sub> (Number of vertices). | 10 |

* **Workload Runtimes**  
  The following timings represent the length of time required to run a single round of tests ran. These timings can be used to determine
  minimum required runtimes for the Virtual Client in order to get results. These are estimates based on the use of prescribed VM SKUs.

  * Expected Runtime on Linux Systems
    * (16-core/vCPU VM) = 30 minutes (Depends on the value supplied for the profile 'Scale' parameter).

* **Usage Examples**  
  The following section provides a few basic examples of how to use the workload profile. Additional usage examples can be found in the
  'Usage Scenarios/Examples' link at the top.


  ``` bash
  VirtualClient.exe --profile=PERF-GRAPH500.json --system=Azure --timeout=1440 --packageStore="{BlobConnectionString|SAS Uri}"

  // Increase the scale factor.
  VirtualClient.exe --profile=PERF-GRAPH500.json --system=Azure --timeout=1440 --packageStore="{BlobConnectionString|SAS Uri}" --parameters=Scale=20
  ```

-----------------------------------------------------------------------

### Resources
* [Azure VM Sizes](https://docs.microsoft.com/en-us/azure/virtual-machines/sizes)
* [Azure Managed Disks](https://azure.microsoft.com/en-us/pricing/details/managed-disks/)