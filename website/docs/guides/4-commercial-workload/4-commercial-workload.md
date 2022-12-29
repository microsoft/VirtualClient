---
id: commercial-workload
sidebar_position: 4
---

# Run Commercial Workloads: Bring Your Own Package
Virtual Client supports running commercial workloads. However, we can not distribute the binary and licenses for the commercial workloads. In those cases, users need to "bring their own binary and license".

## VirtualClient downloads your package
#### .vcpkg file
.vcpkg is just a json file in vcpkg extension, that VC uses to register package information. While in many cases this file is optional, it is highly recommended to make a .vcpkg file when preparing your own package. The file has one required property `name`, and optional properties

This example vcpkg file 
```json
{
    "name": "lshw",
    "description": "Hardware lister for Linux toolset.",
    "version": "B.02.19.59",
    "metadata": {
        "commit": "https://github.com/lyonel/lshw/commit/996aaad9c760efa6b6ffef8518999ec226af049a",
        "tags": "pre-release"
    }
}
```


#### Prepare your package
- There are generally two types of packages: Post-compile, and others (pre-compile/no-compile-needed).
- Post-compile packages are generally OS or architecture specific, they need to be in their 
- The package structure could be workload-specific, refer to the workload documentation to see the packaging instructions.

#### Upload your package to some storage
- Right now VC only supports Azure Storage Account
- We are open to support other major storage services. **Contributions welcomed!**

#### Add package download to your VC profile

    ```json
    {
      "Type": "DependencyPackageInstallation",
      "Parameters": {
        "Scenario": "InstallSPECcpuWorkloadPackage",
        "BlobContainer": "packages",
        "BlobName": "speccpu.2017.1.1.8.zip",
        "PackageName": "speccpu2017",
        "Extract": true
      }
    }
    ```

## Let VirtualClient discover your local packages

#### Put packages in the packages directory with .vcpkg file.
User also have the option to put in workload packages under `virtualclient/packages` directory. Any directory with a `.vcpkg` inside will be registered as a VC package.

#### Define alternative package directory using environment variable
A user of the Virtual Client can define an environment variable called **VCDependenciesPath**. This directory will be used
  to discover packages with the highest priority. If a package is not defined here, the Virtual Client will look for the package in the 
  locations noted below. If a package is found in this location, Virtual Client will not search for other locations. The package found
  here will be used.


  Package parent directory names should ALWAYS be lower-cased (e.g. geekbench5 vs. Geekbench5).

  ```
  e.g.
  set VCDependenciesPath=C:\any\custom\packages\location

  C:\any\custom\packages\location\geekbench5
  C:\any\custom\packages\location\geekbench5\linux-x64
  C:\any\custom\packages\location\geekbench5\linux-arm64
  C:\any\custom\packages\location\geekbench5\win-x64
  C:\any\custom\packages\location\geekbench5\win-arm64
  ```

## List of supported commercial workloads

#### [SPECcpu](../../workloads/speccpu/speccpu.md)
#### [SPECjbb](../../workloads/specjbb/specjbb.md)
#### [SPECpower](../../workloads/specpower/specpower.md)
#### [Geekbench](../../workloads/geekbench/geekbench.md)
