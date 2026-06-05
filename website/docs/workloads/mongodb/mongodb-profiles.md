# MongoDB Workload Profiles
The following profile runs customer-representative or benchmarking scenarios using the YCSB (Yahoo! Cloud Serving Benchmark) workload against
a MongoDB server.

* [Workload Details](./mongodb.md)  
* [Client/Server Workloads](../../guides/0020-client-server.md)

## Client/Server Topology Support
MongoDB workload profiles support running the workload in a client/server topology. This means that the workload is designed to run on 2 distinct systems. The client/server topology is used to include a network component in the overall performance evaluation. In a client/server topology, one system operates in the 'Client' role making calls to the system operating in the 'Server' role. The Virtual Client instances running on the client and server systems will synchronize with each other before running the workload. In order to support a client/server topology, an environment layout file MUST be supplied to each instance of the Virtual Client on the command line to describe the IP address/location of other Virtual Client instances.

* [Environment Layouts](../../guides/0020-client-server.md)

In the environment layout file provided to the Virtual Client, define the role of the client system/VM as "Client" and the role of the server system(s)/VM(s) as "Server".
The spelling of the roles must be exact. The IP addresses of the systems/VMs must be correct as well. The following example illustrates the
idea. The name of the client must match the name of the system or the value of the agent ID passed in on the command line.

``` bash
# Multi-System
# On Client Role System...
./VirtualClient --profile=PERF-MONGODB-YCSB.json --system=Juno --timeout=1440 --clientId=Client01 --layoutPath=/any/path/to/layout.json

# On Server Role System...
./VirtualClient --profile=PERF-MONGODB-YCSB.json --system=Juno --timeout=1440 --clientId=Server01 --layoutPath=/any/path/to/layout.json

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

## PERF-MONGODB-YCSB.json
Runs multiple workload variations using YCSB's built-in workloads to test MongoDB server performance across CPU, Memory, and Disk I/O.
This profile loads a dataset into MongoDB and then runs various read, write, scan, and mixed operation workloads against it using YCSB benchmark.

* [Workload Profile](https://github.com/microsoft/VirtualClient/blob/main/src/VirtualClient/VirtualClient.Main/profiles/PERF-MONGODB-YCSB.json) 

* **Supported Platform/Architectures**
  * linux-x64
  * linux-arm64

* **Supported Operating Systems**  
  * Ubuntu

* **Supports Disconnected Scenarios**  
  * No. Internet connection required.

* **Dependencies**  
  The dependencies defined in the 'Dependencies' section of the profile itself are required in order to run the workload operations effectively.
  * Internet connection.
  * The IP addresses defined in the environment layout (see above) for the Client and Server systems must be correct.
  * The name of the Client and Server instances defined in the environment layout must match the agent/client IDs supplied on the command line (e.g. --clientId)
    or must match the name of the system as defined by the operating system itself.

  Additional information on components that exist within the 'Dependencies' section of the profile can be found in the following locations:
  * [Installing Dependencies](https://microsoft.github.io/VirtualClient/docs/category/dependencies/)

* **Profile Parameters**  
  The following parameters can be optionally supplied on the command line to modify the behaviors of the workload.

  | Parameter                 | Purpose                                                                         | Default Value |
  |---------------------------|---------------------------------------------------------------------------------|---------------|
  | Duration                  | Optional. Defines the length of time to execute each YCSB workload scenario against the MongoDB server. | 00:05:00 |
  | ThreadCount               | Optional. Number of threads to use during workload execution. | {calculate({LogicalCoreCount}/2)} |
  | RecordCount               | Optional. Number of records to load into the database. Affects database size: Small (500000) ~8-10 GB, Medium (2500000) ~40-50 GB, Large (20000000) ~320-400 GB, XLarge (55000000) ~900 GB-1 TB. | 2500000 |
  | Port                      | Optional. The port on which the MongoDB server will listen for traffic. | 27017 |
  | Database                  | Optional. The name of the MongoDB database to use for the workload. | mongodb |
  | DiskFilter                | Optional. Filter for selecting disks to use for MongoDB data storage. | BiggestSize |

* **Workload Scenarios**  
  The profile executes the following YCSB workload scenarios:

  | Scenario                 | YCSB Workload | Description |
  |--------------------------|---------------|-------------|
  | read50_write50           | workloada     | 50% reads, 50% updates |
  | read95_write05           | workloadb     | 95% reads, 5% updates |
  | read100                  | workloadc     | 100% reads |
  | read95_insert05          | workloadd     | 95% reads, 5% inserts (Warning: grows database size) |
  | scan95_insert05          | workloade     | 95% scans, 5% inserts (Warning: grows database size) |
  | read50_readmodifywrite50 | workloadf     | 50% reads, 50% read-modify-write |

  Additional information on YCSB workloads can be found here:
  * [YCSB Core Workloads](https://github.com/brianfrankcooper/YCSB/wiki/Core-Workloads)

* **Database Size Considerations**  
  Database sizes vary based on RecordCount parameter:
  * Small (500,000 records): ~8-10 GB
  * Medium (2,500,000 records): ~40-50 GB (default)
  * Large (20,000,000 records): ~320-400 GB
  * XLarge (55,000,000 records): ~900 GB-1 TB

  **Warning**: The `read95_insert05` (workloadd) and `scan95_insert05` (workloade) scenarios insert new records into the database. This will cause the dataset to grow in size over time. This can lead to a server failure if MongoDB runs out of disk space. Ensure adequate disk space is available.

* **Profile Runtimes**  
  See the 'Metadata' section of the profile for estimated runtimes. These timings represent the length of time required to run a single round of profile 
  actions. These timings can be used to determine minimum required runtimes for the Virtual Client in order to get results. These are often estimates based on the
  number of system cores. 

  Recommended minimum execution time: 15 minutes

* **Usage Examples**  
  The following section provides a few basic examples of how to use the workload profile.

  ``` bash
  # When running in a client/server environment
  ./VirtualClient --profile=PERF-MONGODB-YCSB.json --system=Demo --timeout=1440 --clientId=Client01 --layoutPath="/any/path/to/layout.json" --packageStore="{BlobConnectionString|SAS Uri}"
  ./VirtualClient --profile=PERF-MONGODB-YCSB.json --system=Demo --timeout=1440 --clientId=Server01 --layoutPath="/any/path/to/layout.json" --packageStore="{BlobConnectionString|SAS Uri}"

  # Example with custom parameters
  ./VirtualClient --profile=PERF-MONGODB-YCSB.json --system=Demo --timeout=1440 --clientId=Client01 --layoutPath="/any/path/to/layout.json" --packageStore="{BlobConnectionString|SAS Uri}" --parameters="Duration=00:10:00,,,RecordCount=5000000"
  ```