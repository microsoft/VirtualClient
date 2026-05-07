# Stripe Disks
Virtual Client has a dependency component that can be added to a workload or monitor profile to stripe (RAID 0) multiple disks into a single volume before execution. 
The following section illustrates the details for integrating this into the profile.

:::caution
Do **NOT** format disks before striping. The `StripeDisks` component expects raw, unformatted disks. Formatting disks prior to striping will interfere with the 
RAID 0 array creation and may cause the operation to fail.
:::

## Prerequisite
This component requires the **system_config** package (version **1.1.0** or later) which contains the platform-specific striping scripts. Add a 
`DependencyPackageInstallation` step for `system_config` (>= 1.1.0) in the profile **before** the `StripeDisks` step.

## Supported Platform/Architectures
* linux-x64
* linux-arm64
* win-x64
* win-arm64

## Profile Component Parameters
The following section describes the parameters used by the individual component in the profile.

| **Parameter**    | **Required** | **Description**                                                                                                                 | **Default Value** |
|------------------|--------------|---------------------------------------------------------------------------------------------------------------------------------|-------------------|
| PackageName      | Yes          | The name of the package that contains the striping scripts (i.e. `system_config`).                                              |                   |
| DiskFilter       | No           | A filter expression used to select which disks are eligible for striping. The OS disk is excluded by default.                    | OSDisk:false      |
| DiskCount        | No           | The number of disks to include in the stripe set. When set to 0 all eligible disks are used.                                    | 0                 |
| MountPointPrefix | No           | A prefix for the mount point name of the resulting striped volume (Linux only). The volume is mounted as `{prefix}_raid0`.       | mnt               |
| MountLocation    | No           | The root directory under which the mount point is created (Linux only). If omitted, the path is determined from the current user.  |                   |

## Examples
In this example, all eligible (non-OS) disks are striped into a single RAID 0 volume. The `system_config` package must be installed first.

```json
{
    "Type": "DependencyPackageInstallation",
    "Parameters": {
        "Scenario": "InstallSystemConfigPackage",
        "BlobContainer": "packages",
        "BlobName": "system_config.1.1.0.zip",
        "PackageName": "system_config",
        "Extract": true
    }
},
{
    "Type": "StripeDisks",
    "Parameters": {
        "Scenario": "StripeDataDisks",
        "PackageName": "system_config"
    }
}
```

In this example, only the 4 largest non-OS disks are striped, with a custom mount point prefix.

```json
{
    "Type": "DependencyPackageInstallation",
    "Parameters": {
        "Scenario": "InstallSystemConfigPackage",
        "BlobContainer": "packages",
        "BlobName": "system_config.1.1.0.zip",
        "PackageName": "system_config",
        "Extract": true
    }
},
{
    "Type": "StripeDisks",
    "Parameters": {
        "Scenario": "StripeDataDisks",
        "PackageName": "system_config",
        "DiskCount": 4,
        "DiskFilter": "OSDisk:false",
        "MountPointPrefix": "data"
    }
}
```
