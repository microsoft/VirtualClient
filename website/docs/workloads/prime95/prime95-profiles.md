# Prime95 Workload Profiles
The following profiles run customer-representative or benchmarking scenarios using the
 Prime95 workload.

* [Workload Details](./prime95.md)

## PERF-CPU-PRIME95.json
Runs the Prime95 workload for a specific period of time on the system. This profile is designed to allow the user to run the workload for the purpose of evaluating
the performance of the CPU over various periods of time while also allowing the user to apply a longer-term stress to the system if desired.

* [Workload Profile](https://github.com/microsoft/VirtualClient/blob/main/src/VirtualClient/VirtualClient.Main/profiles/PERF-CPU-PRIME95.json)

* **Supported Platform/Architectures**
  * linux-x64
  * win-x64

* **Supports Disconnected Scenarios**  
  * Yes. When the Prime95 package is included in 'packages' directory of the Virtual Client.
    * [Installing VC Packages](../../dependencies/0001-install-vc-packages.md).

* **Dependencies**  
  The dependencies defined in the 'Dependencies' section of the profile itself are required in order to run the workload operations effectively.
  * Internet connection.

  Additional information on components that exist within the 'Dependencies' section of the profile can be found in the following locations:
  * [Installing Dependencies](https://microsoft.github.io/VirtualClient/docs/category/dependencies/)

* **Profile Parameters**
  The following parameters can be optionally supplied on the command line to modify the behaviors of the workload.

  | Parameter | Purpose | Acceptable Range | Default Value |
  |-----------|---------|------------------|---------------|
  | TimeInMins | Time (in minutes) to run Prime95 StressTest | >0 | 60 |
  | MinTortureFFT | MinTortureFFT Size passed to Prime95. This is valid only for default FFTConfiguration (0) | 1-8192 | 4 |
  | MaxTortureFFT | MaxTortureFFT Size passed to Prime95. This is valid only for default FFTConfiguration (0) | 1-8192 | 8192 |
  | TortureHyperthreading | Switch to toggle Prime95 built-in hyperthreading option. If enabled (1), number of worker threads will be halved. | 0-1 | 1 |
  | FFTConfiguration | Sets FFT Size in certain range. 0: Custom/Default Value, 1: Smallest FFTs 4K-32K, 2: Small FFTs 32K-1024K, 3: Large FFTs 2048K-8192K | 0-3 | 0
  | ThreadCount| Limits the worker threads to specified value | 1 - Number Of Logical Cores in system | Number of logical cores in system |

* **Profile Runtimes**  
  See the 'Metadata' section of the profile for estimated runtimes. These timings represent the length of time required to run a single round of profile 
  actions. These timings can be used to determine minimum required runtimes for the Virtual Client in order to get results. These are often estimates based on the
  number of system cores. 

* **Usage Examples**
  The following section provides a few basic examples of how to use the workload profile.

  ```bash
  # Execute the workload profile
  VirtualClient.exe --profile=PERF-CPU-PRIME95.json --system=Demo --timeout=1440 --packageStore="{BlobConnectionString|SAS Uri}"

  # Override the default parameters to run the workload for a longer period of time
  VirtualClient.exe --profile=PERF-CPU-PRIME95.json --system=Demo --timeout=1440 --packageStore="{BlobConnectionString|SAS Uri}" --parameters="TimeInMins=240,,,FFTConfiguration=1"

  # Override the default parameters to change the "torture settings" when running the workload.
  VirtualClient.exe --profile=PERF-CPU-PRIME95.json --system=Demo --timeout=1440 --packageStore="{BlobConnectionString|SAS Uri}" --parameters="MinTortureFFT=1024,,,MaxTortureFFT=4096,,,TortureHyperthreading=0"
  ```
