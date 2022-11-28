# SPEC CPU Workload Profiles
The following profiles run customer-representative or benchmarking scenarios using the SPEC CPU workload.

* [Workload Details](./SPECcpu.md)  
* [Workload Profile Metrics](./SPECcpuMetrics.md)  
* [Workload Packages](./DependencyPackages.md)
* [Usage Scenarios/Examples](./UsageScenarios.md)

-----------------------------------------------------------------------

### Preliminaries
The profiles below require the ability to download workload packages and dependencies from a package store. In order to download the workload packages, connection information 
must be supplied on the command line. See the 'Workload Packages' documentation above for details on how that works.

<div style="color:green">
<div style="font-weight:600">IMPORTANT</div>
SPECcpu 2017 workloads are long running workloads. Some take more than 2 days to complete. Check the 'Workload Runtimes' section for more details on what
to expect.
</div>
</div>

-----------------------------------------------------------------------

### PERF-SPECCPU-FPRATE.json
Runs the SPEC CPU Floating Point Rate (fprate) benchmark workload on the system focusing on baseline + peak measurements. This workload is an industry standard 
for evaluating the performance of the CPU for processing calculations.

* **Supported Platform/Architectures**
  * linux-x64
  * linux-arm64

* **Supported Operating Systems**
  * Ubuntu 18
  * Ubuntu 20
  * Ubuntu 22

* **Supported Compilers**  
  The following compilers are supported with the workload for this profile. See profile parameters and usage examples below.

  * GCC Compiler Versions = 8, 9, 10

* **Dependencies**  
  The following dependencies must be met to run this workload profile.

  * Workload package must exist in the 'packages' directory or connection information for the package store supplied on the command line (see 'Workload Packages' link above).

* **Profile Parameters**  
  The following parameters can be optionally supplied on the command line to modify the behaviors of the workload. See the 'Usage Scenarios/Examples' above for examples on how to supply parameters to 
  Virtual Client profiles.

  | Parameter                 | Purpose                                                                         | Default value |
  |---------------------------|---------------------------------------------------------------------------------|---------------|
  | CompilerName              | Optional. The name of the compiler to use for compiling CoreMark on the system. | gcc
  | CompilerVersion           | Optional. The version of the compiler to use.  | 10 

* **Recommended Configurations**  
  Note that the term "cores" as used below in describing VM specifications should be inferred as synonymous with the term virtual CPU (vCPU). The configurations
  below cover those used by the VC Team for running this workload as part of the Virtual Client platform. These come from recommendations and empirical
  evidence from running on Azure cloud systems and are designed to mimic "customer-representative" scenarios or to utilize/stress the physical nodes/systems. 
  These configurations have generally proven to be well-suited for net impact analysis on systems where a change is being applied to the physical hardware
  (e.g. a firmware update).

  * Operating System (unless otherwise specified below)
    * Linux Scenarios
       * Publisher: Canonical
       * Offer: UbuntuServer
       * Sku: 18.04-LTS
       * Version: latest
  * AMD Gen6 (Naples) Hardware
    * Virtual Machines (per node)
      * Firmware/Hardware Validations = 1 x 64-core -> Standard_L64_v2
      * Test/QoS = 1 x 16-core -> Standard_L16_v2
    * Minimum: OS disk requires a minimum of 250GB of space (to support SPEC CPU ISO downloads).
    * Minimum: 16 GB of memory/RAM.
      <br/><br/>
  * AMD Gen7 (Rome) Hardware
    * Virtual Machines (per node)
      * Firmware/Hardware Validations (ideal) = 1 x 72-core -> Standard_D72a_v4, Standard_E72a_v4
      * Firmware/Hardware Validations = 1 x 64-core -> Standard_D64a_v4, Standard_E64a_v4
      * Test/QoS = 1 x 16-core -> Standard_D16a_v4, Standard_E16a_v4
    * Minimum: OS disk requires a minimum of 250GB of space (to support SPEC CPU ISO downloads).
    * Minimum: 16 GB of memory/RAM.
      <br/><br/>
  * AMD Gen8 (Milan) Hardware
    * Virtual Machines (per node)
      * Firmware/Hardware Validations (ideal) = 1 x 96-core -> Standard_D96a_v4/v5, Standard_E96a_v4/v5
      * Firmware/Hardware Validations = 1 x 64-core -> Standard_D64a_v4/v5, Standard_E64a_v4/v5
      * Test/QoS = 1 x 16-core -> Standard_D16a_v4/v5, Standard_E16a_v4/v5
    * Minimum: OS disk requires a minimum of 250GB of space (to support SPEC CPU ISO downloads).
    * Minimum: 16 GB of memory/RAM.
      <br/><br/>
  * Intel Gen5 (Broadwell) Hardware
    * Virtual Machines (per node)
      * Firmware/Hardware Validations = 1 x 48-core -> Standard_D48_v3, Standard_E48_v3, Standard_F48_v2
      * Test/QoS = 1 x 16-core -> Standard_D16_v3, Standard_E16_v3, Standard_F16
    * Minimum: OS disk requires a minimum of 250GB of space (to support SPEC CPU ISO downloads).
    * Minimum: 16 GB of memory/RAM.
      <br/><br/>
  * Intel Gen6 (Coffee Lake) Hardware
    * Virtual Machines (per node)
      * Firmware/Hardware Validations = 1 x 64-core -> Standard_D64_v3, Standard_E64_v3, Standard_F64_v2
      * Test/QoS = 1 x 16-core -> Standard_D16_v3, Standard_E16_v3, Standard_F16_v2
    * Minimum: OS disk requires a minimum of 250GB of space (to support SPEC CPU ISO downloads).
    * Minimum: 16 GB of memory/RAM.
      <br/><br/>
  * Intel Gen6 (Skylake) Hardware
    * Virtual Machines (per node)
      * Firmware/Hardware Validations = 1 x 64-core -> Standard_D64_v3, Standard_E64_v3, Standard_F64_v2
      * Test/QoS = 1 x 16-core -> Standard_D16_v3, Standard_E16_v3, Standard_F16_v2
    * Minimum: OS disk requires a minimum of 250GB of space (to support SPEC CPU ISO downloads).
    * Minimum: 16 GB of memory/RAM.
      <br/><br/>
  * Intel Gen7 (Cascade Lake) Hardware
    * Virtual Machines (per node)
      * Firmware/Hardware Validations (ideal) = 1 x 72-core -> Standard_D72_v5, Standard_E72_v5, Standard_F72_v2
      * Firmware/Hardware Validations = 1 x 64-core -> Standard_D64_v3/v4, Standard_E64_v3/v4, Standard_F64_v2
      * Test/QoS = 1 x 16-core -> Standard_D16_v3/v4, Standard_E16_v3/v4, Standard_F16_v2
    * Minimum: OS disk requires a minimum of 250GB of space (to support SPEC CPU ISO downloads).
    * Minimum: 16 GB of memory/RAM.
      <br/><br/>
  * Intel Gen8 (Icelake) Hardware
    * Virtual Machines (per node)
      * Firmware/Hardware Validations (ideal) = 1 x 96-core -> Standard_D96_v5, Standard_E96_v5, Standard_F96_v2
      * Firmware/Hardware Validations = 1 x 64-core -> Standard_D64_v4/v5, Standard_E64_v4/v5, Standard_F64_v2
      * Test/QoS = 1 x 16-core -> Standard_D16_v4/v5, Standard_E16_v4/v5, Standard_F16_v2
    * Minimum: OS disk requires a minimum of 250GB of space (to support SPEC CPU ISO downloads).
    * Minimum: 16 GB of memory/RAM.

