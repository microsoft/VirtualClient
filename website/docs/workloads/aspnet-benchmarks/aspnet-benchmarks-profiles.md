# ASP.NET Benchmarks Workload Profiles
The following profiles run customer-representative or benchmarking scenarios using ASP.NET server workloads.

* [Workload Details](./aspnet-benchmarks.md)
* [Client/Server Workloads](../../guides/0020-client-server.md)

## Client/Server Topology Support
ASP.NET benchmark workload profiles support running the workload on both a single system as well as in a client/server topology. This means that the workload supports
operation on a single system or on 2 distinct systems. The client/server topology is typically used when it is desirable to include a network component in the
overall performance evaluation. In a client/server topology, one system operates in the 'Client' role running the HTTP load generator (Wrk or Bombardier) while the other
system operates in the 'Server' role running the ASP.NET Kestrel application.
The Virtual Client instances running on the client and server systems will synchronize with each other before running the workload. In order to support a client/server topology,
an environment layout file MUST be supplied to each instance of the Virtual Client on the command line to describe the IP address/location of other Virtual Client instances. An
environment layout file is not required for the single system topology.

* [Environment Layouts](../../guides/0020-client-server.md)

In the environment layout file provided to the Virtual Client, define the role of the client system/VM as "Client" and the role of the server system(s)/VM(s) as "Server".
The spelling of the roles must be exact. The IP addresses of the systems/VMs must be correct as well. The following example illustrates the
idea. The name of the client must match the name of the system or the value of the agent ID passed in on the command line.

``` bash
# Single System (environment layout not required)
./VirtualClient --profile=PERF-WEB-ASPNET-WRK.json --system=Demo --timeout=1440

# Multi-System
# On Client Role System...
./VirtualClient --profile=PERF-WEB-ASPNET-WRK.json --system=Demo --timeout=1440 --client-id=Client01 --layout=/any/path/to/layout.json

# On Server Role System...
./VirtualClient --profile=PERF-WEB-ASPNET-WRK.json --system=Demo --timeout=1440 --client-id=Server01 --layout=/any/path/to/layout.json

# Example contents of the 'layout.json' file:
{
    "clients": [
        {
            "name": "Client01",
            "role": "Client",
            "ipAddress": "10.1.0.1"
        },
        {
            "name": "Server01",
            "role": "Server",
            "ipAddress": "10.1.0.2"
        }
    ]
}
```

## PERF-WEB-ASPNET-WRK.json
Runs the ASP.NET TechEmpower JSON serialization benchmark using Wrk. Includes a warm-up pass before the benchmark measurement.
Supports .NET 9 and .NET 10 via the `ParametersOn` conditional parameter system.

