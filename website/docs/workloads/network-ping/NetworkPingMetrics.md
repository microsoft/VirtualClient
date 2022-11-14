# Network Ping/ICMP Workload Metrics
The following document illustrates the type of results that are emitted by the Network Ping workload and captured by the
Virtual Client for net impact analysis.



### Workload-Specific Metrics
The following metrics are emitted by the Network Ping workload itself.

| Execution Profile   | Test Name | Metric Name | Example Value (min) | Example Value (max) | Example Value (avg) | Unit |
|---------------------|-----------|-------------|---------------------|---------------------|---------------------|------|
| PERF-NETWORK-PING.json | Network Ping | # of blips | 1.0 | 1.0 | 1.0 | count |
| PERF-NETWORK-PING.json | Network Ping | avg. number of connections | 7.666666666666667 | 117.06333333333333 | 11.180472020509118 | count |
| PERF-NETWORK-PING.json | Network Ping | avg. round trip time | 0.5066666666666667 | 190.85333333333333 | 87.0910239051444 | milliseconds |
| PERF-NETWORK-PING.json | Network Ping | blip duration | 1019.0 | 8016.0 | 2017.125 | milliseconds |
| PERF-NETWORK-PING.json | Network Ping | dropped pings | 1.0 | 8.0 | 2.0 | count |