* **Workload Runtimes**  
  The following timings represent the length of time required to run a single round of tests ran. These timings can be used to determine
  minimum required runtimes for the Virtual Client in order to get results. These are estimates based on the use of prescribed VM SKUs. This
  particular workload takes multiple days to complete the number of iterations required for valid results.

  * Expected Runtime (8-core/vCPU VM) = 28 hours

* **Usage Examples**  
  The following section provides a few basic examples of how to use the workload profile. Additional usage examples can be found in the
  'Usage Scenarios/Examples' link at the top.

  <div style="font-size:10pt">

  ``` csharp
  ./VirtualClient --profile=PERF-SPECCPU-FPRATE.json --system=Azure --timeout=1440 --packageStore="{BlobConnectionString|SAS Uri}"
  ```
  </div>

-----------------------------------------------------------------------

### PERF-SPECCPU-FPRATE-BASE.json
Runs the SPEC CPU Floating Point Rate (fprate) benchmark workload on the system focusing on baseline measurements. This workload is an industry standard 
for evaluating the performance of the CPU for processing calculations.

* **OS/Architecture Platforms**
  * linux-x64
  * linux-arm64

* **Supported Operating Systems**
  * Ubuntu 18
  * Ubuntu 20
  * Ubuntu 22

* **Supported Compilers**  
  The following compilers are supported with the workload for this profile. See profile parameters and usage examples below.

  * GCC Compiler Versions = 8, 9, 10

* **Dependencies**  
  The following dependencies must be met to run this workload profile.

  * Workload package must exist in the 'packages' directory or connection information for the package store supplied on the command line (see 'Workload Packages' link above).

* **Profile Parameters**  
  The following parameters can be optionally supplied on the command line to modify the behaviors of the workload. See the 'Usage Scenarios/Examples' above for examples on how to supply parameters to 
  Virtual Client profiles.

  | Parameter                 | Purpose                                                                         | Default value |
  |---------------------------|---------------------------------------------------------------------------------|---------------|
  | CompilerName              | Optional. The name of the compiler to use for compiling CoreMark on the system. | gcc
  | CompilerVersion           | Optional. The version of the compiler to use.  | 10 

* **Recommended Configurations**  
  Note that the term "cores" as used below in describing VM specifications should be inferred as synonymous with the term virtual CPU (vCPU). The configurations
  below cover those used by the VC Team for running this workload as part of the Virtual Client platform. These come from recommendations and empirical
  evidence from running on Azure cloud systems and are designed to mimic "customer-representative" scenarios or to utilize/stress the physical nodes/systems. 
  These configurations have generally proven to be well-suited for net impact analysis on systems where a change is being applied to the physical hardware
  (e.g. a firmware update).

  * Operating System (unless otherwise specified below)
    * Linux Scenarios
       * Publisher: Canonical
       * Offer: UbuntuServer
       * Sku: 18.04-LTS
       * Version: latest
  * AMD Gen6 (Naples) Hardware
    * Virtual Machines (per node)
      * Firmware/Hardware Validations = 1 x 64-core -> Standard_L64_v2
      * Test/QoS = 1 x 16-core -> Standard_L16_v2
    * Minimum: OS disk requires a minimum of 250GB of space (to support SPEC CPU ISO downloads).
    * Minimum: 16 GB of memory/RAM.
      <br/><br/>
  * AMD Gen7 (Rome) Hardware
    * Virtual Machines (per node)
      * Firmware/Hardware Validations (ideal) = 1 x 72-core -> Standard_D72a_v4, Standard_E72a_v4
      * Firmware/Hardware Validations = 1 x 64-core -> Standard_D64a_v4, Standard_E64a_v4
      * Test/QoS = 1 x 16-core -> Standard_D16a_v4, Standard_E16a_v4
    * Minimum: OS disk requires a minimum of 250GB of space (to support SPEC CPU ISO downloads).
    * Minimum: 16 GB of memory/RAM.
      <br/><br/>
  * AMD Gen8 (Milan) Hardware
    * Virtual Machines (per node)
      * Firmware/Hardware Validations (ideal) = 1 x 96-core -> Standard_D96a_v4/v5, Standard_E96a_v4/v5
      * Firmware/Hardware Validations = 1 x 64-core -> Standard_D64a_v4/v5, Standard_E64a_v4/v5
      * Test/QoS = 1 x 16-core -> Standard_D16a_v4/v5, Standard_E16a_v4/v5
    * Minimum: OS disk requires a minimum of 250GB of space (to support SPEC CPU ISO downloads).
    * Minimum: 16 GB of memory/RAM.
      <br/><br/>
  * Intel Gen5 (Broadwell) Hardware
    * Virtual Machines (per node)
      * Firmware/Hardware Validations = 1 x 48-core -> Standard_D48_v3, Standard_E48_v3, Standard_F48_v2
      * Test/QoS = 1 x 16-core -> Standard_D16_v3, Standard_E16_v3, Standard_F16
    * Minimum: OS disk requires a minimum of 250GB of space (to support SPEC CPU ISO downloads).
    * Minimum: 16 GB of memory/RAM.
      <br/><br/>
  * Intel Gen6 (Coffee Lake) Hardware
    * Virtual Machines (per node)
      * Firmware/Hardware Validations = 1 x 64-core -> Standard_D64_v3, Standard_E64_v3, Standard_F64_v2
      * Test/QoS = 1 x 16-core -> Standard_D16_v3, Standard_E16_v3, Standard_F16_v2
    * Minimum: OS disk requires a minimum of 250GB of space (to support SPEC CPU ISO downloads).
    * Minimum: 16 GB of memory/RAM.
      <br/><br/>
  * Intel Gen6 (Skylake) Hardware
    * Virtual Machines (per node)
      * Firmware/Hardware Validations = 1 x 64-core -> Standard_D64_v3, Standard_E64_v3, Standard_F64_v2
      * Test/QoS = 1 x 16-core -> Standard_D16_v3, Standard_E16_v3, Standard_F16_v2
    * Minimum: OS disk requires a minimum of 250GB of space (to support SPEC CPU ISO downloads).
    * Minimum: 16 GB of memory/RAM.
      <br/><br/>
  * Intel Gen7 (Cascade Lake) Hardware
    * Virtual Machines (per node)
      * Firmware/Hardware Validations (ideal) = 1 x 72-core -> Standard_D72_v5, Standard_E72_v5, Standard_F72_v2
      * Firmware/Hardware Validations = 1 x 64-core -> Standard_D64_v3/v4, Standard_E64_v3/v4, Standard_F64_v2
      * Test/QoS = 1 x 16-core -> Standard_D16_v3/v4, Standard_E16_v3/v4, Standard_F16_v2
    * Minimum: OS disk requires a minimum of 250GB of space (to support SPEC CPU ISO downloads).
    * Minimum: 16 GB of memory/RAM.
      <br/><br/>
  * Intel Gen8 (Icelake) Hardware
    * Virtual Machines (per node)
      * Firmware/Hardware Validations (ideal) = 1 x 96-core -> Standard_D96_v5, Standard_E96_v5, Standard_F96_v2
      * Firmware/Hardware Validations = 1 x 64-core -> Standard_D64_v4/v5, Standard_E64_v4/v5, Standard_F64_v2
      * Test/QoS = 1 x 16-core -> Standard_D16_v4/v5, Standard_E16_v4/v5, Standard_F16_v2
    * Minimum: OS disk requires a minimum of 250GB of space (to support SPEC CPU ISO downloads).
    * Minimum: 16 GB of memory/RAM.

