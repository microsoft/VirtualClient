# Network Ping/ICMP
This workload uses the out-of-box Windows or Linux network ping commands.

-----------------------------------------------------------------------

### What is Being Tested?

| Name                               | Description |
|------------------------------------|-------------|
| Average Network Round-trip Time    | The average time (in milliseconds) for the ping requests to complete.    |
| Ping Timeouts                      | The number of times the ping requests timeout. |
| Network Blips                      | The number of network blips that occur during testing. A blip is a loss of network connectivity (i.e. a blackout) for a period of 1 second or more. |
| Network Blip Length of Time        | The total time at which the network blip/loss of connectivity persisted. |
| Number of Concurrent Connections   | The number of concurrent connections established at a moment in time.    |

-----------------------------------------------------------------------

### Supported Platforms

* Linux x64
* Linux arm64
* Windows x64
* Windows arm64