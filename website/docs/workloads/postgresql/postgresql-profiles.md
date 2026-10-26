---
slug: /workloads/postgresql-profiles
---

# PostgreSQL Profiles
The following profiles run customer-representative or benchmarking scenarios using PostgreSQL.
Provision matching credentials on each participating VM before running the profiles;
see [PostgreSQL credentials](/docs/dependencies/install-postgresql#postgresql-server-configuration-credentials)
for password-file and `PGPASSWORD` precedence.

* [Workload Details](/docs/workloads/postgresql) 
* [Client/Server Workloads](/docs/guides/client-server)

## Client/Server Topology Support
PostgreSQL workload profiles support running the workload on both a single system as well as in a client/server topology. This means that the workload supports operation on a single system or on 2 distinct systems. The client/server topology is typically used when it is desirable to include a network component in the
overall performance evaluation. In a client/server topology, one system operates in the 'Client' role making calls to the system operating in the 'Server' role. 
The Virtual Client instances running on the client and server systems will synchronize with each other before running the workload. In order to support a client/server topology,
an environment layout file MUST be supplied to each instance of the Virtual Client on the command line to describe the IP address/location of other Virtual Client instances. An environment layout file is not required for the single system topology.

* [Environment Layouts](/docs/guides/client-server)

In the environment layout file provided to the Virtual Client, define the role of the client system/VM as "Client" and the role of the server system(s)/VM(s) as "Server".
The spelling of the roles must be exact. The IP addresses of the systems/VMs must be correct as well. The following example illustrates the
idea. The name of the client must match the name of the system or the value of the agent ID passed in on the command line.

``` bash
# Single System (environment layout not required)
./VirtualClient --profile=PERF-POSTGRESQL-HAMMERDB-TPCC.json --system=Juno --timeout=1440

# Multi-System
# On Client Role System...
./VirtualClient --profile=PERF-POSTGRESQL-HAMMERDB-TPCC.json --system=Juno --timeout=1440 --client-id=Client01 --layout=/any/path/to/layout.json

# On Server Role System...
./VirtualClient --profile=PERF-POSTGRESQL-HAMMERDB-TPCC.json --system=Juno --timeout=1440 --client-id=Server01 --layout=/any/path/to/layout.json
```

Example contents of `layout.json`:

```json
{
    "clients": [
        {
            "name": "Client01",
            "role": "Client",
            "ipAddress": "10.1.0.1"
        },
        {
            "name": "Server01",
            "role": "Server",
            "ipAddress": "10.1.0.2"
        }
    ]
}
```

## Configuring the TPCC Scenario

The TPCC scenario uses HammerDB to create and populate a PostgreSQL database with transactional workload patterns. Database creation and population can only be performed on the Server side.

HammerDB requires the schema-building `VirtualUserCount` to be no greater than
`WarehouseCount`. For a one-warehouse smoke test, set both parameters to `1`.

**Important**: The profile's `StripeDisks` dependency prepares the server storage selected
by `DiskFilter`. `PostgreSQLServerConfiguration` selects the data directory on mounted
storage; HammerDB populates tables in that database rather than distributing tables itself.

```json
{
  "Type": "HammerDBExecutor",
  "Parameters": 
  {
    "Scenario": "PopulatePostgreSQLDatabase",
    "DatabaseName": "$.Parameters.DatabaseName",
    "Workload": "tpcc",
    "SQLServer": "postgresql",
    "PackageName": "hammerdb",
    "VirtualUserCount": "{calculate({LogicalCoreCount})}",
    "WarehouseCount": "{calculate({LogicalCoreCount} * 10)}",
    "Duration": "$.Parameters.Duration",
    "Port": "$.Parameters.Port",
    "Role": "Server"
  }
}
```

## Configuring the TPCH Scenario

The TPCH scenario uses HammerDB to create and populate a PostgreSQL database with analytical workload patterns (decision support queries). Like TPCC, database creation and population can only be performed on the Server side.

**Important**: Use `DiskFilter` to select the server storage prepared by the profile's
`StripeDisks` dependency. HammerDB does not distribute tables across individual disks.

```json
{
  "Type": "HammerDBExecutor",
  "Parameters": 
  {
    "Scenario": "PopulatePostgreSQLDatabase",
    "DatabaseName": "$.Parameters.DatabaseName",
    "Workload": "tpch",
    "SQLServer": "postgresql",
    "PackageName": "hammerdb",
    "VirtualUserCount": "{calculate({LogicalCoreCount})}",
    "ScaleFactor": "10",
    "Port": "$.Parameters.Port",
    "Duration": "$.Parameters.Duration",
    "Role": "Server"
  }
}
```

## Profile Components
There are a lot of moving parts to this workload that allows for both out-of-box and configurable scenarios. Here are the key components to be aware of.

### Dependencies

* **StripeDisks**: Prepares and mounts the selected server disks as striped storage.
* **DependencyPackageInstallation**: Downloads the `postgresql` package on all participating VMs, and the `hammerdb` or `sysbench` package required by the profile. The default password-file location is in the platform-specific `postgresql` directory on each VM.
* **LinuxPackageInstallation**: Download the python3 package to execute the python scripts provided for installation and configuration of the packages.
* **PostgreSQLServerInstallation and PostgreSQLServerConfiguration**: Runs python scripts to install and configure the PostgreSQL server (ie. set up network, database, variables, and users).
* **ApiServer**: Starts the API server for Client-Server workloads.

### Actions

* **HammerDBExecutor**: Creates and populates the benchmark database on the server using HammerDB. Once populated, VC persists the state and skips repeated population. This component supports TPCC (transactional) and TPCH (analytical) scenarios; server storage is prepared by the profile's dependencies.

* **HammerDBServerExecutor**: Signals server readiness and coordinates with clients during the workload.

* **HammerDBClientExecutor**: Runs the workload from the client-side, generating load against the server database. This executor connects to the server and executes the specified workload (TPCC or TPCH) scenarios.

* **SysbenchConfiguration**: Populates benchmark tables or cleans them up with the `Cleanup` action. Clean up existing populated tables before changing their configuration.
* **SysbenchClientExecutor**: Runs the selected Sysbench workload against the server database.
* **SysbenchServerExecutor**: Sets the server online for client interaction.

## PERF-POSTGRESQL-HAMMERDB-TPCC.json
Runs the PostgreSQL workload against the HammerDB tool which generates various transactional traffic patterns against a PostgreSQL server using the TPCC (Transaction Processing Performance Council) benchmark.

* [Workload Profile](https://github.com/microsoft/VirtualClient/blob/main/src/VirtualClient/VirtualClient.Main/profiles/PERF-POSTGRESQL-HAMMERDB-TPCC.json)

## PERF-POSTGRESQL-HAMMERDB-TPCH.json
Runs the PostgreSQL workload against the HammerDB tool which generates analytical workload patterns against a PostgreSQL server using the TPCH (Transaction Processing Performance Council Benchmark H) for decision support scenarios.

* [Workload Profile](https://github.com/microsoft/VirtualClient/blob/main/src/VirtualClient/VirtualClient.Main/profiles/PERF-POSTGRESQL-HAMMERDB-TPCH.json) 

* **Supported Platform-Architectures**
  * linux-x64

* **Dependencies**  
  The dependencies defined in the 'Dependencies' section of the profile itself are required in order to run the workload operations effectively.
  * Internet connection.
  * The IP addresses defined in the environment layout (see above) for the Client and Server systems must be correct.
  * The name of the Client and Server instances defined in the environment layout must match the agent/client IDs supplied on the command line (e.g. --client-id)
    or must match the name of the system as defined by the operating system itself.

  Additional information on components that exist within the 'Dependencies' section of the profile can be found in the following locations:
  * [Installing Dependencies](/docs/category/dependencies/)

* **Profile Parameters**  
  The following parameters can be optionally supplied on the command line to modify the behaviors of the workload. The System Memory (in Megabytes) is represented by the variable "mem" in the formulaic defaults listed below.

  | Parameter                 | Purpose                                                                         | Default value |
  |---------------------------|---------------------------------------------------------------------------------|---------------|
  | DatabaseName     | Optional. Provide the name of the database under test. | "hammerdb_tpcc" or "hammerdb_tpch" |
  | Port     | Optional. Provide the port number that PostgreSQL server will listen on. | 5432 |
  | DiskFilter     | Optional. Filter selecting the server disks to stripe and mount. | "osdisk:false&sizegreaterthan:256g" |
  | Duration     | Optional. Duration for the workload execution. | "00:20:00" |
  | VirtualUserCount          | Optional. Number of virtual users (threads) for the workload setup           |   vCPU    |
  | WarehouseCount             | Optional. Number of warehouses created (TPCC only).                         |  vCPU*10    |
  | ScaleFactor             | Optional. Scale factor for TPCH workload.                         |  10    |
  | SharedMemoryBuffer             | Optional. PostgreSQL shared memory buffer size.                         |  mem*85/100   |

* **Profile Runtimes**  
  See the 'Metadata' section of the profile for estimated runtimes. These timings represent the length of time required to run a single round of profile actions. These timings can be used to determine minimum required runtimes for the Virtual Client in order to get results. These are often estimates based on the
  number of system cores.

* **Usage Examples**  
  The following section provides a few basic examples of how to use the workload profile. Additional usage examples can be found in the
  'Usage Scenarios/Examples' link at the top.

  ``` bash
  # When running on a single system (environment layout not required)
  ./VirtualClient --profile=PERF-POSTGRESQL-HAMMERDB-TPCC.json --system=Demo --timeout=250

  # When running in a client/server environment
  ./VirtualClient --profile=PERF-POSTGRESQL-HAMMERDB-TPCC.json --system=Demo --timeout=1440 --client-id=Client01 --layout="/any/path/to/layout.json"
  ./VirtualClient --profile=PERF-POSTGRESQL-HAMMERDB-TPCC.json --system=Demo --timeout=1440 --client-id=Server01 --layout="/any/path/to/layout.json"
  ```