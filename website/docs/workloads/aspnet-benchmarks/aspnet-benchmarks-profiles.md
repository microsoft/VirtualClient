# ASP.NET Benchmarks Workload Profiles
The following profiles run customer-representative or benchmarking scenarios using ASP.NET server workloads.

* [Workload Details](./aspnet-benchmarks.md)

## PERF-WEB-ASPNET-TEJSON-WRK.json
Runs the ASP.NET TechEmpower JSON serialization benchmark using Wrk. Includes a warm-up pass before the benchmark measurement.
Supports .NET 9 and .NET 10 via the `ParametersOn` conditional parameter system.

* [Workload Profile](https://github.com/microsoft/VirtualClient/blob/main/src/VirtualClient/VirtualClient.Main/profiles/PERF-WEB-ASPNET-TEJSON-WRK.json) 

* **Supported Platform/Architectures**
  * linux-x64
  * linux-arm64
  * win-x64
  * win-arm64

* **Supports Disconnected Scenarios**  
  * No. Internet connection required.

* **Dependencies**  
  The dependencies defined in the 'Dependencies' section of the profile itself are required in order to run the workload operations effectively.
  * Internet connection.

  Additional information on components that exist within the 'Dependencies' section of the profile can be found in the following locations:
  * [Installing Dependencies](https://microsoft.github.io/VirtualClient/docs/category/dependencies/)

* **Profile Parameters**  

  | Parameter                 | Purpose                                                           | Default Value |
  |---------------------------|-------------------------------------------------------------------|---------------|
  | DotNetVersion             | The major .NET version to use. Triggers `ParametersOn` conditional resolution. | 9             |
  | TargetFramework           | The .NET target framework to run.                                 | net9.0        |
  | TestDuration              | Duration of each test run.                                        | 00:00:15      |
  | EmitLatencySpectrum       | When `true`, emits fine-grained latency spectrum data.            | false         |

* **Profile Runtimes**  
  See the 'Metadata' section of the profile for estimated runtimes.

* **Usage Examples**  

  ```bash
  # Run with .NET 9 (default)
  ./VirtualClient --profile=PERF-WEB-ASPNET-TEJSON-WRK.json --system=Demo --timeout=1440

  # Run with .NET 10
  ./VirtualClient --profile=PERF-WEB-ASPNET-TEJSON-WRK.json --system=Demo --timeout=1440 --parameters="DotNetVersion=10"
  ```

## PERF-WEB-ASPNET-TEJSON-WRK-AFFINITY.json
Runs the ASP.NET TechEmpower JSON serialization benchmark using Wrk with CPU core affinity for both server and client.
Includes a warm-up pass before the benchmark measurement. Supports .NET 9 and .NET 10.

* [Workload Profile](https://github.com/microsoft/VirtualClient/blob/main/src/VirtualClient/VirtualClient.Main/profiles/PERF-WEB-ASPNET-TEJSON-WRK-AFFINITY.json) 

* **Supported Platform/Architectures**
  * linux-x64
  * linux-arm64
  * win-x64
  * win-arm64

* **Supports Disconnected Scenarios**  
  * No. Internet connection required.

* **Dependencies**  
  The dependencies defined in the 'Dependencies' section of the profile itself are required in order to run the workload operations effectively.
  * Internet connection.

  Additional information on components that exist within the 'Dependencies' section of the profile can be found in the following locations:
  * [Installing Dependencies](https://microsoft.github.io/VirtualClient/docs/category/dependencies/)

* **Profile Parameters**  

  | Parameter                 | Purpose                                                           | Default Value |
  |---------------------------|-------------------------------------------------------------------|---------------|
  | DotNetVersion             | The major .NET version to use. Triggers `ParametersOn` conditional resolution. | 9             |
  | TargetFramework           | The .NET target framework to run.                                 | net9.0        |
  | ServerCoreAffinity        | CPU core affinity for the server process.                         | 0-7           |
  | ClientCoreAffinity        | CPU core affinity for the client process.                         | 8-15          |
  | TestDuration              | Duration of each test run.                                        | 00:00:15      |
  | EmitLatencySpectrum       | When `true`, emits fine-grained latency spectrum data.            | false         |

* **Profile Runtimes**  
  See the 'Metadata' section of the profile for estimated runtimes.

* **Usage Examples**  

  ```bash
  # Run with default affinity and .NET 9
  ./VirtualClient --profile=PERF-WEB-ASPNET-TEJSON-WRK-AFFINITY.json --system=Demo --timeout=1440

  # Run with .NET 10 and custom core affinity
  ./VirtualClient --profile=PERF-WEB-ASPNET-TEJSON-WRK-AFFINITY.json --system=Demo --timeout=1440 --parameters="DotNetVersion=10,,,ServerCoreAffinity=0-3,,,ClientCoreAffinity=4-7"
  ```

## PERF-WEB-ASPNET-ORCHARD-WRK.json
Runs the ASP.NET OrchardCore CMS benchmark using Wrk. The server runs the OrchardCore.Cms.Web application
and Wrk benchmarks the `/about` endpoint. Includes a warm-up pass before the benchmark measurement.

* [Workload Profile](https://github.com/microsoft/VirtualClient/blob/main/src/VirtualClient/VirtualClient.Main/profiles/PERF-WEB-ASPNET-ORCHARD-WRK.json) 

* **Supported Platform/Architectures**
  * linux-x64
  * linux-arm64

* **Supports Disconnected Scenarios**  
  * No. Internet connection required.

* **Dependencies**  
  The dependencies defined in the 'Dependencies' section of the profile itself are required in order to run the workload operations effectively.
  * Internet connection.

  Additional information on components that exist within the 'Dependencies' section of the profile can be found in the following locations:
  * [Installing Dependencies](https://microsoft.github.io/VirtualClient/docs/category/dependencies/)

* **Profile Parameters**  

  | Parameter                 | Purpose                                                           | Default Value |
  |---------------------------|-------------------------------------------------------------------|---------------|
  | DotNetVersion             | The version of the .NET SDK to download and install.              | 9.0.203       |
  | TargetFramework           | The .NET target framework to run.                                 | net9.0        |
  | ServerPort                | The port the OrchardCore server listens on.                       | 5014          |
  | TestDuration              | Duration of each test run.                                        | 00:00:20      |
  | EmitLatencySpectrum       | When `true`, emits fine-grained latency spectrum data.            | false         |

* **Profile Runtimes**  
  See the 'Metadata' section of the profile for estimated runtimes.

* **Usage Examples**  

  ```bash
  # Execute the workload profile
  ./VirtualClient --profile=PERF-WEB-ASPNET-ORCHARD-WRK.json --system=Demo --timeout=1440
  ```
