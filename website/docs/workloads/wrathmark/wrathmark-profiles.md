# Wrathmark Workload Profiles

The following profile runs an end game simulation using the Wrathmark workload.

* [Workload Details](./wrathmark.md)

## PERF-CPU-WRATHMARK.json

Runs the Wrathmark benchmark workload to assess the performance of a CPU.

* [Workload Profile](https://github.com/microsoft/VirtualClient/blob/main/src/VirtualClient/VirtualClient.Main/profiles/PERF-CPU-WRATHMARK.json)

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
  The following parameters can be optionally supplied on the command line to modify the behaviors of the workload.

  | Parameter                 | Purpose                                                           | Default Value |
  |---------------------------|-------------------------------------------------------------------|---------------|
  | DotNetVersion             | Optional. The version of the [.NET SDK to download and install](https://dotnet.microsoft.com/en-us/download/visual-studio-sdks). | 8.0.100       |
  | TargetFramework           | Optional. The [.NET target framework](https://learn.microsoft.com/en-us/dotnet/standard/frameworks) to run (e.g. net6.0, net7.0). | net8.0        |
  | Subfolder                 | Optional. The sub folder from the [wrath-othello](https://github.com/ricomariani/wrath-othello/tree/main) repository. | wrath-sharp-std-arrays |
  | WrathmarkArgs             | Optional. The arguments to pass into wrathmark. | xlt ..\endgame.txt |

* **Profile Runtimes**  
  See the 'Metadata' section of the profile for estimated runtimes. These timings represent the length of time required to run a single round of profile 
  actions. These timings can be used to determine minimum required runtimes for the Virtual Client in order to get results. These are often estimates based on the
  number of system cores. 

* **Usage Examples**  
  The following section provides a few basic examples of how to use the workload profile.

  ``` bash
  # Execute the workload profile a single time
  VirtualClient.exe --profile=PERF-CPU-WRATHMARK.json --iterations=1

  # Override the profile default parameters to use a different .NET SDK version
  VirtualClient.exe --profile=PERF-CPU-WRATHMARK.json --parameters="TargetFramework=net6.0"
  ```
  