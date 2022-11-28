# SPEC Power Workload Profiles
The following profiles run customer-representative or benchmarking scenarios using the SPEC Power workload.

* [Workload Details](./SPECpower.md)  
* [Workload Profile Metrics](./SPECpowerMetrics.md)  
* [Workload Packages](./DependencyPackages.md)
* [Usage Scenarios/Examples](./UsageScenarios.md)

-----------------------------------------------------------------------

### Preliminaries
The profiles below require the ability to download workload packages and dependencies from a package store. In order to download the workload packages, connection information 
must be supplied on the command line. See the 'Workload Packages' documentation above for details on how that works.

-----------------------------------------------------------------------

### POWER-SPEC30.json
Runs the SPEC Power benchmark workload on the system targeting 30% system resource usage. This workload is an industry standard toolset for evaluating the power
consumption/draw on a system. Each of the different profiles is designed to use a specific percentage of the resources on the 
system in a steady-state usage pattern.

* **Supported Platform/Architectures**
  * linux-x64
  * linux-arm64
  * win-x64
  * win-arm64

* **Supported Operating Systems**
  * Ubuntu 18
  * Ubuntu 20
  * Ubuntu 22
  * Windows 10
  * Windows 11
  * Windows Server 2016
  * Windows Server 2019

* **Dependencies**  
  The following dependencies must be met to run this workload profile.

  * Workload package must exist in the 'packages' directory or connection information for the package store supplied on the command line (see 'Workload Packages' link above).
  * The system on which the workload is running is expected to have physical sensors on the system board in order to capture actual temperature and power readings. For example,
    Azure hosts/blades have sensors built-in to the physical hardware. When this workload runs in a virtual machine on the node/blade, the readings must be captured
    on the blade itself. The CRC flighting system runs an agent on the Azure host that handles the capture of this information. The Virtual Client itself
    is used on the Azure host to capture the temperature and power metrics (using the IPMIUtil toolset).


* **Workload Runtimes**  
  The following timings represent the length of time required to run a single round of tests ran. These timings can be used to determine
  minimum required runtimes for the Virtual Client in order to get results. These are estimates based on the use of prescribed VM SKUs.

  * Expected Runtime (not dependent on VM SKU) = 2 - 3 hours

* **Usage Examples**  
  The following section provides a few basic examples of how to use the workload profile. Additional usage examples can be found in the
  'Usage Scenarios/Examples' link at the top.

  <div style="font-size:10pt">

  ``` csharp
  VirtualClient.exe --profile=POWER-SPEC50.json --system=Azure --timeout=1440 --packageStore="{BlobConnectionString|SAS Uri}"
  ```
  </div>

-----------------------------------------------------------------------

### POWER-SPEC70.json
Runs the SPEC Power benchmark workload on the system targeting 70% system resource usage. This workload is an industry standard toolset for evaluating the power
consumption/draw on a system. Each of the different profiles is designed to use a specific percentage of the resources on the 
system in a steady-state usage pattern.

* **OS/Architecture Platforms**
  * linux-x64
  * linux-arm64
  * win-x64
  * win-arm64

  **Supported Operating Systems**
  * Ubuntu 18
  * Ubuntu 20
  * Ubuntu 22
  * Windows 10
  * Windows 11
  * Windows Server 2016
  * Windows Server 2019

* **Dependencies**  
  The following dependencies must be met to run this workload profile.

  * Workload package must exist in the 'packages' directory or connection information for the package store supplied on the command line (see 'Workload Packages' link above).
  * The system on which the workload is running is expected to have physical sensors on the system board in order to capture actual temperature and power readings. For example,
    Azure hosts/blades have sensors built-in to the physical hardware. When this workload runs in a virtual machine on the node/blade, the readings must be captured
    on the blade itself. The CRC flighting system runs an agent on the Azure host that handles the capture of this information. The Virtual Client itself
    is used on the Azure host to capture the temperature and power metrics (using the IPMIUtil toolset).


* **Workload Runtimes**  
  The following timings represent the length of time required to run a single round of tests ran. These timings can be used to determine
  minimum required runtimes for the Virtual Client in order to get results. These are estimates based on the use of prescribed VM SKUs.

  * Expected Runtime (not dependent on VM SKU) = 2 - 3 hours


-----------------------------------------------------------------------

### Resources
* [Azure VM Sizes](https://docs.microsoft.com/en-us/azure/virtual-machines/sizes)
* [Azure Managed Disks](https://azure.microsoft.com/en-us/pricing/details/managed-disks/)