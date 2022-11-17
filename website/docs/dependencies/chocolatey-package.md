---
id: chocolatey-package
---

# Chocolatey Package Installation
Install packages available in Chocolatey Package in Windows.

- [Chocolatey Official Page](https://chocolatey.org/)
- [Chocolatey Packages](https://community.chocolatey.org/packages)

:::info
This step depends on [Chocolatey Installation](./chocolatey.md).
:::

## Parameters
| **Parameter** | **Required** | **Description**                                                                                                 |
|---------------|--------------|-----------------------------------------------------------------------------------------------------------------|
| PackageName   | Yes          | Reference to the Chotolatey package manager installed by `Chocolatey Installation` step, typically just `choco`.|
| Packages      | Yes          | Comma delimitered list of packages to be installed via Chocolatey.                                              |
| Scenario      | No           | Name for telemetry purpose. Does not change functionality.                                                      |


## Examples
In this example, VC install cygwin and wget package via Chocolatey.
```json
{
    "Type": "ChocolateyPackageInstallation",
    "Parameters": {
    "Scenario": "InstallCygwinOnWindows",
    "PackageName": "choco",
    "Packages": "cygwin,wget"
    }
}
```


### Supported runtimes
win-x64, win-arm64