* **Workload Runtimes**  
  The following timings represent the length of time required to run a single round of tests ran. These timings can be used to determine
  minimum required runtimes for the Virtual Client in order to get results. These are estimates based on the use of prescribed VM SKUs.

  * Expected Runtime (8-core/vCPU VM) = 14 hours

* **Usage Examples**  
  The following section provides a few basic examples of how to use the workload profile. Additional usage examples can be found in the
  'Usage Scenarios/Examples' link at the top.

  <div style="font-size:10pt">

  ``` csharp
  ./VirtualClient --profile=PERF-SPECCPU-FPRATE-BASE.json --system=Azure --timeout=1440 --packageStore="{BlobConnectionString|SAS Uri}"
  ```
  </div>

-----------------------------------------------------------------------

### PERF-SPECCPU-FPSPEED.json
Runs the SPEC CPU Floating Point Speed (fpspeed) benchmark workload on the system focusing on baseline + peak measurements. This workload is an industry standard 
for evaluating the performance of the CPU for processing calculations.

* **OS/Architecture Platforms**
  * linux-x64
  * linux-arm64

* **Supported Operating Systems**
  * Ubuntu 18
  * Ubuntu 20
  * Ubuntu 22

* **Supported Compilers**  
  The following compilers are supported with the workload for this profile. See profile parameters and usage examples below.

  * GCC Compiler Versions = 8, 9, 10

* **Dependencies**  
  The following dependencies must be met to run this workload profile.

  * Workload package must exist in the 'packages' directory or connection information for the package store supplied on the command line (see 'Workload Packages' link above).

* **Profile Parameters**  
  The following parameters can be optionally supplied on the command line to modify the behaviors of the workload. See the 'Usage Scenarios/Examples' above for examples on how to supply parameters to 
  Virtual Client profiles.

  | Parameter                 | Purpose                                                                         | Default value |
  |---------------------------|---------------------------------------------------------------------------------|---------------|
  | CompilerName              | Optional. The name of the compiler to use for compiling CoreMark on the system. | gcc
  | CompilerVersion           | Optional. The version of the compiler to use.  | 10 

* **Recommended Configurations**  
  Note that the term "cores" as used below in describing VM specifications should be inferred as synonymous with the term virtual CPU (vCPU). The configurations
  below cover those used by the VC Team for running this workload as part of the Virtual Client platform. These come from recommendations and empirical
  evidence from running on Azure cloud systems and are designed to mimic "customer-representative" scenarios or to utilize/stress the physical nodes/systems. 
  These configurations have generally proven to be well-suited for net impact analysis on systems where a change is being applied to the physical hardware
  (e.g. a firmware update).

  * Operating System (unless otherwise specified below)
    * Linux Scenarios
       * Publisher: Canonical
       * Offer: UbuntuServer
       * Sku: 18.04-LTS
       * Version: latest
  * AMD Gen6 (Naples) Hardware
    * Virtual Machines (per node)
      * Firmware/Hardware Validations = 1 x 64-core -> Standard_L64_v2
      * Test/QoS = 1 x 16-core -> Standard_L16_v2
    * Minimum: OS disk requires a minimum of 250GB of space (to support SPEC CPU ISO downloads).
    * Minimum: 16 GB of memory/RAM.
      <br/><br/>
  * AMD Gen7 (Rome) Hardware
    * Virtual Machines (per node)
      * Firmware/Hardware Validations (ideal) = 1 x 72-core -> Standard_D72a_v4, Standard_E72a_v4
      * Firmware/Hardware Validations = 1 x 64-core -> Standard_D64a_v4, Standard_E64a_v4
      * Test/QoS = 1 x 16-core -> Standard_D16a_v4, Standard_E16a_v4
    * Minimum: OS disk requires a minimum of 250GB of space (to support SPEC CPU ISO downloads).
    * Minimum: 16 GB of memory/RAM.
      <br/><br/>
  * AMD Gen8 (Milan) Hardware
    * Virtual Machines (per node)
      * Firmware/Hardware Validations (ideal) = 1 x 96-core -> Standard_D96a_v4/v5, Standard_E96a_v4/v5
      * Firmware/Hardware Validations = 1 x 64-core -> Standard_D64a_v4/v5, Standard_E64a_v4/v5
      * Test/QoS = 1 x 16-core -> Standard_D16a_v4/v5, Standard_E16a_v4/v5
    * Minimum: OS disk requires a minimum of 250GB of space (to support SPEC CPU ISO downloads).
    * Minimum: 16 GB of memory/RAM.
      <br/><br/>
  * Intel Gen5 (Broadwell) Hardware
    * Virtual Machines (per node)
      * Firmware/Hardware Validations = 1 x 48-core -> Standard_D48_v3, Standard_E48_v3, Standard_F48_v2
      * Test/QoS = 1 x 16-core -> Standard_D16_v3, Standard_E16_v3, Standard_F16
    * Minimum: OS disk requires a minimum of 250GB of space (to support SPEC CPU ISO downloads).
    * Minimum: 16 GB of memory/RAM.
      <br/><br/>
  * Intel Gen6 (Coffee Lake) Hardware
    * Virtual Machines (per node)
      * Firmware/Hardware Validations = 1 x 64-core -> Standard_D64_v3, Standard_E64_v3, Standard_F64_v2
      * Test/QoS = 1 x 16-core -> Standard_D16_v3, Standard_E16_v3, Standard_F16_v2
    * Minimum: OS disk requires a minimum of 250GB of space (to support SPEC CPU ISO downloads).
    * Minimum: 16 GB of memory/RAM.
      <br/><br/>
  * Intel Gen6 (Skylake) Hardware
    * Virtual Machines (per node)
      * Firmware/Hardware Validations = 1 x 64-core -> Standard_D64_v3, Standard_E64_v3, Standard_F64_v2
      * Test/QoS = 1 x 16-core -> Standard_D16_v3, Standard_E16_v3, Standard_F16_v2
    * Minimum: OS disk requires a minimum of 250GB of space (to support SPEC CPU ISO downloads).
    * Minimum: 16 GB of memory/RAM.
      <br/><br/>
  * Intel Gen7 (Cascade Lake) Hardware
    * Virtual Machines (per node)
      * Firmware/Hardware Validations (ideal) = 1 x 72-core -> Standard_D72_v5, Standard_E72_v5, Standard_F72_v2
      * Firmware/Hardware Validations = 1 x 64-core -> Standard_D64_v3/v4, Standard_E64_v3/v4, Standard_F64_v2
      * Test/QoS = 1 x 16-core -> Standard_D16_v3/v4, Standard_E16_v3/v4, Standard_F16_v2
    * Minimum: OS disk requires a minimum of 250GB of space (to support SPEC CPU ISO downloads).
    * Minimum: 16 GB of memory/RAM.
      <br/><br/>
  * Intel Gen8 (Icelake) Hardware
    * Virtual Machines (per node)
      * Firmware/Hardware Validations (ideal) = 1 x 96-core -> Standard_D96_v5, Standard_E96_v5, Standard_F96_v2
      * Firmware/Hardware Validations = 1 x 64-core -> Standard_D64_v4/v5, Standard_E64_v4/v5, Standard_F64_v2
      * Test/QoS = 1 x 16-core -> Standard_D16_v4/v5, Standard_E16_v4/v5, Standard_F16_v2
    * Minimum: OS disk requires a minimum of 250GB of space (to support SPEC CPU ISO downloads).
    * Minimum: 16 GB of memory/RAM.

* **Workload Runtimes**  
  The following timings represent the length of time required to run a single round of tests ran. These timings can be used to determine
  minimum required runtimes for the Virtual Client in order to get results. These are estimates based on the use of prescribed VM SKUs.

  * Expected Runtime (8-core/vCPU VM) = 6 hours

