# SPECjbb Workload Metrics
The following document illustrates the type of results that are emitted by the SPECjbb workload and captured by the
Virtual Client for net impact analysis.

[SPECjbb2015 Benchmark Result Fields Manual](https://www.spec.org/jbb2015/docs/SPECjbb2015-Result_File_Fields.html)


### Workload-Specific Metrics
The following metrics are captured during the operations of the SPECjbb workload.

#### Metrics
Description of those metrics can't be easily concentrated in one sentence. Please refer to the official document.

[max-jops](https://www.spec.org/jbb2015/docs/SPECjbb2015-Result_File_Fields.html#max-jops)
[critical-jops](https://www.spec.org/jbb2015/docs/SPECjbb2015-Result_File_Fields.html#critical-jops)

| Name                   | Unit           | Description                                             |
|------------------------|----------------|---------------------------------------------------------|
| hbIR (max attempted)   | jOPS           | High Bound Injection Rate (HBIR) (Approximate High Bound of throughput) maximum                 |
| hbIR (settled)         | jOPS           | CHigh Bound Injection Rate (HBIR) (Approximate High Bound of throughput) settled.               |
| max-jOPS               | jOPS           | RT(Response-Throughput) step levels close to max-jOPS.                |
| critical-jOPS          | jOPS           | Geometric mean of jOPS at these SLAs represent the critical-jOPS metric.                |