# DiskSpd Workload Profiles
The following profiles run customer-representative or benchmarking scenarios using the DiskSpd workload.  

* [Workload Details](./diskspd.md)  
* [Testing Specific Disks](../../guides/0220-usage-testing-disks.md)

## PERF-IO-DISKSPD.json
Runs an high stress IO-intensive workload using the DiskSpd toolset to test performance of disks on the system. This profile is a Windows-only profile. 
The profile runs the workload in-parallel on ALL disks that match the "DiskFilter" parameter (see below) by default.

Additionally this profile is designed to auto-scale to the number of cores on the system on which it runs. It uses a simple algorithm to determine 2 key
aspects of the workload execution.

* Total number of jobs/threads = {# of logical cores} / 2  

  ```
  Examples:  
  For a 16-core system:
  16/2 = 16 concurrent jobs/threads per DiskSpd execution (i.e. # threads to run I/O operations against the test file concurrently).

  For a 64-core system:
  64/2 = 32 concurrent jobs/threads per DiskSpd execution (i.e. # threads to run I/O operations against the test file concurrently).
  ```
* Total I/O depth =  512 / {Total number of jobs/threads}  

  ```
  Examples:
  For a 16 core system:
  total # jobs/threads = 16/2 (above) -> 512/16 = 32

  For a 64 core system:
  total # jobs/threads = 64/2 (above) -> 512/32 = 16
  ```

* [Workload Profile](https://github.com/microsoft/VirtualClient/blob/main/src/VirtualClient/VirtualClient.Main/profiles/PERF-IO-DISKSPD.json) 

* **Supported Platform/Architectures**
  * win-x64
  * win-arm64

* **Supported Operating Systems**
  * Windows 10
  * Windows 11
  * Windows Server 2016
  * Windows Server 2019

* **Supports Disconnected Scenarios**  
  * Yes. When the DiskSpd package is included in 'packages' directory of the Virtual Client.
    * [Installing VC Packages](../../dependencies/0001-install-vc-packages.md).

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

* **Profile Parameters**  
  The following parameters can be optionally supplied on the command line to modify the behaviors of the workload.

  | Parameter                 | Purpose                                                                         | Default Value |
  |---------------------------|---------------------------------------------------------------------------------|---------------|
  | DiskFilter                | Optional. Filter allowing the user to select the disks on which to test.<br/><br/>See the link 'Testing Specific Disks' at the top for more details. for more details.     | BiggestSize |
  | DiskFillSize              | Optional. Allows the user to override the default disk fill size used in the DiskSpd profile (e.g. 500GB -> 26GB). This enables the profile to be used in scenarios where the disk size is very small (e.g. local/temp disk -> 32GB in size). Note that this parameter is application ONLY on the Stress profile. | 500G |
  | FileSize                  | Optional. Allows the user to override the default file size used in the DiskSpd profile (e.g. 496GB -> 26GB). This enables the profile to be used in scenarios where the disk size is very small (e.g. local/temp disk -> 32GB in size). Note that this parameter is application ONLY on the Stress profile. | 496G |

* **Profile Runtimes**  
  The following timings represent the length of time required to run a single round of profile actions. These timings can be used to determine
  minimum required runtimes for the Virtual Client in order to get results. These are estimates based on the number of system cores.

  * (16-cores/vCPUs) = 4 - 5 hours (including time required create initial 496G/GB file)
  * (64-cores/vCPUs) = 3 - 4 hours (including time required create initial 496G/GB file)

* **Usage Examples**  
  The following section provides a few basic examples of how to use the workload profile. See the documentation at the top on 'Testing Specific Disks'
  for information on how to target select disks on the system.

  ``` bash
  # Run the workload on the system (default = largest disks)
  VirtualClient.exe --profile=PERF-IO-DISKSPD.json --system=Demo --timeout=1440 --packageStore="{BlobConnectionString|SAS Uri}"

  # The example above runs on the same disks as having DiskFilter=BiggestSize
  VirtualClient.exe --profile=PERF-IO-DISKSPD.json --system=Demo --timeout=1440 --packageStore="{BlobConnectionString|SAS Uri}" --parameters=DiskFilter=BiggestSize

  # Run the workload against the operating system disk
  VirtualClient.exe --profile=PERF-IO-DISKSPD.json --system=Demo --timeout=1440 --packageStore="{BlobConnectionString|SAS Uri}" --parameters=DiskFilter=OSDisk

  # Run the workload against all of the disks except the operating system disk.
  VirtualClient.exe --profile=PERF-IO-DISKSPD.json --system=Demo --timeout=1440 --packageStore="{BlobConnectionString|SAS Uri}" --parameters=DiskFilter=OSDisk:false

  # Run the workload on specific drives/disks
  VirtualClient.exe --profile=PERF-IO-DISKSPD.json --system=Demo --timeout=1440 --packageStore="{BlobConnectionString|SAS Uri}" --parameters=DiskFilter=DiskPath:C:\,D:\

  # A more advanced disk filter supplied
  VirtualClient.exe --profile=PERF-IO-DISKSPD.json --system=Demo --timeout=1440 --packageStore="{BlobConnectionString|SAS Uri}" --parameters=DiskFilter=OSDisk:false&smallestSize,,,DiskFillSize=26G,,,FileSize=26G

  # Run specific scenarios only. Each action in a profile as a 'Scenario' name.
  VirtualClient.exe --profile=PERF-IO-DISKSPD.json --system=Demo --timeout=1440 --packageStore="{BlobConnectionString|SAS Uri}" --scenarios=RandomWrite_4k_BlockSize,RandomWrite_8k_BlockSize,RandomRead_8k_BlockSize,RandomRead_4k_BlockSize
  ```