* **Usage Examples**  
  The following section provides a few basic examples of how to use the workload profile. Additional usage examples can be found in the
  'Usage Scenarios/Examples' link at the top.

  <div style="font-size:10pt">

  ``` csharp
  ./VirtualClient --profile=PERF-SPECCPU-FPSPEED.json --system=Azure --timeout=1440 --packageStore="{BlobConnectionString|SAS Uri}"
  ```
  </div>

-----------------------------------------------------------------------

### PERF-SPECCPU-FPSPEED-BASE.json
Runs the SPEC CPU Floating Point Speed (fpspeed) benchmark workload on the system focusing on baseline measurements. This workload is an industry standard 
for evaluating the performance of the CPU for processing calculations.

* **OS/Architecture Platforms**
  * linux-x64
  * linux-arm64

* **Supported Operating Systems**
  * Ubuntu 18
  * Ubuntu 20
  * Ubuntu 22

* **Supported Compilers**  
  The following compilers are supported with the workload for this profile. See profile parameters and usage examples below.

  * GCC Compiler Versions = 8, 9, 10

* **Dependencies**  
  The following dependencies must be met to run this workload profile.

  * Workload package must exist in the 'packages' directory or connection information for the package store supplied on the command line (see 'Workload Packages' link above).

* **Profile Parameters**  
  The following parameters can be optionally supplied on the command line to modify the behaviors of the workload. See the 'Usage Scenarios/Examples' above for examples on how to supply parameters to 
  Virtual Client profiles.

  | Parameter                 | Purpose                                                                         | Default value |
  |---------------------------|---------------------------------------------------------------------------------|---------------|
  | CompilerName              | Optional. The name of the compiler to use for compiling CoreMark on the system. | gcc
  | CompilerVersion           | Optional. The version of the compiler to use.  | 10 
* **Recommended Configurations**  
  Note that the term "cores" as used below in describing VM specifications should be inferred as synonymous with the term virtual CPU (vCPU). The configurations
  below cover those used by the VC Team for running this workload as part of the Virtual Client platform. These come from recommendations and empirical
  evidence from running on Azure cloud systems and are designed to mimic "customer-representative" scenarios or to utilize/stress the physical nodes/systems. 
  These configurations have generally proven to be well-suited for net impact analysis on systems where a change is being applied to the physical hardware
  (e.g. a firmware update).

  * Operating System (unless otherwise specified below)
    * Linux Scenarios
       * Publisher: Canonical
       * Offer: UbuntuServer
       * Sku: 18.04-LTS
       * Version: latest
  * AMD Gen6 (Naples) Hardware
    * Virtual Machines (per node)
      * Firmware/Hardware Validations = 1 x 64-core -> Standard_L64_v2
      * Test/QoS = 1 x 16-core -> Standard_L16_v2
    * Minimum: OS disk requires a minimum of 250GB of space (to support SPEC CPU ISO downloads).
    * Minimum: 16 GB of memory/RAM.
      <br/><br/>
  * AMD Gen7 (Rome) Hardware
    * Virtual Machines (per node)
      * Firmware/Hardware Validations (ideal) = 1 x 72-core -> Standard_D72a_v4, Standard_E72a_v4
      * Firmware/Hardware Validations = 1 x 64-core -> Standard_D64a_v4, Standard_E64a_v4
      * Test/QoS = 1 x 16-core -> Standard_D16a_v4, Standard_E16a_v4
    * Minimum: OS disk requires a minimum of 250GB of space (to support SPEC CPU ISO downloads).
    * Minimum: 16 GB of memory/RAM.
      <br/><br/>
  * AMD Gen8 (Milan) Hardware
    * Virtual Machines (per node)
      * Firmware/Hardware Validations (ideal) = 1 x 96-core -> Standard_D96a_v4/v5, Standard_E96a_v4/v5
      * Firmware/Hardware Validations = 1 x 64-core -> Standard_D64a_v4/v5, Standard_E64a_v4/v5
      * Test/QoS = 1 x 16-core -> Standard_D16a_v4/v5, Standard_E16a_v4/v5
    * Minimum: OS disk requires a minimum of 250GB of space (to support SPEC CPU ISO downloads).
    * Minimum: 16 GB of memory/RAM.
      <br/><br/>
  * Intel Gen5 (Broadwell) Hardware
    * Virtual Machines (per node)
      * Firmware/Hardware Validations = 1 x 48-core -> Standard_D48_v3, Standard_E48_v3, Standard_F48_v2
      * Test/QoS = 1 x 16-core -> Standard_D16_v3, Standard_E16_v3, Standard_F16
    * Minimum: OS disk requires a minimum of 250GB of space (to support SPEC CPU ISO downloads).
    * Minimum: 16 GB of memory/RAM.
      <br/><br/>
  * Intel Gen6 (Coffee Lake) Hardware
    * Virtual Machines (per node)
      * Firmware/Hardware Validations = 1 x 64-core -> Standard_D64_v3, Standard_E64_v3, Standard_F64_v2
      * Test/QoS = 1 x 16-core -> Standard_D16_v3, Standard_E16_v3, Standard_F16_v2
    * Minimum: OS disk requires a minimum of 250GB of space (to support SPEC CPU ISO downloads).
    * Minimum: 16 GB of memory/RAM.
      <br/><br/>
  * Intel Gen6 (Skylake) Hardware
    * Virtual Machines (per node)
      * Firmware/Hardware Validations = 1 x 64-core -> Standard_D64_v3, Standard_E64_v3, Standard_F64_v2
      * Test/QoS = 1 x 16-core -> Standard_D16_v3, Standard_E16_v3, Standard_F16_v2
    * Minimum: OS disk requires a minimum of 250GB of space (to support SPEC CPU ISO downloads).
    * Minimum: 16 GB of memory/RAM.
      <br/><br/>
  * Intel Gen7 (Cascade Lake) Hardware
    * Virtual Machines (per node)
      * Firmware/Hardware Validations (ideal) = 1 x 72-core -> Standard_D72_v5, Standard_E72_v5, Standard_F72_v2
      * Firmware/Hardware Validations = 1 x 64-core -> Standard_D64_v3/v4, Standard_E64_v3/v4, Standard_F64_v2
      * Test/QoS = 1 x 16-core -> Standard_D16_v3/v4, Standard_E16_v3/v4, Standard_F16_v2
    * Minimum: OS disk requires a minimum of 250GB of space (to support SPEC CPU ISO downloads).
    * Minimum: 16 GB of memory/RAM.
      <br/><br/>
  * Intel Gen8 (Icelake) Hardware
    * Virtual Machines (per node)
      * Firmware/Hardware Validations (ideal) = 1 x 96-core -> Standard_D96_v5, Standard_E96_v5, Standard_F96_v2
      * Firmware/Hardware Validations = 1 x 64-core -> Standard_D64_v4/v5, Standard_E64_v4/v5, Standard_F64_v2
      * Test/QoS = 1 x 16-core -> Standard_D16_v4/v5, Standard_E16_v4/v5, Standard_F16_v2
    * Minimum: OS disk requires a minimum of 250GB of space (to support SPEC CPU ISO downloads).
    * Minimum: 16 GB of memory/RAM.

* **Workload Runtimes**  
  The following timings represent the length of time required to run a single round of tests ran. These timings can be used to determine
  minimum required runtimes for the Virtual Client in order to get results. These are estimates based on the use of prescribed VM SKUs.

  * Expected Runtime (8-core/vCPU VM) = 3 hours

