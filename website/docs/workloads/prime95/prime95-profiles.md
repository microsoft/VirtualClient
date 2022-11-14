# Prime95 Workload Profiles
The following profiles run customer-representative or benchmarking scenarios using the
 Prime95 workload.

* [Workload Details](./prime95.md)
* [Workload Profile Metrics](./prime95-metrics.md)


-----------------------------------------------------------------------

### Preliminaries
The profiles below require the ability to download workload packages and dependencies
from a package store. In order to download the workload packages, connection
information must be supplied on the command line. See the 'Workload Packages'
documentation above for details on how that works.

-----------------------------------------------------------------------

### PERF-CPU-PRIME95.json
Runs the Prime95 workload which runs a continuous torture/stress test on system for given time.

* **Supported Platform/Architectures**
  * linux-x64
  * win-x64

* **Dependencies**
  The following dependencies must be met to run this workload profile.

  * Workload package must exist in the 'packages' directory or connection information for the package store supplied on the command line (see 'Workload Packages' link above).

* **Profile Parameters**
  The following parameters can be optionally supplied on the command line to modify the
  behavior of the workload. See the 'Usage Scenarios/Examples' above for examples on
  how to supply parameters to Virtual Client profiles.

  | Parameter | Purpose | Acceptable Range | Default Value |
  |-----------|---------|------------------|---------------|
  | TimeInMins | Time (in minutes) to run Prime95 StressTest | >0 | 60 |
  | MinTortureFFT | MinTortureFFT Size passed to Prime95. This is valid only for default FFTConfiguration (0) | 1-8192 | 4 |
  | MaxTortureFFT | MaxTortureFFT Size passed to Prime95. This is valid only for default FFTConfiguration (0) | 1-8192 | 8192 |
  | TortureHyperthreading | Switch to toggle Prime95 built-in hyperthreading option. If enabled (1), number of worker threads will be halved. | 0-1 | 1 |
  | FFTConfiguration | Sets FFT Size in certain range. 0: Custom/Default Value, 1: Smallest FFTs 4K-32K, 2: Small FFTs 32K-1024K, 3: Large FFTs 2048K-8192K | 0-3 | 0
  | NumberOfThreads | Limits the worker threads to specified value | 1 - Number Of Logical Cores in system | Number of logical cores in system |

* **Workload Runtimes**
  The Prime95 Workload Runtime can be set as an input parameter in Profile and commandLine. The default value is 60 minutes for a single round of tests run.
  These timings can be used to determine minimum required runtimes for the Virtual Client in order to get results.

* **Usage Examples**
  The following section provides a few basic examples of how to use the workload profile. Additional usage examples can be found in the
  'Usage Scenarios/Examples' link at the top.


  ```bash
  VirtualClient.exe --profile=PERF-CPU-PRIME95.json --system=Azure --timeout=1440 --packageStore="{BlobConnectionString|SAS Uri}"
  VirtualClient.exe --profile=PERF-CPU-PRIME95.json --system=Azure --timeout=1440 --packageStore="{BlobConnectionString|SAS Uri}" --parameters="TimeInMins=120,,,MinTortureFFT=1024,,,MaxTortureFFT=4096,,,TortureHyperthreading=0"
  VirtualClient.exe --profile=PERF-CPU-PRIME95.json --system=Azure --timeout=1440 --packageStore="{BlobConnectionString|SAS Uri}" --parameters="TimeInMins=240,,,FFTConfiguration=1"

  ./VirtualClient --profile=PERF-CPU-PRIME95.json --system=Azure --timeout=1440 --packageStore="{BlobConnectionString|SAS Uri}"
  ./VirtualClient --profile=PERF-CPU-PRIME95.json --system=Azure --timeout=1440 --packageStore="{BlobConnectionString|SAS Uri}" --parameters="TimeInMins=120,,,MinTortureFFT=1024,,,MaxTortureFFT=4096,,,TortureHyperthreading=0"
  ```
