# DiskSpd Workload Profiles
The following profiles run customer-representative or benchmarking scenarios using the DiskSpd workload.  

* [Workload Details](./diskspd.md)  
* [Testing Disks](../../guides/0220-usage-testing-disks.md)

## PERF-IO-DISKSPD.json
Runs a high stress IO-intensive workload using the DiskSpd toolset to test performance of disks on the system. This profile is a Windows-only profile. 
The profile runs the workload in-parallel on ALL disks that match the "DiskFilter" parameter (see below) by default.

Additionally this profile is designed to auto-scale to the number of cores on the system on which it runs. It uses a simple algorithm to determine 2 key
aspects of the workload execution.

* Total number of jobs/threads = \<# of logical cores> / 2  

  ```
  Examples:  
  For a 16-core system:
  16/2 = 16 concurrent jobs/threads per DiskSpd execution (i.e. # threads to run I/O operations against the test file concurrently).

  For a 64-core system:
  64/2 = 32 concurrent jobs/threads per DiskSpd execution (i.e. # threads to run I/O operations against the test file concurrently).
  ```
* Total I/O depth =  512 / \<Total number of jobs / threads> 

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

* **Profile Parameters**  
  The following parameters can be optionally supplied on the command line to modify the behaviors of the workload.

  | Parameter                 | Purpose                                                                         | Default Value |
  |---------------------------|---------------------------------------------------------------------------------|---------------|
  | DiskFilter                | Optional. Filter allowing the user to select the disks on which to test.<br/><br/>See the link 'Testing Disks' at the top for more details. | BiggestSize |
  | DiskFillSize              | Optional. Allows the user to override the default disk fill size used in the FIO profile (e.g. 500GB -> 26GB). This enables the profile to be used in scenarios where the disk size is very small (e.g. local/temp disk -> 32GB in size). | 500GB |
  | Duration                  | Optional. Defines the amount of time to run each FIO scenario/action within the profile. | 5 minutes |
  | FileSize                  | Optional. Allows the user to override the default file size used in the FIO profile (e.g. 496GB -> 26GB). This enables the profile to be used in scenarios where the disk size is very small (e.g. local/temp disk -> 32GB in size). | 496GB |
  | ProcessModel              | Optional. Defines how the FIO processes will be executed. The following are valid process models:<br/><br/><b>SingleProcess</b><br/>Executes a single FIO process running 1 job targeting I/O operations against each disk. Results are separated per-disk.<br/><br/><b>SingleProcessPerDisk</b><br/>Executes a single FIO process for each disk with each process running 1 job targeting I/O operations against that disk (higher stress profile). Results are separated per-disk.<br/><br/><b>SingleProcessAggregated</b><br/>Executes a single FIO process running 1 job per disk targeting I/O operations against that disk. Results are provided as an aggregation across all disks (i.e. a rollup). | SingleProcess |
  | QueueDepth                | Optional. Defines the I/O queue depth to use for the operations | 512/the thread count |
  | ThreadCount               | Optional. Specifies the number of distinct parallel operations/threads to run per job. | # logical processors / 2 |

  * **Profile Component Parameters**  
  The following section describes the parameters used by the individual components in the profile.

  | Parameter                 | Purpose                                                                         | Accepted Values |
  |---------------------------|-------------------------------------------------------------------------------|-----------------|
  | Scenario                  | Scenario used to define the given action of profile. This can be used to specify exact actions to run or exclude from the profile.  | Any string |
  | MetricsScenario           | The name to use as the "scenario" for all metrics output for the particular profile action. | |
  | CommandLine               | The command line parameters for FIO tool set. |     Any Valid FIO arguments            |
  | DiskFilter                | Filter allowing the user to select the disks on which to test. | See the link 'Testing Disks' at the top for more details. |
  | Duration                  | Defines the amount of time to run each FIO scenario/action within the profile. | integer or time span |
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
  VirtualClient.exe --profile=PERF-IO-DISKSPD.json --system=Demo --timeout=1440

  # The example above runs on the same disks as having DiskFilter=BiggestSize
  VirtualClient.exe --profile=PERF-IO-DISKSPD.json --system=Demo --timeout=1440 --parameters=DiskFilter=BiggestSize

  # Run the workload against the operating system disk
  VirtualClient.exe --profile=PERF-IO-DISKSPD.json --system=Demo --timeout=1440 --parameters=DiskFilter=OSDisk

  # Run the workload against all of the disks except the operating system disk.
  VirtualClient.exe --profile=PERF-IO-DISKSPD.json --system=Demo --timeout=1440 --parameters=DiskFilter=OSDisk:false

  # Run the workload on specific drives/disks
  VirtualClient.exe --profile=PERF-IO-DISKSPD.json --system=Demo --timeout=1440 --parameters=DiskFilter=DiskPath:C:\,D:\

  # A more advanced disk filter supplied
  VirtualClient.exe --profile=PERF-IO-DISKSPD.json --system=Demo --timeout=1440 --parameters=DiskFilter=OSDisk:false&smallestSize,,,DiskFillSize=26G,,,FileSize=26G

  # Run specific scenarios only. Each action in a profile as a 'Scenario' name.
  VirtualClient.exe --profile=PERF-IO-DISKSPD.json --system=Demo --timeout=1440 --scenarios=RandomWrite_4k_BlockSize,RandomWrite_8k_BlockSize,RandomRead_8k_BlockSize,RandomRead_4k_BlockSize
  ```

## PERF-IO-DISKSPD-RAWDISK.json
Runs a read I/O workload using the DiskSpd toolset targeting raw physical HDD disks directly (no filesystem). This profile is a Windows-only profile  
designed for bare-metal or JBOD scenarios where disks are not formatted or mounted. It targets disks at the raw block level using DiskSpd's native `#N`  
physical disk index syntax, bypassing the Windows volume manager entirely.