* **Usage Examples**  
  The following section provides a few basic examples of how to use the workload profile. Additional usage examples can be found in the
  'Usage Scenarios/Examples' link at the top.

  <div style="font-size:10pt">

  ``` csharp
  ./VirtualClient --profile=PERF-SPECCPU-FPSPEED-BASE.json --system=Azure --timeout=1440 --packageStore="{BlobConnectionString|SAS Uri}"
  ```
  </div>

-----------------------------------------------------------------------

### PERF-SPECCPU-INTRATE.json
Runs the SPEC CPU Integer Rate (intrate) benchmark workload on the system focusing on baseline + peak measurements. This workload is an industry standard 
for evaluating the performance of the CPU for processing calculations.

* **OS/Architecture Platforms**
  * linux-x64
  * linux-arm64

* **Supported Operating Systems**
  * Ubuntu 18
  * Ubuntu 20
  * Ubuntu 22

* **Supported Compilers**  
  The following compilers are supported with the workload for this profile. See profile parameters and usage examples below.

  * GCC Compiler Versions = 8, 9, 10

* **Dependencies**  
  The following dependencies must be met to run this workload profile.

  * Workload package must exist in the 'packages' directory or connection information for the package store supplied on the command line (see 'Workload Packages' link above).

* **Profile Parameters**  
  The following parameters can be optionally supplied on the command line to modify the behaviors of the workload. See the 'Usage Scenarios/Examples' above for examples on how to supply parameters to 
  Virtual Client profiles.

  | Parameter                 | Purpose                                                                         | Default value |
  |---------------------------|---------------------------------------------------------------------------------|---------------|
  | CompilerName              | Optional. The name of the compiler to use for compiling CoreMark on the system. | gcc
  | CompilerVersion           | Optional. The version of the compiler to use.  | 10 

* **Recommended Configurations**  
  Note that the term "cores" as used below in describing VM specifications should be inferred as synonymous with the term virtual CPU (vCPU). The configurations
  below cover those used by the VC Team for running this workload as part of the Virtual Client platform. These come from recommendations and empirical
  evidence from running on Azure cloud systems and are designed to mimic "customer-representative" scenarios or to utilize/stress the physical nodes/systems. 
  These configurations have generally proven to be well-suited for net impact analysis on systems where a change is being applied to the physical hardware
  (e.g. a firmware update).

  * Operating System (unless otherwise specified below)
    * Linux Scenarios
       * Publisher: Canonical
       * Offer: UbuntuServer
       * Sku: 18.04-LTS
       * Version: latest
  * AMD Gen6 (Naples) Hardware
    * Virtual Machines (per node)
      * Firmware/Hardware Validations = 1 x 64-core -> Standard_L64_v2
      * Test/QoS = 1 x 16-core -> Standard_L16_v2
    * Minimum: OS disk requires a minimum of 250GB of space (to support SPEC CPU ISO downloads).
    * Minimum: 16 GB of memory/RAM.
      <br/><br/>
  * AMD Gen7 (Rome) Hardware
    * Virtual Machines (per node)
      * Firmware/Hardware Validations (ideal) = 1 x 72-core -> Standard_D72a_v4, Standard_E72a_v4
      * Firmware/Hardware Validations = 1 x 64-core -> Standard_D64a_v4, Standard_E64a_v4
      * Test/QoS = 1 x 16-core -> Standard_D16a_v4, Standard_E16a_v4
    * Minimum: OS disk requires a minimum of 250GB of space (to support SPEC CPU ISO downloads).
    * Minimum: 16 GB of memory/RAM.
      <br/><br/>
  * AMD Gen8 (Milan) Hardware
    * Virtual Machines (per node)
      * Firmware/Hardware Validations (ideal) = 1 x 96-core -> Standard_D96a_v4/v5, Standard_E96a_v4/v5
      * Firmware/Hardware Validations = 1 x 64-core -> Standard_D64a_v4/v5, Standard_E64a_v4/v5
      * Test/QoS = 1 x 16-core -> Standard_D16a_v4/v5, Standard_E16a_v4/v5
    * Minimum: OS disk requires a minimum of 250GB of space (to support SPEC CPU ISO downloads).
    * Minimum: 16 GB of memory/RAM.
      <br/><br/>
  * Intel Gen5 (Broadwell) Hardware
    * Virtual Machines (per node)
      * Firmware/Hardware Validations = 1 x 48-core -> Standard_D48_v3, Standard_E48_v3, Standard_F48_v2
      * Test/QoS = 1 x 16-core -> Standard_D16_v3, Standard_E16_v3, Standard_F16
    * Minimum: OS disk requires a minimum of 250GB of space (to support SPEC CPU ISO downloads).
    * Minimum: 16 GB of memory/RAM.
      <br/><br/>
  * Intel Gen6 (Coffee Lake) Hardware
    * Virtual Machines (per node)
      * Firmware/Hardware Validations = 1 x 64-core -> Standard_D64_v3, Standard_E64_v3, Standard_F64_v2
      * Test/QoS = 1 x 16-core -> Standard_D16_v3, Standard_E16_v3, Standard_F16_v2
    * Minimum: OS disk requires a minimum of 250GB of space (to support SPEC CPU ISO downloads).
    * Minimum: 16 GB of memory/RAM.
      <br/><br/>
  * Intel Gen6 (Skylake) Hardware
    * Virtual Machines (per node)
      * Firmware/Hardware Validations = 1 x 64-core -> Standard_D64_v3, Standard_E64_v3, Standard_F64_v2
      * Test/QoS = 1 x 16-core -> Standard_D16_v3, Standard_E16_v3, Standard_F16_v2
    * Minimum: OS disk requires a minimum of 250GB of space (to support SPEC CPU ISO downloads).
    * Minimum: 16 GB of memory/RAM.
      <br/><br/>
  * Intel Gen7 (Cascade Lake) Hardware
    * Virtual Machines (per node)
      * Firmware/Hardware Validations (ideal) = 1 x 72-core -> Standard_D72_v5, Standard_E72_v5, Standard_F72_v2
      * Firmware/Hardware Validations = 1 x 64-core -> Standard_D64_v3/v4, Standard_E64_v3/v4, Standard_F64_v2
      * Test/QoS = 1 x 16-core -> Standard_D16_v3/v4, Standard_E16_v3/v4, Standard_F16_v2
    * Minimum: OS disk requires a minimum of 250GB of space (to support SPEC CPU ISO downloads).
    * Minimum: 16 GB of memory/RAM.
      <br/><br/>
  * Intel Gen8 (Icelake) Hardware
    * Virtual Machines (per node)
      * Firmware/Hardware Validations (ideal) = 1 x 96-core -> Standard_D96_v5, Standard_E96_v5, Standard_F96_v2
      * Firmware/Hardware Validations = 1 x 64-core -> Standard_D64_v4/v5, Standard_E64_v4/v5, Standard_F64_v2
      * Test/QoS = 1 x 16-core -> Standard_D16_v4/v5, Standard_E16_v4/v5, Standard_F16_v2
    * Minimum: OS disk requires a minimum of 250GB of space (to support SPEC CPU ISO downloads).
    * Minimum: 16 GB of memory/RAM.

* **Workload Runtimes**  
  The following timings represent the length of time required to run a single round of tests ran. These timings can be used to determine
  minimum required runtimes for the Virtual Client in order to get results. These are estimates based on the use of prescribed VM SKUs. This
  particular workload takes multiple days to complete the number of iterations required for valid results.

  * Expected Runtime (8-core/vCPU VM) = 16 hours

