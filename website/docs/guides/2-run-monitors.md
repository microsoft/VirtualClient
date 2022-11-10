---
id: run-monitors
sidebar_position: 2
---

# Run monitors while benchmarking


- In the previous tutorial, we ran this command.
```bash
VirtualClient --profile=PERF-CPU-COREMARK.json --profile=MONITORS-NONE.json --iterations=1
```

- `--profile=MONITORS-NONE.json` removes the default monitor. 
- Simply remove it to let VC run a default performance counter monitor
```bash
VirtualClient --profile=PERF-CPU-COREMARK.json --iterations=1
VirtualClient --profile=PERF-CPU-COREMARK.json --profile=MONITORS-DEFAULT.json --iterations=1
```
- VC will start collecting performance counters at an interval, and log as metrics.
- `MONITORS-DEFAULT.json` will install atop on Linux system, and use .NET SDK to read perf counters on Windows

## More monitors profiles are coming


