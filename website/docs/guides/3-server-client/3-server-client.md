---
id: server-client
sidebar_position: 3
---

# Run client -> server workloads

In this document, we are going to run a Redis workload, where server and client are on different systems.

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
An environment json will give every client download about their role and other clients' roles. This enables VC to do complex multi-role workloads, like NGINX with server, client and reverse proxy.
:::

## Run Redis Benchmark

- Run this command on both server and client
    - One benefit of using a EnvironmentLayout file vs. server ipAddress, is that on all VC instances with difference roles, you can use the exact same command.
```bash
sudo ./VirtualClient --clientId=TestClient --profile=PERF-REDIS.json --profile=MONITORS-NONE.json --iterations=1 --packages=https://virtualclient.blob.core.windows.net
/packages --layoutPath=layout.json
```
   
- (WIP)



:::caution
In this profile, VC will download Redis and install Redis server.<br/>
If prefered, run in a Virtual Machine to avoid those changes to your system.
:::

## Read results and logs



## Congratulations !!
You just ran a multi-role workload and benchmark your system with Redis.