* **Usage Examples**  
  The following section provides a few basic examples of how to use the workload profile. Additional usage examples can be found in the
  'Usage Scenarios/Examples' link at the top.

  <div style="font-size:10pt">

  ``` csharp
  ./VirtualClient --profile=PERF-SPECCPU-INTRATE.json --system=Azure --timeout=1440 --packageStore="{BlobConnectionString|SAS Uri}"
  ```
  </div>

-----------------------------------------------------------------------

### PERF-SPECCPU-INTRATE-BASE.json
Runs the SPEC CPU Integer Rate (intrate) benchmark workload on the system focusing on baseline measurements. This workload is an industry standard 
for evaluating the performance of the CPU for processing calculations.

* **OS/Architecture Platforms**
  * linux-x64
  * linux-arm64

* **Supported Operating Systems**
  * Ubuntu 18
  * Ubuntu 20
  * Ubuntu 22

* **Supported Compilers**  
  The following compilers are supported with the workload for this profile. See profile parameters and usage examples below.

  * GCC Compiler Versions = 8, 9, 10

* **Dependencies**  
  The following dependencies must be met to run this workload profile.

  * Workload package must exist in the 'packages' directory or connection information for the package store supplied on the command line (see 'Workload Packages' link above).

* **Profile Parameters**  
  The following parameters can be optionally supplied on the command line to modify the behaviors of the workload. See the 'Usage Scenarios/Examples' above for examples on how to supply parameters to 
  Virtual Client profiles.

  | Parameter                 | Purpose                                                                         | Default value |
  |---------------------------|---------------------------------------------------------------------------------|---------------|
  | CompilerName              | Optional. The name of the compiler to use for compiling CoreMark on the system. | gcc
  | CompilerVersion           | Optional. The version of the compiler to use.  | 10 

* **Recommended Configurations**  
  Note that the term "cores" as used below in describing VM specifications should be inferred as synonymous with the term virtual CPU (vCPU). The configurations
  below cover those used by the VC Team for running this workload as part of the Virtual Client platform. These come from recommendations and empirical
  evidence from running on Azure cloud systems and are designed to mimic "customer-representative" scenarios or to utilize/stress the physical nodes/systems. 
  These configurations have generally proven to be well-suited for net impact analysis on systems where a change is being applied to the physical hardware
  (e.g. a firmware update).

  * Operating System (unless otherwise specified below)
    * Linux Scenarios
       * Publisher: Canonical
       * Offer: UbuntuServer
       * Sku: 18.04-LTS
       * Version: latest
  * AMD Gen6 (Naples) Hardware
    * Virtual Machines (per node)
      * Firmware/Hardware Validations = 1 x 64-core -> Standard_L64_v2
      * Test/QoS = 1 x 16-core -> Standard_L16_v2
    * Minimum: OS disk requires a minimum of 250GB of space (to support SPEC CPU ISO downloads).
    * Minimum: 16 GB of memory/RAM.
      <br/><br/>
  * AMD Gen7 (Rome) Hardware
    * Virtual Machines (per node)
      * Firmware/Hardware Validations (ideal) = 1 x 72-core -> Standard_D72a_v4, Standard_E72a_v4
      * Firmware/Hardware Validations = 1 x 64-core -> Standard_D64a_v4, Standard_E64a_v4
      * Test/QoS = 1 x 16-core -> Standard_D16a_v4, Standard_E16a_v4
    * Minimum: OS disk requires a minimum of 250GB of space (to support SPEC CPU ISO downloads).
    * Minimum: 16 GB of memory/RAM.
      <br/><br/>
  * AMD Gen8 (Milan) Hardware
    * Virtual Machines (per node)
      * Firmware/Hardware Validations (ideal) = 1 x 96-core -> Standard_D96a_v4/v5, Standard_E96a_v4/v5
      * Firmware/Hardware Validations = 1 x 64-core -> Standard_D64a_v4/v5, Standard_E64a_v4/v5
      * Test/QoS = 1 x 16-core -> Standard_D16a_v4/v5, Standard_E16a_v4/v5
    * Minimum: OS disk requires a minimum of 250GB of space (to support SPEC CPU ISO downloads).
    * Minimum: 16 GB of memory/RAM.
      <br/><br/>
  * Intel Gen5 (Broadwell) Hardware
    * Virtual Machines (per node)
      * Firmware/Hardware Validations = 1 x 48-core -> Standard_D48_v3, Standard_E48_v3, Standard_F48_v2
      * Test/QoS = 1 x 16-core -> Standard_D16_v3, Standard_E16_v3, Standard_F16
    * Minimum: OS disk requires a minimum of 250GB of space (to support SPEC CPU ISO downloads).
    * Minimum: 16 GB of memory/RAM.
      <br/><br/>
  * Intel Gen6 (Coffee Lake) Hardware
    * Virtual Machines (per node)
      * Firmware/Hardware Validations = 1 x 64-core -> Standard_D64_v3, Standard_E64_v3, Standard_F64_v2
      * Test/QoS = 1 x 16-core -> Standard_D16_v3, Standard_E16_v3, Standard_F16_v2
    * Minimum: OS disk requires a minimum of 250GB of space (to support SPEC CPU ISO downloads).
    * Minimum: 16 GB of memory/RAM.
      <br/><br/>
  * Intel Gen6 (Skylake) Hardware
    * Virtual Machines (per node)
      * Firmware/Hardware Validations = 1 x 64-core -> Standard_D64_v3, Standard_E64_v3, Standard_F64_v2
      * Test/QoS = 1 x 16-core -> Standard_D16_v3, Standard_E16_v3, Standard_F16_v2
    * Minimum: OS disk requires a minimum of 250GB of space (to support SPEC CPU ISO downloads).
    * Minimum: 16 GB of memory/RAM.
      <br/><br/>
  * Intel Gen7 (Cascade Lake) Hardware
    * Virtual Machines (per node)
      * Firmware/Hardware Validations (ideal) = 1 x 72-core -> Standard_D72_v5, Standard_E72_v5, Standard_F72_v2
      * Firmware/Hardware Validations = 1 x 64-core -> Standard_D64_v3/v4, Standard_E64_v3/v4, Standard_F64_v2
      * Test/QoS = 1 x 16-core -> Standard_D16_v3/v4, Standard_E16_v3/v4, Standard_F16_v2
    * Minimum: OS disk requires a minimum of 250GB of space (to support SPEC CPU ISO downloads).
    * Minimum: 16 GB of memory/RAM.
      <br/><br/>
  * Intel Gen8 (Icelake) Hardware
    * Virtual Machines (per node)
      * Firmware/Hardware Validations (ideal) = 1 x 96-core -> Standard_D96_v5, Standard_E96_v5, Standard_F96_v2
      * Firmware/Hardware Validations = 1 x 64-core -> Standard_D64_v4/v5, Standard_E64_v4/v5, Standard_F64_v2
      * Test/QoS = 1 x 16-core -> Standard_D16_v4/v5, Standard_E16_v4/v5, Standard_F16_v2
    * Minimum: OS disk requires a minimum of 250GB of space (to support SPEC CPU ISO downloads).
    * Minimum: 16 GB of memory/RAM.

* **Workload Runtimes**  
  The following timings represent the length of time required to run a single round of tests ran. These timings can be used to determine
  minimum required runtimes for the Virtual Client in order to get results. These are estimates based on the use of prescribed VM SKUs.

  * Expected Runtime (8-core/vCPU VM) = 9 hours

* **Usage Examples**  
  The following section provides a few basic examples of how to use the workload profile. Additional usage examples can be found in the
  'Usage Scenarios/Examples' link at the top.

  <div style="font-size:10pt">

  ``` csharp
  ./VirtualClient --profile=PERF-SPECCPU-INTRATE-BASE.json --system=Azure --timeout=1440 --packageStore="{BlobConnectionString|SAS Uri}"
  ```
  </div>

