# PostgreSQL Workload Profiles
The following profiles run customer-representative or benchmarking scenarios using the postgresql workload.

* [Workload Details](./postgresql.md) 
* [Client/Server Workloads](../../guides/0020-client-server.md)

## Client/Server Topology Support
PostgreSQL workload profiles support running the workload on both a single system as well as in a client/server topology. This means that the workload supports operation on a single system or on 2 distinct systems. The client/server topology is typically used when it is desirable to include a network component in the
overall performance evaluation. In a client/server topology, one system operates in the 'Client' role making calls to the system operating in the 'Server' role. 
The Virtual Client instances running on the client and server systems will synchronize with each other before running the workload. In order to support a client/server topology,
an environment layout file MUST be supplied to each instance of the Virtual Client on the command line to describe the IP address/location of other Virtual Client instances. An environment layout file is not required for the single system topology.

* [Environment Layouts](../../guides/0020-client-server.md)

In the environment layout file provided to the Virtual Client, define the role of the client system/VM as "Client" and the role of the server system(s)/VM(s) as "Server".
The spelling of the roles must be exact. The IP addresses of the systems/VMs must be correct as well. The following example illustrates the
idea. The name of the client must match the name of the system or the value of the agent ID passed in on the command line.

``` bash
# Single System (environment layout not required)
./VirtualClient --profile=PERF-POSTGRESQL-HAMMERDB-TPCC.json --system=Juno --timeout=1440

# Multi-System
# On Client Role System...
./VirtualClient --profile=PERF-POSTGRESQL-HAMMERDB-TPCC.json --system=Juno --timeout=1440 --clientId=Client01 --layoutPath=/any/path/to/layout.json

# On Server Role System...
./VirtualClient --profile=PERF-POSTGRESQL-HAMMERDB-TPCC.json --system=Juno --timeout=1440 --clientId=Server01 --layoutPath=/any/path/to/layout.json

# Example contents of the 'layout.json' file:
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

A tuned scenario may be selected by denoting it in the HammerDBExecutor.

``` bash
{
  "Type": "HammerDBExecutor",
  "Parameters": 
  {
    "Scenario": "CreatePostgreSQLDatabase",
    "DatabaseName": "$.Parameters.DatabaseName",
    "Workload": "tpcc",
    "SQLServer": "postgresql",
    "PackageName": "hammerdb",
    "VirtualUsers": "1",
    "WarehouseCount": "1",
    "Port": "$.Parameters.Port",
    "Role": "Server"
  }
},
```

## Profile Components
There are a lot of moving parts to this workload that allows for both out-of-box and configurable scenarios. Here are the key components to be aware of.

### Dependencies

* **FormatDisks** and **MountDisks**: Format any unformatted disks on the server, then mount any unmounted disks.
* **DependencyPackageInstallation**: On all VMs, download the hammerdb package; on the server-side, also download the postgresql package.
* **LinuxPackageInstallation**: Download the python3 package to execute the python scripts provided for installation and configuration of the packages.
* **PostgreSQLServerInstallation and PostgreSQLServerConfiguration**: Runs python scripts to install and configure the PostgreSQL server (ie. set up network, database, variables, and users).
* **ApiServer**: Starts the API server for Client-Server workloads.

### Actions

* **HammerDBExecutor**: Populates the PostgreSQL database using the HammerDB tool. The below setup creates the database with 1 warehouse, then distributes the the various tables onto different data disks mounted on the system, as it copies the tables and their schemas into new tables. From there, HammerDB can add additional warehouses to further populate the database tables. Once populated, VC persists the state, and it will not drop or recreate tables.

``` bash
{
  "Type": "HammerDBExecutor",
  "Parameters": 
  {
    "Scenario": "CreatePostgreSQLDatabase",
    "DatabaseName": "$.Parameters.DatabaseName",
    "Workload": "tpcc",
    "SQLServer": "postgresql",
    "PackageName": "hammerdb",
    "VirtualUsers": "1",
    "WarehouseCount": "1",
    "Port": "$.Parameters.Port",
    "Role": "Server"
  }
},
{
  "Type": "PostgreSQLServerConfiguration",
  "Parameters": 
  {
    "Scenario": "DistributePostgreSQLDatabase",
    "Action": "DistributeDatabase",
    "DatabaseName": "$.Parameters.DatabaseName",
    "PackageName": "postgresql",
    "Port": "$.Parameters.Port",
    "Role": "Server"
  }
},
{
  "Type": "HammerDBExecutor",
  "Parameters": 
  {
    "Scenario": "PopulatePostgreSQLDatabase",
    "DatabaseName": "$.Parameters.DatabaseName",
    "Workload": "tpcc",
    "SQLServer": "postgresql",
    "PackageName": "hammerdb",
    "VirtualUsers": "$.Parameters.VirtualUsers",
    "WarehouseCount": "$.Parameters.WarehouseCount",
    "Port": "$.Parameters.Port",
    "Role": "Server"
  }
},
```

* **SysbenchClientExecutor**: Runs a given workload from the client-side on the server database. Note that this action can run with different arguments from HammerDBExecutor. VC does not support dropping and recreating a new database or table configuration within the same profile or system.
* **SysbenchServerExecutor**: Sets the server online for client interaction.

## PERF-POSTGRESQL-HAMMERDB-TPCC.json
Runs the Postgresql workload against to HammerDB tool which generate various network traffic patterns against a Postgresql server. Although this is the default client workload.

* [Workload Profile](https://github.com/microsoft/VirtualClient/blob/main/src/VirtualClient/VirtualClient.Main/profiles/PERF-POSTGRESQL-HAMMERDB-TPCC.json) 

* **Supported Platform/Architectures**
  * linux-x64

* **Supports Disconnected Scenarios**  
  * No. Internet connection required.

* **Dependencies**  
  The dependencies defined in the 'Dependencies' section of the profile itself are required in order to run the workload operations effectively.
  * Internet connection.
  * The IP addresses defined in the environment layout (see above) for the Client and Server systems must be correct.
  * The name of the Client and Server instances defined in the environment layout must match the agent/client IDs supplied on the command line (e.g. --agentId)
    or must match the name of the system as defined by the operating system itself.

  Additional information on components that exist within the 'Dependencies' section of the profile can be found in the following locations:
  * [Installing Dependencies](https://microsoft.github.io/VirtualClient/docs/category/dependencies/)

* **Profile Parameters**  
  The following parameters can be optionally supplied on the command line to modify the behaviors of the workload. The System Memory (in Megabytes) is represented by the variable "mem" in the formulaic defaults listed below.

  | Parameter                 | Purpose                                                                         | Default value |
  |---------------------------|---------------------------------------------------------------------------------|---------------|
  | DatabaseName     | Optional. Provide the name of the database under test. | "tpcc" |
  | Port     | Optional. Provide the port number that PostgreSQL server will listen on. | 5432 |
  | Workload              | Required. Normally "tpcc"                                          | N/A          |
  | SQLServer              | Required. Normally "postgresql"                                          | N/A          |
  | Port              | Required. Normally "5432"                                          | N/A          |
  | VirtualUsers              | Optional. Number of virtual users (threads) for the TPCC workload setup           |   vCPU    |
  | WarehouseCount             | Optional. Number of warehouses created.                         |  mem*15/800    |
  | SharedMemoryBuffer             | Optional. Number of warehouses created.                         |  mem*85/100   |

* **Profile Runtimes**  
  See the 'Metadata' section of the profile for estimated runtimes. These timings represent the length of time required to run a single round of profile actions. These timings can be used to determine minimum required runtimes for the Virtual Client in order to get results. These are often estimates based on the
  number of system cores.

* **Usage Examples**  
  The following section provides a few basic examples of how to use the workload profile. Additional usage examples can be found in the
  'Usage Scenarios/Examples' link at the top.

  ``` bash
  # When running on a single system (environment layout not required)
  ./VirtualClient --profile=PERF-POSTGRESQL-HAMMERDB-TPCC.json --system=Demo --timeout=250 --packageStore="{BlobConnectionString|SAS Uri}"

  # When running in a client/server environment
  ./VirtualClient --profile=PERF-POSTGRESQL-HAMMERDB-TPCC.json --system=Demo --timeout=1440 --clientId=Client01 --layoutPath="/any/path/to/layout.json" --packageStore="{BlobConnectionString|SAS Uri}"
  ./VirtualClient --profile=PERF-POSTGRESQL-HAMMERDB-TPCC.json --system=Demo --timeout=1440 --clientId=Server01 --layoutPath="/any/path/to/layout.json" --packageStore="{BlobConnectionString|SAS Uri}"
  ```