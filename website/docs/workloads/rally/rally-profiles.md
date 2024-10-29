# Rally Workload Profiles

The following profiles run customer-representative or benchmarking scenarios using the Rally workload.

* [Workload Details](./rally.md)
* [Client/Server Workloads](../../guides/0002-getting-started-client-server.md)

## Client/Server Topology Support

Rally (Elasticsearch) workload profiles support running the workload on both a single system as well as in a client/server topology. This means that the workload supports
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
./VirtualClient --profile=PERF-ELASTICSEARCH-RALLY.json --system=Juno --timeout=1440

# Multi-System
# On Client Role System...
./VirtualClient --profile=PERF-ELASTICSEARCH-RALLY.json --system=Juno --timeout=1440 --clientId=ElasticCoordinator --layoutPath=/any/path/to/layout.json

# On Server Role System...
./VirtualClient --profile=PERF-ELASTICSEARCH-RALLY.json --system=Juno --timeout=1440 --clientId=ElasticServer --layoutPath=/any/path/to/layout.json

# Example contents of the 'layout.json' file:
{
    "clients": [
        {
            "name": "ElasticCoordinator",
            "role": "Client",
            "ipAddress": "10.1.0.1"
        },
        {
            "name": "ElasticServer",
            "role": "Server",
            "ipAddress": "10.1.0.2"
        }
    ]
}
```

## PERF-ELASTICSEARCH-RALLY.json

Runs a system-intensive workload using the Rally-Elasticsearch benchmark tool.

* [Workload Profile](https://github.com/microsoft/VirtualClient/blob/main/src/VirtualClient/VirtualClient.Main/profiles/PERF-ELASTICSEARCH-RALLY.json)

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

  | Parameter  |  Purpose    | Default      |
  |------------|-------------|--------------|
  | DiskFilter | Optional. Configures VC to select a disk for Rally to store benchmark data on. | osdisk:false&biggestsize |
  | TrackName  | Required. Name of the track for Rally to run. | N/A |
  | DistributionVersion | Optional. Version of Elasticsearch to benchmark | 8.0.0 |
  | MetricFilters | Optional. Sets Metric Verbosity of telemetry. | Verbosity:1 |

* **Profile Runtimes**  
  See the 'Metadata' section of the profile for estimated runtimes. These timings represent the length of time required to run a single round of profile actions. These timings can be used to determine minimum required runtimes for the Virtual Client in order to get results. These are often estimates based on the
  number of system cores.

* **Usage Examples**
  The following section provides a few basic examples of how to use the workload profile.

    ```bash
    # When running on a single system (environment layout not required)
    ./VirtualClient --profile=PERF-ELASTICSEARCH-RALLY.json --system=Demo --timeout=1440" --packageStore="{BlobConnectionString|SAS Uri}"

    # When running in a client/server environment
    ./VirtualClient --profile=PERF-ELASTICSEARCH-RALLY.json --system=Demo --timeout=1440 --clientId=ElasticCoordinator --layout="/any/path/to/layout.json" --packageStore="{BlobConnectionString|SAS Uri}
    ./VirtualClient --profile=PERF-ELASTICSEARCH-RALLY.json --system=Demo --timeout=1440 --clientId=ElasticServer --layout="/any/path/to/layout.json" --packageStore="{BlobConnectionString|SAS Uri}"
    ```