The profile auto-discovers HDD disks at runtime using `Get-PhysicalDisk | Where-Object { $_.MediaType -eq 'HDD' }`, which correctly enumerates offline  
JBOD drives that DiskPart/DiskManager cannot see. An explicit `RawDiskIndexRange` parameter can be supplied to override auto-discovery. One DiskSpd  
process is launched per discovered disk (`ProcessModel=SingleProcessPerDisk`).

* [Workload Profile](https://github.com/microsoft/VirtualClient/blob/main/src/VirtualClient/VirtualClient.Main/profiles/PERF-IO-DISKSPD-RAWDISK.json)

* **Supported Platform/Architectures**
  * win-x64
  * win-arm64

* **Supported Operating Systems**
  * Windows 10 / Windows 11
  * Windows Server 2016 / 2019 / 2022

* **Supports Disconnected Scenarios**  
  * Yes. When the DiskSpd package is included in the 'packages' directory.

* **Dependencies**
  * Internet connection (for downloading the DiskSpd package on first run).
  * Physical HDD disks present on the system (auto-discovered via `Get-PhysicalDisk`).

* **Scenarios**
  * Random Read Operations
    * 128k block size, queue depth 32, 1 thread per disk (`RandomRead_128k_BlockSize`)

* **Profile Parameters**  
  The following parameters can be optionally supplied on the command line to modify the behaviors of the workload.

  | Parameter          | Purpose | Default Value |
  |--------------------|---------|---------------|
  | CommandLine        | Optional. The DiskSpd command line arguments template. Supports `{Duration.TotalSeconds}` substitution. | `-b128K -d{Duration.TotalSeconds} -o32 -t1 -r -w0 -Sh -L -Rtext` |
  | Duration           | Optional. The duration of each DiskSpd scenario/action. | 1 minute |
  | ProcessModel       | Optional. Defines how DiskSpd processes are distributed across disks. `SingleProcessPerDisk` runs one process per raw disk. | SingleProcessPerDisk |
  | RawDiskIndexRange  | Optional. Overrides auto-discovery when provided. Accepted forms: a hyphen range (e.g. `6-180`), a single index (e.g. `36`), or a comma-separated list (e.g. `37,38,39,40` — see note below). When empty or omitted, HDD disks are auto-discovered via `Get-PhysicalDisk`. | (auto-discovered HDD disks) |

  > **Note on comma-separated lists:** The VC CLI uses `",,,"`  as the delimiter between multiple `--parameters` values. A comma-separated `RawDiskIndexRange` (e.g. `35,38`) must therefore be followed by `,,,` so the parser treats it as a single value rather than splitting on the commas. The simplest way is to append a trailing `",,,"`  (e.g. `"RawDiskIndexRange=35,38,,,"`) or include another parameter (e.g. `"RawDiskIndexRange=37,38,39,40,,,Duration=00:00:30"`). Contiguous ranges via hyphen syntax (e.g. `30-40`) have no such requirement.

* **Usage Examples**  
  The following section provides a few basic examples of how to use the workload profile.

  ``` bash
  # Run the workload — HDD disks are auto-discovered via Get-PhysicalDisk (MediaType=HDD)
  VirtualClient.exe --profile=PERF-IO-DISKSPD-RAWDISK.json --system=Demo --timeout=1440

  # Auto-discover disks with a custom duration (30 seconds per scenario)
  VirtualClient.exe --profile=PERF-IO-DISKSPD-RAWDISK.json --system=Demo --timeout=1440 --parameters="Duration=00:00:30"

  # Target a single disk by index
  VirtualClient.exe --profile=PERF-IO-DISKSPD-RAWDISK.json --system=Demo --timeout=1440 --parameters="RawDiskIndexRange=36,,,Duration=00:00:30"

  # Target a contiguous range of disks using hyphen syntax (disks 30 through 31)
  VirtualClient.exe --profile=PERF-IO-DISKSPD-RAWDISK.json --system=Demo --timeout=1440 --parameters="RawDiskIndexRange=30-31,,,Duration=00:00:30"

  # Target a non-contiguous set of disks using a comma-separated list
  # Append a trailing ",,," so the parser treats the value as a single token
  VirtualClient.exe --profile=PERF-IO-DISKSPD-RAWDISK.json --system=Demo --timeout=1440 --parameters="RawDiskIndexRange=35,38,,,"

  # Comma-separated list combined with another parameter (trailing ",,," not needed in this case)
  VirtualClient.exe --profile=PERF-IO-DISKSPD-RAWDISK.json --system=Demo --timeout=1440 --parameters="RawDiskIndexRange=37,38,39,40,,,Duration=00:00:30"

  # Override the command line (e.g. change block size to 64K)
  VirtualClient.exe --profile=PERF-IO-DISKSPD-RAWDISK.json --system=Demo --timeout=1440 --parameters="CommandLine=-b64K -d{Duration.TotalSeconds} -o32 -t1 -r -w0 -Sh -L -Rtext,,,Duration=00:00:30"
  ```