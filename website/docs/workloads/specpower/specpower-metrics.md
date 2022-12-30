# SPECpower Workload Metrics
The following document illustrates the type of results that are emitted by the SPECpower workload and captured by the
Virtual Client for net impact analysis.

### Workload-Specific Metrics
Note that the SPECpower workload itself does not measure power consumption itself. Please refer to the SPECpower official document on how to setup the power meters, or measure power consumption through other mechanism. The SPECpower workload makes this process more reliable because it is designed
to use resources on the system in a smooth, constant steady state. This typically causes the power usage to remain consistent as well.

The Virtual Client itself emits heartbeats as metrics and these are used only for validation that the SPECpower workload is running.

| Execution Profile    | Test Name | Metric Name | Example Value (min) | Example Value (max) | Example Value (avg) | Unit |
|----------------------|-----------|-------------|---------------------|---------------------|---------------------|------|
| POWER-SPEC30.json | MonitorProcess | Heartbeat | 1 | 1 | 1 |  |
| POWER-SPEC50.json | MonitorProcess | Heartbeat | 1 | 1 | 1 |  |
| POWER-SPEC70.json | MonitorProcess | Heartbeat | 1 | 1 | 1 |  |
| POWER-SPEC100.json | MonitorProcess | Heartbeat | 1 | 1 | 1 |  |
| POWER-SPEC30-Linux.json | MonitorProcess | Heartbeat | 1 | 1 | 1 |  |
| POWER-SPEC50-Linux.json | MonitorProcess | Heartbeat | 1 | 1 | 1 |  |
| POWER-SPEC70-Linux.json | MonitorProcess | Heartbeat | 1 | 1 | 1 |  |
| POWER-SPEC100-Linux.json | MonitorProcess | Heartbeat | 1 | 1 | 1 |  |

