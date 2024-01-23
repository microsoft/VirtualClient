# Hadoop Terasort Workload Profiles
The following profiles run customer-representative or benchmarking scenarios using the
 Hadoop Terasort workload.

* [Workload Details](./hadoop.md)

## PERF-CPU-TERASORT.json
Runs the Hadoop Terasort workload for a specific period of time on the system. This profile is designed to allow the user to run the workload for the purpose of evaluating
the performance of the CPU over various periods of time while also allowing the user to apply a longer-term stress to the system if desired.

* [Workload Profile](https://github.com/microsoft/VirtualClient/blob/main/src/VirtualClient/VirtualClient.Main/profiles/PERF-CPU-TERASORT.json)

* **Supported Platform/Architectures**
  * linux-x64

* **Supports Disconnected Scenarios**  
  * Yes. When the TERASORT package is included in 'packages' directory of the Virtual Client.
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
  | RowCount | Number of rows based on user choice | >0 | 100000 |

* **Profile Runtimes**  
  See the 'Metadata' section of the profile for estimated runtimes. These timings represent the length of time required to run a single round of profile 
  actions. These timings can be used to determine minimum required runtimes for the Virtual Client in order to get results. These are often estimates based on the
  number of system cores. 

* **Usage Examples**
  The following section provides a few basic examples of how to use the workload profile.

  ```bash
  # Execute the workload profile
  VirtualClient.exe --profile=PERF-CPU-TERASORT.json --system=Demo --timeout=1440 --packageStore="{BlobConnectionString|SAS Uri}"

  # Override the default parameters to run the workload for a longer period of time
  VirtualClient.exe --profile=PERF-CPU-TERASORT.json --system=Demo --timeout=1440 --packageStore="{BlobConnectionString|SAS Uri}" --parameters="RowCount=10000"
  ```
