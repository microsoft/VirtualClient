# Flexible I/O Tester (FIO) Workload Profiles
The following profiles run customer-representative or benchmarking scenarios using the Flexible I/O Tester (FIO) workload.  

* [Workload Details](./fio.md)  
* [Workload Profile Metrics](./fio-metrics.md)


-----------------------------------------------------------------------

### Preliminaries
The profiles below require the ability to download workload packages and dependencies from a package store. In order to download the workload packages, connection information 
must be supplied on the command line. See the 'Workload Packages' documentation above for details on how that works.

-----------------------------------------------------------------------

### PERF-IO-FIO.json
Runs an high stress IO-intensive workload using the Flexible IO Tester (FIO) toolset to test performance of disks on the system as well as 
(in VM scenarios) to cause significant CPU stress on an underlying physical host. This profile is typically used on Linux system scenarios as the
FIO toolset is designed primarily for Linux systems. Depending upon the parameters defined (see above), this profile will run the FIO workload on
each of the disks on the system that match the specification. For example if there are 16 remote/managed disks on the system and diskFilter
is default, FIO will be ran on all 16 disks concurrently as part of the profile execution.

Note that this profile is designed to auto-scale to the number of cores on the system on which it runs. It uses a simple algorithm to determine 2 key
aspects of the workload execution.

