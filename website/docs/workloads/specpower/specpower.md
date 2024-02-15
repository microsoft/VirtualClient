# SPECpower
SPEC Power is a workload created and licensed by the Standard Performance Evalution Corporation and is an industry standard benchmark
toolset for measuring power/energy consumption on a system.

* [SPEC Power Documentation](https://www.spec.org/power_ssj2008/#:~:text=The%20SPEC%20Power%20benchmark%20is%20the%20first%20industry-standard,a%20toolset%20for%20use%20in%20improving%20server%20efficiency.)

## How to package SPECpower
:::info
SPECpower2008 is a commercial workload. VirtualClient cannot distribute the license and binary. You need to follow the following steps to package this workload and make it available locally or in a storage that you own.
:::

1. SPECpower can be downloaded here https://pro.spec.org/private/osg/power/, with SPEC credentials. Download ISO file with desired version like `SPECpower_ssj2008-1.12.iso`.


2. Mount or extract to get the content. Please preserve the file structure, except insert one `specpower2008.vcpkg` json file.
  ```treeview {5}
    specpower
    ├───ccs/
    ├───ssj/
    ├───LICENSE.txt
    └───specpower2008.vcpkg
  ```

  `specpower2008.vcpkg` json example
  ```json
  {
    "name": "specpower2008",
    "description": "SPEC Power 2008 workload toolsets.",
    "version": "2008",
    "metadata": {}
  }
  ```


3. Zip the specpower-1.1.8 directory content directly into `specpower-1.1.8.zip`, make sure that no extra `/specpower-1.1.8/` top directory is created.
  ```bash
  7z a specpower-1.1.8.zip ./specpower-1.1.8/*
  ```
    or 
  ```bash
  cd specpower-1.1.8; zip -r ../specpower-1.1.8.zip *
  ```

4. Modify the [SPECpower profiles](https://github.com/microsoft/VirtualClient/blob/main/src/VirtualClient/VirtualClient.Main/profiles/POWER-SPEC50.json) as needed. If you are using your own blob storage, you can use the profile as is. If you are copying the zip file locally under `vc/packages`, you can simply remove the DependencyPackageInstallation step.


## What is Being Measured?
The SPECpower workload is configured to utilize a specific percentage of the resources on a system. It will utilize the CPU, memory and
disk resources in a steady-state pattern that enables accurate measurements of the power used by the system when under load.

Note that this workload relies upon physical sensors on the hardware systems that can accurately measure temperature and power
usage. This is common with specialized cloud hardware systems. The workload itself does not emit specific metrics/measurements. This workload is 
useful because it can keep the system working at a very consistent steady-state for long periods of time helping to ensure accurate identification 
of the component temperatures and power usage during that period of time by the specialized hardware sensors attached to or embedded within the system.

### Workload Configurations
The VC Team uses a few different configurations for power/energy measurement with SPEC Power.

* **SPEC 30**  
  This configuration is designed to use approximately 30% of the resources on the system.

* **SPEC 50**  
  This configuration is designed to use approximately 50% of the resources on the system.

* **SPEC 70**  
  This configuration is designed to use approximately 70% of the resources on the system. When running on a very large Azure VM, this
  will consume a bit more than 50% of the resources on the Azure host underneath.

* **SPEC 100**  
  This configuration is designed to use approximately 100% of the resources on the system. When running on a very large Azure VM, this
  will consume a bit more than 70% of the resources on the Azure host underneath.

## Workload Metrics
Note that the SPEC Power workload itself does not measure power consumption itself. Please refer to the SPECpower official document on how to 
setup the power meters, or measure power consumption through other mechanism. The SPECpower workload makes this process more reliable because it is designed
to use resources on the system in a smooth, constant steady state. This typically causes the power usage to remain consistent as well.

| Scenario | Metric Name | Example Value (min) | Example Value (max) | Example Value (avg) | Unit |
|-----------|-------------|---------------------|---------------------|---------------------|------|
| MonitorProcess | Heartbeat | 1 | 1 | 1 |  |
