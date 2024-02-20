# SPECjbb
The SPECjbb® 2015 benchmark has been developed from the ground up to measure performance based on the latest Java application features. 
It is relevant to all audiences who are interested in Java server performance, including JVM vendors, hardware developers, 
Java application developers, researchers and members of the academic community.

* [SPECjbb Documentation](https://www.spec.org/jbb2015/docs/userguide.pdf)
* [SPECjbb Download](https://pro.spec.org/private/osg/incoming/)  
* [SPECjbb2015 Release](https://pro.spec.org/private/wiki/bin/view/Java/SPECjbb2015_103_Update)

## How to package SPECjbb
:::info
Specjbb2015 is a commercial workload. VirtualClient cannot distribute the license and binary. You need to follow the following steps to package this workload and make it available locally or in a storage that you own.
:::
1. SPECjbb can be downloaded here https://pro.spec.org/private/osg/incoming/, with SPEC credentials. Download the desired SPECjbb version like `specjbb2015-1.03.zip`.

3. Please preserve the file structure, except insert one `specjbb2015.vcpkg` json file.
  ```treeview {6}
    specjbb2015-1.03
    │   run_composite.bat
    │   run_composite.sh
    │   etc...
    │   specjbb2015.jar
    │   specjbb2015.vcpkg
    │   SPECjbb2015_license.txt
    │   SPECjbb2015_readme.txt
    │   src.zip
    │   version.txt
    ├───config/
    ├───docs/
    ├───lib/
    └───redistributable_sources/
  ```

  `specjbb2015.vcpkg` json example
  ```json
  {
    "name": "specjbb2015",
    "description": "SPECjbb 2015 benchmark workload toolsets.",
    "version": "2015",
    "metadata": {}
  }
  ```


  
4. Zip the specjbb2015-1.03 directory into `specjbb2015-1.03.zip`, make sure that no extra `/specjbb2015-1.03/` top directory is created.
  ```bash
  7z a specjbb2015-1.03.zip ./specjbb2015-1.03/*
  ```
    or 
  ```bash
  cd specjbb2015-1.03; zip -r ../specjbb2015-1.03.zip *
  ```
5. Modify the [SPECjbb profile](https://github.com/microsoft/VirtualClient/blob/main/src/VirtualClient/VirtualClient.Main/profiles/PERF-SPECjBB.json) as needed. If you are using your own blob storage, you can use the profile as is. If you are copying the zip file locally under `vc/packages`, you can simply remove the DependencyPackageInstallation step.

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