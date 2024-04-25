# Install MySQL
Virtual Client has a dependency component that can be added to a workload or monitor profile to install MySQL on the system. The following section illustrates the
details for integrating this into the profile.

## Supported Platform/Architectures
* linux-x64
* linux-arm64
* win-x64
* win-arm64

## Profile Component Parameters
The following section describes the parameters used by the individual component in the profile.

| **Parameter** | **Required** | **Description**                                                                                                 |
|---------------|--------------|-----------------------------------------------------------------------------------------------------------------|
| SkipInitialize | No          | Skips Installation of MySQL Server. |

## Example
The following sections provides examples for how to integrate the component into a profile on Linux systems.

* [Profile Example](https://github.com/microsoft/VirtualClient/blob/main/src/VirtualClient/VirtualClient.Main/profiles/PERF-MYSQL-SYSBENCH-OLTP.json)

```json
{
    "Type": "MySQLServerInstallation",
    "Parameters": {
        "Scenario": "InstallMySQLServer",
        "Action": "InstallServer",
        "PackageName": "mysql-server",
    }
}
```
