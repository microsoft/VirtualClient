# Getting Started (Client/Server)
The sections below provide an example for how to run a client/server workload. For this example, the Memtier + Redis workload will be used. It supports 2 roles (Client and Server), 
and the roles are executed on separate systems. The following links provide more information on support for client/server workloads.

* [Client/Server Support](./0020-client-server.md)

:::info
Redis is a Linux-only workload and will not run on Windows systems.
:::

## Preliminary Setup
- You will need two systems that are in the same subnet/internet and able to communicate over the network via basic HTTP protocol (default port = 4500). 
- Create an [environment layout](./0020-client-server.md) file on both systems. This file is typically saved in the directory alongside the Virtual Client 
   executable for ease of reference. The example below illustrates what the contents of the environment layout file might look like.

   ``` json
   # The name of the client instance can be the name of the system or may be any
   # other name desired if the --agentId is provided on the command line.
   {
       "clients": [
         {
            "name": "TestClient",
            "role": "Client",
            "ipAddress": "10.1.0.1"
         },
         {
            "name": "TestServer",
            "role": "Server",
            "ipAddress": "10.1.0.2"
         }
       ]
   }
   ```

:::note
The use of a simple environment layout allows the Virtual Client to support more advanced workload role scenarios. For example, the NGINX workload supports more
than 2 roles: Client, Server and ReverseProxy. In fact any number of roles can be supported so long as the workload or monitor is implemented to support them.
:::

## Run Client/Server (Memtier/Redis) Example
The following section illustrates how to run the Virtual Client example profile (Memtier + Redis) on your Client and Server systems.

:::caution
Running this profile will cause Redis to be installed on the system that is designated as the 'Server' role. If you prefer to avoid having changes made to your
own system, it is advisable to use a different system/virtual machine to run the Server role. Additionally, the Virtual Client instances will modify the firewall
settings on the systems in order to allow for HTTP traffic on the default port used by the Virtual Client.
:::

- Run the following command on Client

  ``` bash
  sudo ./VirtualClient --clientId=TestClient --profile=GET-STARTED-REDIS.json --layoutPath=./layout.json
  ```
- Run the following command on Server

  ```bash
  sudo ./VirtualClient --clientId=TestServer --profile=GET-STARTED-REDIS.json --layoutPath=./layout.json
  ```
- Context on the command lines used:
  - Note that two commands are exactly the same except the `--clientId`. When the client ID is supplied on the command line, it will be the name Virtual Client uses to identify itself.
    If the `--clientId` is not defined on the command line, the default machine name is used. Whichever name you choose, it must match with the name in the environment layout file.

  - The `--layoutPath` option should be provided with the path to the environment layout file that was created in the preliminaries above. Each Virtual Client instance
    will "look up itself" in the environment layout to discover which role it will play on the system in which it is running (e.g. Client or Server).

  - The `--profile` option defines the exact workload or monitor profile(s) to run. Any number of profiles can be defined on the command line. If more than 1 is defined, all
    of the profiles will be merged into 1 at runtime.

## Walking Through the Operations
The following section describes what is happening as the 2 instances of the Virtual Client are running on the different systems.

- The 2 Virtual Client instances will download dependencies (from the package store) required to run the Redis
- The 2 Virtual Client instances will then discover their individual role from the environment layout.
- The Server role instance will startup a self-hosted REST API service within the Virtual Client process. This API will be used to enable the Client role instance
  and the Server role instance to communicate via HTTP messages (on the default port used by the Virtual Client). The Server role will open a port in the firewall
  for the port required (and this is why the workload must be ran in privileged/sudo context).
- The Client role instance will find the Server role instance in the environment layout.
- The Client role instance will then initiate a "handshake" with the Server role instance polling until the Server is online.
- The Client role instance will then poll the Server role instance until the Server indicates it is ready for the workload to be started.
- The Server role instance will install and configure Redis on the system in which it is running.
- After Redis is installed on the system, the Server role instance will respond to the Client role instance that it is ready for the workload to commence.
- The Client role instance will then execute the Memtier workload against the Redis server.
- The benchmark will run for approximately 10 minutes.


## Results and Logs
Each Virtual Client workload or monitor will emit results of some kind. The most important parts of the results will be parsed out of them to form structured "metrics".
Metrics are numeric values that represent measurements for key/important performance and reliability aspects of the workload and the system on which it is running. For
example with the Memtier + Redis workload, the network "throughput" or "bandwidth" are important measurements for the performance of the workload and system.

