# Virtual Client Monitor Profiles
The following sections describe the various monitor profiles that are available with the Virtual Client application. Monitor profiles are used to 
define the background monitors that will run on the system. Monitors are often ran in conjunction with workloads (defined in workload profiles) in
order to capture performance and reliability information from the system while workloads are running.

### MONITORS-NONE.json
Instructs the Virtual Client to not run any monitors at all.


```bash
// Do not run any background monitors.
VirtualClient.exe --profile=PERF-CPU-OPENSSL.json --profile=MONITORS-NONE.json --system=Azure --timeout=1440 --packageStore="{BlobConnectionString|SAS Uri}"
```

### MONITORS-DEFAULT.json
The default monitor profile for the Virtual Client. This profile captures performance counters on the system using one or more different specialized
toolsets. This monitor profile will be used when no other monitor profiles are specified on the command line.

* [Performance Counters](./perf-counter-metrics.md)
* [Atop](./atop.md)

* **OS/Architecture Platform Support**
  * Linux
    * Capture performance counters using the Atop application.
  * Windows
    * Capture performance counters using the .NET SDK.



```
# Run the monitoring facilities only.
VirtualClient.exe --profile=MONITORS-DEFAULT.json

# Runs the default monitor profile.
VirtualClient.exe --profile=PERF-CPU-OPENSSL.json --system=Azure --timeout=1440 --packageStore="{BlobConnectionString|SAS Uri}"

# Monitor profile explicitly defined.
VirtualClient.exe --profile=PERF-CPU-OPENSSL.json --profile=MONITORS-DEFAULT.json --system=Azure --timeout=1440 --packageStore="{BlobConnectionString|SAS Uri}"

```
