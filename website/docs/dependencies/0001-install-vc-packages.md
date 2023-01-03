# Install VC Packages
Virtual Client has a dependency component that can be added to a workload or monitor profile to install dependency packages from a package store. The following section illustrates the
details for integrating this into the profile.

## Preliminaries
Reference the following documentation on Virtual Client dependency packages for additional information on dependency packages.

* [VC Packages](../developing/0040-vc-packages.md)

## Supported Platform/Architectures
* linux-x64
* linux-arm64
* win-x64
* win-arm64

## Profile Component Parameters
The following section describes the parameters used by the individual component in the profile.

| **Parameter** | **Required** | **Description**                                                                                                 |
|---------------|--------------|-----------------------------------------------------------------------------------------------------------------|
| BlobContainer | Yes          | The name of the Azure storage account blob container where the package exists (e.g. packages).                  |
| BlobName      | Yes          | The name of the file/blob to download within the Azure storage account to download (e.g. openssl.3.0.0.zip).    |
| PackageName   | Yes          | The logical name of the package that will be registered with the Virtual Client runtime. This logical name typically matches the name defined within the *.vcpkg file for the package and is the name that other profile components can use to reference/discover the package and its location. |
| Scenario      | No           | A name/identifier for the specific component in the profile. This is used for telemetry purposes only with components in dependency sections of the profile (i.e. cannot be used with --scenarios option on the command line). |
| Extract       | No           | Default = true. True to instruct the Virtual Client that the package is an archive (e.g. .zip, .tgz) and to extract it. False if the file is a standalone file and should be left exactly as-is after download. |

### Example
The following sections provides examples for how to integrate the component into a profile.

* [Profile Example](https://github.com/microsoft/VirtualClient/blob/main/src/VirtualClient/VirtualClient.Main/profiles/PERF-CPU-OPENSSL.json)

<div class="code-section">

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
</div>
