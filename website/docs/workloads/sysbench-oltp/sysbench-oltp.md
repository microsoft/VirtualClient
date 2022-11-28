# Sysbench Workload
Sysbench is an open-source multi-threaded database benchmark tool. As a suite, it offers benchmarks that test many different aspects of the system, but this workload supports only the OLTP .lua script tests on MySQL, PostgreSQL, and MariaDB systems -- VC implements support for testing on a MySQL database.

This suite was pulled from the original GitHub repository.

* [Sysbench GitHub](https://github.com/akopytov/sysbench)  
* [Example Sysbench Uses](https://www.flamingbytes.com/posts/sysbench/)

### What is Being Tested?
The Sysbench test suite executes varied transactions on the database system including reads, writes, and other queries. The list of OLTP benchmarks supported by Sysbench are as follows:

* oltp_read_write
* oltp_read_only
* oltp_write_only
* oltp_delete
* oltp_insert
* oltp_update_index
* oltp_update_non_index
* select_random_points
* select_random_ranges

Sysbench also provides a .lua script named oltp_common that is used to set up the database with tables and records to prepare it for testing.

---

### Client-Server Workflow
In this workload, the client(s) run(s) sysbench; sysbench helps prepare various queries to perform on the server based on the number of tables, records per table, and threads. The server, which hosts the MySQL database, remains idle as long as mysql processes are running. The client only is in charge of downloading the sysbench package, running the appropriate sysbench commands, and logging the output. 

### Workload Configuration
The workload provides a host of scenarios to choose between for coverage of a 1- to 16-core system under test, varying the thread count, record count per table, and benchmark used (as listed above).
The desired scenario can be configured based on the number of logical cores of the VM and desired stress on the system. These parameters were collected as a part of testing and observation on VMs of different sizes. Each runs for 30 minutes.

* 2-core VM: {benchmarkName}_T8_TB16_REC500, {benchmarkName}_T16_TB16_REC1000
* 4-core VM: {benchmarkName}_T16_TB16_REC5000, {benchmarkName}_T32_TB16_REC10000
* 8-core VM: {benchmarkName}_T8_TB32_REC50000, {benchmarkName}_T16_TB32_REC500000
* 16-core VM: {benchmarkName}_T92_TB4_REC50000, {benchmarkName}_T152_TB4_REC100000

The supported benchmark names are as follows: oltp_read_write, oltp_write_only, and oltp_read_only.

* **Supported Platform/Architectures**
  * linux-x64
  * linux-arm64

### Package Dependencies
The following package dependencies are required to be installed on the Unix/Linux system in order to support the requirements of the NAS Parallel workload. Note that the Virtual Client will handle the installation of any required dependencies.
