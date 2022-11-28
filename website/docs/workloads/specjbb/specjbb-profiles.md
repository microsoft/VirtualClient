# SPECjbb Workload Profiles
The following profiles run customer-representative or benchmarking scenarios using the SPECjbb workload.

* [Workload Details](./SPECjbb.md)  
* [Workload Profile Metrics](./SPECjbbMetrics.md)  
* [Workload Packages](./DependencyPackages.md)
* [Usage Scenarios/Examples](./UsageScenarios.md)

-----------------------------------------------------------------------

### Preliminaries
The profiles below require the ability to download workload packages and dependencies from a package store. In order to download the workload packages, connection information 
must be supplied on the command line. See the 'Workload Packages' documentation above for details on how that works.

-----------------------------------------------------------------------

### PERF-SPECJBB.json
Runs the SPECjbb benchmark workload to assess the performance of a Java Server.

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

* **Recommended Configurations**
  * Operating System (unless otherwise specified below)
    * Linux Scenarios
       * Publisher: Canonical
       * Offer: UbuntuServer
       * Sku: 18.04-LTS
       * Version: latest
    * Windows Scenarios
       * Publisher: MicrosoftWindowsServer
       * Offer: WindowsServer
       * Sku: 2019-Datacenter
       * Version: latest 
       <br/><br/>
  * Minimum of 16GB RAM on system<br/><br/>
  * AMD Gen6 (Naples) Hardware
    * Virtual Machines (per node)
      * Firmware/Hardware Validations = 1 x 64-core -> Standard_L64s_v2
      * Test/QoS = 1 x 16-core -> Standard_L16s_v2
      <br/><br/>
  * AMD Gen7 (Rome) Hardware
    * Virtual Machines (per node)
      * Firmware/Hardware Validations (ideal) = 1 x 72-core -> Standard_D72as_v4, Standard_E72as_v4
      * Firmware/Hardware Validations = 1 x 64-core -> Standard_D64as_v4, Standard_E64as_v4
      * Test/QoS = 1 x 16-core -> Standard_D16as_v4, Standard_E16as_v4
      <br/><br/>
  * AMD Gen8 (Milan) Hardware
    * Virtual Machines (per node)
      * Firmware/Hardware Validations (ideal) = 1 x 96-core -> Standard_D96as_v4/v5, Standard_E96as_v4/v5
      * Firmware/Hardware Validations = 1 x 64-core -> Standard_D64as_v4/v5, Standard_E64as_v4/v5
      * Test/QoS = 1 x 16-core -> Standard_D16as_v4/v5, Standard_E16as_v4/v5
      <br/><br/>
  * Intel Gen5 (Broadwell) Hardware
    * Virtual Machines (per node)
      * Firmware/Hardware Validations = 1 x 48-core -> Standard_D48s_v3, Standard_E48s_v3, Standard_F48s_v2
      * Test/QoS = 1 x 16-core -> Standard_D16s_v3, Standard_E16s_v3, Standard_F16s
      <br/><br/>
  * Intel Gen6 (Coffee Lake - 6 core) Hardware
    * Virtual Machines (per node)
      * Firmware/Hardware Validations = 1 x 4-core -> Standard_DC4s
      * Test/QoS = 1 x 4-core -> Standard_DC4s
      <br/><br/>
  * Intel Gen6 (Coffee Lake - 8 core) Hardware
    * Virtual Machines (per node)
      * Firmware/Hardware Validations = 1 x 8-core -> Standard_DC8s_v2
      * Test/QoS = 1 x 8-core -> Standard_DC8s_v2
      <br/><br/>
  * Intel Gen6 (Skylake) Hardware
    * Virtual Machines (per node)
      * Firmware/Hardware Validations = 1 x 64-core -> Standard_D64s_v3, Standard_E64s_v3, Standard_F64s_v2
      * Test/QoS = 1 x 16-core -> Standard_D16s_v3, Standard_E16s_v3, Standard_F16s_v2
      <br/><br/>
  * Intel Gen7 (Cascade Lake) Hardware
    * Virtual Machines (per node)
      * Firmware/Hardware Validations (ideal) = 1 x 72-core -> Standard_D72s_v5, Standard_E72s_v5, Standard_F72s_v2
      * Firmware/Hardware Validations = 1 x 64-core -> Standard_D64s_v3/v4, Standard_E64s_v3/v4, Standard_F64s_v2
      * Test/QoS = 1 x 16-core -> Standard_D16s_v3/v4, Standard_E16s_v3/v4, Standard_F16s_v2
      <br/><br/>
  * Intel Gen8 (Icelake) Hardware
    * Virtual Machines (per node)
      * Firmware/Hardware Validations (ideal) = 1 x 96-core -> Standard_D96s_v5, Standard_E96s_v5, Standard_F96s_v2
      * Firmware/Hardware Validations = 1 x 64-core -> Standard_D64s_v4/v5, Standard_E64s_v4/v5, Standard_F64s_v2
      * Test/QoS = 1 x 16-core -> Standard_D16s_v4/v5, Standard_E16s_v4/v5, Standard_F16s_v2

* **Workload Runtimes**  
  The following timings represent the length of time required to run a single round of tests ran. These timings can be used to determine
  minimum required runtimes for the Virtual Client in order to get results. These are estimates based on the use of prescribed VM SKUs. This
  particular workload takes multiple days to complete the number of iterations required for valid results.

  * Expected Runtime (8-core/vCPU VM) = 3 hours

* **Usage Examples**  
  The following section provides a few basic examples of how to use the workload profile. Additional usage examples can be found in the
  'Usage Scenarios/Examples' link at the top.

  <div style="font-size:10pt">

  ``` csharp
  VirtualClient.exe --profile=PERF-SPECJBB.json --system=Azure --timeout=1440 --packageStore="{BlobConnectionString|SAS Uri}"
  ```
  </div>

-----------------------------------------------------------------------

### Resources
* [Azure VM Sizes](https://docs.microsoft.com/en-us/azure/virtual-machines/sizes)
* [Azure Managed Disks](https://azure.microsoft.com/en-us/pricing/details/managed-disks/)