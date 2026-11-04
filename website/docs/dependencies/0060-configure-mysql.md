---
slug: /dependencies/configure-mysql
---

# Configure MySQL Database
Virtual Client has dependency components that can be added to a workload or monitor profile to create and configure a MySQL database on the system. The following section illustrates the details for integrating this into the profile.

## Preliminaries
Reference the following documentation before proceeding.

* [Install MySQL](/docs/dependencies/install-mysql)

## Supported Platform-Architectures
* linux-x64
* linux-arm64
* win-x64
* win-arm64

## Profile Component Parameters
The following section describes the parameters used by the individual component in the profile.

| **Parameter** | **Required** | **Description**                                                                                                 |
|---------------|--------------|-----------------------------------------------------------------------------------------------------------------|
| Action        | Yes          | Informs of action to take on server configuration.                                                              |
| DatabaseName  | Yes          | Name of database used by the workload.                                                                          |
| Scenario      | No           | Name for telemetry purpose. Does not change functionality.                                                      |

## Actions
The following section describes the different options for the 'Action' parameter noted above.

| **Action**                 | **Description**                                                                                                 |
|----------------------------|-----------------------------------------------------------------------------------------------------------------|
| StartDatabaseServer        | Starts the MySQL server.                                                                                        |
| CreateDatabase             | Creates a database under \<Database Name> in the MySQL server.                                                  |
| RaisedStatementCount       | Increases number of statements MySQL can prepare (only recommended for larger VMs).                             |
| ConfigureNetwork           | Modifies the configuration file to make MySQL server accessible to the private network of VMs.                  |
| CreateUser                 | Creates user access to the server for each client VM in the provided environment layout.                        |

## HammerDB credentials

The Linux x64 MySQL HammerDB profiles opt in to `ConfigureCredentials` after
database creation and before HammerDB population. This action requires the updated
MySQL package containing `configure-mysql-credentials.py`. It creates a dedicated
`hammerdb` account with access to the workload database, without changing root
authentication or enabling password authentication for existing Sysbench profiles.

The configuration action generates a random password in `Password.json` in the
platform-specific MySQL package directory. HammerDB always reads this default
location. The JSON contains a non-empty `Password` string. The file is
created atomically with owner-only permissions (0600) and reused on retries.
Malformed or permissive files fail explicitly. Like other configuration actions,
this action is skipped after successful initialization. A missing file then fails
when HammerDB reads the credential rather than silently rotating it.

HammerDB receives the password only through the child process's
`MYSQLPASSWORD` environment variable. The MySQL Tcl templates resolve it at
runtime without writing it into generated Tcl, printing it, or persisting it
through HammerDB's `diset` command.
Job snapshots are also disabled for these MySQL processes to prevent credentials
from being persisted in HammerDB's job database.

For multi-VM layouts, `ConfigureCredentials` copies the same password to each
`--clientIps` address over authenticated SSH after configuring the MySQL accounts.
The client destination is the same absolute package path as on the server.
The profiles download the package on clients without installing MySQL there.
Files are published atomically with permissions `0600`, owned by the client package
directory's owner. Clients wait up to 40 minutes for the file before initializing
HammerDB. Missing client package directories are also waited for, with the same bound;
failed transfers fail credential configuration rather than saving success.

SSH keys and trusted host keys must already be configured for the account running
the script (**root** for this elevated MySQL action). Clients must allow that SSH
identity to run `sudo -n python3`. See the shared
[SSH provisioning requirements](./0061-install-postgresql.md#automatic-client-distribution).
VC does not use its unauthenticated coordination API to transfer credentials.
Standalone runs create only the local file and do not invoke SSH.

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
},
{
    "Type": "MySQLServerConfiguration",
    "Parameters": {
    "Scenario": "ConfigureNetwork",
    "Action": "ConfigureNetwork",
    "DatabaseName": "$.Parameters.DatabaseName",
    "Role": "Server"
    }
},
{
    "Type": "MySQLServerConfiguration",
    "Parameters": {
    "Scenario": "CreateUser",
    "Action": "CreateUser",
    "DatabaseName": "$.Parameters.DatabaseName",
    "Role": "Server"
    }
}
```
</div>
