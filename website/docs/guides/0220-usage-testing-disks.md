# Usage: Testing Disks
VC automates disk formating and run storage workloads on the target systems. We documented the rules and the mechanism we used to identify disks to run workloads on.

## Disk Mount Points
On certain systems, Virtual Client may need to create mount points on each of the disks in order to have both permissions and a path to
operate disk I/O work. In general, Virtual Client must create mount points when the following is true:

* Disks exist that do not have file system partitions/volumes (i.e. uninitialized disks).
* Disks exist with file system partitions/volumes but without any pre-existing mount points/access paths.

The remainder of this section documents where disk mount points are created on the system when required.

### Mount Points on Unix Systems
The following table illustrates the mount points that will be created when required on Unix systems. When logged in as the "root" on the system,
mount points will be created at the root of the file system directory structure. When logged in as a user other than "root"
(including when running as "sudo"), mount points will be created in the user home directory (e.g. /home/user)

| Logged In User | Disk/Device Paths | Mount Points/Paths |
|----------------|-------------------|-------------------------|
| root           | /dev/sdc1<br/>/dev/sdd1 | /mnt_dev_sdc1<br/>/mnt_dev_sdd1 |
| root           | /dev/sdc1<br/>/dev/sdc2<br/>/dev/sdd1<br/>/dev/sdd2 | /mnt_dev_sdc1<br/>/mnt_dev_sdc2<br/>/mnt_dev_sdd1<br/>/mnt_dev_sdd2 |
| non-root user  | /dev/sdc1<br/>/dev/sdd1 | /mnt_dev_sdc1<br/>/mnt_dev_sdd1 |
| non-root user  | /dev/sdc1<br/>/dev/sdc2<br/>/dev/sdd1<br/>/dev/sdd2 | /home/\{user\}/mnt_dev_sdc1<br/>/home/\{user\}/mnt_dev_sdc2<br/>/home/\{user\}/mnt_dev_sdd1<br/>/home/\{user\}/mnt_dev_sdd2 |

### Mount Points on Windows Systems
Mount points are NOT typically required on Windows systems once disks are formatted and have partitions/volumes. This is because Windows associates 
a letter with each volume (e.g. C:\, D:\) and this the disk volume + file system is accessible through this path.

<mark>Virtual Client does not currently support formatting disks beyond the first 26 disks due to inherent Windows limitations. Virtual Client can
operate on systems with more than 26 total partitions/volumes; however, these must be prepared before running the application</mark>

## General Disk Testing Guidelines
1. When targeting the OS disk, ONLY the OS partition for that disk will be targeted.
2. For systems having disks with more than 1 partition, all partitions will be targeted.
3. On Unix systems having disks with more than 1 partition, the following device paths are excluded: ```/```, ```/mnt```, ```/boot/efi```.
3. On Unix systems, device paths having the following prefixes are targeted: `/dev/hd`, `/dev/sd`, `/dev/nvme`, `/dev/xvd`.
4. On Windows systems, disks that are offline or read-only will not be targeted.

## Disk Filters
Different environments, Azure, AWS/GCP, Lab host, Lab VM, have vastly different configuration of disks. It is a challenge to have a schema that allows you to target the desired disk consistently.
Virtual Client implements a set of DiskFilters to accomplish this. 

Disk Filters are delimited by ampersand '&'. FilterName and FilterValues are delimited by colon, if applicable. 
For example, the following filter will return you the disks that are not OS Disk, and is between 3TB and 5TB in size. 
```bash
OSDisk:false&SizeGreaterThan:3TB&SizeLessThan:5TB
```
It is important to note that filters are calculated in-order and are non-commutative. In other words, the filter will be examined from left to right, in order. For example, if you have a small os disk and a larger data disk, 
"OSDisk:false&SmallestSize" will return you the data disk, and "SmallestSize&OSDisk:false" will return empty.

| FilterName      | Parameter                                      | Description                                                                     |
|-----------------|------------------------------------------------|---------------------------------------------------------------------------------|
| None            | N/A                                            | Return all the disks on system.                                                 |
| BiggestSize     | N/A                                            | Finds all the disks with the biggest size.                                      |
| SmallestSize    | N/A                                            | Finds all the disks with the smallest size.                                     |
| SizeGreaterThan | Long : size in bytes (Support units like 5 GB) | Will return disks greater or equal than defined size.                           |
| SizeLessThan    | Long : size in bytes (Support units like 4 TB) | Will return disks less or equal than defined size.                              |
| SizeEqualTo     | Long : size in bytes (Support units like 3 MB) | Will return disks with in ±1% range of defined size.                            |
| OSDisk          | Bool: include                                  | Default to true, which will return OSDisk only. Set to false to remove OS disk. |
| DiskPath        | string: Disk paths delimited by comma          | Return the disk which matches the provided disk path.                           |



### Default Filter
Default filter means getting the biggest disks.
```bash
BiggestSize
```

### Disk Filter Examples
Here we give some example filters for disk testing scenarios.

1. Test all the biggest disks except your OS disk. The `OSDisk:false` would be redundant if it's smaller than the disks you want to test.
```bash
BiggestSize&OSDisk:false
```

2. Testing the OS disk
```bash
OSDisk
```

3. Testing the smallest non-OS disk.
```bash
Smallest&OSDisk:false
```

4. Test specific disks on your machine with size.
This filter would return all the non-OS disk volumes with size of 4TB ±1%. If you want to test a specific disk with a unique size like 4TB, you can do filters that targets that disk.
```bash
SizeEqualTo:4TB&OSDisk:false
```

5. Test a Disk which has a specific path.
This filter will select the disk, which contains exact match of any specified disk path, in either Disk or Volume. For example, if D:/, E:/ are volumes on same disk and D:/ is provided, the entire disk will be selected.
```bash
DiskPath:C:/,D:/
DiskPath:/dev/sda,/dev/sdb
```
