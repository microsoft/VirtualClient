# Compression/Decompression Workloads Profiles
The following profiles run customer-representative or benchmarking scenarios using the compression/decompression workloads.

* [Workload Details](./compressions.md)  
* [Workload Profile Metrics](./compression-metrics.md)


-----------------------------------------------------------------------

### Preliminaries
The profiles below require the ability to download workload packages and dependencies from a package store. In order to download the workload packages, connection information 
must be supplied on the command line. See the 'Workload Packages' documentation above for details on how that works.

-----------------------------------------------------------------------

### PERF-COMPRESSION.json
Runs the compression/decompression workloads which measures performance in terms of compression and decompression speed. 

* **Supported Platform/Architectures**
  * win-x64
  * win-arm64
  * linux-x64
  * linux-arm64


* **Profile Parameters**  
 <br/><br/>
  The following parameters can be optionally supplied on the command line to modify the behaviors of the workload. See the 'Usage Scenarios/Examples' above for examples on how to supply parameters to 
  Virtual Client profiles.

  | Parameter                 | Purpose                                                                         | Default Value |
  |---------------------------|---------------------------------------------------------------------------------|---------------|
  | InputFilesOrDirs | Optional. Input files and/or directories to be compressed and decompressed. | https://sun.aei.polsl.pl//~sdeor/corpus/silesia.zip
  | InputFiles | Optional. Input files to be compressed and decompressed. | unzipped https://sun.aei.polsl.pl//~sdeor/corpus/silesia.zip

* **Usage Examples**  
  The following section provides a few basic examples of how to use the workload profile. Additional usage examples can be found in the
  'Usage Scenarios/Examples' link at the top.


  ```bash
  ./VirtualClient --profile=PERF-COMPRESSION.json --system=Azure --timeout=1440
  ./VirtualClient --profile=PERF-COMPRESSION.json --system=Azure --timeout=1440 --parameters="InputFiles=abc.zip" 

  ```

