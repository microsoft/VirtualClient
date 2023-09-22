# MsMPI Installation
Microsoft MPI (MS-MPI) is a Microsoft implementation of the Message Passing Interface standard for developing and running parallel applications on the Windows platform.

- [Official Documentation](https://learn.microsoft.com/en-us/message-passing-interface/microsoft-mpi)

## Supported Platform/Architectures
* win-x64
* win-arm64

## Profile Component Parameters
The following section describes the parameters used by the individual component in the profile.

| **Parameter** | **Required** | **Description**                                         |
|---------------|--------------|---------------------------------------------------------|
| PackageName   | Yes          | BlobStorage PackageName.                                |

## Example
The following section describes the parameters used by the individual component in the profile.

```json
    {
      "Type": "DependencyPackageInstallation",
      "Parameters": {
        "Scenario": "DownloadMSMPIPackage",
        "BlobContainer": "packages",
        "BlobName": "msmpi10.1.2.zip",
        "PackageName": "msmpi",
        "Extract": true
      }
    },
    {
      "Type": "MsmpiInstallation",
      "Parameters": {
        "Scenario": "InstallMSMPI",
        "PackageName": "msmpi"
      }
    },
```