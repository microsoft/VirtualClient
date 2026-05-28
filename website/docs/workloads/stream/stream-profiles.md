# STREAM Workload Profiles
The following profiles run customer-representative or benchmarking scenarios using the STREAM workload.

* [Getting Started](https://microsoft.github.io/VirtualClient/)
* [Workload Details](./stream.md)  
* [Workload Profile Metrics](./stream-metrics.md)  
* [Workload Packages](https://github.com/microsoft/VirtualClient/blob/main/website/docs/developing/dependency-packages.md)

-----------------------------------------------------------------------

### Preliminaries
The profiles below require the ability to download workload packages and dependencies from a package store. In order to download the workload packages, connection 
information must be supplied on the command line. See the 'Workload Packages' documentation above for details on how that works.

-----------------------------------------------------------------------

### PERF-MEM-STREAM.json
Runs a Memory-intensive workload using the STREAM Benchmark to test the bandwidth of the Memory. This profile compiles the workload using 'gcc'.

* **OS/Architecture Platforms**
  * linux-x64
  * linux-arm64

* **Supported Operating Systems**
  * Ubuntu 18
  * Ubuntu 20
  * Ubuntu 22

* **Supported Compilers**  
  The following compilers are supported with the workload for this profile. See profile parameters and usage examples below.

  * GCC Compiler Versions = 8, 9, 10, 11

* **Dependencies**  
  The following dependencies must be met to run this workload profile.

  * Workload package must exist in the 'packages' directory or connection information for the package store supplied on the command line (see 'Workload Packages' link above).

* **Profile Parameters**  
  The following parameters can be optionally supplied on the command line to modify the behaviors of the workload. See the 'Usage Scenarios/Examples' above for examples on how to supply parameters to 
  Virtual Client profiles.

  | Parameter                 | Purpose                                                       |Default                                                                      |
  |---------------------------|---------------------------------------------------------------|-----------------------------------------------------------------------------|
  | CompilerVersion           | Not Required. Compiler's Version to install.                  |                                                                           |
  | CompilerParameters        | Not Required. Parameters use to compile the stream binary.    |-fopenmp -mcmodel=large -D_OPENMP -DNTIMES=5000 -DSTREAM_ARRAY_SIZE=100000000|

* **Component/Action Parameters**  
  The following parameters are available in the profile components/actions.

  | Parameter                 | Purpose                                                                                                                 |Default      |
  |---------------------------|-------------------------------------------------------------------------------------------------------------------------|-------------|
  | CompilerVersion           | Not Required. Compiler's Version to install.                                                                            |             |
  | CompilerParameters        | Not Required. Parameters use to compile the stream binary.                                           |-fopenmp -mcmodel=large -D_OPENMP -DNTIMES=5000 -DSTREAM_ARRAY_SIZE=100000000|
  | Toolset                   | Defines the STREAM toolset to use. Valid values include: STREAM and STREAMTriad. Note that the STREAMTriad toolset can be used on Intel CPU systems only. | STREAM |



* **Compiler Flags**  

  | Parameter                    | Purpose                                                                                                                                                |
  |------------------------------|--------------------------------------------------------------------------------------------------------------------------------------------------------|
  | -fopenmp -D_OPENMP           | Using OpenMP for multiple processors.                                                                                                                  |
  | -DNTIMES                     | Flag DNTIMES is for Stream which defines number of iterations of the workload each iteration takes around 10-50 milliseconds depending on VMSKU.       |
  | -DSTREAM_ARRAY_SIZE=100000000| Array size used by the Stream.                                                                                                                         |
  | -mcmodel=large               | It avoids integer overflow while providing array size 100000000.As it uses 64 bit integer instead of default 32 bit integer                            | 

* **Workload Runtimes**  
  The following timings represent the length of time required to run a single round of tests ran. These timings can be used to determine
  minimum required runtimes for the Virtual Client in order to get results. These are estimates based on the use of prescribed VM SKUs.
  It is practical to allow for minimum 1 to 2 hours extra runtime to ensure the tests can complete full test runs.
  * Expected Runtime  = 10 secs

* **Usage Examples**  
  The following section provides a few basic examples of how to use the workload profile. Additional usage examples can be found in the
  'Usage Scenarios/Examples' link at the top.

  <div style="font-size:10pt">

  ``` csharp
  ./VirtualClient --profile=PERF-MEM-STREAM.json --timeout=60 --packageStore="{BlobConnectionString|SAS Uri}"
  ```
  </div>

### PERF-MEM-STREAMTRIAD.json
Runs a Memory-intensive workload using the STREAM Benchmark to test memory bandwidth. This profile is designed by the Intel team to 
maximize the utilization of Intel processors.

* **Supported Platform/Architectures**
  * linux-x64

* **Supported Operating Systems**
  * Ubuntu 18
  * Ubuntu 20
  * Ubuntu 22

* **Dependencies**  
  The following dependencies must be met to run this workload profile.

  * Workload package must exist in the 'packages' directory or connection information for the package store supplied on the command line (see 'Workload Packages' link above).

* **Workload Runtimes**  
  The following timings represent the length of time required to run a single round of tests ran. These timings can be used to determine
  minimum required runtimes for the Virtual Client in order to get results. These are estimates based on the use of prescribed VM SKUs.
  It is practical to allow for minimum 1 to 2 hours extra runtime to ensure the tests can complete full test runs.
  * Expected Runtime  = 10 secs

* **Usage Examples**  
  The following section provides a few basic examples of how to use the workload profile. Additional usage examples can be found in the
  'Usage Scenarios/Examples' link at the top.

  <div style="font-size:10pt">

  ``` csharp
  ./VirtualClient --profile=PERF-MEM-STREAMTRIAD.json --timeout=60 --packageStore="{BlobConnectionString|SAS Uri}"
  ```
  </div>

### PERF-MEM-STREAMMSFT.json
Runs a Memory-intensive workload using the STREAM Benchmark to test memory bandwidth. This profile is designed by Microsoft team to maximize the performance of 1P programs.

* **Supported Platform/Architectures**
  * linux-arm64

* **Supported Operating Systems**
  * Ubuntu 18
  * Ubuntu 20
  * Ubuntu 22

* **Supported Compilers**  
  The following compilers are supported with the workload for this profile. See profile parameters and usage examples below.

  * G++ Compiler Versions = 8, 9, 10, 11

* **Dependencies**  
  The following dependencies must be met to run this workload profile.

  * Workload package must exist in the 'packages' directory or connection information for the package store supplied on the command line (see 'Workload Packages' link above).

* **Workload Runtimes**  
  The following timings represent the length of time required to run a single round of tests ran. These timings can be used to determine
  minimum required runtimes for the Virtual Client in order to get results. These are estimates based on the use of prescribed VM SKUs.
  It is practical to allow for minimum 1 to 2 hours extra runtime to ensure the tests can complete full test runs.
  * Expected Runtime  = 10 secs

* **Usage Examples**  
  The following section provides a few basic examples of how to use the workload profile. Additional usage examples can be found in the
  'Usage Scenarios/Examples' link at the top.
  
* **Profile Parameters**  
  The following parameters can be optionally supplied on the command line to modify the behaviors of the workload. See the 'Usage Scenarios/Examples' above for examples on how to supply parameters to 
  Virtual Client profiles.

  | Parameter                 | Purpose                                                       |Default                                                                      |
  |---------------------------|---------------------------------------------------------------|-----------------------------------------------------------------------------|
  | CompilerName              | Not Required. Compiler used to compile.                       |gcc                                                                          |
  | CompilerVersion           | Not Required. Compiler's Version to install.                  |10                                                                           |
  | CommandLineParameters     | Not Required. Parameters to be used in MSFT Stream.    |--internal-iter 1000 --internal-iter-lat 1000|
  | ThreadCount               | Not Required. Number of threads use to run the workload | No. of Physical Cores. |

* **Component/Action Parameters**  
  The following parameters are available in the profile components/actions.

  | Parameter                 | Purpose                                                                                                                 |Default      |
  |---------------------------|-------------------------------------------------------------------------------------------------------------------------|-------------|
  | CompilerName              | Not Required. Compiler used to compile.                                                                                 |gcc          |
  | CompilerVersion           | Not Required. Compiler's Version to install.                                                                            |10           |
  | CommandLineParameters     | Not Required. Parameters to be used in MSFT Stream.                                                                     |--internal-iter 1000 --internal-iter-lat 1000|
  | ThreadCount               | Not Required. Number of threads use to run the workload                                                                 | No. of Physical Cores. |
  | Toolset                   | Defines the STREAM toolset to use. Valid values include: STREAM , STREAMTriad and STREAMMsft. Note that the STREAMTriad toolset can be used on Intel CPU systems only. | STREAMMSFT |
Note: The default parameters are according to the parameters documentation inorder to have stable results.
[Msft Stream Parameters](./stream.md) 

* **Make file for Msft Stream with**

[MakeFile](./streammsftmakefile.txt) 


  <div style="font-size:10pt">

  ``` csharp
  ./VirtualClient --profile=PERF-MEM-STREAMMSFT.json --timeout=60 --packageStore="{BlobConnectionString|SAS Uri}"
  ```
  </div>
-----------------------------------------------------------------------

### Resources

* [Azure VM Sizes](https://docs.microsoft.com/en-us/azure/virtual-machines/sizes)
* [Azure Managed Disks](https://azure.microsoft.com/en-us/pricing/details/managed-disks/)