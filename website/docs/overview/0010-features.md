---
id: features
---

# Platform Features
The Virtual Client is a unified workload and system monitoring platform for running customer-representative or validation scenarios on virtual machines or physical hosts/blades/servers. 
The platform supports a wide range of different industry standard/benchmark workloads used to measuring various aspects of the system under test (e.g. CPU, I/O, network performance, power consumption). 
The platform additionally provides the ability to capture important performance and reliability measurements from the underlying system. In Azure, the platform supports all business-critical 
environments including guest/VM systems, host/blade systems and on-site lab systems.

The platform runtime application itself is a .NET 8.0 command line application written in C# that has both cross-platform and multi-architecture support. As such, the application can run on both 
Windows and Linux operating system platforms as well as x64 and arm64 architectures. The Virtual Client is the defacto standard workload provider for cloud-scale 
experiments on VMs (guest scenario) and physical blades across the Azure fleet for firmware/hardware net impact analysis.

## Platform Features
The primary goal of the Virtual Client platform is to provide an end-to-end solution for measuring the experience of a customer using cloud-based or targeted systems. The customer perspective
is particularly important for evaluating the net impact of a change to the system on a customer using it such as a firmware update but is equally important for qualifying the readiness of new generations of 
hardware systems (e.g. Gen8, Gen9 blades). To accomplish the goal, the platform supports a number of features required to provide for the complexities of cloud systems.
Goals of the platform include:

* **Provide a unified workload provider platform that covers all environments important to the business used either by customers or by company engineers.**
  * Validations of existing hardware systems.
  * Qualification of new hardware systems (i.e. new generations of hardware).
  * Offer a range of workloads that run on guest/virtual machine systems.
  * Offer a range of workloads that run on host/blade systems.
  * Offer a range of workloads that run on design lab systems.

* **Provide support for a range of different system performance and reliability monitors.**
  * Performance counter capture on guest/VM systems.
  * Performance counter capture on host/blade systems.
  * Power usage and temperature measurements on host/blade systems.
  * System profilers (e.g. Azure Profiler).

* **Provide a common data contract for capturing workload and monitoring metrics to ease reporting needs.**
  * Common schema for emitting workload metrics/measurements.
  * Common schema for emitting system performance counters.
  * Common schema for capturing interesting system events.

* **Provide facilities for capturing/uploading workload and monitoring metrics to a central data store.**
  * Azure Event Hub.
  * Azure Data Explorer/Kusto.
  * Structured Log Files for out-of-band capture.

* **Provide facilities for capturing/uploading files associated with workload executions or background monitors (e.g. profilers).**
  * Azure Blob storage accounts.

* **Provide extensibility for integrating new workloads and scenarios with a minimal amount of work.**
  * Good documentation.
  * Well-vetted and simplified design patterns.
  * Inner "open source" ready code repository.

* **Cover core operating systems used by the majority of cloud system customers.**
  * Windows
  * Windows Server
  * Ubuntu
  * RHEL
  * CENTOS
  * SUSE
  * Mariner
  * more

* **Cover core processor architectures in hardware systems used by cloud system customers.**
  * Intel 64-bit(x64) architecture.
  * AMD 64-bit(x64) architecture.
  * ARM 64-bit (ARM64) architecture.

* **Support advanced scenarios requiring multi-system, client/server topologies (i.e. multi-VM, distributed workloads).**
  * Network scenarios.
  * High-performance compute (HPC) scenarios.
  * Web server scenarios.

* **Able to be used in any automation/orchestration system for running customer-representative scenarios.**
  * Entirely self-contained with no dependency on external systems (with exception of reboot-required scenarios).
  * Generic data nomenclature.
  * Easy command line options.

### Workload Facilities
A fundamental aspect of the Virtual Client platform is that it provides a wide range of different workloads that can be executed on a system. A workload represents
a set of one more operations designed to utilize system resources in a specific way. Some workloads are designed to target singular primitive resources on the system
such as the CPU, memory/RAM, disks. Some workloads are designed to utilize the system holistically (CPU, memory and disk I/O together). Some workloads are designed to
induce faults/problems on the system. All of these targeted scenarios match scenarios common to customers of the Azure cloud using the same systems.

Examples of workload requirements supported by the platform include:

* Measure the performance of the CPU.
* Measure the performance of system memory/RAM.
* Measure the performance of system disk I/O.
* Measure the performance of the GPU (e.g. nVidia). 
* Measure the performance of the network/network stack.
* Measure the performance of high-performance compute (HPC) systems.
* Measure the performance of the system as a whole (e.g. SQL Server).
* Measure the tolerance of the system to kernel stress (i.e. mean-time-to-failure).

### Monitoring Facilities
A second fundamental aspect of the Virtual Client platform is the ability to capture a range of different metrics from the system itself while workloads are running. Aligning the
timing for capturing system monitoring information with the running of a workload produces high fidelity, highly correlated results around the performance of the system. The correlation
of workload measurements with system/OS measurements can provide deep insights into the performance and reliability of the system from a customer perspective.

* [Monitors Available](../monitors/0200-monitor-profiles.md)  

Examples of monitoring requirements supported by the platform include:

* Performance Counters on the system.
* Important system events.
* Power and temperature measurements on the cloud host/blade.
* System profiling/callstack information (e.g. Azure Profiler).

### Workload/Dependency Package Facilities
A final important fundamental of the Virtual Client platform is that it defines a common model/standard for defining workload and dependency packages. Every workload had a different
set of files and dependencies associated. This helps simplify the deployment of the Virtual client into most environments where an internet connection is available. Virtual Client can
simply download the dependencies it needs at runtime. However, this also enables Virtual Client to support "disconnected" scenarios where the systems under test do not have an internet
connection.

Examples of package/dependency requirements supported by the platform include:

* A common model for how to define a workload or dependency package.
* A common model for how to download/install workload or dependency packages.
  * Support for Azure Blob stores.
  * Support for NuGet feeds.
* Support for "drop-in" packages where the workload or dependency package is deployed with the Virtual Client instead of downloaded.

### Data Capture Facilities
Another core ability of the Virtual Client platform is that it provides a consolidated model for defining a data contract/schema for metrics emitted by workloads or by
monitors. This is an important aspect of the platform end-to-end solution in that it makes it possible to integrate many different types of workloads while ensuring a
common methodology for reading the results to determine outcomes.

* [Example Workload Metrics](../workloads/diskspd/diskspd.md)  
* [Example Monitoring Metrics](../monitors/0100-perf-counter-metrics.md)  
* [Azure Event Hub + Azure Data Explorer/Kusto Integration](../guides/0610-integration-event-hub.md)

Examples of data capture requirements supported by the platform include:

* Standardized data contract for representing metrics.
  * Workload-emitted metrics.
  * Performance counters.
  * Power usage and temperature metrics.
  * Interesting system events (e.g. registry changes, .exe executions).
* Trace logging for runtime insights and debugging.
* Azure storage account/blob store support for file uploads (e.g. SEL logs, profiler .bin files).
* Azure Event Hub support for ease-of-integration with other Azure "big-data" resources (e.g. ADX/Kusto).
* A common table and function schema for Azure ADX/Kusto data ingestion.
