# Prime95 Workload Metrics
The following document illustrates the type of results that are emitted by the
Prime95 workload and captured by the Virtual Client for net impact analysis.



### Workload-Specific Metrics
The following metrics are captured during the operations of the Prime95 workload.

|   Execution Profile   |    Metric Name       | Example Value |   Unit   |   Relativity   |
|-----------------------|----------------------|---------------|----------|----------------|
| PERF-CPU-PRIME95.json |  passTestCount       | 12            |          | HigherIsBetter |
| PERF-CPU-PRIME95.json |  failTestCount       | 0             |          | LowerIsBetter  |
| PERF-CPU-PRIME95.json |    testTime          | 600.06        | seconds  | HigherIsBetter |

Note that if the failTestCount is greater than 0, it denotes an overall Prime95 test failure and some harware error.

The testTime is the time for which the system was stressed with torture test. Higher the testTime without
any error, more is the confidence in Prime95 results.
