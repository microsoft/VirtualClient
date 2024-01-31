# ApacheBench Workload Profiles
The following profiles run customer-representative or benchmarking scenarios using the ApacheBench workload.  

* [Workload Details](./apachebench.md)  

## PERF-APACHEBENCH.json
ApacheBench is a benchmarking tool for apache http server. It produces the metrics for request latencies. It performs N number of requests to server and C number of requests at a time. Virtual client set total of 50000 requests in a batch of 50 concurrent requests at a time, per iteration. Apache bench is an industry standard benchmarking toolset.

* [Workload Profile](https://github.com/microsoft/VirtualClient/blob/main/src/VirtualClient/VirtualClient.Main/profiles/PERF-APACHEBENCH.json) 

* **Supported Platform/Architectures**
  * linux-x64
  * linux-arm64
  * win-x64
  * win-arm64

* **Supports Disconnected Scenarios**  
  * No. Internet connection required.

* **Dependencies**  
  The dependencies defined in the 'Dependencies' section of the profile itself are required in order to run the workload operations effectively.
  * Internet connection.

  Additional information on components that exist within the 'Dependencies' section of the profile can be found in the following locations:
  * [Installing Dependencies](https://microsoft.github.io/VirtualClient/docs/category/dependencies/)

* **Profile Parameters**  
  There are 2 parameters required for the workload. The command for the apache bench(ab) is "ab -k -n 50000 -c 50 http://localhost:80".
  By default 50000 request are made to apache http server in a batch 50 in the mentioned scenario at a time.

  | Parameter                 | Purpose                                                                         | Default value |
  |---------------------------|---------------------------------------------------------------------------------|---------------|
  | NoOfRequests              | Total number of requests to be made to server.                                  | 50000         |
  | NoOfConcurrentRequests    | Concurrency level, total number of parallel requests.                           | 50         |

  The parameters are mapped as following with the profile parameters of workload :
  NoOfRequests           - n
  NoOfConcurrentRequests - c
  For parameter details refer to workload details [document](./apachebench.md).


* **Profile Runtimes**  
  See the 'Metadata' section of the profile for estimated runtimes. These timings represent the length of time required to run a single round of profile 
  actions. These timings can be used to determine minimum required runtimes for the Virtual Client in order to get results. These are often estimates based on the
  number of system cores. 

* **Usage Examples**  
  The following section provides a few basic examples of how to use the workload profile.

  ``` bash
  # Execute the workload profile
  ./VirtualClient --profile=PERF-APACHEBENCH.json --system=Demo --timeout=1440
  ```
