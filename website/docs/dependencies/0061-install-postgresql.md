# Install PostgreSQL Database
Virtual Client has dependency components that can be added to a workload or monitor profile to create a PostgreSQL database on the system. The following section illustrates the
details for integrating this into the profile.

- [PostgreSQL Documentation](https://learn.microsoft.com/en-us/dotnet/core/sdk)

## Supported Platform/Architectures
* linux-x64
* linux-arm64
* win-x64
* win-arm64

## Profile Component Parameters
The following section describes the parameters used by the individual component in the profile.

| **Parameter** | **Required** | **Description**                                                                                                 |
|---------------|--------------|-----------------------------------------------------------------------------------------------------------------|
| Password      | No          | The password for the PostgreSQL server under test. Default is the superuser password defined within the package. |

## Example
Here is the usage of the dependency by the PostgreSQL/HammerDB workload as an example. The Postgresql dependency is responsible for downloading the service, but the workload itself is responsible for creating the database and populating it.

* [Profile Example](https://github.com/microsoft/VirtualClient/blob/main/src/VirtualClient/VirtualClient.Main/profiles/PERF-SQL-POSTGRESQL.json)

```json
{
    "Type": "PostgreSQLInstallation",
    "Parameters": {
        "Scenario": "InstallPostgreSQLServer",
        "PackageName": "postgresql"
    }
}
```