# Data/Telemetry Support
The Virtual Client emits a range of different types of data/telemetry as part of the execution of workload and monitoring
profiles. This data/telemetry might for example include measurements/metrics emitted by a particular workload, performance counters
or just common tracing/logging output. This data is useful for using the Virtual Client as a platform for evaluating performance
of a system while under test.

## Categories of Data
Telemetry data emitted is divided into 3 different categories:

* **Logs/Traces**  
  The Virtual Client is heavily instrumented with structured logging/tracing logic. This ensures that the inner workings of the application can
  are easily visible to the user. This is particularly important for debugging scenarios. Errors experienced by the application are captured here
  as well and will contain detailed error + callstack information.

  **Workload and System Metrics**  
  Workload metrics are measurements and information captured from the output of a particular workload (e.g. DiskSpd, FIO, GeekBench) that represent
  performance data from the system under test. Performance counters for example provide measurements from the system as-a-whole and are useful for determining exactly how the resources (e.g. CPU, memory, I/O, network)
  were used during the execution of a workload.

  **System Events**  
  System events describe certain types of important information on the system beyond simple performance measurements. This might for example
  include Windows registry changes or special event logs.

## Metrics
Metrics are generally the most important data coming out from a VirtualClient test. They usually represents measurement and information from a workload or a monitor. Metric is a core contract in VirtualClient which include the following fields.

| **Field Name**         | **Example Value**     | **Description** |
|------------------------|-----------------------|-----------------|
| `Name`            | "md5 16-byte"         | The name of the metric.               |
| `Value`           | 39359.36              | The value of the metric, double type. |
| `Unit`            | "kilobytes/sec"       | The unit of measurement.   |
| `Relativity`      | "HigherIsBetter"      | Defines the metric's relativity interpretation. For example, "HigherIsBetter" means that higher values are considered better for this metric. |
| `Verbosity`  | MetricVerbosity.Critical | Importance of metric. Enum.MetricVerbosity type.  |
| `Metadata`        | `{}`                  | KeyValue pairs of additional metadata related to the metric.       |
| `Description`     | "OpenSSL performance on md5 algorithm " | A detailed explanation of what the metric represents.|


### Metric Filter
Metrics could be filtered with filters in supported workloads. They are comma delimiter list of regex expressions that will be matched with the `Name` field of the Metric object.

For example, a metric filter with `(read|write)_(bandwidth|iops)` regex will capture four metrics: "read_bandwidth, read_iops, write_bandwidth, write_iops".

There is a special set of filters for metric verbosity, which will be covered in next section. Filters except metric verbosity are examined with "OR/union". Metric verbosity filters are "AND/intersection".

Examples
```bash
# metrics that has _p99
_p99
# metrics that match the regex
(read|write)_(bandwidth|iops)
# metrics that match the regex OR contains _p99 or _p50
(read|write)_(bandwidth|iops),_p99,p50
# metric with critical verbosity
MetricVerbosity:Critical
# metric with critical verbosity, AND contains read_
MetricVerbosity:Critical,read_
```

### Metric Verbosity
Metrics have 3 verbosity: Critical, Standard, Informational. The verbosity level indicates the metrics' importance. This could be filtered with MetricFilter in supported workloads.

- Critical  
    Critical metrics represents the most crucial metrics coming from a tool. They should be direct indicator of a system performance. For example, "average total iops" in a IO workload, or a "query per hour" in a database workload is considered critical.  
    Filter for critical metric: MetricVerbosity:Critical
- Standard  
    Standard metrics represents secondary metrics that might correlates with.  
    Filter for standard metric: MetricVerbosity:Standard
- Informational  
    Informational metrics are verbose information that helps to debug performance difference, but they alone don't directly correlate with performance differences. For example, size of database, total threads count or "memory usage in a networking workload" are considered to be informational.  
    Filter for informational metric: MetricVerbosity:Informational


## Log Files
The Virtual Client emits ALL data/telemetry captured from workloads, monitors and from the system to standard log files. Log files can be found 
in the **logs** directory within the Virtual Client application's parent directory itself. Logs are separated into the following categories:

- **Traces**  
  Operational traces about everything the Virtual Client is doing while running useful for debugging/triage purposes.

