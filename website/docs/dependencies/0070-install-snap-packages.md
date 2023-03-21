# Install Snap Packages
Install packages available using the Snap Package Manager.

- [Official Snap Documentation](https://snapcraft.io/docs/snap-tutorials)

:::info
Installing snap packages depends on the successful installation of the snapd service. Follow the example below to make sure the snapd service is installed correctly using the [LinuxPackageInstallation dependency](./0010-install-linux-packages.md) before adding this one.
:::

## Supported Platform/Architectures
* linux-x64
* linux-arm64

## Profile Component Parameters
The following section describes the parameters used by the individual component in the profile.

| **Parameter** | **Required** | **Description**                                                                                                 |
|---------------|--------------|-----------------------------------------------------------------------------------------------------------------|
| Packages      | Yes          | Comma delimitered list of packages to be installed via snap.                                              |
| Scenario      | No           | A name/identifier for the specific component in the profile. This is used for telemetry purposes only with components in dependency sections of the profile (i.e. cannot be used with --scenarios option on the command line).                                                      |

## Examples
In this example, VC installs the snapd service and a few packages using both the SnapPackageInstallation and LinuxPackageInstallation dependencies.

For SUSE distributions, confirm that the version in the zypper repository link matches the one that is being used (ie. Leap 15.4 vs. Leap 15.2 vs. Tumbleweed).

```json
{
    "Type": "LinuxPackageInstallation",
    "Parameters": {
        "Packages-Apt": "snapd",
        "Packages-Dnf": "snapd",
        "Repositories-Yum": "ngompa/snapcore-el7",
        "Packages-Yum": "snapd",
        "Repositories-Zypper": "https://download.opensuse.org/repositories/system:/snappy/openSUSE_Leap_15.4 snappy",
        "Packages-Zypper": "snapd"
    }
},
{
    "Type": "SnapPackageInstallation",
    "Parameters": {
        "Scenario": "InstallDependenciesWithSnap",
        "Packages": "package-1,package-2"
    }
}
```