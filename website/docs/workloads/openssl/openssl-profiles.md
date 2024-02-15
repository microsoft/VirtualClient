# OpenSSL Workload Profiles
The following profiles run customer-representative or benchmarking scenarios using the OpenSSL speed workload.  

* [Workload Details](./openssl.md)  

## PERF-CPU-OPENSSL.json
Runs a CPU-intensive workload using the OpenSSL speed toolset to test the performance of the CPU in processing cryptography/encryption algorithms.
This profile is designed to identify general/broad regressions when compared against a baseline. OpenSSL is an open source industry standard
transport layer security (TLS/SSL) toolset.

:::info
*Note on Multi-Threaded Execution:  
Although the toolset can be used on Windows, the OpenSSL speed workload was designed with Unix as a foundation. Multi-threaded/parallel testing 
is not supported for Windows builds of OpenSSL 3.0.  This means that the OpenSSL speed command will not heavily exercise the CPU resources on the
system. It will use a single core/vCPU to run each test. With Linux builds, the toolset can be configured to use ALL cores/vCPUs available on the
system in-parallel.*
:::

* [Workload Profile](https://github.com/microsoft/VirtualClient/blob/main/src/VirtualClient/VirtualClient.Main/profiles/PERF-CPU-OPENSSL.json) 

* **Supported Platform/Architectures**
  * linux-x64
  * linux-arm64
  * win-x64

* **Supports Disconnected Scenarios**  
  * Yes. When the OpenSSL package is included in 'packages' directory of the Virtual Client.
    * [Installing VC Packages](../../dependencies/0001-install-vc-packages.md).

* **Dependencies**  
  The dependencies defined in the 'Dependencies' section of the profile itself are required in order to run the workload operations effectively.
  * Internet connection.

  Additional information on components that exist within the 'Dependencies' section of the profile can be found in the following locations:
  * [Installing Dependencies](https://microsoft.github.io/VirtualClient/docs/category/dependencies/)

* **Scenarios**  
  The following algorithms are covered by this workload profile.

  * MD5
  * SHA1
  * SHA256
  * SHA512
  * DES-EDE3
  * AES-128-CBC
  * AES-192-CBC
  * AES-256-CBC
  * CAMELLIA-128-CBC
  * CAMELLIA-192-CBC
  * CAMELLIA-256-CBC
  * RSA2048
  * RSA4096

* **Profile Runtimes**  
  See the 'Metadata' section of the profile for estimated runtimes. These timings represent the length of time required to run a single round of profile 
  actions. These timings can be used to determine minimum required runtimes for the Virtual Client in order to get results. These are often estimates based on the
  number of system cores. 

* **Usage Examples**  
  The following section provides a few basic examples of how to use the workload profile.

  ``` bash
  # Execute the workload profile
  ./VirtualClient --profile=PERF-CPU-OPENSSL.json --system=Demo --timeout=1440 --packageStore="{BlobConnectionString|SAS Uri}"

  # Run specific scenarios/actions in the profile.
  ./VirtualClient --profile=PERF-CPU-OPENSSL.json --system=Demo --timeout=1440 --packageStore="{BlobConnectionString|SAS Uri}" --scenarios=SHA1,SHA192,SHA256
  ```