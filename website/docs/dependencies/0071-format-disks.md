# Mount Disks
Virtual Client has a dependency component that can be added to a workload or monitor profile to mount disks before execution. The following section illustrates the
details for integrating this into the profile. 

NOTE: It is going to mount the disk volumes which does not have any mount points and is already formatted. For example if the "Mount

## Supported Platform/Architectures
* linux-x64
* linux-arm64
* win-x64
* win-arm64

## Profile Component Parameters
The following section describes the parameters used by the individual component in the profile.

| **Parameter** | **Required** | **Description**                                         |
|---------------|--------------|---------------------------------------------------------|
| Version       | No           | The version of Docker to download and install.          |


## Examples
In this example, VC partitions unformatted disks on the system. Disks that have existing partitions/volumes are left alone, and the OS disk is never formatted.
Note: Due to limitations in DiskPart on Windows, we do not support initializing disks in parallel.

```json
{
    "Type": "FormatDisks"
},
```