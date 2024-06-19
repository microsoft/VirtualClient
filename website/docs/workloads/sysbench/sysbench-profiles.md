# Sysbench Workload Profiles
The following profiles run customer-representative or benchmarking scenarios using the Sysbench OLTP workload.

* [Workload Details](./sysbench.md)  
* [Client/Server Workloads](../../guides/0020-client-server.md)

## Client/Server Topology Support
Sysbench OLTP workload profiles support running the workload on both a single system as well as in a client/server topology. This means that the workload supports
operation on a single system or on 2 distinct systems. The client/server topology is typically used when it is desirable to include a network component in the
overall performance evaluation. In a client/server topology, one system operates in the 'Client' role making calls to the system operating in the 'Server' role. 
The Virtual Client instances running on the client and server systems will synchronize with each other before running the workload. In order to support a client/server topology,
an environment layout file MUST be supplied to each instance of the Virtual Client on the command line to describe the IP address/location of other Virtual Client instances. An
environment layout file is not required for the single system topology.

* [Environment Layouts](../../guides/0020-client-server.md)

In the environment layout file provided to the Virtual Client, define the role of the client system/VM as "Client" and the role of the server system(s)/VM(s) as "Server".
The spelling of the roles must be exact. The IP addresses of the systems/VMs must be correct as well. The following example illustrates the
idea. The name of the client must match the name of the system or the value of the agent ID passed in on the command line.

