---
id: dependency-package
---

# Dependency Package Installation
Install packages from Azure Storage Account to VC package store.

## Parameters
| **Parameter** | **Required** | **Description**                                                                                                 |
|---------------|--------------|-----------------------------------------------------------------------------------------------------------------|
| BlobContainer | Yes          | Azure Blob container name.                                                                                      |
| BlobName      | Yes          | Azure Blob package name to download.                                                                            |
| PackageName   | Yes          | VC will auto register the downloaded path with package name. Other components can discover path with this name  |
| Scenario      | No           | Name for telemetry purpose. Does not change functionality.                                                      |
| Extract       | No           | Default to true. Auto extract zip files.                                                                        |


## Examples
- In this example, VC will download openssl.3.0.0.zip from the packages container from Azure Storage account, auto extract it, and register the extracted path as a `openssl` package. Other VC components like `OpenSSLExecutor` can directly reference this package by name `openssl`.
```json
{
    "Type": "DependencyPackageInstallation",
    "Parameters": {
        "Scenario": "InstallOpenSSLWorkloadPackage",
        "BlobContainer": "packages",
        "BlobName": "openssl.3.0.0.zip",
        "PackageName": "openssl",
        "Extract": true
    }
}
```


### Supported runtimes
win-x64, win-arm64, linux-x64, linux-arm64