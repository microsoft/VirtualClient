# CoreMark Workload Metrics
The following document illustrates the type of results that are emitted by the CoreMark workload and captured by the
Virtual Client for net impact analysis.

### System Metrics
* [Performance Counters](./PerformanceCounterMetrics.md)
* [Power/Temperature Measurements](./PowerMetrics.md)

### Workload-Specific Metrics
The following metrics are emitted by the CoreMark workload itself.

| Execution Profile   | Test Name | Metric Name | Example Value (min) | Example Value (max) | Example Value (avg) | Unit |
|---------------------|-----------|-------------|---------------------|---------------------|---------------------|------|
| PERF-CPU-COREMARK.json | CoreMark | CoreMark Size | 666.0 | 666.0 | 666.0 | bytes |
| PERF-CPU-COREMARK.json | CoreMark | Iterations | 400000.0 | 800000.0 | 773160.1731601731 | iterations |
| PERF-CPU-COREMARK.json | CoreMark | Iterations/Sec | 19968.051118 | 33889.689062 | 33081.75554433839 | iterations/sec |
| PERF-CPU-COREMARK.json | CoreMark | Parallel PThreads | 2.0 | 2.0 | 2.0 | threads |
| PERF-CPU-COREMARK.json | CoreMark | Total ticks | 12022.0 | 36126.0 | 23365.67617325762 | ticks |
| PERF-CPU-COREMARK.json | CoreMark | Total time (secs) | 12.022 | 36.126 | 23.365676173257606 | secs |
