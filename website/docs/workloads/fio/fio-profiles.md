# Flexible I/O Tester (FIO) Workload Profiles
The following profiles run customer-representative or benchmarking scenarios using the Flexible I/O Tester (FIO) workload.  

* [Workload Details](./fio.md)  
* [Testing Disks](../../guides/0220-usage-testing-disks.md)

## PERF-IO-FIO.json
Runs an IO-intensive workload using the Flexible IO Tester (FIO) toolset to test performance of disks on the system. Although this profile
can run on Windows, it is predominantly used on Linux systems. The profile runs the workload in-parallel on ALL disks that match the "DiskFilter" 
parameter (see below) by default.

* [Workload Profile](https://github.com/microsoft/VirtualClient/blob/main/src/VirtualClient/VirtualClient.Main/profiles/PERF-IO-FIO.json) 

Additionally this profile is designed to auto-scale to the number of cores on the system on which it runs. It uses a simple algorithm to determine 2 key
aspects of the workload execution.

* Total number of jobs/threads = \<# of logical cores> / 2  

  ``` script
  Examples:
  For a 16-core system:
  16/2 = 16 concurrent jobs/threads per DiskSpd execution (i.e. # threads to run I/O operations against the test file concurrently).

  For a 64-core system:
  64/2 = 32 concurrent jobs/threads per DiskSpd execution (i.e. # threads to run I/O operations against the test file concurrently).
  ```
* Total I/O depth =  512 / < Total number of jobs/threads>  

  ``` script
  Examples:
  For a 16 core system:
  total # jobs/threads = 16/2 (above) -> 512/16 = 32

  For a 64 core system:
  total # jobs/threads = 64/2 (above) -> 512/32 = 16
  ```

* **Supported Platform/Architectures**  
  * linux-x64
  * linux-arm64
  * win-x64

* **Supported Operating Systems**
  * Ubuntu 18
  * Ubuntu 20
  * Ubuntu 22
  * Ubuntu 24
  * Azlinux 3

* **Supports Disconnected Scenarios**  
  * No. Internet connection required.

* **Dependencies**  
  The dependencies defined in the 'Dependencies' section of the profile itself are required in order to run the workload operations effectively.
  * Internet connection.
  * Disk mount points exist for the disks to be targeted. Virtual Client will generally ensure that mount points exist by default. Details for mount point creation procedures can be found in the 'Testing Disks' documentation above.
  * Any 'DiskFilter' parameter value used should match the set of disks desired. See the link for 'Testing Disks' above.

  Additional information on components that exist within the 'Dependencies' section of the profile can be found in the following locations:
  * [Installing Dependencies](https://microsoft.github.io/VirtualClient/docs/category/dependencies/)

* **Scenarios**  
  The following scenarios are covered by this workload profile. 

  * Random Write Operations
    * 4k block size, multiple jobs/threads per disk
    * 8k block size, multiple jobs/threads per disk
    * 12k block size, multiple jobs/threads per disk
    * 16k block size, multiple jobs/threads per disk
    * 1024k block size, multiple jobs/threads per disk
  * Random Read Operations
    * 4k block size, multiple jobs/threads per disk
    * 8k block size, multiple jobs/threads per disk
    * 12k block size, multiple jobs/threads per disk
    * 16k block size, multiple jobs/threads per disk
    * 1024k block size, multiple jobs/threads per disk
  * Sequential Write Operations
    * 4k block size, multiple jobs/threads per disk
    * 8k block size, multiple jobs/threads per disk
    * 12k block size, multiple jobs/threads per disk
    * 16k block size, multiple jobs/threads per disk
    * 1024k block size, multiple jobs/threads per disk
  * Sequential Read Operations
    * 4k block size, multiple jobs/threads per disk
    * 8k block size, multiple jobs/threads per disk
    * 12k block size, multiple jobs/threads per disk
    * 16k block size, multiple jobs/threads per disk
    * 1024k block size, multiple jobs/threads per disk
  * Disk/Data Integrity Verification, Random Write Operations
    * 4k block size, queue depth of 1, single job/thread per disk
    * 16k block size, queue depth of 1, single job/thread per disk
    * 1024k block size, queue depth of 1, single job/thread per disk
  * Disk/Data Integrity Verification, Sequential Write Operations
    * 4k block size, queue depth of 1, single job/thread per disk
    * 16k block size, queue depth of 1, single job/thread per disk
    * 1024k block size, queue depth of 1, single job/thread per disk

* **Profile Parameters**  
  The following parameters can be optionally supplied on the command line to modify the behaviors of the workload.

  | Parameter                 | Purpose                                                                         | Default Value |
  |---------------------------|---------------------------------------------------------------------------------|---------------|
  | DataIntegrityFileSize     | Optional. Defines the size of the file/disk space that will be used for profile disk integrity scenarios/actions. | 4GB |
  | DiskFilter                | Optional. Filter allowing the user to select the disks on which to test.<br/><br/>See the link 'Testing Disks' at the top for more details. | BiggestSize |
  | DiskFillSize              | Optional. Allows the user to override the default disk fill size used in the FIO profile (e.g. 500GB -> 26GB). This enables the profile to be used in scenarios where the disk size is very small (e.g. local/temp disk -> 32GB in size). | 500GB |
  | Duration                  | Optional. Defines the amount of time to run each FIO scenario/action within the profile. | 5 minutes |
  | Engine                    | Optional. Defines the I/O engine to use for the FIO operations (e.g. posixaio, libaio, windowsaio). | Linux = libaio, Windows = windowsaio |
  | FileSize                  | Optional. Allows the user to override the default file size used in the FIO profile (e.g. 496GB -> 26GB). This enables the profile to be used in scenarios where the disk size is very small (e.g. local/temp disk -> 32GB in size). | 496GB |
  | ProcessModel              | Optional. Defines how the FIO processes will be executed. The following are valid process models:<br/><br/><b>SingleProcess</b><br/>Executes a single FIO process running 1 job targeting I/O operations against each disk. Results are separated per-disk.<br/><br/><b>SingleProcessPerDisk</b><br/>Executes a single FIO process for each disk with each process running 1 job targeting I/O operations against that disk (higher stress profile). Results are separated per-disk.<br/><br/><b>SingleProcessAggregated</b><br/>Executes a single FIO process running 1 job per disk targeting I/O operations against that disk. Results are provided as an aggregation across all disks (i.e. a rollup). | SingleProcess |
  | QueueDepth                | Optional. Defines the I/O queue depth to use for the operations | 512/the thread count |
  | ThreadCount               | Optional. Specifies the number of distinct parallel operations/threads to run per job. | # logical processors / 2 |

  * **Profile Component Parameters**  
  The following section describes the parameters used by the individual components in the profile.

  | Parameter                 | Purpose                                                                         | Accepted Values |
  |---------------------------|-------------------------------------------------------------------------------|-----------------|
  | Scenario                  | Scenario use to define the given action of profile. This can be used to specify exact actions to run or exclude from the profile.  | Any string |
  | MetricsScenario           | The name to use as the "scenario" for all metrics output for the particular profile action. | |
  | CommandLine               | The command line parameters for FIO tool set. |     Any Valid FIO arguments            |
  | DiskFilter                | Filter allowing the user to select the disks on which to test. | See the link 'Testing Disks' at the top for more details. |
  | Duration                  | Defines the amount of time to run each FIO scenario/action within the profile. | integer or time span |
  | Engine                    | Optional. Defines the I/O engine to use for the FIO operations (e.g. posixaio, libaio, windowsaio). | Linux = libaio, Windows = windowsaio |
  | PackageName               | The logical name for FIO package downloaded and that contains the toolset. | |
  | ProcessModel              | Defines how the FIO processes will be executed. | <b>SingleProcess</b><br/>Executes a single FIO process running 1 job targeting I/O operations against each disk. Results are separated per-disk.<br/><br/><b>SingleProcessPerDisk</b><br/>Executes a single FIO process for each disk with each process running 1 job targeting I/O operations against that disk (higher stress profile). Results are separated per-disk.<br/><br/><b>SingleProcessAggregated</b><br/>Executes a single FIO process running 1 job per disk targeting I/O operations against that disk. Results are provided as an aggregation across all disks (i.e. a rollup). |
  | QueueDepth                | Defines the I/O queue depth to use for the operations | integer |
  | Tags                      | Tags useful for telemetry data | Any comma-separated string |
  | ThreadCount               | Specifies the number of distinct parallel operations/threads to run per job. | |

* **Profile Runtimes**  
  See the 'Metadata' section of the profile for estimated runtimes. These timings represent the length of time required to run a single round of profile 
  actions. These timings can be used to determine minimum required runtimes for the Virtual Client in order to get results. These are often estimates based on the
  number of system cores. 

* **Usage Examples**  
  The following section provides a few basic examples of how to use the workload profile. See the documentation at the top on 'Testing Disks'
  for information on how to target select disks on the system.

  ``` bash
  # Run the workload on the system (default = largest disks)
  ./VirtualClient --profile=PERF-IO-FIO.json --system=Demo --timeout=1440 --packageStore="{BlobConnectionString|SAS Uri}"

  # The example above runs on the same disks as having DiskFilter=BiggestSize
  ./VirtualClient --profile=PERF-IO-FIO.json --system=Demo --timeout=1440 --packageStore="{BlobConnectionString|SAS Uri}" --parameters=DiskFilter=BiggestSize

  # Run the workload against the operating system disk
  ./VirtualClient --profile=PERF-IO-FIO.json --system=Demo --timeout=1440 --packageStore="{BlobConnectionString|SAS Uri}" --parameters=DiskFilter=OSDisk

  # Run the workload against all of the disks except the operating system disk.
  ./VirtualClient --profile=PERF-IO-FIO.json --system=Demo --timeout=1440 --packageStore="{BlobConnectionString|SAS Uri}" --parameters=DiskFilter=OSDisk:false

  # Run the workload on specific drives/disks
  ./VirtualClient --profile=PERF-IO-FIO.json --system=Demo --timeout=1440 --packageStore="{BlobConnectionString|SAS Uri}" --parameters=DiskFilter=DiskPath:/dev/sdc1,/dev/sdd1

  # Run against smaller disks on the system
  ./VirtualClient --profile=PERF-IO-FIO.json --system=Demo --timeout=1440 --packageStore="{BlobConnectionString|SAS Uri}" --parameters=DiskFilter=OSDisk:false&smallestSize,,,DiskFillSize=26G,,,FileSize=26G

  # Run specific scenarios only. Each action in a profile as a 'Scenario' name.
  ./VirtualClient --profile=PERF-IO-FIO.json --system=Demo --timeout=1440 --packageStore="{BlobConnectionString|SAS Uri}" --scenarios=RandomWrite_4k_BlockSize,RandomWrite_8k_BlockSize,RandomRead_8k_BlockSize,RandomRead_4k_BlockSize
  ```

-----------------------------------------------------------------------

## PERF-IO-FIO-OLTP.json
Runs an IO-intensive workload using the Flexible IO Tester (FIO) toolset. Multi-throughput OLTP-C workload to emulate a SQL Server OLTP disk 
workload by running four workload compononents in-parallel: random reads, random writes, sequential reads and sequential writes each with an overall 
weight/percentage. 

  ``` script
  Examples:
  For Total IOPS = 5000, Random Read Weight = 5416, Random Write Weight = 4255, Sequential Read Weight = 0 , Sequential Write Weight = 329
  - Random Read IOPS = (5000 * 5416)/(5416+4255+0+329) = 2708
  - Random Write IOPS = (5000 * 4255)/(5416+4255+0+329) = 2128
  - Sequential Read IOPS = (5000 * 0)/(5416+4255+0+329) = 0
  - Sequential Write IOPS = (5000 * 329)/(5416+4255+0+329) = 164
  ```

Random IO : It represents the Database of OLTP-C workload.
Sequential IO : It represents the logs of OLTP-C workload.
Therefore, they are performed on different disks

* **Supported Platform/Architectures**
  * linux-x64
  * linux-arm64
  * win-x64  

* **Supported Operating Systems**
  * Ubuntu 18
  * Ubuntu 20
  * Ubuntu 22
  * Ubuntu 24
  * Azlinux 3

* **Dependencies**  
  The dependencies defined in the 'Dependencies' section of the profile itself are required in order to run the workload operations effectively.
  * Internet connection.
  * Disk mount points exist for the disks to be targeted. Virtual Client will generally ensure that mount points exist by default. Details for mount point creation procedures can be found in the 'Testing Disks' documentation above.
  * Any 'DiskFilter' parameter value used should match the set of disks desired. See the link for 'Testing Disks' above.

  Additional information on components that exist within the 'Dependencies' section of the profile can be found in the following locations:
  * [Installing Dependencies](https://microsoft.github.io/VirtualClient/docs/category/dependencies/)

* **Profile Parameters**  
  The following parameters can be optionally supplied on the command line to modify the behaviors of the workload.

  | Parameter                 | Purpose                                                                         | Default Value |
  |---------------------------|---------------------------------------------------------------------------------|---------------|
  | DiskFilter                | Disk filter to choose disks. Default is to test on biggest non-OS disks.             | BiggestSize |
  | RandomIOFileSize          | Optional. Allows the user to override the default random io file size used in the profile (e.g. 124GB -> 26GB). This enables the profile to be used in scenarios where the disk size is very small (e.g. local/temp disk -> 32GB in size). | 124GB |
  | SequentialIOFileSize      | Optional. Allows the user to override the default random io file size used in the profile. | 20GB |
  | DirectIO                  | Optional. Set to true to avoid using I/O buffering and to operate directly against the disk. Set to false to use I/O buffering. | true |
  | InitializeDisksInParallel | Optional. Specifies whether uninitialized/unformatted disks on the system should be initialized + formatted in parallel. | true (initialized in-parallel) |
  | SequentialDiskCount | Optional. Specifies the number of disk that will have Sequential I/O from Selected Disks. | 1 |
  
  
* **Profile Component Parameters** 
  The following section describes the parameters used by the individual components in the profile.

  | Parameter                 | Purpose                                                                         | 
  |---------------------------|---------------------------------------------------------------------------------|
  | DirectIO | Direct IO parameter for FIO toolset |
  | DurationSec | Type of Input Output operation |
  | JobFiles | Template job files to be used |
  | RandomReadBlockSize  | Random read component's Block size. If it is provided it overwrites the DefaultRandomIOBlockSize for Random read component.  |
  | RandomReadNumJobs | Random read component's Number of jobs. If it is provided it overwrites the DefaultNumJobs for Random read component. |
  | RandomWriteBlockSize  | Random write component's Block size. If it is provided it overwrites the DefaultRandomIOBlockSize for Random write component.  |
  | RandomWriteNumJobs | Random write component's Number of jobs. If it is provided it overwrites the DefaultNumJobs for Random write component. |
  | SequentialReadBlockSize  | Sequential read component's Block size. If it is provided it overwrites the DefaultSequentialIOBlockSize for Sequential read component.  |
  | SequentialReadNumJobs | Sequential read component's Number of jobs. If it is provided it overwrites the DefaultNumJobs for Sequential read component. |
  | SequentialWriteBlockSize  | Sequential write component's Block size. If it is provided it overwrites the DefaultSequentialIOBlockSize for Sequential write component.  |
  | SequentialWriteNumJobs | Sequential write component's Number of jobs. If it is provided it overwrites the DefaultNumJobs for Sequential write component. |
  | ProcessModel              |  Allows the user to override the default value you can selection Single Process for all disk(SingleProcess) or 1 process for each disk under test (SingleProcessPerDisk). |
  | Scenario                  | Scenario use to define the given action of profile  |
  | Tags                      | Tags useful for telemetry data |

* **Profile Runtimes**  
  See the 'Metadata' section of the profile for estimated runtimes. These timings represent the length of time required to run a single round of profile 
  actions. These timings can be used to determine minimum required runtimes for the Virtual Client in order to get results. These are often estimates based on the
  number of system cores. 
  
* **Usage Examples**  
  The following section provides a few basic examples of how to use the workload profile.

  ``` bash
  # Run the workload on the system
  ./VirtualClient --profile=PERF-IO-FIO-OLTP.json --timeout=1440 --packageStore="{BlobConnectionString|SAS Uri}"
  ```