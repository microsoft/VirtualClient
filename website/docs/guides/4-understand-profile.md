---
id: understand-profile
sidebar_position: 4
---

# Understand VC components and profiles


### Scenario: Overriding Parameters in a Workload Profile
Each profile definition available in the Virtual Client can have parameters that are overridable. In general, scenario profiles are defined to such that certain aspects of
running the workload are set in stone if you will. Workload profiles are designed with certain amounts of expertise and empirical evidence in mind. However, there are sometimes
a small set of reasonable variations that make sense for flexibility.

Note that you can determine which parameters can be overridden on the command line in a workload profile by looking at the profile definition itself. Any parameters that
are in the 'Parameters' section at the top of a profile definition can be overridden. The documentation for each of the profiles also provides these details. See the following 
profile definitions for examples.

* [FIO Workload Profile](https://github.com/microsoft/VirtualClient/blob/main/src/VirtualClient/VirtualClient.Main/profiles/PERF-IO-FIO.json&version=GBmaster)



```
VirtualClient.exe --profile=PERF-IO-FIO.json --timeout=180 --packages="{BlobStoreConnectionString|SAS URI}" --parameters="DiskFilter=OSDisk:false&biggestSize"
```

---

### Scenario: Running Specific Scenarios in a Workload Profile
Certain profiles available for the Virtual Client have more than one action/scenario that is executed. These profiles are designed to test
the system in various ways to produce robust performance evaluations. The Virtual Client supports the ability to specify a specific
set of scenarios within the profile. This is useful for cases where certain scenarios show unexpected performance outcomes and the user
would like to run those specific scenarios again (vs. the entire profile). See the following profile for examples of scenario definitions within 
the profile:

* [PERF-IO-FIO.json](https://github.com/microsoft/VirtualClient/blob/main/src/VirtualClient/VirtualClient.Main/profiles/PERF-IO-FIO.json)

Each action/step defined in a profile can have a 'Scenario' parameter:

``` json
{
    "Type": "FioExecutor",
    "Parameters": {
        "Scenario": "DiskFill",
        "CommandLine": "--name=disk_fill --size=[filesize] --numjobs=1 --rw=write --bs=256k --iodepth=64 --direct=1 --overwrite=1 --thread",
        ...
    }
},
{
    "Type": "FioExecutor",
    "Parameters": {
        "Scenario": "RandomWrite_4k_BlockSize",
        "CommandLine": "--name=fio_randwrite_[filesize]_4k_d[queuedepth]_th[threads] --size=[filesize] --numjobs=[threads] --rw=randwrite --bs=4k --iodepth=[queuedepth] --direct=1 --ramp_time=30 --runtime=300 --time_based --overwrite=1 --thread --group_reporting --output-format=json",
        ...
    }
},
{
    "Type": "FioExecutor",
    "Parameters": {
        "Scenario": "RandomWrite_8k_BlockSize",
        "CommandLine": "--name=fio_randwrite_[filesize]_4k_d[queuedepth]_th[threads] --size=[filesize] --numjobs=[threads] --rw=randwrite --bs=4k --iodepth=[queuedepth] --direct=1 --ramp_time=30 --runtime=300 --time_based --overwrite=1 --thread --group_reporting --output-format=json",
        ...
    }
},
{
    "Type": "FioExecutor",
    "Parameters": {
        "Scenario": "RandomWrite_12k_BlockSize",
        "CommandLine": "--name=fio_randwrite_[filesize]_4k_d[queuedepth]_th[threads] --size=[filesize] --numjobs=[threads] --rw=randwrite --bs=4k --iodepth=[queuedepth] --direct=1 --ramp_time=30 --runtime=300 --time_based --overwrite=1 --thread --group_reporting --output-format=json",
        ...
    }
}
```

To include multiple scenarios, supply a set of scenario names on the command line delimited by a comma or semi-colon as is illustrated below.


```
# Run the DiskFill, RandomWrite_4k_BlockSize and RandomWrite_8k_BlockSize scenarios only
VirtualClient.exe --profile=PERF-IO-FIO.json --timeout=180 --packages="{BlobStoreConnectionString|SAS URI}" --scenarios=DiskFill,RandomWrite_4k_BlockSize,RandomWrite_8k_BlockSize
```

---

### Scenario: Excluding Specific Scenarios in a Workload Profile
Similarly to the above example where specific scenarios are specified, specific scenarios can be excluded as well. This is useful for cases where the user wants to run most components in
a profile except for a few.

* [PERF-IO-FIO.json](https://github.com/microsoft/VirtualClient/blob/main/src/VirtualClient/VirtualClient.Main/profiles/PERF-IO-FIO.json)

Each action/step defined in a profile can have a 'Scenario' parameter:

``` json
{
    "Type": "FioExecutor",
    "Parameters": {
        "Scenario": "DiskFill",
        "CommandLine": "--name=disk_fill --size=[filesize] --numjobs=1 --rw=write --bs=256k --iodepth=64 --direct=1 --overwrite=1 --thread",
        ...
    }
},
{
    "Type": "FioExecutor",
    "Parameters": {
        "Scenario": "RandomWrite_4k_BlockSize",
        "CommandLine": "--name=fio_randwrite_[filesize]_4k_d[queuedepth]_th[threads] --size=[filesize] --numjobs=[threads] --rw=randwrite --bs=4k --iodepth=[queuedepth] --direct=1 --ramp_time=30 --runtime=300 --time_based --overwrite=1 --thread --group_reporting --output-format=json",
        ...
    }
},
{
    "Type": "FioExecutor",
    "Parameters": {
        "Scenario": "RandomWrite_8k_BlockSize",
        "CommandLine": "--name=fio_randwrite_[filesize]_4k_d[queuedepth]_th[threads] --size=[filesize] --numjobs=[threads] --rw=randwrite --bs=4k --iodepth=[queuedepth] --direct=1 --ramp_time=30 --runtime=300 --time_based --overwrite=1 --thread --group_reporting --output-format=json",
        ...
    }
},
{
    "Type": "FioExecutor",
    "Parameters": {
        "Scenario": "RandomWrite_12k_BlockSize",
        "CommandLine": "--name=fio_randwrite_[filesize]_4k_d[queuedepth]_th[threads] --size=[filesize] --numjobs=[threads] --rw=randwrite --bs=4k --iodepth=[queuedepth] --direct=1 --ramp_time=30 --runtime=300 --time_based --overwrite=1 --thread --group_reporting --output-format=json",
        ...
    }
}
```


To exclude scenarios, supply a set of scenario names on the command line delimited by a comma or semi-colon to exclude as is illustrated below. These scenario names should have a minus (-) sign in
front of the name (e.g. -RandomWrite_8k_BlockSize). Note that in the case where there are scenarios included as well as excluded having the same name, included scenarios take priority 
(e.g. --scenarios=scenario1,-scenario1 -> scenario1 will be included).


```
# Exclude the RandomWrite_4k_BlockSize and RandomWrite_8k_BlockSize scenarios. Run everything else.
VirtualClient.exe --profile=PERF-IO-FIO.json --timeout=03:00:00 --packages="{BlobStoreConnectionString|SAS URI}" --scenarios=-RandomWrite_4k_BlockSize,-RandomWrite_8k_BlockSize
```

### Scenario: Running a Custom Profile on the File System
Virtual Client can run profiles that are not necessarily part of the original release package. For example, a user can create a custom profile and place it in a folder that is not in the Virtual Client application
folder itself. This is very helpful for debugging scenarios (see the section at the bottom of the [Developer Guide](../developing/develop-guide.md) for examples). The one requirement for this scenario is that the actions, monitors or
dependencies in the custom profile must be known to the version of the Virtual Client runtime executable being used to run the profile.


```
# Run a custom profile from somewhere else on the file system.
# On Windows
VirtualClient.exe --profile=S:\source\debugging\CUSTOM-PROFILE.json --timeout=180 --packages="{BlobStoreConnectionString|SAS URI}"

# On Linux
./VirtualClient --profile=/home/user/source/debugging/CUSTOM-PROFILE.json --timeout=03:00:00 --packages="{BlobStoreConnectionString|SAS URI}"
```

---

### Scenario: Running a Custom Profile Downloaded from a URI
Virtual Client can run profiles that are downloaded from a URI. For example, a user can create a custom profile and upload it to an Azure storage account. This is very helpful for enabling quick support for new variations
of a profile without needing to recompile the Virtual Client application. One requirement for this scenario is that the actions, monitors or dependencies in the custom profile must be known to the version of the 
Virtual Client runtime executable being used to run the profile. The second requirement is that the profile resource at the URI location must allow for anonymous access. Virtual Client does not supply any credentials
to the target endpoint. For example in an Azure storage account, a blob container can be created with "Blob Anonymous Read" access. This access allows a user to download the profile/blob but he/she cannot scan the container
or modify the blob.


```
# Run a custom profile downloaded from a URI location.
VirtualClient.exe --profile=https://any.blob.core.windows.net/profiles/CUSTOM-PROFILE.json --timeout=180 --packages="{BlobStoreConnectionString|SAS URI}"
```

---

### Scenario: Running a Custom Monitoring Profile
There are cases where the user wants to run a specific set of monitors behind the scenes while workloads are running. The Virtual Client application allows
the user to define a specific monitoring profile on the command line in conjunction with the workload profile.


```
VirtualClient.exe --profile=PERF-NETWORK.json --timeout=180 --packages="{BlobStoreConnectionString|SAS URI}" --parameters="ProfilingEnabled=true,,,ProfilingMode=Interval"
```

---