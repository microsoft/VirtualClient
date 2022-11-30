# Sysbench OLTP Workload Profiles

The following profiles run customer-representative or benchmarking scenarios using the Sysbench OLTP workload.

* [Workload Details](./sysbench-oltp.md)  
* [Workload Profile Metrics](./sysbench-oltp-metrics.md)  

-----------------------------------------------------------------------

### Preliminaries
The profiles below require the ability to download workload packages and dependencies from a package store. In order to download the workload packages, connection information must be supplied on the command line. See the 'Workload Packages' documentation above for details on how that works.

-----------------------------------------------------------------------

### PERF-MYSQL-SYSBENCH-OLTP.json

Runs an intensive workload using the Sysbench Benchmark to test the bandwidth of CPU, Memory, and Disk I/O.

* **OS/Architecture Platforms**
  * linux-x64
  * linux-arm64

* **Supported Operating Systems**
  * Ubuntu 18
  * Ubuntu 20
  * Ubuntu 22

* **Dependencies**  
  The following dependencies must be met to run this workload profile.

  * Workload package must exist in the 'packages' directory or connection information for the package store supplied on the command line (see 'Workload Packages' link above).

* **Profile Parameters**  
  The following parameters can be optionally supplied on the command line to modify the behaviors of the workload. See the 'Usage Scenarios/Examples' above for examples on how to supply parameters to Virtual Client profiles.

  | Parameter                 | Purpose                                                                                                                 |Default      |
  |---------------------------|-------------------------------------------------------------------------------------------------------------------------|-------------|
  | DatabaseName              | Not Required. Configure the name of database under test.                                                                |sbtest          |

* **Workload Runtimes**
  The following timings represent the length of time required to run a single round of tests ran. These timings can be used to determine
  minimum required runtimes for the Virtual Client in order to get results. These are estimates based on the use of prescribed VM SKUs.
  It is practical to allow for 1 to 2 hours extra runtime to ensure the tests can complete full test runs.

  * Expected Runtime on Linux systems
    * (2-core/vCPU VM) = 3.5 hours

* **Usage Examples**
  The following section provides a few basic examples of how to use the workload profile. Additional usage examples can be found in the
  'Usage Scenarios/Examples' link at the top.

  ``` bash
  ./VirtualClient --profile=PERF-MYSQL-SYSBENCH-OLTP.json --system=Azure --timeout=1440 --scenarios=oltp_read_write_T1_TB4_REC100 --layout="{Path to layout file}"
  ./VirtualClient --profile=PERF-MYSQL-SYSBENCH-OLTP.json --system=Azure --timeout=1440 --scenarios=oltp_read_write_T1_TB4_REC100 --parameters="DatabaseName=mytestDB" --layout="{Path to layout file}"
  ```