-----------------------------------------------------------------------

### PERF-SPECCPU-INTSPEED.json
Runs the SPEC CPU Integer Speed (intspeed) benchmark workload on the system focusing on baseline + peak measurements. This workload is an industry standard 
for evaluating the performance of the CPU for processing calculations.

* **OS/Architecture Platforms**
  * linux-x64
  * linux-arm64

* **Supported Operating Systems**
  * Ubuntu 18
  * Ubuntu 20
  * Ubuntu 22

* **Supported Compilers**  
  The following compilers are supported with the workload for this profile. See profile parameters and usage examples below.

  * GCC Compiler Versions = 8, 9, 10

* **Dependencies**  
  The following dependencies must be met to run this workload profile.

  * Workload package must exist in the 'packages' directory or connection information for the package store supplied on the command line (see 'Workload Packages' link above).

* **Profile Parameters**  
  The following parameters can be optionally supplied on the command line to modify the behaviors of the workload. See the 'Usage Scenarios/Examples' above for examples on how to supply parameters to 
  Virtual Client profiles.

  | Parameter                 | Purpose                                                                         | Default value |
  |---------------------------|---------------------------------------------------------------------------------|---------------|
  | CompilerName              | Optional. The name of the compiler to use for compiling CoreMark on the system. | gcc
  | CompilerVersion           | Optional. The version of the compiler to use.  | 10 

* **Recommended Configurations**  
  Note that the term "cores" as used below in describing VM specifications should be inferred as synonymous with the term virtual CPU (vCPU). The configurations
  below cover those used by the VC Team for running this workload as part of the Virtual Client platform. These come from recommendations and empirical
  evidence from running on Azure cloud systems and are designed to mimic "customer-representative" scenarios or to utilize/stress the physical nodes/systems. 
  These configurations have generally proven to be well-suited for net impact analysis on systems where a change is being applied to the physical hardware
  (e.g. a firmware update).

  * Operating System (unless otherwise specified below)
    * Linux Scenarios
       * Publisher: Canonical
       * Offer: UbuntuServer
       * Sku: 18.04-LTS
       * Version: latest
  * AMD Gen6 (Naples) Hardware
    * Virtual Machines (per node)
      * Firmware/Hardware Validations = 1 x 64-core -> Standard_L64_v2
      * Test/QoS = 1 x 16-core -> Standard_L16_v2
    * Minimum: OS disk requires a minimum of 250GB of space (to support SPEC CPU ISO downloads).
    * Minimum: 16 GB of memory/RAM.
      <br/><br/>
  * AMD Gen7 (Rome) Hardware
    * Virtual Machines (per node)
      * Firmware/Hardware Validations (ideal) = 1 x 72-core -> Standard_D72a_v4, Standard_E72a_v4
      * Firmware/Hardware Validations = 1 x 64-core -> Standard_D64a_v4, Standard_E64a_v4
      * Test/QoS = 1 x 16-core -> Standard_D16a_v4, Standard_E16a_v4
    * Minimum: OS disk requires a minimum of 250GB of space (to support SPEC CPU ISO downloads).
    * Minimum: 16 GB of memory/RAM.
      <br/><br/>
  * AMD Gen8 (Milan) Hardware
    * Virtual Machines (per node)
      * Firmware/Hardware Validations (ideal) = 1 x 96-core -> Standard_D96a_v4/v5, Standard_E96a_v4/v5
      * Firmware/Hardware Validations = 1 x 64-core -> Standard_D64a_v4/v5, Standard_E64a_v4/v5
      * Test/QoS = 1 x 16-core -> Standard_D16a_v4/v5, Standard_E16a_v4/v5
    * Minimum: OS disk requires a minimum of 250GB of space (to support SPEC CPU ISO downloads).
    * Minimum: 16 GB of memory/RAM.
      <br/><br/>
  * Intel Gen5 (Broadwell) Hardware
    * Virtual Machines (per node)
      * Firmware/Hardware Validations = 1 x 48-core -> Standard_D48_v3, Standard_E48_v3, Standard_F48_v2
      * Test/QoS = 1 x 16-core -> Standard_D16_v3, Standard_E16_v3, Standard_F16
    * Minimum: OS disk requires a minimum of 250GB of space (to support SPEC CPU ISO downloads).
    * Minimum: 16 GB of memory/RAM.
      <br/><br/>
  * Intel Gen6 (Coffee Lake) Hardware
    * Virtual Machines (per node)
      * Firmware/Hardware Validations = 1 x 64-core -> Standard_D64_v3, Standard_E64_v3, Standard_F64_v2
      * Test/QoS = 1 x 16-core -> Standard_D16_v3, Standard_E16_v3, Standard_F16_v2
    * Minimum: OS disk requires a minimum of 250GB of space (to support SPEC CPU ISO downloads).
    * Minimum: 16 GB of memory/RAM.
      <br/><br/>
  * Intel Gen6 (Skylake) Hardware
    * Virtual Machines (per node)
      * Firmware/Hardware Validations = 1 x 64-core -> Standard_D64_v3, Standard_E64_v3, Standard_F64_v2
      * Test/QoS = 1 x 16-core -> Standard_D16_v3, Standard_E16_v3, Standard_F16_v2
    * Minimum: OS disk requires a minimum of 250GB of space (to support SPEC CPU ISO downloads).
    * Minimum: 16 GB of memory/RAM.
      <br/><br/>
  * Intel Gen7 (Cascade Lake) Hardware
    * Virtual Machines (per node)
      * Firmware/Hardware Validations (ideal) = 1 x 72-core -> Standard_D72_v5, Standard_E72_v5, Standard_F72_v2
      * Firmware/Hardware Validations = 1 x 64-core -> Standard_D64_v3/v4, Standard_E64_v3/v4, Standard_F64_v2
      * Test/QoS = 1 x 16-core -> Standard_D16_v3/v4, Standard_E16_v3/v4, Standard_F16_v2
    * Minimum: OS disk requires a minimum of 250GB of space (to support SPEC CPU ISO downloads).
    * Minimum: 16 GB of memory/RAM.
      <br/><br/>
  * Intel Gen8 (Icelake) Hardware
    * Virtual Machines (per node)
      * Firmware/Hardware Validations (ideal) = 1 x 96-core -> Standard_D96_v5, Standard_E96_v5, Standard_F96_v2
      * Firmware/Hardware Validations = 1 x 64-core -> Standard_D64_v4/v5, Standard_E64_v4/v5, Standard_F64_v2
      * Test/QoS = 1 x 16-core -> Standard_D16_v4/v5, Standard_E16_v4/v5, Standard_F16_v2
    * Minimum: OS disk requires a minimum of 250GB of space (to support SPEC CPU ISO downloads).
    * Minimum: 16 GB of memory/RAM.

* **Workload Runtimes**  
  The following timings represent the length of time required to run a single round of tests ran. These timings can be used to determine
  minimum required runtimes for the Virtual Client in order to get results. These are estimates based on the use of prescribed VM SKUs.

  * Expected Runtime (8-core/vCPU VM) = 9 hours

* **Usage Examples**  
  The following section provides a few basic examples of how to use the workload profile. Additional usage examples can be found in the
  'Usage Scenarios/Examples' link at the top.

  <div style="font-size:10pt">

  ``` csharp
  ./VirtualClient --profile=PERF-SPECCPU-INTSPEED.json --system=Azure --timeout=1440 --packageStore="{BlobConnectionString|SAS Uri}"
  ```
  </div>

-----------------------------------------------------------------------

