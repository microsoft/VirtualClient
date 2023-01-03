# Network Ping/ICMP
This workload uses the out-of-box Windows or Linux network ping commands.

## What is Being Tested?
The Network Ping workload measures network round-trip times for ping/ICMP communications as well as network downtime/blips.

| Name                               | Description |
|------------------------------------|-------------|
| Average Network Round-trip Time    | The average time (in milliseconds) for the ping requests to complete.    |
| Ping Timeouts                      | The number of times the ping requests timeout. |
| Network Blips                      | The number of network blips that occur during testing. A blip is a loss of network connectivity (i.e. a blackout) for a period of 1 second or more. |
| Network Blip Length of Time        | The total time at which the network blip/loss of connectivity persisted. |
| Number of Concurrent Connections   | The number of concurrent connections established at a moment in time.    |

## Workload Metrics
The following metrics are emitted by the Network Ping workload itself.

| Metric Name | Example Value (min) | Example Value (max) | Example Value (avg) | Unit |
|-------------|---------------------|---------------------|---------------------|------|
| # of blips | 1.0 | 1.0 | 1.0 | count |
| avg. number of connections | 7.666666666666667 | 117.06333333333333 | 11.180472020509118 | count |
| avg. round trip time | 0.5066666666666667 | 190.85333333333333 | 87.0910239051444 | milliseconds |
| blip duration | 1019.0 | 8016.0 | 2017.125 | milliseconds |
| dropped pings | 1.0 | 8.0 | 2.0 | count |