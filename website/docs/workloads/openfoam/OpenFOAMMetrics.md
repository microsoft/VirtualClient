# OpenFOAM Workload Metrics
The following document illustrates the type of results that are emitted by the OpenFOAM workload and captured by the
Virtual Client for net impact analysis.



### Workload-Specific Metrics
The following metrics are emitted by the OpenFOAM workload itself.

| Execution Profile   | Test Name | Metric Name | Example Value (min) | Example Value (max) | Example Value (avg) | Unit |
|---------------------|-----------|-------------|---------------------|---------------------|---------------------|------|
| PERF-OPENFOAM.json (linux-x64) | pitzDaily | Iterations/min | 1575.48 | 1600.37 | 1690.7 | itrs/min |
| PERF-OPENFOAM.json (linux-x64) | airFoil2D | Iterations/min | 2413.6 | 2435.79 | 2420.9 | itrs/min |
| PERF-OPENFOAM.json (linux-x64) | elbow | Iterations/min | 17518.9 | 17605.5 | 16556.7 | itrs/min |
| PERF-OPENFOAM.json (linux-x64) | motorbike | Iterations/min | 17.70 | 17.71 | 17.72 | itrs/min |
| PERF-OPENFOAM.json (linux-x64) | lockExchange | Iterations/min | 32.25 | 32.27 | 32.30 | itrs/min |
| PERF-OPENFOAM.json (linux-arm64) | pitzDaily | Iterations/min | 1111.28 | 1132.17 | 1120.7 | itrs/min |
| PERF-OPENFOAM.json (linux-arm64) | airFoil2D | Iterations/min | 2000.6 | 1936.79 | 1972.9 | itrs/min |
| PERF-OPENFOAM.json (linux-arm64) | elbow | Iterations/min | 16216.9 | 16238.5 | 16280.7 | itrs/min |
| PERF-OPENFOAM.json (linux-arm64) | lockExchange | Iterations/min | 38.6 | 38.8 | 38.7 | itrs/min |