- **Metrics**  
  Important measurements captured from the workload and the system that can be used to analyze the performance and reliability of the workload and correspondingly
  the system on which it is running.

- **Counters**  
  Similar to metrics capturing important measurements from the system itself that can be used to analyze the performance, reliability and resource usage on the system
  while the workload is running.

## Metadata Contract
Within the different categories of data emitted by the Virtual Client (as noted above), the Virtual Client includes a range of different metadata about the
system and runtime context as it runs. This "metadata contract" is divided into a few different categories of metadata by default. The following section describes
those categories and illustrates examples of the structure of the telemetry messages emitted.

* **User-Defined Metadata**
  Metadata that is supplied by the user on the command line (e.g. --metadata:Prop1=Value1) will be included in the output of telemetry within the "metadata" section
  of the telemetry structure.

* **Dependency Metadata**  
  Dependency metadata describes different dependencies that were required/used during the execution of the Virtual Client. This information might for example include packages
  that were downloaded or installed on the system.

* **Host/OS Metadata**  
  Host metadata includes information about the host, the operating system and the hardware for the system in which Virtual Client is running. This information is included in the 
  "metadata_host" section of the telemetry structure.

* **Runtime Metadata**  
  Runtime metadata describes the instructions provided to the Virtual Client on the command line and aspects that are specific to the running instance of the application.
  This information is included in the "metadata_runtime" section of the telemetry structure.

