# STREAM Workload Supplemental
The following information is additional/supplemental to the documentation available for the STREAM workload. This information is intended for
use by teams internal to Microsoft and their affiliates.

## System Recommendations
The following sections provide recommendations to consider when running Virtual Client profiles (workloads, monitors and tests) on
a system.

### PERF-MEM-STREAM.json
The following configurations are general recommendations for use when running this profile on cloud hardware systems and virtual machines.

* **Recommended Configurations**  
  Note that the term "cores" as used below in describing VM specifications should be inferred as synonymous with the term virtual CPU (vCPU). The configurations
  below cover those used by the CRC team for running this workload as part of the Virtual Client platform. These come from recommendations and empirical
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
      <br/><br/>
  * AMD Gen7 (Rome) Hardware
    * Virtual Machines (per node)
      * Firmware/Hardware Validations (ideal) = 1 x 72-core -> Standard_D72a_v4, Standard_E72a_v4
      * Firmware/Hardware Validations = 1 x 64-core -> Standard_D64a_v4, Standard_E64a_v4
      * Test/QoS = 1 x 16-core -> Standard_D16a_v4, Standard_E16a_v4
      <br/><br/>
  * AMD Gen8 (Milan) Hardware
    * Virtual Machines (per node)
      * Firmware/Hardware Validations (ideal) = 1 x 96-core -> Standard_D96a_v4/v5, Standard_E96a_v4/v5
      * Firmware/Hardware Validations = 1 x 64-core -> Standard_D64a_v4/v5, Standard_E64a_v4/v5
      * Test/QoS = 1 x 16-core -> Standard_D16a_v4/v5, Standard_E16a_v4/v5
      <br/><br/>
  * Intel Gen5 (Broadwell) Hardware 
    * Virtual Machines (per node)
      * Firmware/Hardware Validations = 1 x 48-core -> Standard_D48_v3, Standard_E48_v3, Standard_F48_v2
      * Test/QoS = 1 x 16-core -> Standard_D16_v3, Standard_E16_v3, Standard_F16
      <br/><br/>
  * Intel Gen6 (Coffee Lake) Hardware
    * Virtual Machines (per node)
      * Firmware/Hardware Validations = 1 x 64-core -> Standard_D64_v3, Standard_E64_v3, Standard_F64_v2
      * Test/QoS = 1 x 16-core -> Standard_D16_v3, Standard_E16_v3, Standard_F16_v2
      <br/><br/>
  * Intel Gen6 (Skylake) Hardware
    * Virtual Machines (per node)
      * Firmware/Hardware Validations = 1 x 64-core -> Standard_D64_v3, Standard_E64_v3, Standard_F64_v2
      * Test/QoS = 1 x 16-core -> Standard_D16_v3, Standard_E16_v3, Standard_F16_v2
      <br/><br/>
  * Intel Gen7 (Cascade Lake) Hardware
    * Virtual Machines (per node)
      * Firmware/Hardware Validations (ideal) = 1 x 72-core -> Standard_D72_v5, Standard_E72_v5, Standard_F72_v2
      * Firmware/Hardware Validations = 1 x 64-core -> Standard_D64_v3/v4, Standard_E64_v3/v4, Standard_F64_v2
      * Test/QoS = 1 x 16-core -> Standard_D16_v3/v4, Standard_E16_v3/v4, Standard_F16_v2
      <br/><br/>
  * Intel Gen8 (Icelake) Hardware
    * Virtual Machines (per node)
      * Firmware/Hardware Validations (ideal) = 1 x 96-core -> Standard_D96_v5, Standard_E96_v5, Standard_F96_v2
      * Firmware/Hardware Validations = 1 x 64-core -> Standard_D64_v4/v5, Standard_E64_v4/v5, Standard_F64_v2
      * Test/QoS = 1 x 16-core -> Standard_D16_v4/v5, Standard_E16_v4/v5, Standard_F16_v2