- Log files can be found in the **logs** directory within the Virtual Client application's parent directory itself. Logs are separated into the following categories:
  - **Traces**  
    operational traces about everything the Virtual Client is doing while running useful for debugging/triage purposes.

  - **Metrics**  
    Important measurements captured from the workload and the system that can be used to analyze the performance and reliability of the workload and correspondingly
    the system on which it is running.

  - **Counters**  
    Similar to metrics capturing important measurements from the system itself that can be used to analyze the performance, reliability and resource usage on the system
    while the workload is running.

- The Virtual Client captures quite a few metrics for the Memtier + Redis workload. Two most critical metrics for Redis are "throughput" and "P99 latency". The full
  list of metrics captured are documented here: [Redis Workload Metrics](../workloads/redis/redis.md)

  - Example Throughput Metric Result  
    ```json {16-19}
    {
        "timestamp": "2022-11-16T05:35:55.2321711+00:00",
        "level": "Information",
        "message": "RedisMemtier.ScenarioResult",
        "agentId": "testclient",
        "appVersion": "1.6.0.0",
        "clientId": "testclient",
        "executionProfileName": "GET-STARTED-REDIS.json",
        "executionProfilePath": "/home/azureuser/virtualclient/profiles/GET-STARTED-REDIS.json",
        "executionSystem": null,
        "experimentId": "5c6d967f-7c84-4d74-9099-65b6f629d61e",
        "metadata": {"experimentId":"5c6d967f-7c84-4d74-9099-65b6f629d61e","agentId":"TestClient"},
        "metricCategorization": "",
        "metricDescription": "",
        "metricMetadata": {},
        "metricName": "Throughput",
        "metricRelativity": "HigherIsBetter",
        "metricUnit": "req/sec",
        "metricValue": 3551162.35,
        "parameters": {"scenario":"Memtier_4t_1c","role":"Client","port":"6379","packageName":"Redis","ThreadCount":"4","ClientCount":"1","RunCount":"1","durationInSecs":"60","pipelineDepth":"32","bind":"1","profileIteration":1,"profileIterationStartTime":"2022-11-16T05:30:34.5264899Z"},
        "platformArchitecture": "linux-x64",
        "scenarioEndTime": "2022-11-16T05:35:55.2210932+00:00",
        "scenarioName": "Memtier_4t_1c",
        "scenarioStartTime": "2022-11-16T05:31:55.1121065+00:00",
        "systemInfo": ...,
        "toolName": "RedisMemtier",
        "etc": ...
    }
    ```
  - Example P99 latency Metric Result  
    ```json {16-19}
    {
        "timestamp": "2022-11-16T05:35:55.2326483+00:00",
        "level": "Information",
        "message": "RedisMemtier.ScenarioResult",
        "agentId": "testclient",
        "appVersion": "1.6.0.0",
        "clientId": "testclient",
        "executionProfileName": "GET-STARTED-REDIS.json",
        "executionProfilePath": "/home/azureuser/virtualclient/profiles/GET-STARTED-REDIS.json",
        "executionSystem": null,
        "experimentId": "5c6d967f-7c84-4d74-9099-65b6f629d61e",
        "metadata": {"experimentId":"5c6d967f-7c84-4d74-9099-65b6f629d61e","agentId":"TestClient"},
        "metricCategorization": "",
        "metricDescription": "",
        "metricMetadata": {},
        "metricName": "P99lat",
        "metricRelativity": "LowerIsBetter",
        "metricUnit": "msec",
        "metricValue": 0.255,
        "parameters": {"scenario":"Memtier_4t_1c","role":"Client","port":"6379","packageName":"Redis","ThreadCount":"4","ClientCount":"1","RunCount":"1","durationInSecs":"60","pipelineDepth":"32","bind":"1","profileIteration":1,"profileIterationStartTime":"2022-11-16T05:30:34.5264899Z"},
        "platformArchitecture": "linux-x64",
        "scenarioEndTime": "2022-11-16T05:35:55.2210932+00:00",
        "scenarioName": "Memtier_4t_1c",
        "scenarioStartTime": "2022-11-16T05:31:55.1121065+00:00",
        "systemInfo": ...,
        "toolName": "RedisMemtier",
        "etc": ...
    }
    ```


## Conclusion
Congratulations! You have just ran a multi-role, client/server workload on your systems using the Memtier + Redis benchmark. The following links provide additional insights into
running different workloads. The example profile used for this walkthrough is a scaled-down version of a full profile meant to run quickly for illustrative purposes.
The full profile (noted below) will take a few hours to complete.

- [Example Profile from Above](https://github.com/microsoft/VirtualClient/blob/main/src/VirtualClient/VirtualClient.Main/profiles/GET-STARTED-REDIS.json)
- [Redis Workload](../workloads/redis/redis.md)
- [Redis Workload Profiles](../workloads/redis/redis-profiles.md)
- [Redis Workload Metrics](../workloads/redis/redis.md)