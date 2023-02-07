# Network Ping/ICMP Workload Profiles
The following profiles run customer-representative or benchmarking scenarios using basic network pings/ICMP protocol.  

* [Workload Details](./network-ping.md)  

## PERF-NETWORK-PING.json
Runs a basis network ping test given an IP address target. Note that the target must have the ICMP protocol enabled in order to
respond to network ping requests.

* [Workload Profile](https://github.com/microsoft/VirtualClient/blob/main/src/VirtualClient/VirtualClient.Main/profiles/PERF-NETWORK-PING.json) 

* **Supported Platform/Architectures**
  * linux-x64
  * linux-arm64
  * win-x64
  * win-arm64

* **Supports Disconnected Scenarios**  
  * Yes. If the system local/loopback address (127.0.0.1) is used as the target IP address.

* **Dependencies**  
  The dependencies defined in the 'Dependencies' section of the profile itself are required in order to run the workload operations effectively.
  * Internet connection.
  * The IP address specified on the command line (see 'Parameters' below) must be related to a valid system/node/VM that is online with firewall open
    to receive ICMP ping requests.

  Additional information on components that exist within the 'Dependencies' section of the profile can be found in the following locations:
  * [Installing Dependencies](https://microsoft.github.io/VirtualClient/docs/category/dependencies/)

* **Profile Parameters**  
  The following parameters can be optionally supplied on the command line to modify the behaviors of the workload.

  | Parameter   | Purpose |
  |-------------|---------|
  | IPAddress   | Required. The IP address of the target endpoint to which to send network pings and measure round trip response times. Loopback address can be used: 127.0.0.1.  |

* **Profile Runtimes**  
  See the 'Metadata' section of the profile for estimated runtimes. These timings represent the length of time required to run a single round of profile 
  actions. These timings can be used to determine minimum required runtimes for the Virtual Client in order to get results. These are often estimates based on the
  number of system cores. 

* **Usage Examples**  
  The following section provides a few basic examples of how to use the workload profile.

  ``` bash
  # Execute the workload profile
  VirtualClient.exe --profile=PERF-NETWORK-PING.json --system=Demo --timeout=1440 --parameters=IPAddress=1.2.3.4
  ```