1) Total number of jobs/threads = {# of logical cores} / 2


```
   Examples:  
     For a 16-core system:
     16/2 = 8 concurrent jobs/threads per FIO execution (i.e. # threads to run I/O operations against the test file concurrently).

     For a 64-core system:
     64/2 = 32 concurrent jobs/threads per FIO execution (i.e. # threads to run I/O operations against the test file concurrently).
```

2) Total I/O depth =  512 / {Total number of jobs/threads}


 ```
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

* **Dependencies**  
  The following dependencies must be met to run this workload profile.

  * Workload package must exist in the 'packages' directory or connection information for the package store supplied on the command line (see 'Workload Packages' link above).

* **Scenarios**  
  The following scenarios are covered by this workload profile. Note that the default disk fill size is 500GB and the default file size (drive space used)
  is 496GB. Both of these defaults can be overridden on the command line with parameters as noted below. Note that the workload will be ran on ALL disks that
  match the category (e.g. remote/managed, system disks) concurrently. For example if there are 16 remote/managed disks on the system when targeting these
  types of disks, the workload will be ran on each of the 16 disks at the same time concurrently. See the 'Profile Parameters' section below for more information
  on targeting specific types of disks on the system.

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
  * Random Write Operation Disk/Data Integrity Verification
    * 4k block size, queue depth of 1, single job/thread per disk
    * 16k block size, queue depth of 1, single job/thread per disk
    * 1024k block size, queue depth of 1, single job/thread per disk
  * Sequential Write Operation Disk/Data Integrity Verification
    * 4k block size, queue depth of 1, single job/thread per disk
    * 16k block size, queue depth of 1, single job/thread per disk
    * 1024k block size, queue depth of 1, single job/thread per disk

* **Profile Parameters**  
  Note that the default behavior of this workload is to test the remote/managed disks only. The following parameters can be optionally supplied
  on the command line to change this default behavior. See the 'Usage Scenarios/Examples' above for examples on how to supply parameters to 
  Virtual Client profiles.

  | Parameter                 | Purpose                                                                         | Default Value |
  |---------------------------|---------------------------------------------------------------------------------|---------------|
  | DiskFilter           | Disk filter to choose disks. Default is to test on biggest non-OS disks.             | BiggestSize |
  | DiskFillSize              | Optional. Allows the user to override the default disk fill size used in the FIO profile (e.g. 500GB -> 26GB). This enables the profile to be used in scenarios where the disk size is very small (e.g. local/temp disk -> 32GB in size). | 500GB |
  | FileSize                  | Optional. Allows the user to override the default file size used in the FIO profile (e.g. 496GB -> 26GB). This enables the profile to be used in scenarios where the disk size is very small (e.g. local/temp disk -> 32GB in size). | 496GB |

* **Testing Small-Size Disks (e.g. local/temp disk)**  
  See the section below for important details to understand when testing local/temp disks.

* **Workload Runtimes**  
  * Expected Runtime on Linux systems 
    * (64-core/vCPU VM) = 3 - 4 hours (including time required create initial 496GB file on each disk)
  * Expected Runtimes on Windows systems
    * (64-core/vCPU VM) = 4 - 5 hours (including time required create initial 496GB file on each disk)

* **Usage Examples**  
  The following section provides a few basic examples of how to use the workload profile. Additional usage examples can be found in the
  'Usage Scenarios/Examples' link at the top.


  ``` bash
  // Tests the remote/managed disks by default
  ./VirtualClient --profile=PERF-IO-FIO.json --system=Azure --timeout=1440 --packageStore="{BlobConnectionString|SAS Uri}"

  // Test other types of disks
  ./VirtualClient --profile=PERF-IO-FIO.json --system=Azure --timeout=1440 --packageStore="{BlobConnectionString|SAS Uri}" --parameters=DiskFilter=OSDisk:true
  ./VirtualClient --profile=PERF-IO-FIO.json --system=Azure --timeout=1440 --packageStore="{BlobConnectionString|SAS Uri}" --parameters=DiskFilter=OSDisk:false&smallestSize
  ./VirtualClient --profile=PERF-IO-FIO.json --system=Azure --timeout=1440 --packageStore="{BlobConnectionString|SAS Uri}" --parameters=DiskFilter=OSDisk:false&biggestSize
  ./VirtualClient --profile=PERF-IO-FIO.json --system=Azure --timeout=1440 --packageStore="{BlobConnectionString|SAS Uri}" --parameters=DiskFilter=SizeEqualTo:1TB

  // Test specific Linux devices
  ./VirtualClient --profile=PERF-IO-FIO.json --system=Azure --timeout=1440 --packageStore="{BlobConnectionString|SAS Uri}" --parameters=DiskFilter=DiskPath:/dev/sdc1,/dev/sdd1

  // Test on the local/temp disk that is 32GB in total size. Override the default file size of 496G.
  ./VirtualClient --profile=PERF-IO-FIO.json --system=Azure --timeout=1440 --packageStore="{BlobConnectionString|SAS Uri}" --parameters=DiskFilter=OSDisk:false&smallestSize,,,DiskFillSize=26GB,,,FileSize=26GB

  // Run specific scenarios only. Each action in a profile as a 'Scenario' name.
  ./VirtualClient --profile=PERF-IO-FIO.json --system=Azure --timeout=1440 --packageStore="{BlobConnectionString|SAS Uri}" --scenarios=RandomWrite_4k_BlockSize,RandomWrite_8k_BlockSize,RandomRead_8k_BlockSize,RandomRead_4k_BlockSize
  ```

-----------------------------------------------------------------------

### PERF-IO-FIO-DISCOVERY.json
Runs an high stress IO-intensive workload using the Flexible IO Tester (FIO) toolset. FIO Discovery measures throughput as a function of increasing queue depth for multiple operation types(Random Read,Random Write,Sequential Write & Sequential Read) and block sizes.
The following workload runs on raw disks directly for example "/dev/sda","/dev/sdc",etc on linux.

Note that this profile uses a simple algorithm to determine the total number of jobs/threads & Queue Depth Per Thread. Shown below.

1) Total number of jobs/threads = Minimum(ScenarioQueueDepth, MaxThreadsThreshold)
Below in 'Profile Parameters' section below these 2 parameters are described.


```
   Examples:  
     For ScenarioQueueDepth = 1, MaxThreadsThreshold = 4:
     Total number of jobs/threads = Minimum(1,4) = 1

     For ScenarioQueueDepth = 16, MaxThreadsThreshold = 4:
     Total number of jobs/threads = Minimum(16,4) = 4 
```

2) Queue Depth per Thread = (ScenarioQueueDepth + Threads - 1) / Threads
Where Threads = Total number of jobs/threads


```
   Examples:  
     For ScenarioQueueDepth = 16, Threads = 5:
     Queue Depth per Thread = (16 + 5 -1)/5 = 4

     For ScenarioQueueDepth = 16, MaxThreadsThreshold = 6:
     Queue Depth per Thread = (16 + 6 -1)/6 ~= 3 [Round down to closest integer]
