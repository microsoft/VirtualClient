# Graph500 Workload Profiles
The following profiles run customer-representative or benchmarking scenarios using the Graph500 workload.  

* [Workload Details](./graph500.md)  

## PERF-GRAPH500.json
Runs a data-intensive workload using the Graph500 toolset to test the performance of underlying hardware.
This profile is designed to identify general/broad regressions when compared against a baseline by validating the time taken to create a graph, perform 
BFS(Breadth First Search) and SSSP(Single Source Shortest Path).

* **Supported Platform/Architectures**
  * linux-x64
  * linux-arm64

* **Supports Disconnected Scenarios**  
  * No. Internet connection required.

* **Dependencies**  
  The dependencies defined in the 'Dependencies' section of the profile itself are required in order to run the workload operations effectively.
  * Internet connection.

  Additional information on components that exist within the 'Dependencies' section of the profile can be found in the following locations:
  * [Installing Dependencies](https://microsoft.github.io/VirtualClient/docs/category/dependencies/)

* **Profile Parameters**  
  The following parameters can be optionally supplied on the command line to modify the behaviors of the workload.

  | Parameter                 | Purpose                                                                         | Default Value |
  |---------------------------|---------------------------------------------------------------------------------|---------------|
  | EdgeFactor                | Optional.EdgeFactor is ratio of the graph’s edge count to its vertex count (i.e., half the average degree of a vertex in the graph). EdgeFactor helps in determining the number of edges for the graph that this workload creates.  Number of edges = EdgeFactor * Number of vertices.  **Note:** Number of vertices is calculated from the following <b>Scale</b> parameter.| 16 |
  | Scale                     | Optional.Scale is logarithm base two of the number of vertices. Scale is used in determining Number of Vertices for the graph that this workload creates.  Scale = log<sub>2</sub> (Number of vertices). | 10 |

* **Profile Runtimes**  
  See the 'Metadata' section of the profile for estimated runtimes. These timings represent the length of time required to run a single round of profile 
  actions. These timings can be used to determine minimum required runtimes for the Virtual Client in order to get results. These are often estimates based on the
  number of system cores. 

* **Usage Examples**  
  The following section provides a few basic examples of how to use the workload profile. Additional usage examples can be found in the
  'Usage Scenarios/Examples' link at the top.

  ``` bash
  # Execute the workload profile
  VirtualClient.exe --profile=PERF-GRAPH500.json --system=Demo --timeout=1440 --packageStore="{BlobConnectionString|SAS Uri}"

   # Override the profile default parameters to use a different scale factor
  VirtualClient.exe --profile=PERF-GRAPH500.json --system=Demo --timeout=1440 --packageStore="{BlobConnectionString|SAS Uri}" --parameters="Scale=20"
  ```