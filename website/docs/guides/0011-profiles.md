# Profiles
The Virtual Client defines the work/operations that will happen on a system in structured JSON documents called "profiles". Profiles can be thought
of as recipes for how to utilize resources and to work the system. Profiles are divided into different sections within a profile including Metadata, Parameters,
Actions, Monitors and Dependencies.

Although a profile may contain any number of these sections, none of the sections are absolutely required. This allows users flexibility when creating profiles for
reusability. For example, it is common for the Virtual Client team to have profiles specific to running workloads on the system separat from profiles specific to running
monitors on the system. This allows a user to run a set of monitors with any number of different sets of workloads. In the examples below, the profiles that start with the
term 'PERF' are ones that are designed to run workloads on the system. Profiles that start with the term 'MONITORS' are designed to run different sets of monitors on the 
system in the background while the workloads are running.

``` bash
VirtualClient.exe --profile=PERF-CPU-COREMARK.json --profile=MONITORS-DEFAULT.json ...
VirtualClient.exe --profile=PERF-CPU-OPENSSL.json --profile=MONITORS-DEFAULT.json ...
VirtualClient.exe --profile=PERF-IO-FIO.json --profile=MONITORS-DEFAULT.json ...
```

## Metadata
The section 'Metadata' within the profile defines supplemental or context information on the purpose of the profile, general recommendations and designations for
supported OS platforms and CPU architectures. Metadata is generally meant to serve as informational only and do not affect the operations of the Virtual Client with
regards to the profile. There are a few metadata instructions however that do affect the operations at runtime. The following table describes some of the common metadata
properties that one might find in a profile.

| Metadata Property               | Description | Default Value |
|---------------------------------|-------------|---------------|
| SupportsIterations              | Optional. True/False. This metadata property DOES affect the operations of the Virtual Client. When set to false, it indicates that the profile does not support profile iterations (i.e. --iterations on the command line). | |
| RecommendedMinimumExecutionTime | Optional. Provides a recommendation for the minimum length of time for running the profile. This time is typically based on the amount of time expected to execute all actions in the profile 1 full round. Actions are generally executed in sequential order. Note that this is an estimate based on empirical evidence, but it is always a good idea to leave a little extra time buffer. | |
| SupportedPlatforms              | Optional. Defines a set of OS platforms and CPU architectures on which the profile (and all components within) is confirmed to run correctly (e.g. win-x64 -> Windows OS, X64 architecture). | |
| SupportedOperatingSystems       | Optional. Defines a set of operating systems on which the profile is confirmed to run correctly (e.g. Ubuntu, CentOS, Windows). This list does not indicate that the Virtual Client will run on every version of these operating systems. Focus on latest versions of the operating systems for support. | |

## Parameters
The section 'Parameters' within the profile defines a set of 1 or more parameters (typically with default values) that can be used to override the default values in the components that
are part of the 'Actions', 'Monitors' or 'Dependencies' sections. In general, Virtual Client profiles are not meant to be overly general purpose. They represent tested and vetted recipes
for how to utilize resources and work a given system. Whereas any number of parameters can be defined in the 'Parameters' section, it is common practice to allow overrides to a minimum
set. This helps to ensure the purpose and consistency of the profile operations on the system cannnot diverge too far from the original intentions.

```
"Parameters": {
    "CompilerName": "gcc",
    "CompilerVersion": "10",
    "ThreadCount": null
},
 "Actions": [
    {
        "Type": "CoreMarkProExecutor",
        "Parameters": {
            "Scenario": "CoreMarkProBenchmark",
            "PackageName": "coremarkpro"
        }
    }
]
```

## Inline Parameter References
There are scenarios where the author of a profile would like to reference parameters defined in the set associated with the action, monitor or dependency within the value
for another parameter. This is typically used to allow global parameters to be defined at the profile level and then referenced parameters defined at the action level.
This technique is also used to help make profiles as "self-describing" as possible by avoiding the need to define values within the code itself (and thus out of site for a user
of the application + profile). The following example illustrates how this works with a few variations on the concept.

* **Global Parameter References**  
  The parameters at the top of a profile are "global" parameters meaning that they can be applied to any of the action, monitor or dependency components within the profile.
  Global profile parameters can be overridden on the command line by a user of the Virtual Client. This is helpful for allowing a user to have a bit more flexibility into the
  behavior of Virtual Client when running the profile. For example, a common use case it to allow the user to explicitly define the number of threads that should be used when
  running a workload thus enabling he/she to tailor the workload to systems with different amounts of CPU resources.

  ``` json
  // The 'ThreadCount' parameter defined in the profile global parameters is
  // referenced in an individual action.
  {
    "Description": "CoreMark Performance Workload",
    "Metadata": {
    },
    "Parameters": {
        "ThreadCount": 8
    },
    "Actions": [
        {
            "Type": "CoreMarkExecutor",
            "Parameters": {
                "Scenario": "ScoreSystem",
                "PackageName": "coremark",
                "ThreadCount": "$.Parameters.ThreadCount"
            }
        }
    ]
  }
  ```

  ``` bash
  # A user of the Virtual Client could then define different values for the 'ThreadCount' parameter on the 
  # command line if desired.
  VirtualClient.exe --profile=EXAMPLE-PROFILE.json --parameters="ThreadCount=4"
  VirtualClient.exe --profile=EXAMPLE-PROFILE.json --parameters="ThreadCount=16"
  VirtualClient.exe --profile=EXAMPLE-PROFILE.json --parameters="ThreadCount=32"
  ```
