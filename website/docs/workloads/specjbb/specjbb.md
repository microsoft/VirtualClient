# SPECjbb (coming soon...)
The SPECjbb® 2015 benchmark has been developed from the ground up to measure performance based on the latest Java application features. 
It is relevant to all audiences who are interested in Java server performance, including JVM vendors, hardware developers, 
Java application developers, researchers and members of the academic community.

* [SPECjbb Documentation](https://www.spec.org/jbb2015/docs/userguide.pdf)
* [SPECjbb Download](https://pro.spec.org/private/osg/incoming/)  
* [SPECjbb2015 Release](https://pro.spec.org/private/wiki/bin/view/Java/SPECjbb2015_103_Update)

:::caution Not Supported Yet...
*This workload is supported but not yet made available in the Virtual Client package store. The Virtual Client team is currently working to define and document the process for integration 
of commercial workloads into the Virtual Client that require purchase and/or license. Please bear with us while we are figuring this process out.*
:::

## What is Being Measured?
* [max-jops](https://www.spec.org/jbb2015/docs/SPECjbb2015-Result_File_Fields.html#max-jops)  
* [critical-jops](https://www.spec.org/jbb2015/docs/SPECjbb2015-Result_File_Fields.html#critical-jops)

| Name                   | Unit           | Description                                             |
|------------------------|----------------|---------------------------------------------------------|
| hbIR (max attempted)   | jOPS           | High Bound Injection Rate (HBIR) (Approximate High Bound of throughput) maximum                 |
| hbIR (settled)         | jOPS           | CHigh Bound Injection Rate (HBIR) (Approximate High Bound of throughput) settled.               |
| max-jOPS               | jOPS           | RT(Response-Throughput) step levels close to max-jOPS.                |
| critical-jOPS          | jOPS           | Geometric mean of jOPS at these SLAs represent the critical-jOPS metric.                |

## Workload Metrics
The following metrics are examples of those captured by the Virtual Client when running the SPECjbb workload.

| Metric Name | Example Value (min) | Example Value (max) | Example Value (avg) | Unit |
|-------------|---------------------|---------------------|---------------------|------|
| critical-jOPS | 915.0 | 11579.0 | 3946.5767634854776 | jOPS |
| hbIR (max attempted) | 1949.0 | 23838.0 | 10757.738589211618 | jOPS |
| hbIR (settled) | 1814.0 | 22823.0 | 9998.61825726141 | jOPS |
| max-jOPS | 1799.0 | 21454.0 | 9114.796680497926 | jOPS |