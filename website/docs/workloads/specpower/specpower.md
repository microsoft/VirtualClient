# SPEC Power Workload
SPEC Power is a workload created and licensed by the Standard Performance Evalution Corporation and is an industry standard benchmark
toolset for measuring power/energy consumption on a system.

* [SPEC Power Documentation](https://www.spec.org/power_ssj2008/#:~:text=The%20SPEC%20Power%20benchmark%20is%20the%20first%20industry-standard,a%20toolset%20for%20use%20in%20improving%20server%20efficiency.)

---

### What is Being Tested?
The SPEC Power workload is configured to utilize a specific percentage of the resources on a system. It will utilize the CPU, memory and
disk resources in a steady-state pattern that enables accurate measurements of the power used by the system when under load.

### Workload Configurations
The VC Team uses a few different configurations for power/energy measurement with SPEC Power.

* #### SPEC 30  
  This configuration is designed to use approximately 30% of the resources on the system.

* #### SPEC 50  
  This configuration is designed to use approximately 50% of the resources on the system.

* #### SPEC 70  
  This configuration is designed to use approximately 70% of the resources on the system. When running on a very large Azure VM, this
  will consume a bit more than 50% of the resources on the Azure host underneath.

* #### SPEC 100  
  This configuration is designed to use approximately 100% of the resources on the system. When running on a very large Azure VM, this
  will consume a bit more than 70% of the resources on the Azure host underneath.

---

### Supported Platforms

* Linux x64
* Linux arm64
* Windows x64
* Windows arm64
