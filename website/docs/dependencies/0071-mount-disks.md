# Mount Disks
Virtual Client has a dependency component that can be added to a workload or monitor profile to mount disks before execution. The following section illustrates the details for integrating this into the profile.

NOTE: It is going to mount the disk volumes which does not have any mount points and are already formatted.
It is going to name using the mount point prefix like following example: mountpointprefix0,mountpointprefix1,etc.

## Supported Platform/Architectures

* linux-x64
* linux-arm64
* win-x64
* win-arm64

## Profile Component Parameters
The following section describes the parameters used by the individual component in the profile.

| **Parameter**    | **Required** | **Description**                                                  | **Default Value**     |
|------------------|--------------|------------------------------------------------------------------|-----------------------|
| DiskFilter       | No           | The diskfilter will select the disks to be mounted.              | OSDisk:false          |
| MountPointPrefix | No           | This gives the prefix name for mount point names for volumes.    | mountPoint            |


## Examples
In this example, VC partitions unformatted disks on the system. Disks that have existing partitions/volumes are left alone, and the OS disk is never formatted.

```json
{
    "Type": "FormatDisks",
    "Parameters": {
        "Scenario": "InitializeDisks"
    }
},
{
    "Type": "MountDisks",
    "Parameters": {
        "Scenario": "MountDataDisk",
        "MountPointPrefix": "/mlperftraining",
        "DiskFilter":  "BiggestSize"
    }
}
```