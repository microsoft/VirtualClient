# Install CUDA and NVIDIA GPU Drivers
Virtual Client has a dependency component that can be added to a workload or monitor profile to install CUDA and NVIDIA drivers in Linux and Windows systems. The following section illustrates the
details for integrating this into the profile.

- [NVIDIA Official Drivers Page](https://www.nvidia.com/Download/index.aspx)
- [CUDA Toolkit Downloads](https://developer.nvidia.com/cuda-downloads)

## Supported Platform/Architectures
* linux-x64 (Ubuntu, Debian, CentOS7, RHEL7, RHEL8, SUSE)
* win-x64

## Profile Component Parameters for Windows
This dependency component can be used to download the drivers on Windows either from Web using Wget, or from a blob storage using the DependencyPackageInstallation. 
The following section describes the parameters used by the individual component in the profile in Windows:

| **Parameter** | **Required** | **Description**            |                 **Default**                     |
|---------------|--------------|----------------------------|-------------------------------------------------|
| PackageName   | Yes          | The logical name of the package that will be registered with the Virtual Client runtime. This name can be used by other profile components to reference the installation parent directory location for Drivers. |  |
| Scenario      | No           | A name/identifier for the specific component in the profile. This is used for telemetry purposes only with components in dependency sections of the profile (i.e. cannot be used with --scenarios option on the command line). |  |
| RebootRequired | No | Whether or not reboot is required after installing the drivers. | false |

## Profile Component Parameters for Linux
The following section describes the parameters used by the individual component in the profile in Windows:

| **Parameter** | **Required** | **Description**            |                 **Default**                     |
|---------------|--------------|----------------------------|-------------------------------------------------|
| PackageName   | Yes          | The logical name of the package that will be registered with the Virtual Client runtime. This name can be used by other profile components to reference the installation parent directory location for Drivers. |  |
| Scenario      | No           | A name/identifier for the specific component in the profile. This is used for telemetry purposes only with components in dependency sections of the profile (i.e. cannot be used with --scenarios option on the command line). |  |
| LinuxCudaVersion | Yes | The version of CUDA to be installed in Linux Systems |  |
| LinuxDriverVersion | Yes | The version of Nvidia GPU driver to be installed in Linux Systems |  |
| LinuxLocalRunFile | Yes | The link to local runfile to install Cuda and Nvidia GPU driver in Linux Systems | |
| Username | No | The user who has the ssh identity registered for. | The current logged in user. |
| RebootRequired | No | Whether or not reboot is required after installing the drivers. | true |

## Example
The following sections provides examples for how to integrate the component into a profile.
  
### Windows example for downloading drivers from Web
A sample URL for NVIDIA Drivers for Windows 10/11 is mentioned in example. The exact URL for the specific OS and Driver Version can be taken from NVIDIA Drivers website, given above.
  <div class="code-section">

  ```json
  {
      "Type": "WgetPackageInstallation",
      "Parameters": {
          "Scenario": "DownloadCudaAndNvidiaDriverUsingWget",
          "PackageUri": "https://us.download.nvidia.com/tesla/528.33/528.33-data-center-tesla-desktop-win10-win11-64bit-dch-international.exe",
          "PackageName": "nvidiaDrivers",
          "Extract": true
      }
  },
  {
      "Type": "CudaAndNvidiaGPUDriverInstallation",
      "Parameters": {
          "Scenario": "InstallCudaAndNvidiaGPUDriverForWindows",
          "RebootRequired": false,
          "PackageName": "nvidiaDrivers"  
      }
  }
  ```
  </div>
  
### Windows example for downloading drivers from Web

  <div class="code-section">

  ```json
{
      "Type": "DependencyPackageInstallation",
      "Parameters": {
          "Scenario": "DownloadCudaAndNvidiaDriverFromBlob",
          "BlobContainer": "packages",
          "BlobName": "<package-name-in-blob>",
          "PackageName": "nvidiaDrivers",
          "Extract": true
      }
  },
  {
      "Type": "CudaAndNvidiaGPUDriverInstallation",
      "Parameters": {
          "Scenario": "InstallCudaAndNvidiaGPUDriverForWindows",
          "RebootRequired": false,
          "PackageName": "nvidiaDrivers"  
      }
  }
  ```
  </div>
  

### Linux example for downloading drivers
A sample URL for NVIDIA Drivers RunFile for Linux Ubuntu is mentioned in example. The exact URL for the specific OS and Driver Version can be taken from CUDA Toolkit website, given above.
  <div class="code-section">

  ```json
  {
      "Type": "NvidiaCudaInstallation",
      "Parameters": {
          "Scenario": "InstallNvidiaCuda",
          "LinuxCudaVersion": "12.0",
          "LinuxDriverVersion": "525",
          "Username": "",
          "LinuxLocalRunFile": "https://developer.download.nvidia.com/compute/cuda/12.0.0/local_installers/cuda_12.0.0_525.60.13_linux.run"
      }
  },
  ```
  </div>