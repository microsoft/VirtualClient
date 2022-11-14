# DeathStarBench Workload Profiles
The following profiles run customer-representative or benchmarking scenarios using the DeathStarBench workload.  

* [Workload Details](./DeathStarBench.md)  
* [Workload Profile Metrics](./DeathStarBenchMetrics.md)  


-----------------------------------------------------------------------

### Preliminaries
The profiles below require the ability to download workload packages and dependencies from a package store. In order to download the workload packages, connection information 
must be supplied on the command line. See the 'Workload Packages' documentation above for details on how that works.

-----------------------------------------------------------------------

### PERF-NETWORK-DEATHSTARBENCH.json
Runs a Network intensive workload using the DeathStarBench toolset to test the Network performance in processing http load to the server.

* **Supported Platform/Architectures**
  * linux-x64

* **Supported Operating Systems**
   * Ubuntu

* **Dependencies**  
  The following dependencies must be met to run this workload profile.

  * Workload package must exist in the 'packages' directory or connection information for the package store supplied on the command line (see 'Workload Packages' link above).

* **Profile Parameters**  
  The following parameters can be optionally supplied on the command line to modify the behaviors of the workload. See the 'Usage Scenarios/Examples' above for examples on how to supply parameters to 
  Virtual Client profiles.

  | Parameter                 | Purpose                                                                         | Default value |
  |---------------------------|---------------------------------------------------------------------------------|---------------|
  | NumberOfThreads           | Optional. This specifies the number of threads to be created to send http load for each workload scenario. | 20
  | NumberOfConnections        | Optional. This specifies the number of connections to be created to send http load for each workload scenario that we can have. | 1000
  | Duration                   | Optional. This specifies the time for which the http load will be sent. | 300s
  | RequestPerSec          | Optional. This specifies the constant throughput load. | 1000 
  | GraphType           | Optional. This specifies the type of graph to be used in **socialNetwork** scenario. | socfb-Reed98 [Allowed values : socfb-Reed98, ego-twitter] |

* **Note**: NumberOfConnections should be greater than or equals to NumberOfThreads. (NumberOfConnections >= NumberOfThreads) 


* **Workload Runtimes**  
  The following timings represent the length of time required to run a single round of tests ran. These timings can be used to determine
  minimum required runtimes for the Virtual Client in order to get results. These are estimates based on the use of prescribed VM SKUs.
  This particular workload runtime is affected by the 'Duration' parameter of the  profile. The default value for this
  is 300 seconds (5 mins). This value can be overridden on the command line as noted in the 'Parameters' section above.

  * Expected Runtimes on Linux systems ~ 45 min

* **Usage Examples**  
  The following section provides a few basic examples of how to use the workload profile. Additional usage examples can be found in the
  'Usage Scenarios/Examples' link at the top.


  ```csharp
  ./VirtualClient --profile=PERF-NETWORK-DEATHSTARBENCH.json --system=Azure --timeout=180 --layout="{Path to layout file}" --packageStore="{BlobConnectionString|SAS Uri}" 


  // Change the default parameters.
  ./VirtualClient --profile=PERF-NETWORK-DEATHSTARBENCH.json --system=Azure --timeout=1440 --layout="{Path to layout file}" --packageStore="{BlobConnectionString|SAS Uri}" --parameters=Duration=60s,,,NumberOfThreads=2,,,NumberOfConnections=100
  ```
