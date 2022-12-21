# Install MySQL Database
Virtual Client has dependency components that can be added to a workload or monitor profile to create and configure a MySQL database on the system. The following section illustrates the
details for integrating this into the profile.

## Preliminaries
Reference the following documentation before proceeding.

* [Install MySQL](https://microsoft.github.io/VirtualClient/docs/dependencies/install-mysql)

## Supported Platform/Architectures
* linux-x64
* linux-arm64
* win-x64
* win-arm64

## Parameters
| **Parameter** | **Required** | **Description**                                                                                                 |
|---------------|--------------|-----------------------------------------------------------------------------------------------------------------|
| Action        | Yes          | Informs of action to take on server configuration.                                                              |
| DatabaseName  | Yes          | Name of database used by the workload.                                                                          |
| Scenario      | No           | Name for telemetry purpose. Does not change functionality.                                                      |

## Actions
| **Action**                 | **Description**                                                                                                 |
|----------------------------|-----------------------------------------------------------------------------------------------------------------|
| StartDatabaseServer        | Starts the MySQL server.                                                                                        |
| CreateDatabase             | Creates a database under {Database Name} in the MySQL server.                                                   |
| RaisedStatementCount       | Increases number of statements MySQL can prepare (only recommended for larger VMs).                             |

## Example
Here is the usage of the dependency by the Sysbench OLTP workload as an example, that uses all three actions to configure the server.

* [Profile Example](https://github.com/microsoft/VirtualClient/blob/main/src/VirtualClient/VirtualClient.Main/profiles/PERF-MYSQL-SYSBENCH-OLTP.json)

<div class="code-section">

```json
{
    "Type": "MySQLServerConfiguration",
    "Parameters": {
    "Scenario": "StartDatabaseServer",
    "Action": "StartServer",
    "DatabaseName": "$.Parameters.DatabaseName",
    "Role":  "Server"
    }
},
{
    "Type": "MySQLServerConfiguration",
    "Parameters": {
    "Scenario": "CreateDatabase",
    "Action": "CreateDatabase",
    "DatabaseName": "$.Parameters.DatabaseName",
    "Role": "Server"
    }
},
{
    "Type": "MySQLServerConfiguration",
    "Parameters": {
    "Scenario": "RaisedStatementCount",
    "Action": "RaisedStatementCount",
    "DatabaseName": "$.Parameters.DatabaseName",
    "Role": "Server"
    }
}
```
</div>
