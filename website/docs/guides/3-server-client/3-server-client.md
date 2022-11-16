---
id: server-client
sidebar_position: 3
---

# Run client -> server workloads : Redis

In this document, we are going to run a quick round of Redis workload, where server and client are on different systems.

:::info
Redis is a Linux only workload.
:::

## Environment Setup
1. You need two systems that are in the same subnet/internet and able to see each other. 
2. You need an [**EnvironmentLayout Json**](./environment-layout.md) file in both systems. This file has three information: `clientId`, `ipAdress` and `role`.
3. In this example, client runs on `10.1.0.1`, server runs on `10.1.0.2`. The layout looks like this.

```json
{
    "clients": [
        {
            "name": "TestClient",
            "role": "Client",
            "privateIPAddress": "10.1.0.1"
        },
        {
            "name": "TestServer",
            "role": "Server",
            "privateIPAddress": "10.1.0.2"
        }
    ]
}
```

:::note
Wonder why not just use a *--serverIpAddress* like other tools? Great question.<br/>
An environment json will give every client downledge about their role and other clients' roles. This enables VC to do complex multi-role workloads, like NGINX with server, client and reverse proxy.
:::

## Run Redis Benchmark
:::caution
In this profile, VC will download Redis and install Redis server.<br/>
If prefered, run in a Virtual Machine to avoid those changes to your system.
:::

- Run this command on client
    ```bash
    sudo ./VirtualClient --clientId=TestClient --profile=GET-STARTED-REDIS.json --profile=MONITORS-NONE.json --iterations=1 --packages=https://virtualclient.blob.core.windows.net/packages --layoutPath=layout.json
    ```
- Run this command on server
    ```bash
    sudo ./VirtualClient --clientId=TestServer --profile=GET-STARTED-REDIS.json --profile=MONITORS-NONE.json --iterations=1 --packages=https://virtualclient.blob.core.windows.net/packages --layoutPath=layout.json
    ```
- Notice the two commands are exactly the same except the `--clientId`. The clientId is default to the machine name. You don't need to pass in `clientId` if the `name` in layout.json matches your actual machine name.
- `--layoutPath` should point to the layout json file you just created. Relative and absolute paths are both supported.
- The two VC will install Redis on server, Memtier on client, handshake, and then start the benchmarking.
- The benchmark might run for about 10 minutes, get a cup of ☕.


## Read results and logs
- Similar to the previous tutorial, the metric is in file `logs/metrics-20221116.log`.
- VC captures quite a lot of metrics for Redis: [Full Redis Metrics](../../workloads/redis/redis-metrics.md)
- Two most critical metrics for Redis are throughput and P99 latency

- Throughput
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
        "parameters": {"scenario":"Memtier_4t_1c","role":"Client","port":"6379","packageName":"Redis","numberOfThreads":"4","numberOfClients":"1","numberOfRuns":"1","durationInSecs":"60","pipelineDepth":"32","bind":"1","profileIteration":1,"profileIterationStartTime":"2022-11-16T05:30:34.5264899Z"},
        "platformArchitecture": "linux-x64",
        "scenarioEndTime": "2022-11-16T05:35:55.2210932+00:00",
        "scenarioName": "Memtier_4t_1c",
        "scenarioStartTime": "2022-11-16T05:31:55.1121065+00:00",
        "systemInfo": ...,
        "toolName": "RedisMemtier",
        "etc": ...
    }
    ```
- P99 latency
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
        "parameters": {"scenario":"Memtier_4t_1c","role":"Client","port":"6379","packageName":"Redis","numberOfThreads":"4","numberOfClients":"1","numberOfRuns":"1","durationInSecs":"60","pipelineDepth":"32","bind":"1","profileIteration":1,"profileIterationStartTime":"2022-11-16T05:30:34.5264899Z"},
        "platformArchitecture": "linux-x64",
        "scenarioEndTime": "2022-11-16T05:35:55.2210932+00:00",
        "scenarioName": "Memtier_4t_1c",
        "scenarioStartTime": "2022-11-16T05:31:55.1121065+00:00",
        "systemInfo": ...,
        "toolName": "RedisMemtier",
        "etc": ...
    }
    ```


## Congratulations !!
You just ran a multi-role workload and benchmark your system with Redis.

- The profile `GET-STARTED-REDIS` is a highly stripped down version Redis, with just 1 client and 4 threads.
- The regular [`PERF-REDIS.json`](https://github.com/microsoft/VirtualClient/blob/main/src/VirtualClient/VirtualClient.Main/profiles/PERF-REDIS.json) has a diverse combinations of Redis benchmark. We recommend run the full profile to benchmark Redis performance holistically. The full profile might take couple hours.