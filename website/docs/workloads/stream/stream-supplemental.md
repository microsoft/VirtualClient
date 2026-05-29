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

### PERF-MEM-STREAMTRIAD.json
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

### PERF-MEM-STREAMMSFT.json
MSFT STREAM Parameters (Can't be used by STREAM and STREAMTriad) :
  --rnd-threads RND_THREADS: Number of total threads to randomize the work.
  --threads THREADS: Number of threads to split the work.
  --lat-threads LAT_THREADS: Number of threads running latency test
  --l2tol2-threads L2toL@_THREADS: Number of threads running latency test from L2 to L2
  --bw-array BW_ARRAY_SIZE: Array size in 1KB blocks per thread to measure bandwidth.
  --lat-array LAT_ARRAY_SIZE: Array size in 1KB blocks to measure latency.
  --lat-accesses LAT_ACCESSES: Number of accesses to measure latency.
  --lat-seq : Measure latency using sequential address on a 64B basic.
  --l2tol2-array L2toL2_ARRAY_SIZE: Array size in 1KB blocks to measure L2 to L2 latency.
  --iter ITERATIONS: Number of iterations to measure performance.
  --internal-iter INTERNAL_ITER: Internal iteration for bandwidth loop to guarantee stable results.
  --internal-iter-lat INTERNAL_ITER_LAT: Internal iteration for latency loop to guarantee stable results.
  --pattern PATTERN: Type of pattern, 0 is fixed, 1 is maximum switch and 2 is fully random.
  --enable-lat ENABLE_LAT: Measure latency accessing to a vector randomly
  --enable-l2tol2 ENABLE_L2toL2: Measure latency accessing remote l2 vector randomly. This test disables all other
  --verbose VERBOSE: 0 to no verborse level, 1 is basic verbose level to check synch points and 2 is for debug.
  --limit-time SECONDS: Number of seconds to run per iteration
  --kernel KERNEL: 0 is READ
                   1 is COPY
                   2 is SCALE
                   3 is ADD
                   4 is TRIAD
                   5 is WRITE
                   6 is ALL (0-5)
                   7 is NONE (ENABLE_LAT is required on this mode)
                   8 is 1R1W
                   9 is 2R1W
  --total-size-kb TOTAL_SIZE: Total Array size in KB to measure bandwidth, the tool will split it across all threads.
  --total-size-mb TOTAL_SIZE: Total Array size in MB to measure bandwidth, the tool will split it across all threads.
  --total-size-gb TOTAL_SIZE: Total Array size in GB to measure bandwidth, the tool will split it across all threads.
  --use-scalar: This parameters enables standard scalar kernels
  --use-sve: This parameters enables SVE kernels
  --use-neon: This parameters enables NEON 8.1 kernels (default)
  --use-numa: This parameters forces each thread to alloc the memory to be NUMA aware
  --numa-sequential: This parameters forces NUMA0 until it's fully used
  --numa-inverted: This parameters forces memory allocation on the oposite NUMA node
  --numa-alloc node_id: This parameters forces memory allocation on a fix NUMA node
  --silent: This parameters disables all the outputs except the performance report
  --perCoreReport: This parameters the performance report per core
  --streams-seq: This parameters runs the streams kernels in the same order
  --help provides this info