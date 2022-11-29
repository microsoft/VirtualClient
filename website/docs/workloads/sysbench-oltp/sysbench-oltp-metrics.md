# Sysbench Workload Metrics

The following document illustrates the type of results that are emitted by the Sysbench workload and captured by the
Virtual Client for net impact analysis.

### Workload-Specific Metrics

The following metrics are captured during the operations of the Sysbench OLTP workload.

#### Metrics

| Execution Profile     | Test Name | Metric Name | Example Value |
|-----------------------|-----------|-------------|---------------|
| PERF-MYSQL-SYSBENCH-OLTP.json | Sysbench OLTP | # read queries | 5503894 |
| PERF-MYSQL-SYSBENCH-OLTP.json | Sysbench OLTP | # write queries | 259534 |
| PERF-MYSQL-SYSBENCH-OLTP.json | Sysbench OLTP | # other queries | 1284332 |
| PERF-MYSQL-SYSBENCH-OLTP.json | Sysbench OLTP | # transactions | 257421 |
| PERF-MYSQL-SYSBENCH-OLTP.json | Sysbench OLTP | transactions/sec | 153.01 |
| PERF-MYSQL-SYSBENCH-OLTP.json | Sysbench OLTP | # queries | 5948220 |
| PERF-MYSQL-SYSBENCH-OLTP.json | Sysbench OLTP | queries/sec | 2850.17 |
| PERF-MYSQL-SYSBENCH-OLTP.json | Sysbench OLTP | # ignored errors | 0 |
| PERF-MYSQL-SYSBENCH-OLTP.json | Sysbench OLTP | ignored errors/sec | 0.00 |
| PERF-MYSQL-SYSBENCH-OLTP.json | Sysbench OLTP | # reconnects | 0 |
| PERF-MYSQL-SYSBENCH-OLTP.json | Sysbench OLTP | reconnects/sec | 0.00 |
| PERF-MYSQL-SYSBENCH-OLTP.json | Sysbench OLTP | elapsed time | 1800.0012 |
| PERF-MYSQL-SYSBENCH-OLTP.json | Sysbench OLTP | latency min | 7.19 |
| PERF-MYSQL-SYSBENCH-OLTP.json | Sysbench OLTP | latency avg | 26.97 |
| PERF-MYSQL-SYSBENCH-OLTP.json | Sysbench OLTP | latency max | 682.33 |
| PERF-MYSQL-SYSBENCH-OLTP.json | Sysbench OLTP | latency 95p | 67.58 |
| PERF-MYSQL-SYSBENCH-OLTP.json | Sysbench OLTP | latency sum | 7458935.25 |
