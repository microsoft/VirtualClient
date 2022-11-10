# DiskSpd Workload Profiles
The following profiles run customer-representative or benchmarking scenarios using the DiskSpd workload.  

* [Workload Details](./DiskSpd.md)  
* [Workload Profile Metrics](./DiskSpdMetrics.md)
* [Workload Packages](./DependencyPackages.md)
* [Usage Scenarios/Examples](./UsageScenarios.md)

-----------------------------------------------------------------------

### Preliminaries
The profiles below require the ability to download workload packages and dependencies from a package store. In order to download the workload packages, connection information 
must be supplied on the command line. See the 'Workload Packages' documentation above for details on how that works.

-----------------------------------------------------------------------

### PERF-IO-DISKSPD.json
Runs an high stress IO-intensive workload using the DiskSpd toolset to test performance of disks on the system as well as 
(in VM scenarios) to cause significant CPU stress on an underlying physical host. DiskSpd is a Microsoft-developed, industry standard 
benchmarking toolset for Windows. This profile is a Windows-only profile.

Note that this profile is designed to auto-scale to the number of cores on the system on which it runs. It uses a simple algorithm to determine 2 key
aspects of the workload execution.

1) Total number of jobs/threads = {# of logical cores} / 2


```
   Examples:  
     For a 16-core system:
     16/2 = 16 concurrent jobs/threads per DiskSpd execution (i.e. # threads to run I/O operations against the test file concurrently).

     For a 64-core system:
     64/2 = 32 concurrent jobs/threads per DiskSpd execution (i.e. # threads to run I/O operations against the test file concurrently).
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
  * win-x64
  * win-arm64

* **Supported Operating Systems**
  * Windows 10
  * Windows 11
  * Windows Server 2016
  * Windows Server 2019

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

* **Profile Parameters**  
  Note that the default behavior of this workload is to test the remote/managed disks only. The following parameters can be optionally supplied
  on the command line to change this default behavior. See the 'Usage Scenarios/Examples' above for examples on how to supply parameters to 
  Virtual Client profiles.

  | Parameter                 | Purpose                                                                         | Default Value |
  |---------------------------|---------------------------------------------------------------------------------|---------------|
  | DiskFilter           | Optional. Defines disk filters. Default is to test on all biggest non-OS disks.     | BiggestSize |
  | DiskFillSize              | Optional. Allows the user to override the default disk fill size used in the DiskSpd profile (e.g. 500GB -> 26GB). This enables the profile to be used in scenarios where the disk size is very small (e.g. local/temp disk -> 32GB in size). Note that this parameter is application ONLY on the Stress profile. | 500G |
  | FileSize                  | Optional. Allows the user to override the default file size used in the DiskSpd profile (e.g. 496GB -> 26GB). This enables the profile to be used in scenarios where the disk size is very small (e.g. local/temp disk -> 32GB in size). Note that this parameter is application ONLY on the Stress profile. | 496G |

* **Testing Small-Size Disks (e.g. local/temp disk)**  
  See the section below for important details to understand when testing local/temp disks.

* **Workload Runtimes**  
  * Expected Runtime on Linux Systems
    * (64-core/vCPU VM) = 3 - 4 hours (including time required create initial 496G/GB file)

* **Usage Examples**  
  The following section provides a few basic examples of how to use the workload profile. Additional usage examples can be found in the
  'Usage Scenarios/Examples' link at the top.


  ``` bash
  // Tests the remote/managed disks by default
  VirtualClient.exe --profile=PERF-IO-DISKSPD.json --system=Azure --timeout=1440 --packageStore="{BlobConnectionString|SAS Uri}"

  VirtualClient.exe --profile=PERF-IO-DISKSPD.json --system=Azure --timeout=1440 --packageStore="{BlobConnectionString|SAS Uri}" --parameters=DiskFilter=OSDisk
  VirtualClient.exe --profile=PERF-IO-DISKSPD.json --system=Azure --timeout=1440 --packageStore="{BlobConnectionString|SAS Uri}" --parameters=DiskFilter=OSDisk:false
  VirtualClient.exe --profile=PERF-IO-DISKSPD.json --system=Azure --timeout=1440 --packageStore="{BlobConnectionString|SAS Uri}" --parameters=DiskFilter=BiggestSize

  // Test specific Windows drives/disks
  VirtualClient.exe --profile=PERF-IO-DISKSPD.json --system=Azure --timeout=1440 --packageStore="{BlobConnectionString|SAS Uri}" --parameters=DiskFilter=DiskPath:C:\,D:\

  // Test on the local/temp disk that is 32GB in total size. Override the default file size of 496G.
  // Also note that DiskSpd file sizes are different than other profiles (e.g. 26G vs. 26GB).
  VirtualClient.exe --profile=PERF-IO-DISKSPD.json --system=Azure --timeout=1440 --packageStore="{BlobConnectionString|SAS Uri}" --parameters=DiskFilter=OSDisk:false&smallestSize,,,DiskFillSize=26G,,,FileSize=26G

  // Run specific scenarios only. Each action in a profile as a 'Scenario' name.
  VirtualClient.exe --profile=PERF-IO-DISKSPD.json --system=Azure --timeout=1440 --packageStore="{BlobConnectionString|SAS Uri}" --scenarios=RandomWrite_4k_BlockSize,RandomWrite_8k_BlockSize,RandomRead_8k_BlockSize,RandomRead_4k_BlockSize
  ```

-----------------------------------------------------------------------

### Disk Testing Scenarios
The Virtual Client supports a range of different disk testing scenarios on both Azure VMs as well as Azure physical hosts. The following
documentation provides context into how to run disk performance tests for these scenarios.

* [Disk Testing Scenarios](./DiskTestingScenarios.md)

-----------------------------------------------------------------------

### Resources
* [Azure VM Sizes](https://docs.microsoft.com/en-us/azure/virtual-machines/sizes)
* [Azure Managed Disks](https://azure.microsoft.com/en-us/pricing/details/managed-disks/)