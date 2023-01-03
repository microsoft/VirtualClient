# SPECpower (coming soon...)
SPEC Power is a workload created and licensed by the Standard Performance Evalution Corporation and is an industry standard benchmark
toolset for measuring power/energy consumption on a system.

* [SPEC Power Documentation](https://www.spec.org/power_ssj2008/#:~:text=The%20SPEC%20Power%20benchmark%20is%20the%20first%20industry-standard,a%20toolset%20for%20use%20in%20improving%20server%20efficiency.)

:::caution Not Supported Yet...
*This workload is supported but not yet made available in the Virtual Client package store. The Virtual Client team is currently working to define and document the process for integration 
of commercial workloads into the Virtual Client that require purchase and/or license. Please bear with us while we are figuring this process out.*
:::

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