``` bash
# Single System (environment layout not required)
./VirtualClient --profile=PERF-MYSQL-OLTP-SYSBENCH.json --system=Juno --timeout=1440

# Multi-System
# On Client Role System...
./VirtualClient --profile=PERF-MYSQL-OLTP-SYSBENCH.json --system=Juno --timeout=1440 --clientId=Client01 --layoutPath=/any/path/to/layout.json

# On Server Role System...
./VirtualClient --profile=PERF-MYSQL-OLTP-SYSBENCH.json--system=Juno --timeout=1440 --clientId=Server01 --layoutPath=/any/path/to/layout.json

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

## Balanced/In Memory Scenario Support
In addition to the standard configuration, Virtual Client offers two tuned scenarios (Balanced and In-Memory) and one configurable scenario (Configure) to run the 
Sysbench workload. A database scenario can be selected by denoting it in the profile, under both the MySQLConfiguration Dependency and the SysbenchConfiguration Action. The Default 
scenario is Balanced.

* **Balanced**  
  The database size is about twice as big as the memory/RAM on the system. Half of the database will fit in memory, and half will fit on disk.
  Target CPU usage is about 40-60% with somewhat heavy disk I/O usage. The configuration supports 1-5 additional data disks, and the database will be
  distributed among the disks as proportionately as possible. The default is 1 thread and 10^vCPU number of records.

* **InMemory**  
  The database size is just about the size of the memory/RAM on the system. Target CPU usage is about 80-90%, with a significant amount of disk I/O usage.

* **Configure**  
  Allows for user-defined table count, record size, and thread counts.

``` bash
{
  "Type": "SysbenchConfiguration",
  "Parameters": 
  {
    "Scenario": "oltp_read_write_T8_TB16_REC500",
    "BenchmarkName": "OLTP",
    "DatabaseName": "sbtest",
    "DatabaseScenario": "Configure",
    "Threads": "8",
    "NumTables": "16",
    "RecordCount": "500",
    "Duration": "00:20:00",
    "Workload": "oltp_read_write",
    "PackageName": "sysbench"
    "Role": "Server",
  }
},
```

## Profile Components
There are a lot of moving parts to this workload that allows for both out-of-box and configurable scenarios. Here are the key components to be aware of.

### Dependencies

* **FormatDisks** and **MountDisks**  
  Format any unformatted disks on the server, then mount any unmounted disks.

* **DependencyPackageInstallation**  
  On all VMs, download the sysbench package; on the server-side, also download the mysql-server package.

* **LinuxPackageInstallation**  
  Download the python3 package to execute the python scripts provided for installation and configuration of the packages.

* **MySQLServerInstallation and MySQLConfiguration**  
  Runs scripts to install and configure the MySQL server (ie. set up network, database, variables, and users).

* **ApiServer**  
  Starts the API server for Client-Server workloads.

### Actions

* **SysbenchConfiguration**  
  Populates the MySQL database using the Sysbench tool. Note that the default Sysbench settings does not populate a database on the data disks, but on the OS disk. Thus, the 
  recommended profile format is as follows. The below setup creates N tables in the database with one record each. Only then can MySQL equally distribute the database onto different 
  data disks mounted on the system, as it copies the tables and their schemas into new tables. From there, Sysbench can add additional records to further populate the database tables. 
  Once populated, VC persists the state, and it will not drop or recreate tables.

  ``` bash
  {
      "Type": "SysbenchConfiguration",
      "Parameters": 
      {
        "Scenario": "PopulateMySQLDatabase",
        "BenchmarkName": "TPCC",
        "DatabaseName": "sbtest",
        "WarehouseCount": 1,
        "NumTables": 10,
        "PackageName": "sysbench",
        "Role": "Server"
      }
    },
    {
      "Type": "MySQLServerConfiguration",
      "Parameters": 
      {
        "Scenario": "DistributeMySQLDatabase",
        "BenchmarkName": "TPCC",
        "Action": "DistributeDatabase",
        "DatabaseName": "sbtest",
        "NumTables": 10,
        "PackageName": "mysql-server",
        "Role": "Server"
      }
    },
    {
      "Type": "SysbenchConfiguration",
      "Parameters": 
      {
        "Scenario": "PopulateMySQLDatabase",
        "BenchmarkName": "TPCC",
        "DatabaseName": "sbtest",
        "NumTables": 10,
        "WarehouseCount": 100,
        "PackageName": "sysbench",
        "Role": "Server"
      }
  }
  ```

* **SysbenchClientExecutor**  
  Runs a given workload from the client-side on the server database. Note that this action can run with different arguments from SysbenchConfiguration -- though a database of 10 tables
  with 10,000 records each can be created, a workload can be run on 5 tables for 1000 records if desired. VC does not support dropping and recreating a new database or table 
  configuration within the same profile or system.

* **SysbenchServerExecutor**  
  Sets the server online for client interaction.

## PERF-MYSQL-OLTP-SYSBENCH.json
Runs a system-intensive workload using the Sysbench Benchmark to test the bandwidth of CPU, Memory, and Disk I/O.

* [Workload Profile](https://github.com/microsoft/VirtualClient/blob/main/src/VirtualClient/VirtualClient.Main/profiles/PERF-MYSQL-OLTP-SYSBENCH.json) 

* **Supported Platform/Architectures**
  * linux-x64
  * linux-arm64

* **Supports Disconnected Scenarios**  
  * No. Internet connection required.

* **Dependencies**  
  The dependencies defined in the 'Dependencies' section of the profile itself are required in order to run the workload operations effectively.
  * Internet connection.

  Additional information on components that exist within the 'Dependencies' section of the profile can be found in the following locations:
  * [Installing Dependencies](https://microsoft.github.io/VirtualClient/docs/category/dependencies/)

* **Profile Parameters**  
  The following parameters can be optionally supplied on the command line. See the 'Usage Scenarios/Examples' above for examples on how to supply parameters to Virtual Client profiles.

  | Parameter                 | Purpose                                                                                                                 |Default      |
  |---------------------------|-------------------------------------------------------------------------------------------------------------------------|-------------|
  | DatabaseName              | Not Required. Configure the name of database under test.                                                                |sbtest          |
  | DatabaseScenario              | Not Required. Configures the scenario in which to stress the database.                                      | Balanced          |
  | BenchmarkName              | Required. Name of the Benchmark to run. (e.g. OLTP)                                     | N/A          |
  | Duration              | Required. Timespan duration of the workload.                                                               | N/A          |
  | Workload              | Required. Name of benchmark to run; options listed [here](./sysbench.md)                                          | N/A          |

  The following additional parameters can be optionally supplied on the command line under the "Configure" scenario for a custom set up.

  | Parameter                 | Purpose                                                                                                                 |Default      |
  |---------------------------|-------------------------------------------------------------------------------------------------------------------------|-------------|
  | Threads              | Not Required. Number of threads to use during workload execution.                | vCPU * 8     |
  | RecordCount          | Not Required. Number of records per table in the database.                   | 10^(log2(vCPU) + 2)         |
  | NumTables             | Not Required. Number of tables created in the database.                         | 10              |

* **Profile Runtimes**  
  See the 'Metadata' section of the profile for estimated runtimes. These timings represent the length of time required to run a single round of profile 
  actions. These timings can be used to determine minimum required runtimes for the Virtual Client in order to get results. These are often estimates based on the
  number of system cores.

* **Usage Examples**
  The following section provides a few basic examples of how to use the workload profile.

  ``` bash
  # When running on a single system (environment layout not required)
  ./VirtualClient --profile=PERF-MYSQL-OLTP-SYSBENCH.json --system=Demo --timeout=1440" --packageStore="{BlobConnectionString|SAS Uri}

  # Override the default database name
  ./VirtualClient --profile=PERF-MYSQL-OLTP-SYSBENCH.json --system=Demo --timeout=1440" --parameters="DatabaseName=mytestDB" --packageStore="{BlobConnectionString|SAS Uri}

  # When running in a client/server environment
  ./VirtualClient --profile=PERF-MYSQL-OLTP-SYSBENCH.json --system=Demo --timeout=1440 --clientId=Client01 --layout="/any/path/to/layout.json" --packageStore="{BlobConnectionString|SAS Uri}
  ./VirtualClient --profile=PERF-MYSQL-OLTP-SYSBENCH.json --system=Demo --timeout=1440 --clientId=Server01  --layout="/any/path/to/layout.json" --packageStore="{BlobConnectionString|SAS Uri}
  ```

## PERF-MYSQL-TPCC-SYSBENCH.json
Runs a system-intensive workload using the Sysbench Benchmark to test the bandwidth of CPU, Memory, and Disk I/O.

* [Workload Profile](https://github.com/microsoft/VirtualClient/blob/main/src/VirtualClient/VirtualClient.Main/profiles/PERF-MYSQL-TPCC-SYSBENCH.json) 

* **Supported Platform/Architectures**
  * linux-x64
  * linux-arm64

* **Supports Disconnected Scenarios**  
  * No. Internet connection required.

* **Dependencies**  
  The dependencies defined in the 'Dependencies' section of the profile itself are required in order to run the workload operations effectively.
  * Internet connection.

  Additional information on components that exist within the 'Dependencies' section of the profile can be found in the following locations:
  * [Installing Dependencies](https://microsoft.github.io/VirtualClient/docs/category/dependencies/)

* **Profile Parameters**  
  The following parameters can be optionally supplied on the command line. See the 'Usage Scenarios/Examples' above for examples on how to supply parameters to Virtual Client profiles.

  | Parameter                 | Purpose                                                                                                                 |Default      |
  |---------------------------|-------------------------------------------------------------------------------------------------------------------------|-------------|
  | DatabaseName              | Not Required. Configure the name of database under test.                                                                |sbtest          |
  | DatabaseScenario           | Not Required. Configures the scenario in which to stress the database.                                      | Balanced          |
  | BenchmarkName              | Required. Name of the Benchmark to run. (e.g. TPCC)                                     | N/A          |
  | Duration              | Required. Timespan duration of the workload.                                                               | N/A          |
  | Workload              | Required. Use "tpcc" workload.                                   | N/A          |

  The following additional parameters can be optionally supplied on the command line under the "Configure" scenario for a custom set up.

  | Parameter                 | Purpose                                                                                                                 |Default      |
  |---------------------------|-------------------------------------------------------------------------------------------------------------------------|-------------|
  | Threads              | Not Required. Number of threads to use during workload execution.                | vCPU * 8     |
  | WarehouseCount       | Not Required. Number of warehouses per table in the database.                                               | 100       |
  | NumTables             | Not Required. Number of tables created in the database.                         | 10              |

* **Profile Runtimes**  
  See the 'Metadata' section of the profile for estimated runtimes. These timings represent the length of time required to run a single round of profile 
  actions. These timings can be used to determine minimum required runtimes for the Virtual Client in order to get results. These are often estimates based on the
  number of system cores.

* **Usage Examples**
  The following section provides a few basic examples of how to use the workload profile.

  ``` bash
  # When running on a single system (environment layout not required)
  ./VirtualClient --profile=PERF-MYSQL-TPCC-SYSBENCH.json --system=Demo --timeout=1440" --packageStore="{BlobConnectionString|SAS Uri}

  # Override the default database name
  ./VirtualClient --profile=PERF-MYSQL-TPCC-SYSBENCH.json --system=Demo --timeout=1440" --parameters="DatabaseName=mytestDB" --packageStore="{BlobConnectionString|SAS Uri}

  # When running in a client/server environment
  ./VirtualClient --profile=PERF-MYSQL-TPCC-SYSBENCH.json --system=Demo --timeout=1440 --clientId=Client01 --layout="/any/path/to/layout.json" --packageStore="{BlobConnectionString|SAS Uri}
  ./VirtualClient --profile=PERF-MYSQL-TPCC-SYSBENCH.json --system=Demo --timeout=1440 --clientId=Server01  --layout="/any/path/to/layout.json" --packageStore="{BlobConnectionString|SAS Uri}
  ```