* **Scenario Metadata**
  Scenario metadata is information specific to a given component within a Virtual Client profile. This might include the parameters for a action, monitor or dependency component.
  It might include installed compiler versions, the names of tools executed and their command line arguments or supplemental information in the context of a workload that is running.
  Developers onboarding components to the Virtual Client have the option to add additional scenario-specific metadata as is desirable to provide rich context to the 
  execution of workloads and monitors.

  *Example Metadata/Telemetry from Physical System*  
  ``` json
  {
    "timestamp": "2023-08-07T17:52:51.6630597Z",
    "level": "Information",
    "message": "OpenSSL.ScenarioResult",
    "customDimensions": {
        "appPlatformVersion": "1.6.0.0",
        "appVersion": "1.6.0.0",
        "clientId": "longrun",
        "executionProfileName": "PERF-CPU-OPENSSL.json",
        "executionProfilePath": "S:\\Debug\\PERF-CPU-OPENSSL.json",
        "executionSystem": null,
        "experimentId": "dbd29735-589d-4f15-adc7-2f1ec3991f03",
        "metricCategorization": "",
        "metricDescription": "",
        "metricMetadata": {},
        "metricName": "md5 16-byte",
        "metricRelativity": "HigherIsBetter",
        "metricUnit": "kilobytes/sec",
        "metricValue": 39359.36,
        "platformArchitecture": "win-x64",
        "scenarioArguments": "S:\\VirtualClient\\content\\win-x64\\packages\\openssl.3.0.0\\win-x64\\bin\\openssl.exe speed -elapsed -seconds 100 md5",
        "scenarioEndTime": "2023-08-08T00:52:51.6232956Z",
        "scenarioName": "OpenSSL Speed",
        "scenarioStartTime": "2023-08-08T00:52:21.4592999Z",
        "tags": "CPU,OpenSSL,Cryptography",
        "toolName": "OpenSSL",
        "toolVersion": "",
        "metadata": {
            "experimentId": "dbd29735-589d-4f15-adc7-2f1ec3991f03",
            "agentId": "LONGRUN"
        },
        "metadata_dependencies": {
            "package_openssl": "openssl.3.0.0.zip"
        },
        "metadata_host": {
            "computerName": "LONGRUN",
            "cpuArchitecture": "X64",
            "cpuSockets": 1,
            "cpuPhysicalCores": 4,
            "cpuPhysicalCoresPerSocket": 4,
            "cpuLogicalProcessors": 8,
            "cpuLogicalProcessorsPerCore": 8,
            "cpuCacheBytes_L1d": 131072,
            "cpuCacheBytes_L1i": 131072,
            "cpuCacheBytes_L1": 262144,
            "cpuCacheBytes_L2": 1048576,
            "cpuCacheBytes_L3": 8388608,
            "cpuLastCacheBytes": 8388608,
            "memoryBytes": 17179869184,
            "numaNodes": 0,
            "osFamily": "Windows",
            "osName": "Windows",
            "osDescription": "Microsoft Windows NT 10.0.19045.0",
            "osVersion": "10.0.19045.0",
            "osPlatformArchitecture": "win-x64",
            "parts": [
                {
                    "type": "CPU",
                    "vendor": "Intel",
                    "description": "Intel64 Family 6 Model 142 Stepping 11, GenuineIntel",
                    "family": "6",
                    "model": "Intel(R) Core(TM) i7-8565U CPU @ 1.80GHz",
                    "stepping": "11",
                },
                {
                    "type": "Memory",
                    "vendor": "SK Hynix",
                    "description:": "SK Hynix HMA81GS6JJR8N-VK"
                    "bytes": 8589934592,
                    "speed": 2400,
                    "partNumber": "HMA81GS6JJR8N-VK"
                },
                {
                    "type": "Memory",
                    "vendor": "SK Hynix",
                    "description:": "SK Hynix HMA81GS6JJR8N-VK"
                    "bytes": 8589934592,
                    "speed": 2400,
                    "partNumber": "HMA81GS6JJR8N-VK"
                },
                {
                    "type": "Network",
                    "vendor": "Intel",
                    "description": "Intel(R) Wireless-AC 9560 160MHz"
                },
                {
                    "type": "Network",
                    "vendor": "Realtek",
                    "description": "Realtek PCIe GBE Family Controller"
                }
            ]
        },
        "metadata_runtime": {
            "exitWait": "00:30:00",
            "layout": null,
            "logToFile": false,
            "iterations": 2,
            "profiles": "PERF-CPU-OPENSSL.json",
            "timeout": null,
            "timeoutScope": null,
            "scenarios": null
        },
        "metadata_scenario": {
            "scenario": "MD5",
            "packageName": "openssl",
            "extract": "True",
            "monitorFrequency": "12:00:00",
            "monitorWarmupPeriod": "00:05:00",
            "commandArguments": "speed -elapsed -seconds 100 md5",
            "tags": "CPU,OpenSSL,Cryptography",
            "profileIteration": 1,
            "profileIterationStartTime": "2023-08-08T00:52:20.7673266Z",
            "toolName": "OpenSSL Speed",
            "toolArguments": "S:\\VirtualClient\\content\\win-x64\\packages\\openssl.3.0.0\\win-x64\\bin\\openssl.exe speed -elapsed -seconds 100 md5",
            "toolVersion": null,
            "packageVersion": "3.0.0"
        }
    }
  }
  ```

  *Example Metadata/Telemetry from Physical System*  
  ``` json
  {
    "timestamp": "2023-08-07T17:52:51.6630597Z",
    "level": "Information",
    "message": "OpenSSL.ScenarioResult",
    "customDimensions": {
        "appPlatformVersion": "1.6.0.0",
        "appVersion": "1.6.0.0",
        "clientId": "longrun",
        "executionProfileName": "PERF-CPU-OPENSSL.json",
        "executionProfilePath": "S:\\Debug\\PERF-CPU-OPENSSL.json",
        "executionSystem": null,
        "experimentId": "dbd29735-589d-4f15-adc7-2f1ec3991f03",
        "metricCategorization": "",
        "metricDescription": "",
        "metricMetadata": {},
        "metricName": "md5 16-byte",
        "metricRelativity": "HigherIsBetter",
        "metricUnit": "kilobytes/sec",
        "metricValue": 39359.36,
        "platformArchitecture": "win-x64",
        "scenarioArguments": "/home/user/VirtualClient/packages/openssl.3.0.0/linux-x64/bin/openssl speed -multi 4 -elapsed -seconds 100 md5",
        "scenarioEndTime": "2023-08-08T00:52:51.6232956Z",
        "scenarioName": "OpenSSL Speed",
        "scenarioStartTime": "2023-08-08T00:52:21.4592999Z",
        "tags": "CPU,OpenSSL,Cryptography",
        "toolName": "OpenSSL",
        "toolVersion": "",
        "metadata": {
            "experimentId": "dbd29735-589d-4f15-adc7-2f1ec3991f03",
            "agentId": "LONGRUN"
        },
        "metadata_dependencies": {
            "package_openssl": "openssl.3.0.0.zip"
        },
        "metadata_host": {
            "computerName": "Linux-Host",
            "osFamily": "Unix",
            "osName": "Ubuntu",
            "osDescription": "Unix 4.15.0.140",
            "osVersion": "4.15.0.140",
            "osPlatformArchitecture": "linux-x64",
            "cpuArchitecture": "X64",
            "cpuSockets": 1,
            "cpuPhysicalCores": 4,
            "cpuPhysicalCoresPerSocket": 4,
            "cpuLogicalProcessors": 4,
            "cpuLogicalProcessorsPerCore": 4,
            "numaNodes": 1,
            "cpuCacheBytes_L1d": 32768,
            "cpuCacheBytes_L1i": 32768,
            "cpuCacheBytes_L1": 65536,
            "cpuCacheBytes_L2": 2097152,
            "cpuLastCacheBytes": 2097152,
            "memoryBytes": 8589934592,
            "parts": [
                {
                    "type": "CPU",
                    "vendor": "AMD",
                    "description": "AMD A8-7410 APU with AMD Radeon R5 Graphics Family 22 Model 48 Stepping 1, AuthenticAMD",
                    "family": "22",
                    "model": "AMD A8-7410 APU with AMD Radeon R5 Graphics",
                    "stepping": "1",
                },
                {
                    "type": "Memory",
                    "vendor": "Micron",
                    "description": "Micron, 16KTF1G64HZ-1G9P1",
                    "bytes": 8589934592,
                    "speed": 1866,
                    "partNumber": "16KTF1G64HZ-1G9P1"
                },
                {
                    "type": "Network",
                    "vendor": "Realtek",
                    "description": "Realtek Semiconductor Co., Ltd. RTL8188EE Wireless Network Adapter (rev 01)"
                },
                {
                    "type": "Network",
                    "vendor": "Realtek",
                    "description": "Realtek Semiconductor Co., Ltd. RTL810xE PCI Express Fast Ethernet controller (rev 07)"
                }
            ]
        },
        "metadata_runtime": {
            "exitWait": "00:30:00",
            "layout": null,
            "logToFile": false,
            "iterations": 2,
            "profiles": "PERF-CPU-OPENSSL.json",
            "timeout": null,
            "timeoutScope": null,
            "scenarios": null
        },
        "metadata_scenario": {
            "scenario": "MD5",
            "packageName": "openssl",
            "extract": "True",
            "monitorFrequency": "12:00:00",
            "monitorWarmupPeriod": "00:05:00",
            "commandArguments": "speed -elapsed -seconds 100 md5",
            "tags": "CPU,OpenSSL,Cryptography",
            "profileIteration": 1,
            "profileIterationStartTime": "2023-08-08T00:52:20.7673266Z",
            "toolName": "OpenSSL Speed",
            "toolArguments": "/home/user/VirtualClient/packages/openssl.3.0.0/linux-x64/bin/openssl speed -multi 4 -elapsed -seconds 100 md5",
            "toolVersion": null,
            "packageVersion": "3.0.0"
        }
     }
  }
  ```

  *Example Metadata/Telemetry from Virtual Machine System*  
  ``` json
  {
    "timestamp": "2023-08-10T22:34:41.4831010Z",
    "level": "Information",
    "message": "OpenSSL.ScenarioResult",
    "customDimensions": {
        "appPlatformVersion": "0.0.1.0",
        "appVersion": "1.7.02377.1059",
        "clientId": "demo-vm02",
        "executionProfileName": "PERF-CPU-OPENSSL.json",
        "executionProfilePath": "/home/junovmadmin/VirtualClient/content/linux-x64/profiles/PERF-CPU-OPENSSL.json",
        "executionSystem": null,
        "experimentId": "416dccea-1745-4c13-9a5d-558e87aea533",
        "metricCategorization": "",
        "metricDescription": "",
        "metricMetadata": {},
        "metricName": "md5 256-byte",
        "metricRelativity": "HigherIsBetter",
        "metricUnit": "kilobytes/sec",
        "metricValue": 468409.66,
        "platformArchitecture": "linux-x64",
        "scenarioArguments": "speed -multi 2 -elapsed -seconds 100 md5",
        "scenarioEndTime": "2023-08-10T22:34:41.4631165Z",
        "scenarioName": "OpenSSL Speed",
        "scenarioStartTime": "2023-08-10T22:24:41.4456628Z",
        "tags": "CPU,OpenSSL,Cryptography",
        "toolName": "OpenSSL",
        "toolResults": "",
        "toolVersion": "",
        "metadata": {
            "experimentId": "416dccea-1745-4c13-9a5d-558e87aea533",
            "agentId": "demo-vm02"
        },
        "metadata_dependencies": { 
            "package_openssl": "openssl.3.0.0.zip"
        },
        "metadata_host": {
            "computerName": "demo-vm02",
            "osFamily": "Unix",
            "osName": "Ubuntu",
            "osDescription": "Unix 5.15.0.1041",
            "osVersion": "5.15.0.1041",
            "osPlatformArchitecture": "linux-x64",
            "cpuArchitecture": "X64",
            "cpuSockets": 1,
            "cpuPhysicalCores": 1,
            "cpuPhysicalCoresPerSocket": 1,
            "cpuLogicalProcessors": 2,
            "cpuLogicalProcessorsPerCore": 2,
            "numaNodes": 1,
            "cpuCacheBytes_L1i": 32768,
            "cpuCacheBytes_L1d": 49152,
            "cpuCacheBytes_L1": 81920,
            "cpuCacheBytes_L3": 50331648,
            "cpuLastCacheBytes": 50331648,
            "memoryBytes": 8078536,
            "parts": [
                {
                    "type": "CPU",
                    "vendor": "Intel",
                    "description": "Intel(R) Xeon(R) Platinum 8370C CPU @ 2.80GHz Family 6 Model 106 Stepping 6, GenuineIntel",
                    "family": "6",
                    "model": "Intel(R) Xeon(R) Platinum 8370C CPU @ 2.80GHz",
                    "stepping": "6",
                },
                {
                    "type": "Network",
                    "vendor": "Mellanox",
                    "description": "Mellanox Technologies MT27800 Family [ConnectX-5 Virtual Function] (rev 80)"
                }
            ]
        },
        "metadata_runtime": {
            "exitWait": "00:30:00",
            "layout": null,
            "logToFile": false,
            "iterations": 3,
            "profiles": "PERF-CPU-OPENSSL.json",
            "timeout": null,
            "timeoutScope": null,
            "scenarios": null
        },
        "metadata_scenario": {
            "scenario": "MD5",
            "commandArguments": "speed -elapsed -seconds 100 md5",
            "packageName": null,
            "tags": "CPU,OpenSSL,Cryptography",
            "profileIteration": 1,
            "profileIterationStartTime": "2023-08-10T22:24:41.4182226Z",
            "toolName": "OpenSSL Speed",
            "toolArguments": "/home/junovmadmin/VirtualClient/content/linux-x64/packages/openssl.3.0.0/linux-x64/bin/openssl speed -multi 2 -elapsed -seconds 100 md5",
            "toolVersion": null,
            "packageVersion": null
        }
     }
  }
  ```

  *Example Metadata/Telemetry from Virtual Machine System*  
  ``` json
  {
    "timestamp": "2023-08-08T23:51:19.0305451Z",
    "level": "Information",
    "message": "CoreMark.ScenarioResult",
    "customDimensions": {
        "appPlatformVersion": "0.0.1.0",
        "appVersion": "1.7.02377.1059",
        "clientId": "demo-vm02",
        "executionProfileName": "PERF-CPU-COREMARK.json",
        "executionProfilePath": "/home/junovmadmin/VirtualClient/content/linux-x64/profiles/PERF-CPU-COREMARK.json",
        "executionSystem": null,
        "experimentId": "6d87917a-abd4-4cdc-aa89-58ead3fa5c66",
        "metricCategorization": "",
        "metricDescription": "",
        "metricMetadata": {},
        "metricName": "Iterations/Sec",
        "metricRelativity": "HigherIsBetter",
        "metricUnit": "iterations/sec",
        "metricValue": 25151.959757,
        "platformArchitecture": "linux-x64",
        "scenarioArguments": "XCFLAGS=\"-DMULTITHREAD=2 -DUSE_PTHREAD\" REBUILD=1 LFLAGS_END=-pthread",
        "scenarioEndTime": "2023-08-08T23:51:19.0036861Z",
        "scenarioName": "CoreMark",
        "scenarioStartTime": "2023-08-08T23:50:18.3523003Z",
        "tags": "",
        "toolName": "CoreMark",
        "toolVersion": "",
        "metadata": {
            "experimentId": "6d87917a-abd4-4cdc-aa89-58ead3fa5c66",
            "agentId": "demo-vm02"
        },
        "metadata_dependencies": {
            "compilerVersion_cc":"10.5.0",
            "compilerVersion_gcc":"10.5.0",
            "compilerVersion_gfortran":"10.5.0",
        },
        "metadata_host": {
            "computerName": "demo-vm02",
            "cpuArchitecture": "X64",
            "cpuSockets": 1,
            "cpuPhysicalCores": 1,
            "cpuPhysicalCoresPerSocket": 1,
            "cpuLogicalProcessors": 2,
            "cpuLogicalProcessorsPerCore": 2,
            "cpuCacheBytes_L1i": 32768,
            "cpuCacheBytes_L1d": 49152,
            "cpuCacheBytes_L1": 81920,
            "cpuCacheBytes_L3": 50331648,
            "cpuLastCacheBytes": 50331648,
            "memoryBytes": 8078536,
            "numaNodes": 1,
            "osFamily": "Unix",
            "osName": "Ubuntu",
            "osDescription": "Unix 5.15.0.1041",
            "osVersion": "5.15.0.1041",
            "osPlatformArchitecture": "linux-x64",
            "parts": [
                {
                    "type": "CPU",
                    "vendor": "Intel",
                    "description": "Intel(R) Xeon(R) Platinum 8370C CPU @ 2.80GHz Family 6 Model 106 Stepping 6, GenuineIntel",
                    "family": "6",
                    "model": "Intel(R) Xeon(R) Platinum 8370C CPU @ 2.80GHz",
                    "stepping": "6",
                },
                {
                    "type": "Network",
                    "vendor": "Mellanox",
                    "description": "Mellanox Technologies MT27800 Family [ConnectX-5 Virtual Function] (rev 80)"
                }
            ]
        },
        "metadata_runtime": {
            "exitWait": "00:30:00",
            "layout": null,
            "logToFile": false,
            "iterations": 3,
            "profiles": "PERF-CPU-COREMARK.json,",
            "timeout": null,
            "timeoutScope": null,
            "scenarios": null
        },
        "metadata_scenario": {
            "scenario": "ExecuteCoremarkBenchmark",
            "packageName": "coremark",
            "threadCount": null,
            "profileIteration": 1,
            "profileIterationStartTime": "2023-08-08T23:50:18.3340033Z",
            "toolName": "CoreMark",
            "toolArguments": "XCFLAGS=\"-DMULTITHREAD=2 -DUSE_PTHREAD\" REBUILD=1 LFLAGS_END=-pthread",
            "toolVersion": null,
            "packageVersion": null
        }
     }
  }
  ```

  *Example Metadata/Telemetry from Virtual Machine System*  
  ``` json
  {
    "timestamp": "2023-08-08T23:51:19.0305451Z",
    "level": "Information",
    "message": "Superbench.ScenarioResult",
    "customDimensions": {
        "appPlatformVersion": "0.0.1.0",
        "appVersion": "1.7.02377.1059",
        "clientId": "demo-vm02",
        "executionProfileName": "PERF-GPU-SUPERBENCH.json",
        "executionProfilePath": "/home/junovmadmin/VirtualClient/content/linux-x64/profiles/PERF-GPU-SUPERBENCH.json",
        "executionSystem": null,
        "experimentId": "6d87917a-abd4-4cdc-aa89-58ead3fa5c66",
        "metricCategorization": "default.yaml",
        "metricDescription": "",
        "metricMetadata": {},
        "metricName": "resnet_models/pytorch-resnet101/fp16_train_step_time",
        "metricRelativity": "Undefined",
        "metricUnit": "",
        "metricValue": "576.05609750747681",
        "operatingSystemPlatform": "Unix",
        "platformArchitecture": "linux-x64",
        "profileFriendlyName": "SuperBenchmark GPU Performance Workload",
        "scenarioArguments": "run --host-list localhost -c default.yaml",
        "scenarioEndTime": "2023-08-16T03:52:31.1568631Z",
        "scenarioName": "SuperBenchmark",
        "scenarioStartTime": "2023-08-16T03:19:56.9093053Z",
        "tags": "",
        "toolName": "SuperBenchmark",
        "toolVersion": ""
        "metadata": {
            "experimentId": "6d87917a-abd4-4cdc-aa89-58ead3fa5c66",
            "agentId": "demo-vm02"
        },
        "metadata_dependencies": { 
            "gpuVendor": "Nvidia",
            "gpuDriverVersion": "525",
            "cudaVersion": "12.0",
        },
        "metadata_host": {
            "computerName": "demo-vm02",
            "cpuArchitecture": "X64",
            "cpuSockets": 1,
            "cpuPhysicalCores": 1,
            "cpuPhysicalCoresPerSocket": 1,
            "cpuLogicalProcessors": 2,
            "cpuLogicalProcessorsPerCore": 2,
            "cpuCacheBytes_L1i": 32768,
            "cpuCacheBytes_L1d": 49152,
            "cpuCacheBytes_L1": 81920,
            "cpuCacheBytes_L3": 50331648,
            "cpuLastCacheBytes": 50331648,
            "memoryBytes": 8078536,
            "numaNodes": 1,
            "osFamily": "Unix",
            "osName": "Ubuntu",
            "osDescription": "Unix 5.15.0.1041",
            "osVersion": "5.15.0.1041",
            "osPlatformArchitecture": "linux-x64",
            "parts": [
                {
                    "type": "CPU",
                    "vendor": "Intel",
                    "description": "Intel(R) Xeon(R) Platinum 8370C CPU @ 2.80GHz Family 6 Model 106 Stepping 6, GenuineIntel",
                    "family": "6",
                    "model": "Intel(R) Xeon(R) Platinum 8370C CPU @ 2.80GHz",
                    "stepping": "6",
                },
                {
                    "type": "Network",
                    "vendor": "Mellanox",
                    "description": "Mellanox Technologies MT27800 Family [ConnectX-5 Virtual Function] (rev 80)"
                }
            ]
        },
        "metadata_runtime": {
            "exitWait": "00:30:00",
            "layout": null,
            "logToFile": false,
            "iterations": 3,
            "profiles": "PERF-GPU-SUPERBENCH.json,",
            "timeout": null,
            "timeoutScope": null,
            "scenarios": null
        },
        "metadata_scenario": {
            "scenario": "ExecuteCoremarkBenchmark",
            "packageName": "coremark",
            "threadCount": null,
            "profileIteration": 1,
            "profileIterationStartTime": "2023-08-08T23:50:18.3340033Z",
            "toolName": "CoreMark",
            "toolArguments": "XCFLAGS=\"-DMULTITHREAD=2 -DUSE_PTHREAD\" REBUILD=1 LFLAGS_END=-pthread",
            "toolVersion": null,
            "packageVersion": null
        }
     }
  }
  ```

