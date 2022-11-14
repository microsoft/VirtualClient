# HPCG Workload Metrics
The following document illustrates the type of results that are emitted by the HPCG workload and captured by the
Virtual Client for net impact analysis.


### Workload-Specific Metrics
The following metrics are captured during the operations of the HPCG workload.

#### Metrics
The HPCG rating is is a weighted GFLOP/s (billion floating operations per second) value that is composed of the operations performed in the PCG iteration
phase over the time taken. The overhead time of problem construction and any modifications to improve performance are divided by 500 iterations 
(the amortization weight) and added to the runtime.


| Name                   | Unit           | Description                                             |
|------------------------|----------------|---------------------------------------------------------|
| Total Gflop/s          | GFLOP/s        | Weighted GFLOP/s value that is composed of the operations performed in the PCG iteration phase.    |