# Network Ping/ICMP Workload Profiles
The following profiles run customer-representative or benchmarking scenarios using basic network pings/ICMP protocol.  

* [Workload Details](./network-ping.md)  
* [Workload Profile Metrics](./NetworkPingMetrics.md)
* [Usage Scenarios/Examples](./UsageScenarios.md)

-----------------------------------------------------------------------

### PERF-NETWORK-PING.json
Runs a basis network ping test given an IP address target. Note that the target must have the ICMP protocol enabled in order to
respond to network ping requests.

* **Supported Platform/Architectures**
  * linux-x64
  * linux-arm64
  * win-x64
  * win-arm64

* **Supported Operating Systems**
  * Ubuntu 18
  * Ubuntu 20
  * Ubuntu 22
  * Windows 10
  * Windows 11
  * Windows Server 2016
  * Windows Server 2019

* **Dependencies**  
  The following dependencies must be met to run this workload profile.

  * The IP address specified on the command line (see 'Parameters' below) must be related to a valid system/node/VM that is online with firewall open
    to receive ICMP ping requests.

* **Profile Parameters**  
  The following parameters can be supplied on the command line. See the 'Usage Scenarios/Examples' above for examples on how to supply parameters to 
  Virtual Client profiles.

  | Parameter   | Purpose |
  |-------------|---------|
  | IPAddress   | Required. The IP address of the target endpoint to which to send network pings and measure round trip response times. This is a required parameter for this profile execution.  |

* **Workload Runtimes**  
  The following timings represent the length of time required to run a single round of tests ran. These timings can be used to determine
  minimum required runtimes for the Virtual Client in order to get results. These are estimates based on the use of prescribed VM SKUs.

  * Expected Runtime (2-core/vCPU VM) = 5 - 10 minutes
  * Expected Runtime (4-core/vCPU VM) = 5 - 10 minutes

* **Usage Examples**  
  The following section provides a few basic examples of how to use the workload profile. Additional usage examples can be found in the
  'Usage Scenarios/Examples' link at the top.


  ``` bash
  VirtualClient.exe --profile=PERF-NETWORK-PING.json --system=Azure --timeout=1440 --parameters=IPAddress=1.2.3.4
  ```

-----------------------------------------------------------------------

### Resources
* [Azure VM Sizes](https://docs.microsoft.com/en-us/azure/virtual-machines/sizes)
* [Azure Managed Disks](https://azure.microsoft.com/en-us/pricing/details/managed-disks/)