* **Component Inline Parameter References**
  The parameters within a given component can themselves reference the values of other parameters within the same component. This is often used to make
  usage patterns clear and self-describing within the profile. Note that this is not supported for all profiles. There is an implementation requirement in
  Virtual Client required. However, the user can typical determine if a profile supports it because it will be using it already.

  ``` json
  // Below the parameters FileSize, QueueDepth and ThreadCount are used in the value of the CommandLine parameter.
  // Additionally, the FileSize parameter is a global parameter. This illustrates combining the 2 techniques together 
  // in a single profile.
  {
    "Description": "FIO I/O Stress Performance Workload",
    "Metadata": {
    },
    "Parameters": {
        "FileSize": "496G"
    },
    "Actions": [
        {
            "Type": "FioExecutor",
            "Parameters": {
                "Scenario": "RandomWrite_4k_BlockSize",
                "PackageName": "fio",
                "CommandLine": "--name=fio_randwrite_{FileSize}_4k_d{QueueDepth}_th{ThreadCount} --size={FileSize} --numjobs={ThreadCount} --rw=randwrite --bs=4k --iodepth={QueueDepth} ...",
                "FileSize": "$.Parameters.FileSize",
                "QueueDepth": 32,
                "ThreadCount": 16
            }
        }
    ]
  }
  ```

## Well-Known Parameters
There are certain parameters that can be used in a profile that represent aspects of the system that differ from one to another. These parameters are used to allow
designers of profiles to be as expressive as possible with intentions. One of the express goals of the Virtual Client team with regards to profile design is
to have profiles be as clear and self-describing as possible. It helps users to establish context into the purpose of the profile and what it will do when running on
the system. The following table describes the set of well-known parameters that can be used in profiles.

| Parameter                           | Description |
|-------------------------------------|-------------|
| LogicalCoreCount                    | Represents the number of logical cores/vCPUs on the system. |
| PhysicalCoreCount                   | Represents the number of physical cores on the system. |
| PackagePath:\{package_name\}          | Represents the path to a package that is installed on the system by one of the dependency components (e.g. \{PackagePath\:openssl} ...resolving to /home/users/virtualclient/packages/openssl). |
| PackagePath/Platform:\{package_name\} | Represents the "platform-specific" path to a package that is installed on the system by one of the dependency components. Platform-specific paths are a Virtual Client concept. They represent paths within a given package that contain toolsets and scripts for different OS platforms and CPU architectures  (e.g. \{PackagePath/Platform:openssl\} ...resolving to /home/users/virtualclient/packages/openssl/linux-x64, /home/users/virtualclient/packages/openssl/win-arm64). |

``` json
"Actions": [
    {
        "Type": "RedisServerExecutor",
        "Parameters": {
            "Scenario": "Server",
            "PackageName": "redis",
            "CommandLine": "--protected-mode no --io-threads {ServerThreadCount} --maxmemory-policy noeviction --ignore-warnings ARM64-COW-BUG --save",
            "BindToCores": true,
            "Port": "$.Parameters.ServerPort",
            "ServerInstances": "{LogicalCoreCount}",
            "ServerThreadCount": "$.Parameters.ServerThreadCount",
            "Role": "Server"
        }
    }
]

"Dependencies": [
    {
        "Type": "WgetPackageInstallation",
        "Parameters": {
            "Scenario": "InstallMemcached",
            "PackageName": "memcached",
            "PackageUri": "https://memcached.org/files/memcached-1.6.17.tar.gz",
            "SubPath": "memcached-1.6.17",
            "Notes": "Example path to package -> /packages/memcached/memcached-1.6.17"
        }
    },
    {
        "Type": "ExecuteCommand",
        "Parameters": {
            "Scenario": "CompileMemcached",
            "Platforms": "linux-x64,linux-arm64",
            "Command": "bash -c './configure'&&make",
            "WorkingDirectory": "{PackagePath:memcached}"
        }
    }
]

"Dependencies": [
    {
        "Type": "DependencyPackageInstallation",
        "Parameters": {
            "Scenario": "InstallCoreMark",
            "BlobContainer": "packages",
            "BlobName": "coremark.1.0.0.zip",
            "PackageName": "coremark",
            "Extract": true
        }
    },
    {
        "Type": "ExecuteCommand",
        "Parameters": {
            "Scenario": "CompileCoremark",
            "Platforms": "linux-x64,linux-arm64",
            "Command": "bash -c './configure'&&make",
            "WorkingDirectory": "{PackagePath/Platform:memcached}"
        }
    }
]
```