```

* **Supported Platform/Architectures**
  * linux-x64
  * linux-arm64

* **Dependencies**  
  The following dependencies must be met to run this workload profile.

  * Workload package must exist in the 'packages' directory or connection information for the package store supplied on the command line (see 'Workload Packages' link above).

* **Scenarios**  
  The following scenarios are covered by this workload profile. Note that the default disk fill size is 134GB and the default file size (drive space used)
  is 134GB. Both of these defaults can be overridden on the command line with parameters as noted below. Note that the workload will be ran on ALL disks that
  match the "DiskFilter" paremeter concurrently. For example if there are 16 remote/managed disks on the system when targeting these
  types of disks, the workload will be ran on each of the 16 disks at the same time concurrently. See the 'Profile Parameters' section below for more information
  on targeting specific types of disks on the system.

  * Random Write Operations
    * 4k block size, queue depths [1,4,16,64,256,1024] ,multiple jobs/threads per disk
    * 8k block size, queue depths [1,4,16,64,256,1024] ,multiple jobs/threads per disk
    * 16k block size, queue depths [1,4,16,64,256,1024] ,multiple jobs/threads per disk
    * 64k block size, queue depths [1,4,16,64,256,1024] ,multiple jobs/threads per disk
    * 256k block size, queue depths [1,4,16,64,256,1024] ,multiple jobs/threads per disk
    * 1024k block size, queue depths [1,4,16,64,256,1024] ,multiple jobs/threads per disk
  * Random Read Operations
    * 4k block size, queue depths [1,4,16,64,256,1024] ,multiple jobs/threads per disk
    * 8k block size, queue depths [1,4,16,64,256,1024] ,multiple jobs/threads per disk
    * 16k block size, queue depths [1,4,16,64,256,1024] ,multiple jobs/threads per disk
    * 64k block size, queue depths [1,4,16,64,256,1024] ,multiple jobs/threads per disk
    * 256k block size, queue depths [1,4,16,64,256,1024] ,multiple jobs/threads per disk
    * 1024k block size, queue depths [1,4,16,64,256,1024] ,multiple jobs/threads per disk
  * Sequential Write Operations
    * 4k block size, queue depths [1,4,16,64,256,1024] ,multiple jobs/threads per disk
    * 8k block size, queue depths [1,4,16,64,256,1024] ,multiple jobs/threads per disk
    * 16k block size, queue depths [1,4,16,64,256,1024] ,multiple jobs/threads per disk
    * 64k block size, queue depths [1,4,16,64,256,1024] ,multiple jobs/threads per disk
    * 256k block size, queue depths [1,4,16,64,256,1024] ,multiple jobs/threads per disk
    * 1024k block size, queue depths [1,4,16,64,256,1024] ,multiple jobs/threads per disk
  * Sequential Read Operations
    * 4k block size, queue depths [1,4,16,64,256,1024] ,multiple jobs/threads per disk
    * 8k block size, queue depths [1,4,16,64,256,1024] ,multiple jobs/threads per disk
    * 16k block size, queue depths [1,4,16,64,256,1024] ,multiple jobs/threads per disk
    * 64k block size, queue depths [1,4,16,64,256,1024] ,multiple jobs/threads per disk
    * 256k block size, queue depths [1,4,16,64,256,1024] ,multiple jobs/threads per disk
    * 1024k block size, queue depths [1,4,16,64,256,1024] ,multiple jobs/threads per disk
  
* **Profile Parameters**  
  Note that the default behavior of this workload is to test the remote/managed disks only. The following parameters can be optionally supplied
  on the command line to change this default behavior. See the 'Usage Scenarios/Examples' above for examples on how to supply parameters to 
  Virtual Client profiles.

  | Parameter                 | Purpose                                                                         | Default Value |
  |---------------------------|---------------------------------------------------------------------------------|---------------|
  | DiskFilter           | Disk filter to choose disks. Default is to test on biggest non-OS disks.             | BiggestSize |
  | DiskFillSize              | Optional. Allows the user to override the default disk fill size used in the FIO profile (e.g. 134GB -> 26GB). This enables the profile to be used in scenarios where the disk size is very small (e.g. local/temp disk -> 32GB in size). | 134GB |
  | FileSize                  | Optional. Allows the user to override the default file size used in the FIO profile (e.g. 134GB -> 26GB). This enables the profile to be used in scenarios where the disk size is very small (e.g. local/temp disk -> 32GB in size). | 134GB |
  | ProcessModel                 | Optional. Allows the user to override the default value you can selection Single Process for all disk(SingleProcess) or 1 process for each disk under test (SingleProcessPerDisk). | SingleProcess |
  | MaxThresholdThreads                 | Optional. Allows the user to override the maximum number of threads used by FIO.By default if 'null' is given as value. It will use the number cores of the machine | Number of CPU cores |
  | QueueDepths                 | Optional. Allows the user to override the a comma seperated list of queuedepths to iterate. A single queueDepth can be named as ScenarioQueueDepth | "1,4,16,64,256,1024" |
  
* **FIO Discovery Executor Parameters** 
Note these can't be changed from command line.

  | Parameter                 | Purpose                                                                         | Accepted Values |
  |---------------------------|-------------------------------------------------------------------------------|-----------------|
  | CommandLine               | The command line parameters for FIO tool set.                                 |     Any Valid FIO arguments            |
  | BlockSize                 | The block size for FIO tool set. | 4k;8k;16k |
  | IOType                    | Type of Input Output operation | RandRead;RandWrite;Read;Write |
  | PackageName               | The package name for FIO toolset | fio |
  | Scenario                  | Scenario use to define the given action of profile  | Any string |
  | Tags                      | Tags usefull for telemetry data | Any comma seperated string |
  | DeleteTestFilesOnFinish   | Should we delete tests file on finishing Virtual Client Execution. | true or false |

* **Testing Small-Size Disks (e.g. local/temp disk)**  
  See the section below for important details to understand when testing local/temp disks.

* **Workload Runtimes**  
  * Expected Runtime on Linux systems 
    * (64-core/vCPU VM) = 13 - 14 hours (including time required create initial 496GB file on each disk)
  
* **Usage Examples**  
  The following section provides a few basic examples of how to use the workload profile. Additional usage examples can be found in the
  'Usage Scenarios/Examples' link at the top.


  ``` csharp
  // Tests the remote/managed disks by default
  ./VirtualClient --profile=PERF-IO-FIO-DISCOVERY.json --system=Azure --timeout=1440 --packageStore="{BlobConnectionString|SAS Uri}"

  // Test other types of disks
  ./VirtualClient --profile=PERF-IO-FIO-DISCOVERY.json --system=Azure --timeout=1440 --packageStore="{BlobConnectionString|SAS Uri}" --parameters=DiskFilter=OSDisk:true
  ./VirtualClient --profile=PERF-IO-FIO-DISCOVERY.json --system=Azure --timeout=1440 --packageStore="{BlobConnectionString|SAS Uri}" --parameters=DiskFilter=OSDisk:false&smallestSize
  ./VirtualClient --profile=PERF-IO-FIO-DISCOVERY.json --system=Azure --timeout=1440 --packageStore="{BlobConnectionString|SAS Uri}" --parameters=DiskFilter=OSDisk:false&biggestSize
  ./VirtualClient --profile=PERF-IO-FIO-DISCOVERY.json --system=Azure --timeout=1440 --packageStore="{BlobConnectionString|SAS Uri}" --parameters=DiskFilter=SizeEqualTo:1TB

  // Test specific Linux devices
  ./VirtualClient --profile=PERF-IO-FIO-DISCOVERY.json --system=Azure --timeout=1440 --packageStore="{BlobConnectionString|SAS Uri}" --parameters=DiskFilter=DiskPath:/dev/sdc1,/dev/sdd1

  // Test on the local/temp disk that is 32GB in total size. Override the default file size of 496G.
  ./VirtualClient --profile=PERF-IO-FIO-DISCOVERY.json --system=Azure --timeout=1440 --packageStore="{BlobConnectionString|SAS Uri}" --parameters=DiskFilter=OSDisk:false&smallestSize,,,DiskFillSize=26GB,,,FileSize=26GB

  // Run specific queuedepths
  ./VirtualClient --profile=PERF-IO-FIO-DISCOVERY.json --system=Azure --timeout=1440 --packageStore="{BlobConnectionString|SAS Uri}"  --parameters="DiskFilter=OSDisk:false&biggestSize,,,QueueDepths=\"4,16,256\""
  ```

-----------------------------------------------------------------------

### PERF-IO-FIO-MULTITHROUGHPUT.json

Runs an high stress IO-intensive workload using the Flexible IO Tester (FIO) toolset.Multi-throughput OLTP-C emulates a SQL Server OLTP disk workload by running four concurrent workload Components: random reads and writes, sequential reads and writes. If weight provided to any of the component is 0 then it is absent from system.
In the given profile
The following workload runs on raw disks directly for example "/dev/sda","/dev/sdc",etc on linux.

Note that this profile uses a simple algorithm to determine the RandomIODisk(On which Random reads and writes components run Concurrently) & SequentialIODisk(On which Sequential reads and writes components run Concurrently), TotalIOPS, ComponentIOPS.

1) Random IO Disk and Sequential IO Disk
Random IO Disk = Biggest Disk among the filtered disks.
Sequential IO Disk = Second Biggest Disk among the filtered disks.


```
   Examples:  
     
     For FilteredDisks : [(/dev/sda1,1TB), (/dev/sdb1,2TB), (/dev/sdc1,4TB)]
     Random IO Disk = (/dev/sdc1,4TB)
     Sequential IO Disk = (/dev/sdb1,2TB)
     NOTE: AT MAX 2 DISKS can be utilized by this profile.

     For FilteredDisks : [(/dev/sda1,1TB), (/dev/sdb1,2TB), (/dev/sdc1,4TB)]
     Random IO Disk = (/dev/sda1,1TB)
     Sequential IO Disk = (/dev/sda1,1TB)

