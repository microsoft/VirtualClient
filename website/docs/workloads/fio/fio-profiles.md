# Flexible I/O Tester (FIO) Workload Profiles
The following profiles run customer-representative or benchmarking scenarios using the Flexible I/O Tester (FIO) workload.  

* [Workload Details](./fio.md)  
* [Testing Specific Disks](../../guides/0220-usage-testing-disks.md)

## PERF-IO-FIO.json
Runs an IO-intensive workload using the Flexible IO Tester (FIO) toolset to test performance of disks on the system. Although this profile
can run on Windows, it is predominantly used on Linux systems. The profile runs the workload in-parallel on ALL disks that match the "DiskFilter" 
parameter (see below) by default.

* [Workload Profile](https://github.com/microsoft/VirtualClient/blob/main/src/VirtualClient/VirtualClient.Main/profiles/PERF-IO-FIO.json) 

Additonally this profile is designed to auto-scale to the number of cores on the system on which it runs. It uses a simple algorithm to determine 2 key
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

* **Supports Disconnected Scenarios**  
  * No. Internet connection required.

* **Dependencies**  
  The dependencies defined in the 'Dependencies' section of the profile itself are required in order to run the workload operations effectively.
  * Internet connection.
  * Any 'DiskFilter' parameter value used should match the set of disks desired. See the link for 'Testing Specific Disks' above.

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
  | DiskFilter                | Optional. Filter allowing the user to select the disks on which to test.<br/><br/>See the link 'Testing Specific Disks' at the top for more details. | BiggestSize |
  | DiskFillSize              | Optional. Allows the user to override the default disk fill size used in the FIO profile (e.g. 500GB -> 26GB). This enables the profile to be used in scenarios where the disk size is very small (e.g. local/temp disk -> 32GB in size). | 500GB |
  | FileSize                  | Optional. Allows the user to override the default file size used in the FIO profile (e.g. 496GB -> 26GB). This enables the profile to be used in scenarios where the disk size is very small (e.g. local/temp disk -> 32GB in size). | 496GB |
  | InitializeDisksInParallel | Optional. Specifies whether uninitialized/unformatted disks on the system should be initialized + formatted in parallel. | false (initialized sequentially) |

* **Profile Runtimes**  
  See the 'Metadata' section of the profile for estimated runtimes. These timings represent the length of time required to run a single round of profile 
  actions. These timings can be used to determine minimum required runtimes for the Virtual Client in order to get results. These are often estimates based on the
  number of system cores. 

* **Usage Examples**  
  The following section provides a few basic examples of how to use the workload profile. See the documentation at the top on 'Testing Specific Disks'
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

## PERF-IO-FIO-DISCOVERY.json
Runs an IO-intensive workload using the Flexible IO Tester (FIO) toolset. FIO Discovery measures throughput as a function of increasing queue depth for multiple operation 
types(Random Read,Random Write,Sequential Write & Sequential Read) and block sizes. The workload runs directly against the raw disks without having the file system involved
(e.g. /dev/sda, /dev/sdc).

This profile uses an algorithm to determine the total number of jobs/threads as well as queue depth for each job/thread.

* Total number of jobs/threads = Minimum(ScenarioQueueDepth, MaxThreadsThreshold). These 2 parameters are described below.

  ``` script
  Examples:  
  For ScenarioQueueDepth = 1, MaxThreadsThreshold = 4:
  Total number of jobs/threads = Minimum(1,4) = 1

  For ScenarioQueueDepth = 16, MaxThreadsThreshold = 4:
  Total number of jobs/threads = Minimum(16,4) = 4 
  ```

* Queue Depth per Thread = (ScenarioQueueDepth + Threads - 1) / Threads where Threads = Total number of jobs/threads

  ``` script
  Examples:  
  For ScenarioQueueDepth = 16, Threads = 5:
  Queue Depth per Thread = (16 + 5 -1)/5 = 4

  For ScenarioQueueDepth = 16, MaxThreadsThreshold = 6:
  Queue Depth per Thread = (16 + 6 -1)/6 ~= 3 [Round down to closest integer]
  ```

* **Supported Platform/Architectures**
  * linux-x64
  * linux-arm64
  * win-x64  

* **Supported Operating Systems**
  * Ubuntu 18
  * Ubuntu 20
  * Ubuntu 22

* **Dependencies**  
  The dependencies defined in the 'Dependencies' section of the profile itself are required in order to run the workload operations effectively.
  * Internet connection.
  * Any 'DiskFilter' parameter value used should match the set of disks desired. See the link for 'Testing Specific Disks' above.

  Additional information on components that exist within the 'Dependencies' section of the profile can be found in the following locations:
  * [Installing Dependencies](https://microsoft.github.io/VirtualClient/docs/category/dependencies/)

* **Scenarios**  
  The following scenarios are covered by this workload profile. For each one of the scenarios, the profile runs the workload through the range of
  queue depths noted (by default) in sequence.

  * Random Write Operations
    * 4k block size, queue depths [1,4,16,64,256,1024], multiple jobs/threads per disk
    * 8k block size, queue depths [1,4,16,64,256,1024], multiple jobs/threads per disk
    * 16k block size, queue depths [1,4,16,64,256,1024], multiple jobs/threads per disk
    * 64k block size, queue depths [1,4,16,64,256,1024], multiple jobs/threads per disk
    * 256k block size, queue depths [1,4,16,64,256,1024], multiple jobs/threads per disk
    * 1024k block size, queue depths [1,4,16,64,256,1024], multiple jobs/threads per disk
  * Random Read Operations
    * 4k block size, queue depths [1,4,16,64,256,1024], multiple jobs/threads per disk
    * 8k block size, queue depths [1,4,16,64,256,1024], multiple jobs/threads per disk
    * 16k block size, queue depths [1,4,16,64,256,1024], multiple jobs/threads per disk
    * 64k block size, queue depths [1,4,16,64,256,1024], multiple jobs/threads per disk
    * 256k block size, queue depths [1,4,16,64,256,1024], multiple jobs/threads per disk
    * 1024k block size, queue depths [1,4,16,64,256,1024], multiple jobs/threads per disk
  * Sequential Write Operations
    * 4k block size, queue depths [1,4,16,64,256,1024], multiple jobs/threads per disk
    * 8k block size, queue depths [1,4,16,64,256,1024], multiple jobs/threads per disk
    * 16k block size, queue depths [1,4,16,64,256,1024], multiple jobs/threads per disk
    * 64k block size, queue depths [1,4,16,64,256,1024], multiple jobs/threads per disk
    * 256k block size, queue depths [1,4,16,64,256,1024], multiple jobs/threads per disk
    * 1024k block size, queue depths [1,4,16,64,256,1024], multiple jobs/threads per disk
  * Sequential Read Operations
    * 4k block size, queue depths [1,4,16,64,256,1024], multiple jobs/threads per disk
    * 8k block size, queue depths [1,4,16,64,256,1024], multiple jobs/threads per disk
    * 16k block size, queue depths [1,4,16,64,256,1024], multiple jobs/threads per disk
    * 64k block size, queue depths [1,4,16,64,256,1024], multiple jobs/threads per disk
    * 256k block size, queue depths [1,4,16,64,256,1024], multiple jobs/threads per disk
    * 1024k block size, queue depths [1,4,16,64,256,1024], multiple jobs/threads per disk
  
* **Profile Parameters**  
  The following parameters can be optionally supplied on the command line to modify the behaviors of the workload.

  | Parameter                 | Purpose                                                                         | Default Value |
  |---------------------------|---------------------------------------------------------------------------------|---------------|
  | DiskFilter                | Optional. Filter allowing the user to select the disks on which to test.<br/><br/>See '[disk testing scenarios](https://github.com/microsoft/VirtualClient/blob/main/website/docs/guides/usage-scenarios/test-disks.md)' for more details. | BiggestSize |
  | DiskFillSize              | Optional. Allows the user to override the default disk fill size used in the FIO profile (e.g. 134GB -> 26GB). This enables the profile to be used in scenarios where the disk size is very small (e.g. local/temp disk -> 32GB in size). | 134GB |
  | FileSize                  | Optional. Allows the user to override the default file size used in the FIO profile (e.g. 134GB -> 26GB). This enables the profile to be used in scenarios where the disk size is very small (e.g. local/temp disk -> 32GB in size). | 134GB |
  | ProcessModel              | Optional. Allows the user to override the default value you can selection Single Process for all disk(SingleProcess) or 1 process for each disk under test (SingleProcessPerDisk). | SingleProcess |
  | MaxThreads                | Optional. Allows the user to override the maximum number of threads used by FIO.By default if 'null' is given as value. It will use the number cores of the machine | Number of CPU cores |
  | QueueDepths               | Optional. Allows the user to override the a comma seperated list of queuedepths to iterate. A single queueDepth can be named as ScenarioQueueDepth | "1,4,16,64,256,1024" |
  | DirectIO                  | Optional. Set to true to avoid using I/O buffering and to operate directly against the disk. Set to false to use I/O buffering. | true |
  | InitializeDisksInParallel | Optional. Specifies whether uninitialized/unformatted disks on the system should be initialized + formatted in parallel. | true (initialized in-parallel) |
  
* **Profile Component Parameters**  
  The following section describes the parameters used by the individual components in the profile.

  | Parameter                 | Purpose                                                                         | Accepted Values |
  |---------------------------|-------------------------------------------------------------------------------|-----------------|
  | Scenario                  | Scenario use to define the given action of profile. This can be used to specify exact actions to run or exclude from the profile.  | Any string |
  | CommandLine               | The command line parameters for FIO tool set. |     Any Valid FIO arguments            |
  | BlockSize                 | The block size for FIO tool set. | 4k;8k;16k |
  | DurationSecs              | The number of seconds to run the FIO scenario/action |  |
  | IOType                    | Type of Input Output operation | RandRead;RandWrite;Read;Write |
  | PackageName               | The logical name for FIO package downloaded and that contains the toolset. | fio |
  | Tags                      | Tags usefull for telemetry data | Any comma seperated string |
  | DeleteTestFilesOnFinish   | Not used. |  |
  | Tests                     | Not used. |  |

* **Profile Runtimes**  
  See the 'Metadata' section of the profile for estimated runtimes. These timings represent the length of time required to run a single round of profile 
  actions. These timings can be used to determine minimum required runtimes for the Virtual Client in order to get results. These are often estimates based on the
  number of system cores. 
  
* **Usage Examples**  
  The following section provides a few basic examples of how to use the workload profile.

  ``` bash
  # Run the workload on the system (default = largest disks)
  ./VirtualClient --profile=PERF-IO-FIO-DISCOVERY.json --system=Demo --timeout=1440 --packageStore="{BlobConnectionString|SAS Uri}"

  # The example above runs on the same disks as having DiskFilter=BiggestSize
  ./VirtualClient --profile=PERF-IO-FIO-DISCOVERY.json --system=Demo --timeout=1440 --packageStore="{BlobConnectionString|SAS Uri}" --parameters=DiskFilter=BiggestSize

  # Run the workload against the operating system disk
  ./VirtualClient --profile=PERF-IO-FIO-DISCOVERY.json --system=Demo --timeout=1440 --packageStore="{BlobConnectionString|SAS Uri}" --parameters=DiskFilter=OSDisk

  # Run the workload against all of the disks except the operating system disk.
  ./VirtualClient --profile=PERF-IO-FIO-DISCOVERY.json --system=Demo --timeout=1440 --packageStore="{BlobConnectionString|SAS Uri}" --parameters=DiskFilter=OSDisk:false

  # Run the workload on specific drives/disks
  ./VirtualClient --profile=PERF-IO-FIO-DISCOVERY.json --system=Demo --timeout=1440 --packageStore="{BlobConnectionString|SAS Uri}" --parameters=DiskFilter=DiskPath:/dev/sdc1,/dev/sdd1

  # Run against smaller disks on the system
  ./VirtualClient --profile=PERF-IO-FIO-DISCOVERY.json --system=Demo --timeout=1440 --packageStore="{BlobConnectionString|SAS Uri}" --parameters=DiskFilter=OSDisk:false&smallestSize,,,DiskFillSize=26G,,,FileSize=26G
  
  # Override the default queue depths
  ./VirtualClient --profile=PERF-IO-FIO-DISCOVERY.json --system=Demo --timeout=1440 --packageStore="{BlobConnectionString|SAS Uri}"  --parameters="QueueDepths="4,16,256"
  ```

-----------------------------------------------------------------------

## PERF-IO-FIO-MULTITHROUGHPUT.json
Runs an IO-intensive workload using the Flexible IO Tester (FIO) toolset. Multi-throughput OLTP-C workload to emulate a SQL Server OLTP disk 
workload by running four workload compononents in-parallel: random reads, random writes, sequential reads and sequential writes each with an overall 
weight/percentage defined. 
A weight of 0 for and of the workload components will cause that component to be excluded from the overall operations. 

Random IO : It represents the Database of OLTP-C workload.
Sequential IO : It represents the logs of OLTP-C workload.
Therefore, they are performed on different disks

The workload runs directly against the raw disks without having the file system involved (e.g. /dev/sda, /dev/sdc);

This profile uses an algorithm to determine the amount of IOPS to run against the disks & Random IO ,Sequential IO Disks:

* Number of disks used to Perform Sequential I/O = Sequential Disks Count (Smallest Disks)
* Number of disks used to Perform Random I/O = Total Filtered Disks - Sequential Disks Count  

  ``` script
  Example 1:
  Given Disks Matching 'DiskFilter' = [(/dev/sda1 = 1TB), (/dev/sdb1 = 64GB), (/dev/sdc1 = 1TB)]
  Sequential Disks Count = 1
  - Disk Used to Perform Random I/O = (/dev/sdc1 = 1TB, /dev/sda1 = 1TB)  
  - Disk Used to Perform Sequential I/O = (/dev/sdb1 = 64GB)  
  
  Example 2:
  Given Disks Matching 'DiskFilter' = [(/dev/sda1 = 1TB), (/dev/sdb1 = 2TB), (/dev/sdc1 = 4TB)] 
  Sequential Disks Count = 2
  - Disk Used to Perform Random I/O = (/dev/sdc1 = 4TB )  
  - Disk Used to Perform Sequential I/O = (/dev/sdb1 = 2TB, /dev/sda1 = 1TB)  
  ```

* Total IOPS = (TargetIOPS * ScenarioTargetPercentage)/100 (parameters described below).

  ``` script
  Example 1:
  For Target IOPS = 5000, Scenario Target Percentage = 10  
  - Total IOPS = (5000 x 10)/100 = 500  

  Example 2:
  For Target IOPS = 5555, Scenario Target Percentage = 10  
  - Total IOPS = (5555 x 10)/100 = 555  
  ```

* Component IOPS = ((Total IOPS) * (Component Weight))/(Total Weight)

  ``` script
  Examples:
  For Total IOPS = 1200, Random Read Weight = 40, Random Write Weight = 40, Sequential Read Weight = 0 , Sequential Write Weight = 40
  - Random Read IOPS = (1200 * 40)/(40+40+0+40) = 400
  - Random Write IOPS = (1200 * 40)/(40+40+0+40) = 400
  - Sequential Read IOPS = (1200 * 0)/(40+40+0+40) = 0
  - Sequential Write IOPS = (1200 * 40)/(40+40+0+40) = 400
  ```

* **Supported Platform/Architectures**
  * linux-x64
  * linux-arm64
  * win-x64  

* **Dependencies**  
  The dependencies defined in the 'Dependencies' section of the profile itself are required in order to run the workload operations effectively.
  * Internet connection.
  * Any 'DiskFilter' parameter value used should match the set of disks desired. See the link for 'Testing Specific Disks' above.

  Additional information on components that exist within the 'Dependencies' section of the profile can be found in the following locations:
  * [Installing Dependencies](https://microsoft.github.io/VirtualClient/docs/category/dependencies/)

* **Scenarios**  
  The following scenarios are covered by this workload profile.

  * 10 target percentage, totalIOPS 500, Random read & write jobs on RandomIODisk & Sequential read & write jobs on SequentialIODisk
  * 40 target percentage, totalIOPS 2000, Random read & write jobs on RandomIODisk & Sequential read & write jobs on SequentialIODisk
  * 90 target percentage, totalIOPS 4500, Random read & write jobs on RandomIODisk & Sequential read & write jobs on SequentialIODisk
  * 98 target percentage, totalIOPS 4900, Random read & write jobs on RandomIODisk & Sequential read & write jobs on SequentialIODisk
  * 100 target percentage, totalIOPS 5000, Random read & write jobs on RandomIODisk & Sequential read & write jobs on SequentialIODisk
  * 102 target percentage, totalIOPS 5100, Random read & write jobs on RandomIODisk & Sequential read & write jobs on SequentialIODisk
  * 110 target percentage, totalIOPS 5100, Random read & write jobs on RandomIODisk & Sequential read & write jobs on SequentialIODisk

* **Profile Parameters**  
  The following parameters can be optionally supplied on the command line to modify the behaviors of the workload.

  | Parameter                 | Purpose                                                                         | Default Value |
  |---------------------------|---------------------------------------------------------------------------------|---------------|
  | DefaultNumJobs            | Optional. Allows the user to override Number of jobs for each component (Random read component,Random write component,Sequential read component,Sequential write component)             | 1 |
  | DiskFilter                | Disk filter to choose disks. Default is to test on biggest non-OS disks.             | BiggestSize |
  | RandomIOFileSize          | Optional. Allows the user to override the default random io file size used in the profile (e.g. 124GB -> 26GB). This enables the profile to be used in scenarios where the disk size is very small (e.g. local/temp disk -> 32GB in size). | 124GB |
  | SequentialIOFileSize      | Optional. Allows the user to override the default random io file size used in the profile. | 20GB |
  | TargetIOPS                | Optional. Allows the user to override the default value for Target IOPS for all the components combined. | 5000 |
  | TargetPercents            | Optional. Allows the user to override the target percent list which is use to determine Total IOPS. | "10,40,90,98,100,102,110" |
  | DirectIO                  | Optional. Set to true to avoid using I/O buffering and to operate directly against the disk. Set to false to use I/O buffering. | true |
  | InitializeDisksInParallel | Optional. Specifies whether uninitialized/unformatted disks on the system should be initialized + formatted in parallel. | true (initialized in-parallel) |
  | SequentialDiskCount | Optional. Specifies the number of disk that will have Sequential I/O from Selected Disks. | 1 |
  
  
* **Profile Component Parameters** 
  The following section describes the parameters used by the individual components in the profile.

  | Parameter                 | Purpose                                                                         | Profile Values |
  |---------------------------|---------------------------------------------------------------------------------|-----------------|
  | DefaultRandomIOBlockSize  | Default Block size value for Random Read and Write. | 8k |
  | DefaultRandomIOQueueDepth | Default QueueDepth value for Random Read and Write. | 512 |
  | DefaultSequentialIOBlockSize  | Default Block size value for Sequential Read and Write. | 56K|
  | DefaultSequentialIOQueueDepth | Default Queue Depth value for Sequential Read and Write.|  64|
  | DirectIO | Direct IO parameter for FIO toolset |"1"|
  | GroupReporting               | Group Reporting parameter for FIO toolset| "0" |
  | RandomReadBlockSize  | Random read component's Block size. If it is provided it overwrites the DefaultRandomIOBlockSize for Random read component.  | null |
  | RandomReadNumJobs | Random read component's Number of jobs. If it is provided it overwrites the DefaultNumJobs for Random read component. | null |
  | RandomReadQueueDepth | Random read component's Queue Depth. If it is provided it overwrites the DefaultRandomIOQueueDepth for Random read component. | null |
  | RandomReadWeight | Weight of Random read component being use to calculate the IOPS of random read component. | 5416 |
  | RandomWriteBlockSize  | Random write component's Block size. If it is provided it overwrites the DefaultRandomIOBlockSize for Random write component.  | null |
  | RandomWriteNumJobs | Random write component's Number of jobs. If it is provided it overwrites the DefaultNumJobs for Random write component. | null |
  | RandomWriteQueueDepth | Random write component's Queue Depth. If it is provided it overwrites the DefaultRandomIOQueueDepth for Random write component. | null |
  | RandomWriteWeight | Weight of Random write component being use to calculate the IOPS of random write component. | 4255 |
  | SequentialReadBlockSize  | Sequential read component's Block size. If it is provided it overwrites the DefaultSequentialIOBlockSize for Sequential read component.  | null |
  | SequentialReadNumJobs | Sequential read component's Number of jobs. If it is provided it overwrites the DefaultNumJobs for Sequential read component. | null |
  | SequentialReadQueueDepth | Sequential read component's Queue Depth. If it is provided it overwrites the DefaultSequentialIOQueueDepth for Sequential read component. | null |
  | SequentialReadWeight | Weight of Sequential read component being use to calculate the IOPS of random read component. | 0 |
  | SequentialWriteBlockSize  | Sequential write component's Block size. If it is provided it overwrites the DefaultSequentialIOBlockSize for Sequential write component.  | null  |
  | SequentialWriteNumJobs | Sequential write component's Number of jobs. If it is provided it overwrites the DefaultNumJobs for Sequential write component. | null |
  | SequentialWriteQueueDepth | Sequential write component's Queue Depth. If it is provided it overwrites the DefaultSequentialIOQueueDepth for Sequential write component. | null |
  | SequentialWriteWeight | Weight of Sequential write component being use to calculate the IOPS of random write component. | 329 |
  | DurationSec | Type of Input Output operation | 300 |
  | TemplateJobFile | File name for the template job file.| oltp-c.fio.jobfile |
  | Scenario                  | Scenario use to define the given action of profile  | fio_multithroughput |
  | Tags                      | Tags usefull for telemetry data | IO,FIO,MultiThroughput,OLTP |

* **Profile Runtimes**  
  See the 'Metadata' section of the profile for estimated runtimes. These timings represent the length of time required to run a single round of profile 
  actions. These timings can be used to determine minimum required runtimes for the Virtual Client in order to get results. These are often estimates based on the
  number of system cores. 
  
* **Usage Examples**  
  The following section provides a few basic examples of how to use the workload profile.

  ``` bash
  # Run the workload on the system (default = largest disks)
  ./VirtualClient --profile=PERF-IO-FIO-MULTITHROUGHPUT.json --system=Demo --timeout=1440 --packageStore="{BlobConnectionString|SAS Uri}"

  # The example above runs on the same disks as having DiskFilter=BiggestSize
  ./VirtualClient --profile=PERF-IO-FIO-MULTITHROUGHPUT.json --system=Demo --timeout=1440 --packageStore="{BlobConnectionString|SAS Uri}" --parameters=DiskFilter=BiggestSize

  # Run the workload against the operating system disk
  ./VirtualClient --profile=PERF-IO-FIO-MULTITHROUGHPUT.json --system=Demo --timeout=1440 --packageStore="{BlobConnectionString|SAS Uri}" --parameters=DiskFilter=OSDisk

  # Run the workload against all of the disks except the operating system disk.
  ./VirtualClient --profile=PERF-IO-FIO-MULTITHROUGHPUT.json --system=Demo --timeout=1440 --packageStore="{BlobConnectionString|SAS Uri}" --parameters=DiskFilter=OSDisk:false

  # Run the workload on specific drives/disks
  ./VirtualClient --profile=PERF-IO-FIO-MULTITHROUGHPUT.json --system=Demo --timeout=1440 --packageStore="{BlobConnectionString|SAS Uri}" --parameters=DiskFilter=DiskPath:/dev/sdc1,/dev/sdd1

  # Run against smaller disks on the system
  ./VirtualClient --profile=PERF-IO-FIO-MULTITHROUGHPUT.json --system=Demo --timeout=1440 --packageStore="{BlobConnectionString|SAS Uri}" --parameters=DiskFilter=OSDisk:false&smallestSize,,,DiskFillSize=26G,,,FileSize=26G
  
  # Override the default target percentages
  ./VirtualClient --profile=PERF-IO-FIO-MULTITHROUGHPUT.json --system=Demo --timeout=1440 --packageStore="{BlobConnectionString|SAS Uri}"  --parameters="TargetPercents="40,80,120"
  ```