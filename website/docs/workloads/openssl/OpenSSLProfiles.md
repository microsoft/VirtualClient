# OpenSSL Workload Profiles
The following profiles run customer-representative or benchmarking scenarios using the OpenSSL speed workload.  

* [Workload Details](./OpenSSL.md)  
* [Workload Profile Metrics](./OpenSSLMetrics.md)  


-----------------------------------------------------------------------

### Preliminaries
The profiles below require the ability to download workload packages and dependencies from a package store. In order to download the workload packages, connection information 
must be supplied on the command line. See the 'Workload Packages' documentation above for details on how that works.

-----------------------------------------------------------------------

### PERF-CPU-OPENSSL.json
Runs a CPU-intensive workload using the OpenSSL speed toolset to test the performance of the CPU in processing cryptography/encryption algorithms.
This profile is designed to identify general/broad regressions when compared against a baseline. OpenSSL is an open source industry standard
transport layer security (TLS/SSL) toolset.

* **Supported Platform/Architectures**
  * linux-x64
  * linux-arm64
  * win-x64

* **Supported Operating Systems**
  * Ubuntu 18
  * Ubuntu 20
  * Ubuntu 22
  * Windows 10
  * Windows 11
  * Windows Server 2016
  * Windows Server 2019

Note on Multi-Threaded Execution
  Although the toolset can be used on Windows, the OpenSSL speed workload was designed with Unix as a foundation. Multi-threaded/parallel testing 
  is not supported for Windows builds of OpenSSL 3.0.  This means that the OpenSSL speed command will not heavily exercise the CPU resources on the
  system. It will use a single core/vCPU to run each test. With Linux builds, the toolset can be configured to use ALL cores/vCPUs available on the
  system in-parallel.


* **Dependencies**  
  The following dependencies must be met to run this workload profile.

  * Workload package must exist in the 'packages' directory or connection information for the package store supplied on the command line (see 'Workload Packages' link above).

* **Scenarios**  
  The following scenarios are covered by this workload profile.

  * MD5 algorithm
  * SHA1 algorithm
  * SHA256 algorithm
  * SHA512 algorithm
  * DES-EDE3 algorithm
  * AES-128-CBC algorithm
  * AES-192-CBC algorithm
  * AES-256-CBC algorithm
  * CAMELLIA-128-CBC algorithm
  * CAMELLIA-192-CBC algorithm
  * CAMELLIA-256-CBC algorithm


* **Workload Runtimes**  
  The following timings represent the length of time required to run a single round of tests ran. These timings can be used to determine
  minimum required runtimes for the Virtual Client in order to get results. These are estimates based on the use of prescribed VM SKUs.
  It is practical to allow for 1 to 2 hours extra runtime to ensure the tests can complete full test runs.

  * Expected Runtime (2-core/vCPU VM) = 2 hours

* **Usage Examples**  
  The following section provides a few basic examples of how to use the workload profile. Additional usage examples can be found in the
  'Usage Scenarios/Examples' link at the top.


  ``` csharp
  ./VirtualClient --profile=PERF-CPU-OPENSSL.json --system=Azure --timeout=1440 --packageStore="{BlobConnectionString|SAS Uri}"

  // Certain scenarios only
  ./VirtualClient --profile=PERF-CPU-OPENSSL.json --system=Azure --timeout=1440 --packageStore="{BlobConnectionString|SAS Uri}" --scenarios=SHA1,SHA192,SHA256
  ```

-----------------------------------------------------------------------

### Resources
* [Azure VM Sizes](https://docs.microsoft.com/en-us/azure/virtual-machines/sizes)
* [Azure Managed Disks](https://azure.microsoft.com/en-us/pricing/details/managed-disks/)