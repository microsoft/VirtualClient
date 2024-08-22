# Run Commercial Workloads: Bring Your Own Package
Virtual Client supports running commercial workloads. However, we can not distribute the binary and licenses for the commercial workloads. In those cases, users need to "bring their own binary and license".

:::warning
*The Virtual Client team is currently working to define and document the process for integration of commercial workloads into the Virtual Client.
The contents of this document are NOT complete and are meant only to illustrate the basic concepts. Please bear with us while we are figuring this
process out.*
:::

## Supported commercial workloads
The following workloads are commercial (requiring purchase and/or license) software supported by the Virtual Client.

* **[Geekbench](../workloads/geekbench/geekbench.md)**
* **[SPECcpu](../workloads/speccpu/speccpu.md)**
* **[SPECjbb](../workloads/specjbb/specjbb.md)**
* **[SPECpower](../workloads/specpower/specpower.md)**  

## Supporting Commercial Workloads
The following sections describe how the process of integrating commercial workloads into the Virtual Client works. With commercial workloads, the user must
have purchased the software or the license and have created a VC package for integration with the runtime platform. The following steps describe how this typically
comes together.

## Step 1: VirtualClient Downloads Your Package
### .vcpkg file
.vcpkg is just a json file in vcpkg extension, that VC uses to register package information. While in many cases this file is optional, it is 
highly recommended to make a .vcpkg file when preparing your own package. The file has one required property `name`, and optional properties

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

### Prepare your package
- There are generally two types of packages: Post-compile, and others (pre-compile/no-compile-needed).
- Post-compile packages are generally OS or architecture specific, they need to be in their 
- The package structure could be workload-specific, refer to the workload documentation to see the packaging instructions.

### Upload your package to some storage
- Right now VC only supports Azure Storage Account
- We are open to support other major storage services. **Contributions welcomed!**

### Add package download to your VC profile

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

### Put packages in the packages directory with .vcpkg file.
User also have the option to put in workload packages under `virtualclient/packages` directory. Any directory with a `.vcpkg` inside will be registered as a VC package.

### Define alternative package directory using environment variable
A user of the Virtual Client can define an environment variable called `VC_PACKAGES_DIR`. This directory will be used as an override to the 
default 'packages' folder location.

It is recommended that package directory names ALWAYS be lower-cased (e.g. geekbench5 vs. Geekbench5).

```
e.g.
 # Windows example
  set VC_PACKAGES_DIR=C:\any\custom\packages\location

  C:\any\custom\packages\location\geekbench5
  C:\any\custom\packages\location\geekbench5\geekbench5.vcpkg
  C:\any\custom\packages\location\geekbench5\linux-x64
  C:\any\custom\packages\location\geekbench5\linux-arm64
  C:\any\custom\packages\location\geekbench5\win-x64
  C:\any\custom\packages\location\geekbench5\win-arm64

  # Linux example
  export VC_PACKAGES_DIR=/home/user/any/custom/packages/location

  /home/user/any/custom/packages/location/geekbench5
  /home/user/any/custom/packages/location/geekbench5/geekbench5.vcpkg
  /home/user/any/custom/packages/location/geekbench5/linux-x64
  /home/user/any/custom/packages/location/geekbench5/linux-arm64
  /home/user/any/custom/packages/location/geekbench5/win-x64
  /home/user/any/custom/packages/location/geekbench5/win-arm64
```
