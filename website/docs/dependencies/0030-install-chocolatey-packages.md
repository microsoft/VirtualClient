# Installation Chocolatey Packages
Install packages available in Chocolatey Package in Windows.

- [Chocolatey Official Page](https://chocolatey.org/)
- [Chocolatey Packages](https://community.chocolatey.org/packages)

:::info
This step depends on the [installation of Chocolatey](./0020-install-chocolatey.md).
:::

## Supported Platform/Architectures
* win-x64
* win-arm64

## Profile Component Parameters
The following section describes the parameters used by the individual component in the profile.

| **Parameter** | **Required** | **Description**                                                                                                 |
|---------------|--------------|-----------------------------------------------------------------------------------------------------------------|
| PackageName   | Yes          | The logical name of the Chocolatey package manager that will be registered with the Virtual Client runtime. The name commonly used is 'chocolatey'. |
| Packages      | Yes          | Comma delimitered list of packages to be installed via Chocolatey.                                              |
| Scenario      | No           | A name/identifier for the specific component in the profile. This is used for telemetry purposes only with components in dependency sections of the profile (i.e. cannot be used with --scenarios option on the command line). |


## Examples
In this example, VC install cygwin and wget package via Chocolatey.

```json
{
    "Type": "ChocolateyPackageInstallation",
    "Parameters": {
    "Scenario": "InstallCygwinOnWindows",
    "PackageName": "chocolatey",
    "Packages": "cygwin,wget"
    }
}
```