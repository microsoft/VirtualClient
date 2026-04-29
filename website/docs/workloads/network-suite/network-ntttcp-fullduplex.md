# NTTTCP Full-Duplex Mode
The existing `PERF-NETWORK-NTTTCP.json` profile supports an optional `DuplexMode` parameter that enables bidirectional network throughput testing.
In full-duplex mode, both nodes simultaneously send and receive traffic, producing separate TX and RX throughput metrics per direction.

* [Network Suite Workload Details](./network-suite.md)
* [Client/Server Workloads](../../guides/0020-client-server.md)

## How Full-Duplex Differs from Half-Duplex
In the standard (half-duplex) mode, one node sends while the other receives. In full-duplex mode, each node runs **two NTttcp processes 
simultaneously**: one sender and one receiver. This results in 4 NTttcp processes total across the two systems.

| Direction | Port | Client | Server |
|-----------|------|--------|--------|
| Forward | base port (default 5500) | Sender (`-s`) | Receiver (`-r`) |
| Reverse | base port + 100 (default 5600) | Receiver (`-r`) | Sender (`-s`) |

## Usage

``` bash
# Half-duplex (default, unchanged behavior)
VirtualClient.exe --profile=PERF-NETWORK-NTTTCP.json --clientId=Client01 --layout-path=/path/to/layout.json

# Full-duplex (override DuplexMode parameter)
VirtualClient.exe --profile=PERF-NETWORK-NTTTCP.json --clientId=Client01 --layout-path=/path/to/layout.json --parameters=DuplexMode=Full
```

## PERF-NETWORK-NTTTCP.json
The standard NTttcp profile now includes the `DuplexMode` parameter (default: `Half`). All existing scenarios work unchanged. 
Override with `--parameters=DuplexMode=Full` to enable full-duplex.

* [Workload Profile](https://github.com/microsoft/VirtualClient/blob/main/src/VirtualClient/VirtualClient.Main/profiles/PERF-NETWORK-NTTTCP.json)

* **Supported Platform/Architectures**
  * linux-x64
  * linux-arm64
  * win-x64
  * win-arm64

* **Supports Disconnected Scenarios**  
  * No. Internet connection required.

* **Dependencies**  
  Same as the standard PERF-NETWORK-NTTTCP.json profile:
  * Internet connection.
  * The IP addresses defined in the environment layout for the Client and Server systems must be correct.
  * The name of the Client and Server instances defined in the environment layout must match the agent/client IDs supplied on the command line.
  * Ports 5500 (forward direction) and 5600 (reverse direction) must be available on both systems when using full-duplex mode.

* **Profile Parameters**  
  The following parameter is added for full-duplex support. All other parameters remain unchanged.

  | Parameter | Purpose | Default |
  |-----------|---------|---------|
  | DuplexMode | Set to "Full" for bidirectional testing. Any other value (or unset) uses standard unidirectional testing. | Half |

* **Scenarios**  
  All existing NTTTCP scenarios support full-duplex mode. When `DuplexMode=Full`, each scenario runs both send and receive
  processes on each node. The existing scenario names are unchanged — the `direction` and `duplexMode` metadata in telemetry
  distinguish full-duplex results from half-duplex.

## Workload Metrics
Each scenario produces **four sets of metrics** — sender and receiver metrics on both client and server. The scenario name includes the direction:

| Metric Name | Unit | Relativity |
|-------------|------|------------|
| ThroughputMbps | mbps | Higher is better |
| TotalBytesMB | MB | Higher is better |
| AvgBytesPerCompl | B | - |
| AvgFrameSize | B | - |
| AvgPacketsPerInterrupt | packets/interrupt | - |
| InterruptsPerSec | count/sec | - |
| PacketsRetransmitted | count | Lower is better |
| Errors | count | Lower is better |
| CyclesPerByte | cycles/byte | Lower is better |
| AvgCpuPercentage | % | - |
| TcpAverageRtt | ms | Lower is better |

**Telemetry scenario names** follow the pattern:
* `{Scenario} Client Send` — Client's sender metrics (forward direction)
* `{Scenario} Client Receive` — Client's receiver metrics (reverse direction)
* `{Scenario} Server Send` — Server's sender metrics (reverse direction)  
* `{Scenario} Server Receive` — Server's receiver metrics (forward direction)
