# Kafka Workload Profiles
The following profiles run customer-representative or benchmarking scenarios using the Kafka workload.

* [Workload Details](./kafka.md)  
* [Client/Server Workloads](../../guides/0020-client-server.md)

## Client/Server Topology Support
Kafka workload profiles support running the workload on both a single system as well as in a client/server topology. This means that the workload supports
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
./VirtualClient --profile=PERF-KAFKA.json --system=Juno --timeout=1440

# Multi-System
# On Client Role System...
./VirtualClient --profile=PERF-KAKFA.json --system=Juno --timeout=1440 --clientId=Client01 --layoutPath=/any/path/to/layout.json

# On Server Role System...
./VirtualClient --profile=PERF-KAFKA.json --system=Juno --timeout=1440 --clientId=Server01 --layoutPath=/any/path/to/layout.json

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

## PERF-KAFKA.json
Runs the Kafka workload using this profile which uses kafka-producer-perf-test and kafka-consumer-perf-test load generation to produce and consume data respectively.

* [Workload Profile](https://github.com/microsoft/VirtualClient/blob/main/src/VirtualClient/VirtualClient.Main/profiles/PERF-KAFKA.json) 

* **Supported Platform/Architectures**
  * win-x64 
  * win-arm64

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
  The following parameters can be optionally supplied on the command line to modify the behaviors of the workload.

  | Parameter                 | Purpose                                                                         | Default Value |
  |---------------------------|---------------------------------------------------------------------------------|---------------|
  | ServerInstances           | Optional. Number of kafka servers to run   | 3 |
  | ServerPort                | Optional. The initial port on which the Kafka servers will listen for traffic. Additional ports will be used for each 1 server instance defined in an alternate manner (e.g. 9092, 9094, 9096) | 9092 |
  | RecordSize              | Optional. It defines the size of record| 100 |
  | NumberOfRecords          | Optional. Total number of records to sent/consume| 5000000 |

* **Component Parameters**  
  The following parameters describe the parameters within the profile components.

  | Server Role Parameter     | Purpose                                                                         | Default Value |
  |---------------------------|---------------------------------------------------------------------------------|---------------|
  | Scenario                  | Scenario use to define the purpose of the action in the profile. This can be used to specify exact actions to run or exclude from the profile. | |
  | PackageName               | The name of the package that contains the Kafka server binaries/scripts.    |               |
  | Port                | Optional. The initial port on which the Kafka servers will listen for traffic. Additional ports will be used for each 1 server instance defined in an alternate manner (e.g. 9092, 9094, 9096) | 9092 |
  | ServerInstances           | The number of distinct Kafka server instances to run concurrently. | 3 |


  | Client Role Parameter     | Purpose                                                                         | Default Value |
  |---------------------------|---------------------------------------------------------------------------------|---------------|
  | Scenario                  | Scenario use to define the purpose of the action in the profile. This can be used to specify exact actions to run or exclude from the profile. | |
  | CommandLine               | The command line to use for executing the Kafka workload against the Memcached server. Note that the --port and --server options will be added automatically by the executor. For the --key-pattern option, 'S' means sequential distribution, 'R' means uniform random distribution and 'G' means Gaussian distribution of object. | |
  | PackageName               | The name of the package that contains the Kafka benchmark binaries/scripts.  | |
  | WarmUp                    | True if the component/action is meant to be used to warmup the Memcached server. Metrics will not be captured in warmup steps. | false |
  | ServerInstances           | The number of distinct Kafka server instances to run concurrently. Same value as in Server | 3 |
  | CommandType           | Command type value can be one of - Setup, ProducerTest and ConsumerTest | |


* **Profile Runtimes**  
  See the 'Metadata' section of the profile for estimated runtimes. These timings represent the length of time required to run a single round of profile 
  actions. These timings can be used to determine minimum required runtimes for the Virtual Client in order to get results. These are often estimates based on the
  number of system cores. 

* **Usage Examples**  
  The following section provides a few basic examples of how to use the workload profile. Additional usage examples can be found in the
  'Usage Scenarios/Examples' link at the top.

  ``` bash
  # When running on a single system (environment layout not required)
  ./VirtualClient --profile=PERF-KAFKA.json --system=Demo --timeout=250 --packageStore="{BlobConnectionString|SAS Uri}"

  # When running in a client/server environment
  ./VirtualClient --profile=PERF-KAFKA.json --system=Demo --timeout=1440 --clientId=Client01 --layoutPath="/any/path/to/layout.json" --packageStore="{BlobConnectionString|SAS Uri}"
  ./VirtualClient --profile=PERF-KAFKA.json --system=Demo --timeout=1440 --clientId=Server01 --layoutPath="/any/path/to/layout.json" --packageStore="{BlobConnectionString|SAS Uri}"
  ```