## Larger-Scale Scenarios
For larger-scale scenarios where the Virtual Client may be ran on many systems for long periods of time each producing a lot of data/telemetry. In these
scenarios, it is not that easy to gather log files from systems and to parse out the meaningful information from them required to analyze the performance
and reliability of the systems. Virtual Client supports options to integrate with "big data" cloud resources for these type of scenarios.

### Azure Event Hubs Support
Azure Event Hubs is a large-scale messaging platform available in the Azure cloud. The platform integrates with other data storage and analytics resources
such as Azure Data Explorer/Kusto and Azure Storage Accounts. This makes Event Hubs a very nice option for emitting data/telemetry from the operations
of the Virtual Client. Event Hubs can support both the scale and the need to aggregate/consolidate the data so that it can be readily analyzed. The Virtual Client
allows users to request data/telemetry be sent to a set of Event Hubs by supplying the connection string to the Event Hub Namespace on the command line.

See the following documentation for more information:
* [Event Hubs Integration](./0610-integration-event-hub.md)

### Azure Storage Account Support
An Azure Storage Account is a large-scale file/blob storage platform available in the Azure cloud. The platform is one of the most fundamental resources available
in the Azure cloud and it integrates with many other resources such as Azure Data Factory. The Virtual Client allows users to request files and logs content to 
be uploaded to a Storage Account by passing in a connection string or a SAS URI to the Storage Account on the command line.

See the following documentation for more information:
* [Storage Account Integration](./0600-integration-blob-storage.md)