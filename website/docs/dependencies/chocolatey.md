---
id: chocolatey
---

# Chocolatey Installation
Virtual Client installs chocolatey package manager on Windows. This step only installs chocolatey package manager itself. Checkout [`Chocolatey Package Installation`](./chocolatey-package.md) to see how to use Chocolatey to install choco packages.

- [Chocolatey Official Page](https://chocolatey.org/)
- [Chocolatey Packages](https://community.chocolatey.org/packages)

## Parameters
| **Parameter** | **Required** | **Description**                                                                                                 |
|---------------|--------------|-----------------------------------------------------------------------------------------------------------------|
| PackageName   | Yes          | VC will auto register the "choco.exe" as `choco` package. Other components can reference by this name           |
| Scenario      | No           | Name for telemetry purpose. Does not change functionality.                                                      |


## Examples
```json
{
    "Type": "ChocolateyInstallation",
    "Parameters": {
    "Scenario": "InstallChocolatey",
    "PackageName": "choco"
    }
}
```


### Supported runtimes
win-x64, win-arm64