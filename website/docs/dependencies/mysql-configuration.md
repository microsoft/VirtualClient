---
id: mysql-configuration
---

# MySQL Server Configuration
Virtual Client can configure a MySQL server should the workload require it.

:::info
This dependency does not download the MySQL server itself. That can be done using [Chocolatey Package Installation](https://microsoft.github.io/VirtualClient/docs/dependencies/chocolatey-package/) or [Linux Package Installation](https://microsoft.github.io/VirtualClient/docs/dependencies/linux-package-installation/), depending on your platform.
:::

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

### Supported runtimes
win-x64, win-arm64, linux-x64, linux-arm64