## Actions
The section 'Actions' within the profile defines a set of 1 or more toolsets to execute on the system. The term 'workload' is often used synonymously with 'Actions' to 
describe the software that is the focus of the work Virtual Client will perform on the system. For example, the section might include an action responsible for executing
the GeekBench5 toolset/workload on the system.

``` json
"Actions": [
    {
        "Type": "GeekbenchExecutor",
        "Parameters": {
            "Scenario": "ScoreSystem",
            "CommandLine": "--no-upload",
            "PackageName": "geekbench5"
        }
    }
]
```

## Dependencies
The section 'Dependencies' within the profile defines a set of 1 or more dependencies that must be installed/provisioned on the system in order to run the
toolsets associated with the 'Actions' and 'Monitors'. For example, there may be packages that contain the toolsets that must be downloaded or Git repos to clone.

``` json
"Dependencies": [
    {
        "Type": "CompilerInstallation",
        "Parameters": {
            "Scenario": "InstallCompiler",
            "CompilerName": "$.Parameters.CompilerName",
            "CompilerVersion": "$.Parameters.CompilerVersion"
        }
    },
    {
        "Type": "GitRepoClone",
        "Parameters": {
            "Scenario": "InstallCoreMarkPro",
            "RepoUri": "https://github.com/eembc/coremark-pro.git",
            "PackageName": "coremarkpro"
        }
    }
]
```

## Monitors
The section 'Monitors' with the profile defines a set of 1 or more toolsets to run in the background capturing information from the system while 'Actions' are running.
For example, it is common practice to capture performance counters from the system while workloads are running. This enables users to get a good idea of the resources
required to support the workloads and how the operating system + hardware is operating while under duress. Monitors always run in-parallel with the actions/workloads.

``` json
"Monitors": [
    {
        "Type": "PerfCounterMonitor",
        "Parameters": {
            "Scenario": "CaptureCounters",
            "MonitorFrequency": "$.Parameters.MonitorFrequency",
            "MonitorWarmupPeriod": "$.Parameters.MonitorWarmupPeriod"
        }
    },
    {
        "Type": "LspciMonitor",
        "Parameters": {
        "Scenario": "CaptureDeviceInformation",
        "MonitorFrequency": "12:00:00",
        "MonitorWarmupPeriod": "$.Parameters.MonitorWarmupPeriod"
        }
    }
]
```

## Using Multiple Profiles
Virtual Client allows the user to define any number of profiles on the command line. When multiple profiles are defined on the command line, they are merged into a 
single profile by the Virtual Client and then executed. The components are merged in the exact order as the profiles they exist within as supplied on the command line. 
For the sake of telemetry, the very first profile defined will be the one used in the information output.

```
# For Example, given the following command line:
VirtualClient.exe --profile=PROFILE1.json --profile=PROFILE2.json --profile=MONITORS1.json

# ...And given the profiles have components as follows:
[PROFILE1.json]
  - Actions:
       - Action 1
       - Action 2

  - Dependencies:
       - Dependency 1
       - Dependency 2

[PROFILE2.json]
  - Actions:
       - Action 3
       - Action 4

  - Dependencies:
       - Dependency 3
       - Dependency 4

[MONITORS1.json]
  - Monitors:
       - Monitor 1
       - Monitor 2

  - Dependencies:
       - Dependency 3
       - Dependency 4

# ...Virtual Client will merge the 3 profiles into 1 before execution with components merged in the order supplied
#    on the commandline. The very first profile defined on the command line will be used for telemetry purposes.
[PROFILE1.json]
  - Actions:
       - Action 1
       - Action 2
       - Action 3
       - Action 4

  - Dependencies:
       - Dependency 1
       - Dependency 2
       - Dependency 3
       - Dependency 4

  - Monitors:
       - Monitor 1
       - Monitor 2
```

## Order of Execution
 Additionally, there is an order to the execution of components within a profile by the Virtual Client. Components in the 'Dependencies'
section are all executed first and in sequential order as they are defined within the profile. After the dependencies are executed, the components within the 'Monitors' section are executed.
Monitors differ from other types of components in that they are executed in-parallel. Finally, the components within the 'Actions' section are execute. Actions are executed in 
sequential order as they are defined within the profile. These components begin execution almost as soon as the monitor components have started.

```
# Profile Execution
----> 1) Dependencies

----> 2) Monitors  (all in-parallel)
-----------> Monitor 1, Monitor 2, Monitor 3

----> 3) Actions (in sequential order just as soon as the monitors are running)
-----------> Action 1 
-----------> Action 2
-----------> Action 3
-----------> Action 4
```