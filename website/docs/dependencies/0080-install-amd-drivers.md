# Install AMD GPU Drivers
Virtual Client has a dependency component that can be added to a workload or monitor profile to install AMD drivers in Linux and Windows systems. The following section illustrates the
details for integrating this into the profile.

- [AMD Official Drivers Page](https://www.amd.com/en/support)
- [AMD GPU Drivers SETUP Profile](https://github.com/microsoft/VirtualClient/blob/main/src/VirtualClient/VirtualClient.Main/profiles/SETUP-GPU-AMDDRIVERS.json)
- [Windows Drivers] (https://learn.microsoft.com/en-us/azure/virtual-machines/windows/n-series-amd-driver-setup)
- [Linux Drivers] (https://repo.radeon.com/amdgpu-install)

## Supported Platform/Architectures
* linux-x64 (Ubuntu)
* win-x64

## Profile Parameters for Windows
This dependency component can be used to download the drivers on Windows from a blob storage using the DependencyPackageInstallation. 
The following section describes the parameters used by the individual component in the profile in Windows:

| **Parameter** | **Required** | **Description**            |                 **Default**                     |
|---------------|--------------|----------------------------|-------------------------------------------------|
| PackageName   | Yes          | The logical name of the package that will be registered with the Virtual Client runtime. This name can be used by other profile components to reference the installation parent directory location for Drivers. |  |
| Scenario      | No           | A name/identifier for the specific component in the profile. This is used for telemetry purposes only with components in dependency sections of the profile (i.e. cannot be used with --scenarios option on the command line). |  |
| RebootRequired | No | Whether or not reboot is required after installing the drivers. | false |
| GpuModel | Yes | model of GPU | mi25 |

## Profile Parameters for Linux
The following section describes the parameters used by the individual component in the profile in Windows:

| **Parameter** | **Required** | **Description**            |                 **Default**                     |
|---------------|--------------|----------------------------|-------------------------------------------------|
| PackageName   | Yes          | The logical name of the package that will be registered with the Virtual Client runtime. This name can be used by other profile components to reference the installation parent directory location for Drivers. |  |
| Scenario      | No           | A name/identifier for the specific component in the profile. This is used for telemetry purposes only with components in dependency sections of the profile (i.e. cannot be used with --scenarios option on the command line). |  |
| LinuxInstallationFile | Yes | The link to installation file to install AMD GPU driver in Linux Systems | |
| Username | No | The user who has the ssh identity registered for. | The current logged in user. |
| RebootRequired | No | Whether or not reboot is required after installing the drivers. | true |

## Supported Windows GPUs
* mi25
* v620

## Supported Linux GPUs
* all

Note: Virtual Client is only tested with MI200x systems using installation file - https://repo.radeon.com/amdgpu-install/5.5/ubuntu/focal/amdgpu-install_5.5.50500-1_all.deb.

## Supported Linux Distros
* Ubunutu
