# NAS Parallel Workload Metrics
The following document illustrates the type of results that are emitted by the NAS Parallel workload and captured by the
Virtual Client for net impact analysis.

### Workload-Specific Metrics

| Name                   | Unit           |Supported Scenarios| Description                                                                             |
|------------------------|----------------|-------------------|-----------------------------------------------------------------------------------------|
| ExecutionTime          |Seconds         | MPI,OMP           |Total execution time of the benchmark/scenario. (Lower the value better the performance) |
| Mop/s total            |Mop/s           | MPI,OMP           |Total Millions of operations per second. (Higher the value better the performance)       |
| Mop/s/thread           |Mop/s           | OMP               |Millions of operations per second per thread. (Higher the value better the performance)  |
| Mop/s/process          |Mop/s           | MPI               |Millions of operations per second per process. (Higher the value better the performance) |

### Telemetry Metrics example

The following metrics are emitted by the NAS Parallel workload itself.(Note: The number of metrics/results depend on the workload profile)

|MetricName|ScenarioName|Avg_MetricValue|
|:----|:----|:----|
|ExecutionTime|OMP ep.D.x|415.44|
|Mop/s total|OMP ep.D.x|330.82|
|Mop/s/thread|OMP ep.D.x|41.35|
|ExecutionTime|OMP is.C.x|2|
|Mop/s/thread|OMP is.C.x|83.86|
|Mop/s total|OMP is.C.x|670.88|
|ExecutionTime|OMP cg.D.x|2,114.09|
|Mop/s/thread|OMP cg.D.x|215.39|
|Mop/s total|OMP cg.D.x|1,723.15|
|ExecutionTime|OMP bt.D.x|4,017.33|
|Mop/s/thread|OMP bt.D.x|1,815.13|
|Mop/s total|OMP bt.D.x|14,521.04|