* [Workload Profile](https://github.com/microsoft/VirtualClient/blob/main/src/VirtualClient/VirtualClient.Main/profiles/PERF-WEB-ASPNET-WRK.json) 

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
  * The IP addresses defined in the environment layout (see above) for the Client and Server systems must be correct.
  * The name of the Client and Server instances defined in the environment layout must match the agent/client IDs supplied on the command line (e.g. --client-id)
    or must match the name of the system as defined by the operating system itself.

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
  # When running on a single system (environment layout not required)
  ./VirtualClient --profile=PERF-WEB-ASPNET-WRK.json --system=Demo --timeout=1440

  # Run with .NET 10
  ./VirtualClient --profile=PERF-WEB-ASPNET-WRK.json --system=Demo --timeout=1440 --parameters="DotNetVersion=10"

  # When running in a client/server environment
  ./VirtualClient --profile=PERF-WEB-ASPNET-WRK.json --system=Demo --timeout=1440 --client-id=Client01 --layout="/any/path/to/layout.json"
  ./VirtualClient --profile=PERF-WEB-ASPNET-WRK.json --system=Demo --timeout=1440 --client-id=Server01 --layout="/any/path/to/layout.json"
  ```

## PERF-WEB-ASPNET-WRK-AFFINITY.json
Runs the ASP.NET TechEmpower JSON serialization benchmark using Wrk with CPU core affinity for both server and client.
Includes a warm-up pass before the benchmark measurement. Supports .NET 9 and .NET 10.

* [Workload Profile](https://github.com/microsoft/VirtualClient/blob/main/src/VirtualClient/VirtualClient.Main/profiles/PERF-WEB-ASPNET-WRK-AFFINITY.json) 

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
  * The IP addresses defined in the environment layout (see above) for the Client and Server systems must be correct.
  * The name of the Client and Server instances defined in the environment layout must match the agent/client IDs supplied on the command line (e.g. --client-id)
    or must match the name of the system as defined by the operating system itself.

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
  # When running on a single system (environment layout not required)
  ./VirtualClient --profile=PERF-WEB-ASPNET-WRK-AFFINITY.json --system=Demo --timeout=1440

  # Run with .NET 10 and custom core affinity
  ./VirtualClient --profile=PERF-WEB-ASPNET-WRK-AFFINITY.json --system=Demo --timeout=1440 --parameters="DotNetVersion=10,,,ServerCoreAffinity=0-3,,,ClientCoreAffinity=4-7"

  # When running in a client/server environment
  ./VirtualClient --profile=PERF-WEB-ASPNET-WRK-AFFINITY.json --system=Demo --timeout=1440 --client-id=Client01 --layout="/any/path/to/layout.json"
  ./VirtualClient --profile=PERF-WEB-ASPNET-WRK-AFFINITY.json --system=Demo --timeout=1440 --client-id=Server01 --layout="/any/path/to/layout.json"
  ```

## PERF-WEB-ASPNET-BOMBARDIER.json
Runs the ASP.NET TechEmpower JSON serialization benchmark using Bombardier as the HTTP client
with 256 concurrent connections. Uses .NET 8 SDK.

* [Workload Profile](https://github.com/microsoft/VirtualClient/blob/main/src/VirtualClient/VirtualClient.Main/profiles/PERF-WEB-ASPNET-BOMBARDIER.json) 

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
  * The IP addresses defined in the environment layout (see above) for the Client and Server systems must be correct.
  * The name of the Client and Server instances defined in the environment layout must match the agent/client IDs supplied on the command line (e.g. --client-id)
    or must match the name of the system as defined by the operating system itself.

  Additional information on components that exist within the 'Dependencies' section of the profile can be found in the following locations:
  * [Installing Dependencies](https://microsoft.github.io/VirtualClient/docs/category/dependencies/)

* **Profile Parameters**  

  | Parameter                 | Purpose                                                           | Default Value |
  |---------------------------|-------------------------------------------------------------------|---------------|
  | DotNetVersion             | The version of the .NET SDK to download and install.              | 8.0.204       |
  | TargetFramework           | The .NET target framework to run.                                 | net8.0        |
  | ServerPort                | The port the Kestrel server listens on.                           | 9876          |

* **Profile Runtimes**  
  See the 'Metadata' section of the profile for estimated runtimes.

* **Usage Examples**  

  ```bash
  # When running on a single system (environment layout not required)
  ./VirtualClient --profile=PERF-WEB-ASPNET-BOMBARDIER.json --system=Demo --timeout=1440

  # When running in a client/server environment
  ./VirtualClient --profile=PERF-WEB-ASPNET-BOMBARDIER.json --system=Demo --timeout=1440 --client-id=Client01 --layout="/any/path/to/layout.json"
  ./VirtualClient --profile=PERF-WEB-ASPNET-BOMBARDIER.json --system=Demo --timeout=1440 --client-id=Server01 --layout="/any/path/to/layout.json"
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
  * The IP addresses defined in the environment layout (see above) for the Client and Server systems must be correct.
  * The name of the Client and Server instances defined in the environment layout must match the agent/client IDs supplied on the command line (e.g. --client-id)
    or must match the name of the system as defined by the operating system itself.

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
  # When running on a single system (environment layout not required)
  ./VirtualClient --profile=PERF-WEB-ASPNET-ORCHARD-WRK.json --system=Demo --timeout=1440

  # When running in a client/server environment
  ./VirtualClient --profile=PERF-WEB-ASPNET-ORCHARD-WRK.json --system=Demo --timeout=1440 --client-id=Client01 --layout="/any/path/to/layout.json"
  ./VirtualClient --profile=PERF-WEB-ASPNET-ORCHARD-WRK.json --system=Demo --timeout=1440 --client-id=Server01 --layout="/any/path/to/layout.json"
  ```
