# Furmark Workload Profiles
The following profiles run customer-representative or benchmarking scenarios using the Furmark workload.  

* [Workload Details](./furmark.md)  

## PERF-GPU-FURMARK.json
Runs an IO-intensive workload using the Furmark toolset to test performance of GPU on the system. This tool currently only supports
Windows

* [Workload Profile](https://github.com/microsoft/VirtualClient/blob/main/src/VirtualClient/VirtualClient.Main/profiles/PERF-GPU-FURMARK.json) 


* **Supported Platform/Architectures**  
  * win-x64
  * win-arm64

* **Supports Disconnected Scenarios**  
  * No. Internet connection required.

* **Dependencies**  
  The dependencies defined in the 'Dependencies' section of the profile itself are required in order to run the workload operations effectively.
  * Furmark needs AMD GPU drivers that needs to be installed on the system prior to running this profile. 
  On Virtual machines you can install AMD GPU drivers from [here](https://go.microsoft.com/fwlink/?linkid=2234555)

  * VC already provides a feature of downloading and installing AMD GPU Drivers. For running Furmark we need to run two profiles in parallel.
      1. [Furmark Profile](https://github.com/microsoft/VirtualClient/blob/main/src/VirtualClient/VirtualClient.Main/profiles/PERF-GPU-FURMARK.json)
      2. [AMD GPU Driver installation profile](https://github.com/microsoft/VirtualClient/blob/main/src/VirtualClient/VirtualClient.Main/profiles/DEPENDENCY-AMD-GPU-DRIVER.json)

      Note: Check **Usage Examples** to the end of this document.
  
  **Command to install AMD GPU Driver:**
  ```
  {DriverExecutable}.exe /S /v/qn

  Parameters description
  /S - This switch stands for "silent" mode.
  /qn - This switch stands for "quiet" or "no interface" 
  /v - Verbose or display the status.
  ```


  Additional information on components that exist within the 'Dependencies' section of the profile can be found in the following locations:
  * [Installing Dependencies](https://microsoft.github.io/VirtualClient/docs/category/dependencies/)

* **Scenarios**  
  The following scenarios are covered by this workload profile. 

  * Furmmark_640X360_8
    * 640 pixels - Width of the Furmark GUI
    * 360 pixels- Height of the Furmark GUI
    * 8 - Antialiasing
  * Furmmark_2160X11520_2
    * 2160 pixels - Width of the Furmark GUI
    * 11520 pixels- Height of the Furmark GUI
    * 2 - Antialiasing
  * Furmmark_2160X11520_8
    * 2160 pixels - Width of the Furmark GUI
    * 11520 pixels- Height of the Furmark GUI
    * 8 - Antialiasing
  * Furmmark_1440X2560_2
    * 1440 pixels - Width of the Furmark GUI
    * 2560 pixels- Height of the Furmark GUI
    * 2 - Antialiasing
  

* **Profile Parameters**  
  The following parameters can be optionally supplied on the command line to modify the behaviors of the workload.

  | Parameter                 | Purpose                                                                         | Default Value |
  |---------------------------|---------------------------------------------------------------------------------|---------------|
  | Height             | Optional. Height in pixels for determining the resolution of the rendering GUI | 640 |
  | Width              | Optional. Width in pixels for determining the resolution of the rendering GUI | 480 |
  | Antialiasing       | Optional. Antialiasing is a technique used in computer graphics and image processing to reduce visual artifacts that may occur when displaying or rendering images with sharp boundaries or diagonal lines. Antialiasing smooths out these jagged edges by adding intermediate pixels that blend the colors between the edges, creating a smoother appearance.| 4 |
  | Time               | Optional. Amount of time in milliseconds for which we want to run the furmark tool and stress GPU | 60000 |

* **Profile Runtimes**  
  See the 'Metadata' section of the profile for estimated runtimes. These timings represent the length of time required to run a single round of profile 
  actions. These timings can be used to determine minimum required runtimes for the Virtual Client in order to get results. These are often estimates based on the
  number of system cores. 

* **Usage Examples**  
  The following section provides a few basic examples of how to use the workload profile. 

  ``` bash
  # Run the workload on the system
  ./VirtualClient --profile=PERF-GPU-FURMARK.json --system=Demo --timeout=1440 --packageStore="{BlobConnectionString|SAS Uri}"

  # Run Furmark for more amount of time by passing it as parameter to the command line.
  ./VirtualClient --profile=PERF-GPU-FURMARK.json --system=Demo --timeout=1440 --packageStore="{BlobConnectionString|SAS Uri}" --parameters=Time=120000

  # Run Furmark and AMD GPU Driver installation parallely for running experiments through Juno or automation purposes. 
  ./VirtualClient --profile=PERF-GPU-FURMARK.json --profile=DEPENDENCY-AMD-GPU-DRIVER.json --system=Demo --timeout=1440 --packageStore="{BlobConnectionString|SAS Uri}" --parameters=Time=120000