### PERF-SPECCPU-INTSPEED-BASE.json
Runs the SPEC CPU Integer Speed (intspeed) benchmark workload on the system focusing on baseline measurements. This workload is an industry standard 
for evaluating the performance of the CPU for processing calculations.

* **OS/Architecture Platforms**
  * linux-x64
  * linux-arm64

* **Supported Operating Systems**
  * Ubuntu 18
  * Ubuntu 20
  * Ubuntu 22

* **Supported Compilers**  
  The following compilers are supported with the workload for this profile. See profile parameters and usage examples below.

  * GCC Compiler Versions = 8, 9, 10

* **Dependencies**  
  The following dependencies must be met to run this workload profile.

  * Workload package must exist in the 'packages' directory or connection information for the package store supplied on the command line (see 'Workload Packages' link above).

* **Profile Parameters**  
  The following parameters can be optionally supplied on the command line to modify the behaviors of the workload. See the 'Usage Scenarios/Examples' above for examples on how to supply parameters to 
  Virtual Client profiles.

  | Parameter                 | Purpose                                                                         | Default value |
  |---------------------------|---------------------------------------------------------------------------------|---------------|
  | CompilerName              | Optional. The name of the compiler to use for compiling CoreMark on the system. | gcc
  | CompilerVersion           | Optional. The version of the compiler to use.  | 10 

* **Recommended Configurations**  
  Note that the term "cores" as used below in describing VM specifications should be inferred as synonymous with the term virtual CPU (vCPU). The configurations
  below cover those used by the VC Team for running this workload as part of the Virtual Client platform. These come from recommendations and empirical
  evidence from running on Azure cloud systems and are designed to mimic "customer-representative" scenarios or to utilize/stress the physical nodes/systems. 
  These configurations have generally proven to be well-suited for net impact analysis on systems where a change is being applied to the physical hardware
  (e.g. a firmware update).

  * Operating System (unless otherwise specified below)
    * Linux Scenarios
       * Publisher: Canonical
       * Offer: UbuntuServer
       * Sku: 18.04-LTS
       * Version: latest
  * AMD Gen6 (Naples) Hardware
    * Virtual Machines (per node)
      * Firmware/Hardware Validations = 1 x 64-core -> Standard_L64_v2
      * Test/QoS = 1 x 16-core -> Standard_L16_v2
    * Minimum: OS disk requires a minimum of 250GB of space (to support SPEC CPU ISO downloads).
    * Minimum: 16 GB of memory/RAM.
      <br/><br/>
  * AMD Gen7 (Rome) Hardware
    * Virtual Machines (per node)
      * Firmware/Hardware Validations (ideal) = 1 x 72-core -> Standard_D72a_v4, Standard_E72a_v4
      * Firmware/Hardware Validations = 1 x 64-core -> Standard_D64a_v4, Standard_E64a_v4
      * Test/QoS = 1 x 16-core -> Standard_D16a_v4, Standard_E16a_v4
    * Minimum: OS disk requires a minimum of 250GB of space (to support SPEC CPU ISO downloads).
    * Minimum: 16 GB of memory/RAM.
      <br/><br/>
  * AMD Gen8 (Milan) Hardware
    * Virtual Machines (per node)
      * Firmware/Hardware Validations (ideal) = 1 x 96-core -> Standard_D96a_v4/v5, Standard_E96a_v4/v5
      * Firmware/Hardware Validations = 1 x 64-core -> Standard_D64a_v4/v5, Standard_E64a_v4/v5
      * Test/QoS = 1 x 16-core -> Standard_D16a_v4/v5, Standard_E16a_v4/v5
    * Minimum: OS disk requires a minimum of 250GB of space (to support SPEC CPU ISO downloads).
    * Minimum: 16 GB of memory/RAM.
      <br/><br/>
  * Intel Gen5 (Broadwell) Hardware
    * Virtual Machines (per node)
      * Firmware/Hardware Validations = 1 x 48-core -> Standard_D48_v3, Standard_E48_v3, Standard_F48_v2
      * Test/QoS = 1 x 16-core -> Standard_D16_v3, Standard_E16_v3, Standard_F16
    * Minimum: OS disk requires a minimum of 250GB of space (to support SPEC CPU ISO downloads).
    * Minimum: 16 GB of memory/RAM.
      <br/><br/>
  * Intel Gen6 (Coffee Lake) Hardware
    * Virtual Machines (per node)
      * Firmware/Hardware Validations = 1 x 64-core -> Standard_D64_v3, Standard_E64_v3, Standard_F64_v2
      * Test/QoS = 1 x 16-core -> Standard_D16_v3, Standard_E16_v3, Standard_F16_v2
    * Minimum: OS disk requires a minimum of 250GB of space (to support SPEC CPU ISO downloads).
    * Minimum: 16 GB of memory/RAM.
      <br/><br/>
  * Intel Gen6 (Skylake) Hardware
    * Virtual Machines (per node)
      * Firmware/Hardware Validations = 1 x 64-core -> Standard_D64_v3, Standard_E64_v3, Standard_F64_v2
      * Test/QoS = 1 x 16-core -> Standard_D16_v3, Standard_E16_v3, Standard_F16_v2
    * Minimum: OS disk requires a minimum of 250GB of space (to support SPEC CPU ISO downloads).
    * Minimum: 16 GB of memory/RAM.
      <br/><br/>
  * Intel Gen7 (Cascade Lake) Hardware
    * Virtual Machines (per node)
      * Firmware/Hardware Validations (ideal) = 1 x 72-core -> Standard_D72_v5, Standard_E72_v5, Standard_F72_v2
      * Firmware/Hardware Validations = 1 x 64-core -> Standard_D64_v3/v4, Standard_E64_v3/v4, Standard_F64_v2
      * Test/QoS = 1 x 16-core -> Standard_D16_v3/v4, Standard_E16_v3/v4, Standard_F16_v2
    * Minimum: OS disk requires a minimum of 250GB of space (to support SPEC CPU ISO downloads).
    * Minimum: 16 GB of memory/RAM.
      <br/><br/>
  * Intel Gen8 (Icelake) Hardware
    * Virtual Machines (per node)
      * Firmware/Hardware Validations (ideal) = 1 x 96-core -> Standard_D96_v5, Standard_E96_v5, Standard_F96_v2
      * Firmware/Hardware Validations = 1 x 64-core -> Standard_D64_v4/v5, Standard_E64_v4/v5, Standard_F64_v2
      * Test/QoS = 1 x 16-core -> Standard_D16_v4/v5, Standard_E16_v4/v5, Standard_F16_v2
    * Minimum: OS disk requires a minimum of 250GB of space (to support SPEC CPU ISO downloads).
    * Minimum: 16 GB of memory/RAM.

* **Workload Runtimes**  
  The following timings represent the length of time required to run a single round of tests ran. These timings can be used to determine
  minimum required runtimes for the Virtual Client in order to get results. These are estimates based on the use of prescribed VM SKUs.

  * Expected Runtime (8-core/vCPU VM) = 4 hours

* **Usage Examples**  
  The following section provides a few basic examples of how to use the workload profile. Additional usage examples can be found in the
  'Usage Scenarios/Examples' link at the top.

  <div style="font-size:10pt">

  ``` csharp
  ./VirtualClient --profile=PERF-SPECCPU-INTSPEED-BASE.json --system=Azure --timeout=1440 --packageStore="{BlobConnectionString|SAS Uri}"
  ```
  </div>

-----------------------------------------------------------------------

### Resources
* [Azure VM Sizes](https://docs.microsoft.com/en-us/azure/virtual-machines/sizes)
* [Azure Managed Disks](https://azure.microsoft.com/en-us/pricing/details/managed-disks/)