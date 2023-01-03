# Sysbench OLTP
Sysbench is an open-source multi-threaded database benchmark tool for database online transacation processing (OLTP) operations against a
MySQL database.

* [Sysbench GitHub](https://github.com/akopytov/sysbench)  
* [Example Sysbench Uses](https://www.flamingbytes.com/posts/sysbench/)

## What is Being Measured?
The Sysbench test suite executes varied transactions on the database system including reads, writes, and other queries. The list of OLTP benchmarks 
supported by Sysbench are as follows:

| Benchmark Name        | Description                                                           |
|-----------------------|-----------------------------------------------------------------------|
| oltp_read_write       | Measures performance of read and write queries on MySQL database      |
| oltp_read_only        | Measures performance of only read queries on MySQL database           |
| oltp_write_only       | Measures performance of only write queries on MySQL database          |
| oltp_delete           | Measures performance of only delete queries on the MySQL database     |
| oltp_insert           | Measures performance of only insert queries on MySQL database         |
| oltp_update_index     | Measures performance of index updates on the MySQL database           |
| oltp_update_non_index | Measures performance of non-index updates on the MySQL database       |
| select_random_points  | Measures performance of random point select on the MySQL database     |
| select_random_ranges  | Measures performance of random range select on the MySQL database     |


## Workload Metrics
The following metrics are examples of those captured by the Virtual Client when running the Sysbench OLTP workload

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
