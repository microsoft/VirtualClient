# Format Disks
Virtual Client has a dependency component that can be added to a workload or monitor profile to format disks before execution. The following section illustrates the
details for integrating this into the profile.

## Supported Platform/Architectures
* linux-x64
* linux-arm64
* win-x64
* win-arm64

## Examples
In this example, VC partitions unformatted disks on the system. Disks that have existing partitions/volumes are left alone, and the OS disk is never formatted.
Note: Due to limitations in DiskPart on Windows, we do not support initializing disks in parallel.

```json
{
    "Type": "FormatDisks"
},
```