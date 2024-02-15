# Wait Executor Dependency
Virtual Client has a dependency component that can be added to a workload or monitor profile to wait for given amount of time between actions/dependencies steps in VC. The following section illustrates the
details for integrating this into the profile.

## Supported Platform/Architectures
* linux-x64
* linux-arm64
* win-x64
* win-arm64

## Profile Component Parameters
The following section describes the parameters used by the individual component in the profile.

| **Parameter** | **Required** | **Description**                                                                                                 |
|---------------|--------------|-----------------------------------------------------------------------------------------------------------------|
| Duration      | No           | Default= "00:05:00". The amount of time required to wait.                      |
| Scenario      | No           | A name/identifier for the specific component in the profile. This is used for telemetry purposes only with components in dependency sections of the profile (i.e. cannot be used with --scenarios option on the command line). |

### Example
The following sections provides examples for how to integrate the component into a profile.

<div class="code-section">

```json
{
  "Description": "SPEC CPU 2017 Integer and Floating Point (SPECrate) Benchmark Workload",
  "MinimumExecutionInterval": "00:05:00",
  "MinimumRequiredExecutionTime": "02:00:00",
  "Metadata": {
    "RecommendedMinimumExecutionTime": "(4-cores)=02:00:00,(16-cores)=05:00:00,(64-cores)=10:00:00",
    "SupportedPlatforms": "linux-x64,linux-arm64,win-x64,win-arm64",
    "SupportedOperatingSystems": "CBL-Mariner,CentOS,Debian,RedHat,Suse,Ubuntu,Windows"
  },
  "Parameters": {
    "CompilerName": "gcc",
    "CompilerVersion": "10",
    "RunPeak": false,
    "BaseOptimizingFlags": "-g -O3 -march=native",
    "PeakOptimizingFlags": "-g -Ofast -march=native -flto",
    "Duration": "00:05:00"
  },
  "Actions": [
    {
      "Type": "SpecCpuExecutor",
      "Parameters": {
        "Scenario": "ExecuteSPECIntRateBenchmark",
        "CompilerVersion": "$.Parameters.CompilerVersion",
        "SpecProfile": "intrate",
        "PackageName": "speccpu2017",
        "RunPeak": "$.Parameters.RunPeak",
        "BaseOptimizingFlags": "$.Parameters.BaseOptimizingFlags",
        "PeakOptimizingFlags": "$.Parameters.PeakOptimizingFlags"
      }
    },
    {
      "Type": "WaitExecutor",
      "Parameters": {
        "Scenario": "WaitForTimeProvided",
        "Duration": "$.Parameters.Duration"
      }
    },
    {
      "Type": "SpecCpuExecutor",
      "Parameters": {
        "Scenario": "ExecuteSPECFPRateBenchmark",
        "CompilerVersion": "$.Parameters.CompilerVersion",
        "SpecProfile": "fprate",
        "PackageName": "speccpu2017",
        "RunPeak": "$.Parameters.RunPeak",
        "BaseOptimizingFlags": "$.Parameters.BaseOptimizingFlags",
        "PeakOptimizingFlags": "$.Parameters.PeakOptimizingFlags"
      }
    }
  ],
  "Dependencies": [
  {
      "Type": "WaitExecutor",
      "Parameters": {
        "Scenario": "WaitForTimeProvided",
        "Duration": "$.Parameters.Duration"
      }
    },
    {
      "Type": "ChocolateyInstallation",
      "Parameters": {
        "Scenario": "InstallChocolatey",
        "PackageName": "chocolatey"
      }
    }
  ]
}
```
</div>
