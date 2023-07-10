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
| Packages      | Yes          | Comma-delimited list of packages to be installed via snap.                                              |
| Scenario      | No           | A name/identifier for the specific component in the profile. This is used for telemetry purposes only with components in dependency sections of the profile (i.e. cannot be used with --scenarios option on the command line).                                                      |
| AllowUpgrades | No          | True/False. If true, previously installed packages will be upgraded during the process of installation.  If false, the package will be skipped.        |

## Examples
In this example, VC installs the snapd service and a few packages using both the SnapPackageInstallation and LinuxPackageInstallation dependencies.

For SUSE distributions, confirm that the version in the zypper repository link matches the specific distribution that is being used on the VM (ie. Leap 15.4 vs. Leap 15.2 vs. Tumbleweed). More info on installing snapd on SUSE can be found [in the offical snap documentation](https://snapcraft.io/docs/installing-snap-on-opensuse).

For CentOS7 distributions, the epel-release repository must be installed before the snapd service. More info on that can be found [here](https://snapcraft.io/install/snapd/centos).
For RHEL7 distributions, the latest repository name can be found [here](https://snapcraft.io/install/snapd/rhel).

```json
{
    "Type": "LinuxPackageInstallation",
    "Parameters": {
        "Packages-Apt": "snapd",
        "Packages-Dnf": "snapd",
        "Packages-Yum": "epel-release,snapd",
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