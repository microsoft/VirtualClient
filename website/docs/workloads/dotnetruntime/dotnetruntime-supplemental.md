# DotNetRuntime Workload Supplemental
The following information is additional/supplemental to the documentation available for the DotNetRuntime workload. This information is intended for
use by teams internal to Microsoft and their affiliates.

## System Recommendations
The following sections provide recommendations to consider when running Virtual Client profiles (workloads, monitors and tests) on
a system.

### PERF-CPU-DOTNETRUNTIME.json
The following configurations are general recommendations for use when running this profile on cloud hardware systems and virtual machines.

* **Recommended Configurations (Azure Cloud)**  
  Note that the term "cores" as used below in describing VM specifications should be inferred as synonymous with the term virtual CPU (vCPU). The configurations
  below cover those used by the CRC team for running this workload as part of the Virtual Client platform. These come from recommendations and empirical
  evidence from running on Azure cloud systems and are designed to mimic "customer-representative" scenarios or to utilize/stress the physical nodes/systems. 
  These configurations have generally proven to be well-suited for net impact analysis on systems where a change is being applied to the physical hardware
  (e.g. a firmware update).

  * Operating System (unless otherwise specified below)
    * Windows Scenarios
       * Publisher: MicrosoftWindowsServer
       * Offer: WindowsServer
       * Sku: 2019-Datacenter
       * Version: latest 
       <br/><br/>
  * AMD Gen6 (Naples) Hardware
    * Virtual Machines (per node)
      * Firmware/Hardware Validations = 10 x 2-core -> Standard_L2_v2
      * Firmware/Hardware Validations (Intel R1) = TBD
      * Test/QoS = 1 x 2-core -> Standard_L2_v2
      <br/><br/>
  * AMD Gen7 (Rome) Hardware
    * Virtual Machines (per node)
      * Firmware/Hardware Validations = 10 x 2-core -> Standard_D2a_v4, Standard_E2a_v4
      * Firmware/Hardware Validations (Intel R1) = TBD
      * Test/QoS = 1 x 2-core -> Standard_D2a_v4, Standard_E2a_v4
      <br/><br/>
  * AMD Gen8 (Milan) Hardware
    * Virtual Machines (per node)
      * Firmware/Hardware Validations = 10 x 2-core -> Standard_D2a_v4/v5, Standard_E2a_v4/v5
      * Firmware/Hardware Validations (Intel R1) = TBD
      * Test/QoS = 1 x 2-core -> Standard_D2a_v4/v5, Standard_E2a_v4/v5
      <br/><br/>
  * Intel Gen5 (Broadwell) Hardware
    * Virtual Machines (per node)
      * Firmware/Hardware Validations = 10 x 2-core -> Standard_D2_v3, Standard_E2_v3, Standard_F2_v2
      * Firmware/Hardware Validations (Intel R1) = TBD
      * Test/QoS = 1 x 2-core -> Standard_D2_v3, Standard_E2_v3, Standard_F2
      <br/><br/>
  * Intel Gen6 (Coffee Lake) Hardware
    * Virtual Machines (per node)
      * Firmware/Hardware Validations = 10 x 2-core -> Standard_D2_v3, Standard_E2_v3, Standard_F2_v2
      * Firmware/Hardware Validations (Intel R1) = TBD
      * Test/QoS = 1 x 2-core -> Standard_D2_v3, Standard_E2_v3, Standard_F2_v2
      <br/><br/>
  * Intel Gen6 (Skylake) Hardware
    * Virtual Machines (per node)
      * Firmware/Hardware Validations = 10 x 2-core -> Standard_D2_v3, Standard_E2_v3, Standard_F2_v2
      * Firmware/Hardware Validations (Intel R1) = TBD
      * Test/QoS = 1 x 2-core -> Standard_D2_v3, Standard_E2_v3, Standard_F2_v2
      <br/><br/>
  * Intel Gen7 (Cascade Lake) Hardware
    * Virtual Machines (per node)
      * Firmware/Hardware Validations = 10 x 2-core -> Standard_D2_v3/v4, Standard_E2_v3/v4, Standard_F2_v2
      * Firmware/Hardware Validations (Intel R1) = TBD
      * Test/QoS = 1 x 2-core -> Standard_D2_v3/v4, Standard_E2_v3/v4, Standard_F2_v2
      <br/><br/>
  * Intel Gen8 (Icelake) Hardware
    * Virtual Machines (per node)
      * Firmware/Hardware Validations = 10 x 2-core -> Standard_D2_v4/v5, Standard_E2_v4/v5, Standard_F2_v2
      * Firmware/Hardware Validations (Intel R1) = TBD
      * Test/QoS = 1 x 2-core -> Standard_D2_v4/v5, Standard_E2_v4/v5, Standard_F2_v2