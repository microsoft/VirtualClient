# SPECpower Workload Profiles
The following profiles run customer-representative or benchmarking scenarios using the SPEC Power workload.

* [Workload Details](./specpower.md)  

## POWER-SPEC30.json
Runs the SPEC Power benchmark workload on the system targeting 30% system resource usage. This workload is an industry standard toolset for evaluating the power
consumption/draw on a system. Each of the different profiles is designed to use a specific percentage of the resources on the 
system in a steady-state usage pattern.

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
  * The system on which the workload is running is expected to have physical sensors on the system board in order to capture actual temperature and power readings. For example,
    Azure hosts/blades have sensors built-in to the physical hardware. When this workload runs in a virtual machine on the node/blade, the readings must be captured
    on the blade itself. The CRC flighting system runs an agent on the Azure host that handles the capture of this information. The Virtual Client itself
    is used on the Azure host to capture the temperature and power metrics (using the IPMIUtil toolset).

  Additional information on components that exist within the 'Dependencies' section of the profile can be found in the following locations:
  * [Installing Dependencies](https://microsoft.github.io/VirtualClient/docs/category/dependencies/)

* **Profile Runtimes**  
  See the 'Metadata' section of the profile for estimated runtimes. These timings represent the length of time required to run a single round of profile 
  actions. These timings can be used to determine minimum required runtimes for the Virtual Client in order to get results. These are often estimates based on the
  number of system cores. 

* **Usage Examples**  
  The following section provides a few basic examples of how to use the workload profile.

  ``` bash
  # Execute the workload profile
  VirtualClient.exe --profile=POWER-SPEC30.json --system=Juno --timeout=1440 --packageStore="{BlobConnectionString|SAS Uri}"
  ```

## POWER-SPEC50.json
Runs the SPEC Power benchmark workload on the system targeting 50% system resource usage. This workload is an industry standard toolset for evaluating the power
consumption/draw on a system. Each of the different profiles is designed to use a specific percentage of the resources on the 
system in a steady-state usage pattern.

* **OS/Architecture Platforms**
  * linux-x64
  * linux-arm64
  * win-x64
  * win-arm64

* **Supports Disconnected Scenarios**  
  * No. Internet connection required.

* **Dependencies**  
  The dependencies defined in the 'Dependencies' section of the profile itself are required in order to run the workload operations effectively.
  * Internet connection.
  * The system on which the workload is running is expected to have physical sensors on the system board in order to capture actual temperature and power readings. For example,
    Azure hosts/blades have sensors built-in to the physical hardware. When this workload runs in a virtual machine on the node/blade, the readings must be captured
    on the blade itself. The CRC flighting system runs an agent on the Azure host that handles the capture of this information. The Virtual Client itself
    is used on the Azure host to capture the temperature and power metrics (using the IPMIUtil toolset).

  Additional information on components that exist within the 'Dependencies' section of the profile can be found in the following locations:
  * [Installing Dependencies](https://microsoft.github.io/VirtualClient/docs/category/dependencies/)

* **Profile Runtimes**  
  See the 'Metadata' section of the profile for estimated runtimes. These timings represent the length of time required to run a single round of profile 
  actions. These timings can be used to determine minimum required runtimes for the Virtual Client in order to get results. These are often estimates based on the
  number of system cores. 

* **Usage Examples**  
  The following section provides a few basic examples of how to use the workload profile.

  ``` bash
  # Execute the workload profile
  VirtualClient.exe --profile=POWER-SPEC50.json --system=Juno --timeout=1440 --packageStore="{BlobConnectionString|SAS Uri}"
  ```

## POWER-SPEC70.json
Runs the SPEC Power benchmark workload on the system targeting 70% system resource usage. This workload is an industry standard toolset for evaluating the power
consumption/draw on a system. Each of the different profiles is designed to use a specific percentage of the resources on the 
system in a steady-state usage pattern.

* **OS/Architecture Platforms**
  * linux-x64
  * linux-arm64
  * win-x64
  * win-arm64

* **Supports Disconnected Scenarios**  
  * No. Internet connection required.

* **Dependencies**  
  The dependencies defined in the 'Dependencies' section of the profile itself are required in order to run the workload operations effectively.
  * Internet connection.
  * The system on which the workload is running is expected to have physical sensors on the system board in order to capture actual temperature and power readings. For example,
    Azure hosts/blades have sensors built-in to the physical hardware. When this workload runs in a virtual machine on the node/blade, the readings must be captured
    on the blade itself. The CRC flighting system runs an agent on the Azure host that handles the capture of this information. The Virtual Client itself
    is used on the Azure host to capture the temperature and power metrics (using the IPMIUtil toolset).

  Additional information on components that exist within the 'Dependencies' section of the profile can be found in the following locations:
  * [Installing Dependencies](https://microsoft.github.io/VirtualClient/docs/category/dependencies/)

* **Profile Runtimes**  
  See the 'Metadata' section of the profile for estimated runtimes. These timings represent the length of time required to run a single round of profile 
  actions. These timings can be used to determine minimum required runtimes for the Virtual Client in order to get results. These are often estimates based on the
  number of system cores. 

* **Usage Examples**  
  The following section provides a few basic examples of how to use the workload profile.

  ``` bash
  # Execute the workload profile
  VirtualClient.exe --profile=POWER-SPEC70.json --system=Juno --timeout=1440 --packageStore="{BlobConnectionString|SAS Uri}"
  ```

## POWER-SPEC100.json
Runs the SPEC Power benchmark workload on the system targeting 100% system resource usage. This workload is an industry standard toolset for evaluating the power
consumption/draw on a system. Each of the different profiles is designed to use a specific percentage of the resources on the 
system in a steady-state usage pattern.

* **OS/Architecture Platforms**
  * linux-x64
  * linux-arm64
  * win-x64
  * win-arm64

* **Supports Disconnected Scenarios**  
  * No. Internet connection required.

* **Dependencies**  
  The dependencies defined in the 'Dependencies' section of the profile itself are required in order to run the workload operations effectively.
  * Internet connection.
  * The system on which the workload is running is expected to have physical sensors on the system board in order to capture actual temperature and power readings. For example,
    Azure hosts/blades have sensors built-in to the physical hardware. When this workload runs in a virtual machine on the node/blade, the readings must be captured
    on the blade itself. The CRC flighting system runs an agent on the Azure host that handles the capture of this information. The Virtual Client itself
    is used on the Azure host to capture the temperature and power metrics (using the IPMIUtil toolset).

  Additional information on components that exist within the 'Dependencies' section of the profile can be found in the following locations:
  * [Installing Dependencies](https://microsoft.github.io/VirtualClient/docs/category/dependencies/)

* **Profile Runtimes**  
  See the 'Metadata' section of the profile for estimated runtimes. These timings represent the length of time required to run a single round of profile 
  actions. These timings can be used to determine minimum required runtimes for the Virtual Client in order to get results. These are often estimates based on the
  number of system cores. 

* **Usage Examples**  
  The following section provides a few basic examples of how to use the workload profile.

  ``` bash
  # Execute the workload profile
  VirtualClient.exe --profile=POWER-SPEC100.json --system=Juno --timeout=1440 --packageStore="{BlobConnectionString|SAS Uri}"
  ```