```

2) Total IOPS = (TargetIOPS * ScenarioTargetPercentage)/100
Below in 'Profile Parameters' section below these 2 parameters are described.


```
   Examples:  
     
     For Target IOPS = 5000, Scenario Target Percentage = 10
     Total IOPS = (5000*10)/100 = 500

     For Target IOPS = 5555, Scenario Target Percentage = 10
     Total IOPS = (5555*10)/100 = 555

```

3) Component IOPS = ((Total IOPS) * (Component Weight))/(Total Weight)


```
   Examples:  
     
     For Total IOPS = 1200, Random Read Weight = 40, Random Write Weight = 40, Sequential Read Weight = 0 , Sequential Write Weight = 40
     Random Read IOPS = (1200 * 40)/(40+40+0+40) = 400
     Random Write IOPS = (1200 * 40)/(40+40+0+40) = 400
     Sequential Read IOPS = (1200 * 0)/(40+40+0+40) = 0
     Sequential Write IOPS = (1200 * 40)/(40+40+0+40) = 400

```

* **Supported Platform/Architectures**
  * linux-x64
  * linux-arm64

* **Dependencies**  
  The following dependencies must be met to run this workload profile.

  * Workload package must exist in the 'packages' directory or connection information for the package store supplied on the command line (see 'Workload Packages' link above).

* **Scenarios**  
  The following scenarios are covered by this workload profile. Note that the default random IO fill size is 124GB and the default sequential IO fill size is 20GB. Both of these defaults can be overridden on the command line with parameters as noted below.

  * 10 target percentage, totalIOPS 500, Random read & write jobs on RandomIODisk & Sequential read & write jobs on SequentialIODisk
  * 40 target percentage, totalIOPS 2000, Random read & write jobs on RandomIODisk & Sequential read & write jobs on SequentialIODisk
  * 90 target percentage, totalIOPS 4500, Random read & write jobs on RandomIODisk & Sequential read & write jobs on SequentialIODisk
  * 98 target percentage, totalIOPS 4900, Random read & write jobs on RandomIODisk & Sequential read & write jobs on SequentialIODisk
  * 100 target percentage, totalIOPS 5000, Random read & write jobs on RandomIODisk & Sequential read & write jobs on SequentialIODisk
  * 102 target percentage, totalIOPS 5100, Random read & write jobs on RandomIODisk & Sequential read & write jobs on SequentialIODisk
  * 110 target percentage, totalIOPS 5100, Random read & write jobs on RandomIODisk & Sequential read & write jobs on SequentialIODisk


* **Profile Parameters**  
  Note that the default behavior of this workload is to test the remote/managed disks only. The following parameters can be optionally supplied
  on the command line to change this default behavior. See the 'Usage Scenarios/Examples' above for examples on how to supply parameters to 
  Virtual Client profiles.

  | Parameter                 | Purpose                                                                         | Default Value |
  |---------------------------|---------------------------------------------------------------------------------|---------------|
  | DefaultNumJobs            | Optional. Allows the user to override Number of jobs for each component (Random read component,Random write component,Sequential read component,Sequential write component)             | 1 |
  | DiskFilter           | Disk filter to choose disks. Default is to test on biggest non-OS disks.             | BiggestSize |
  | RandomIOFileSize              | Optional. Allows the user to override the default random io file size used in the profile (e.g. 124GB -> 26GB). This enables the profile to be used in scenarios where the disk size is very small (e.g. local/temp disk -> 32GB in size). | 124GB |
  | SequentialIOFileSize                  | Optional. Allows the user to override the default random io file size used in the profile. | 20GB |
  | TargetIOPS                 | Optional. Allows the user to override the default value for Target IOPS for all the components combined. | 5000 |
  | TargetPercents                 | Optional. Allows the user to override the target percent list which is use to determine Total IOPS. | "10,40,90,98,100,102,110" |
  
  
* **FIO Multi Throughput Executor Parameters** 
Note these can't be changed from command line. Also the above parameters are also part of these paramters.


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

* **Testing Small-Size Disks (e.g. local/temp disk)**  
  See the section below for important details to understand when testing local/temp disks.

* **Workload Runtimes**  
  * Expected Runtime on Linux systems 
    * (64-core/vCPU VM) = 2-3 hours (including time required create initial 496GB file on each disk)
  
* **Usage Examples**  
  The following section provides a few basic examples of how to use the workload profile. Additional usage examples can be found in the
  'Usage Scenarios/Examples' link at the top.


  ``` csharp
  // Tests the remote/managed disks by default
  ./VirtualClient --profile=PERF-IO-FIO-MULTITHROUGHPUT.json --system=Azure --timeout=1440 --packageStore="{BlobConnectionString|SAS Uri}"

  // Test other types of disks
  ./VirtualClient --profile=PERF-IO-FIO-MULTITHOUGHPUT.json --system=Azure --timeout=1440 --packageStore="{BlobConnectionString|SAS Uri}" --parameters=DiskFilter=OSDisk:true
  ./VirtualClient --profile=PERF-IO-FIO-MULTITHOUGHPUT.json --system=Azure --timeout=1440 --packageStore="{BlobConnectionString|SAS Uri}" --parameters=DiskFilter=OSDisk:false&smallestSize
  ./VirtualClient --profile=PERF-IO-FIO-MULTITHOUGHPUT.json --system=Azure --timeout=1440 --packageStore="{BlobConnectionString|SAS Uri}" --parameters=DiskFilter=OSDisk:false&biggestSize
  ./VirtualClient --profile=PERF-IO-FIO-MULTITHOUGHPUT.json --system=Azure --timeout=1440 --packageStore="{BlobConnectionString|SAS Uri}" --parameters=DiskFilter=SizeEqualTo:1TB

  // Test specific Linux devices
  ./VirtualClient --profile=PERF-IO-FIO-MULTITHOUGHPUT.json --system=Azure --timeout=1440 --packageStore="{BlobConnectionString|SAS Uri}" --parameters=DiskFilter=DiskPath:/dev/sdc1,/dev/sdd1

  // Run specific queuedepths
  ./VirtualClient --profile=PERF-IO-FIO-MULTITHROUGHPUT.json --system=Azure --timeout=1440 --packageStore="{BlobConnectionString|SAS Uri}"  --parameters="DiskFilter=OSDisk:false&biggestSize,,,TargetPercents=\"40,80,120\""
  ```

-----------------------------------------------------------------------

### Disk Testing Scenarios
The VirtualClient supports a range of different disk testing scenarios on both Azure VMs as well as Azure physical hosts. The following
documentation provides context into how to run disk performance tests for these scenarios.

* [Disk Testing Scenarios](./DiskTestingScenarios.md)

-----------------------------------------------------------------------

### Resources
* [Azure VM Sizes](https://docs.microsoft.com/en-us/azure/virtual-machines/sizes)
* [Azure Managed Disks](https://azure.microsoft.com/en-us/pricing/details/managed-disks/)