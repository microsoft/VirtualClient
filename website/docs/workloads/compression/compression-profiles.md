---
id: compression-profiles
sidebar_position: 1
---

# Compression/Decompression Workloads Profiles
The following profiles run customer-representative or benchmarking scenarios using the compression/decompression workloads.

* [Workload Details](./compression.md)  
* [Command Line Usage](https://microsoft.github.io/VirtualClient/docs/guides/0010-command-line/)

## Preliminaries
The profiles below require the ability to download workload packages and dependencies from a package store. In order to download the workload packages, connection information 
must be supplied on the command line. See the 'Workload Packages' documentation above for details on how that works.

## PERF-COMPRESSION.json
Runs the compression/decompression workloads which measures performance in terms of compression and decompression speed.  

* [Workload Profile](https://github.com/microsoft/VirtualClient/blob/main/src/VirtualClient/VirtualClient.Main/profiles/PERF-COMPRESSION.json) 

* **Supported Platform/Architectures**
  * win-x64  
    * 7zip
  * win-arm64
    * 7-zip
  * linux-x64
    * Lzbench
    * GZip
    * PBZip2
  * linux-arm64
    * Lzbench
    * GZip
    * PBZip2

* **Supported Operating Systems**
  * Ubuntu 18
  * Ubuntu 20
  * Ubuntu 22
  * Windows 10
  * Windows 11
  * Windows Server 2016
  * Windows Server 2019

* **Supports Disconnected Scenarios**  
  * No. Internet connection required.

* **Dependencies**  
  The dependencies defined in the 'Dependencies' section of the profile itself are required in order to run the workload operations effectively.
  * Internet connection.

  Additional information on components that exist within the 'Dependencies' section of the profile can be found in the following locations:
  * [Installing Dependencies](https://microsoft.github.io/VirtualClient/docs/category/dependencies/)

* **Profile Parameters**  
  The following parameters can be optionally supplied on the command line to modify the behaviors of the workload.

  | Parameter                 | Purpose                                                                         | Default Value |
  |---------------------------|---------------------------------------------------------------------------------|---------------|
  | CompilerName              | Optional. The name of the compiler to use for compilation on the system. Note that only GCC is supported currently. | gcc |
  | CompilerVersion           | Optional. The version of the compiler.                                          | 10 |
  | InputFilesOrDirs          | Optional. Input files and/or directories to be compressed and decompressed. | https://sun.aei.polsl.pl//~sdeor/corpus/silesia.zip
  | InputFiles                | Optional. Input files to be compressed and decompressed. | unzipped https://sun.aei.polsl.pl//~sdeor/corpus/silesia.zip

* **Profile Runtimes**  
  The following timings represent the length of time required to run a single round of profile actions. These timings can be used to determine
  minimum required runtimes for the Virtual Client in order to get results. These are estimates based on the number of system cores.

  * (8-cores/vCPUs) = 3 hours

* **Usage Examples**  
  The following section provides a few basic examples of how to use the workload profile.

  ```bash
  # Execute the workload profile
  ./VirtualClient --profile=PERF-COMPRESSION.json --system=Demo --timeout=1440 --packageStore="{BlobConnectionString|SAS Uri}"

  # Override the profile default parameters to use a different GCC compiler version
  ./VirtualClient --profile=PERF-COMPRESSION.json --system=Demo --timeout=1440 --parameters="CompilerVersion=11"
  ```

