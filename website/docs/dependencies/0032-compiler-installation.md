# Compiler Installation
This dependency provides support for installing either the GCC or AOCC compiler, along with any appropriate packages.

## Supported Platform/Architectures
* linux-x64
* linux-arm64
* win-x64
* win-arm64

## Profile Component Parameters
The following section describes the parameters used by the individual component in the profile.

| **Parameter** | **Default Value** | **Description**                                                   |
|---------------|--------------|------------------------------------------------------------------------|
| CompilerName  | gcc          | Optional. The name of the compiler to be installed (ie. "gcc" or "aocc") |
| CompilerVersion | N/A        | Optional. The version of the compiler to be installed (e.g. 10) |
| CygwinPackages | N/A         | Optional. Windows Only. Comma-delimited list of packages that needs to be installed with cygwin (e.g. gcc-fortran,python3).
Note: VC automatically installs make & cmake with Cygwin. |

## Compilers Supported

| **CompilerName** | **Supported Platform/Architectures**                                   |
|------------------|------------------------------------------------------------------------|
|gcc               | linux-arm64,linux-x64,win-arm64,win-x64                                |
|aocc              | linux-arm64,linux-x64                                                  |
|charm++           | linux-arm64,linux-x64                                                  |

## Example
The following section describes the parameters used by the individual component in the profile.

* [Profile Example](https://github.com/microsoft/VirtualClient/blob/main/src/VirtualClient/VirtualClient.Main/profiles/PERF-SPECCPU-FPRATE.json)

```json
{
    "Type": "CompilerInstallation",
    "Parameters": {
        "Scenario": "InstallCompiler",
        "CompilerName": "gcc",
        "CompilerVersion": "10",
        "CygwinPackages": "gcc-g++,gcc-fortran,gcc,libiconv-devel"
    }
},
```