---
id: getting-started
sidebar_position: 1
---

# Getting Started : OpenSSL

In this document, we are going to run a "hello-world" version of VirtualClient: benchmark your system's crypotography performance, with OpenSSL Speed, using SHA256 algorithm.

## Installation

Virtual Client is a self-contained .NET 6 application. "Installation" practically means copying the VirtualClient package into your system. It runs out-of-box on [all OS supported by .NET 6](https://github.com/dotnet/core/blob/main/release-notes/6.0/supported-os.md).

#### *NuGet package*
- We publish NuGet Package at https://www.nuget.org/packages/VirtualClient
- You could download directly from NuGet https://www.nuget.org/api/v2/package/VirtualClient/0.0.5
- You could also use powershell 
    ```powershell
    PM> NuGet\Install-Package VirtualClient -Version 0.0.5
    ```
- The .nupkg NuGet package is just a .zip file, you can unzip with programs like 7zip, or rename .nupkg to .zip and unzip.
- VC executable could be find in those paths
```treeview
VirtualClient/
├── content/
|   ├── linux-arm64
|   |   └── VirtualClient
|   ├── linux-x64
|   |   └── VirtualClient
|   ├── win-arm64
|   |   └── VirtualClient.exe
|   └── win-x64
|       └── VirtualClient.exe
└── etc.
```

#### *Build yourself*
- You need to [install .Net SDK 6.0.X](https://dotnet.microsoft.com/en-us/download/dotnet/6.0)
- Use build script at the root of the repo build.cmd
    ```bash
    build.cmd
    ```
- You will find VC binary in corresponding arch/runtimes folder. 
    ```bash
    VirtualClient\out\bin\Debug\ARM64\VirtualClient.Main\net6.0\linux-arm64\publish\VirtualClient
    VirtualClient\out\bin\Debug\ARM64\VirtualClient.Main\net6.0\win-arm64\publish\VirtualClient.exe
    VirtualClient\out\bin\Debug\x64\VirtualClient.Main\net6.0\linux-x64\publish\VirtualClient
    VirtualClient\out\bin\Debug\x64\VirtualClient.Main\net6.0\win-x64\publish\VirtualClient.exe
    ```
- VirtualClient is a self-contained .NET app. When you use VC, you need to copy over the entire `/publish/` folder

## Run a simple VC profile

- Execute this command
```bash
sudo ./VirtualClient --profile=GET-STARTED-OPENSSL.json --profile=MONITORS-NONE.json --iterations=1 --packages=https://virtualclient.blob.core.windows.net/packages
```
- [`--profile=GET-STARTED-OPENSSL.json`](https://github.com/microsoft/VirtualClient/blob/main/src/VirtualClient/VirtualClient.Main/profiles/GET-STARTED-OPENSSL.json) tells VC to run a stripped down version of OpenSSL benchmark. With SHA256 algorithm.
    - VC supports remote profile, you can reference a url to a json file.
    - `--profile=https://raw.githubusercontent.com/microsoft/VirtualClient/main/src/VirtualClient/VirtualClient.Main/profiles/GET-STARTED-OPENSSL.json` is equavilent to `--profile=GET-STARTED-OPENSSL.json`.
- VirtualClient has a default profile, [`--profile=MONITORS-NONE.json`](https://github.com/microsoft/VirtualClient/blob/main/src/VirtualClient/VirtualClient.Main/profiles/MONITORS-NONE.json) overrides that behavior in this one-time run.
- `--iteration=1` Tells VC to run this profile once. Default behavior is to run profiles repetatively until timeout.
- `--packages=https://virtualclient.blob.core.windows.net/packages` defines the packages store that VC will download OpenSSL binary from. Not every workload needs binary download. You can also use your own binary and package store if desired.


:::caution
In this profile, VC will download OpenSSL binaries onto your system, under `/virtualclient/packages/openssl.3.0.0/`.<br/>
If prefered, run in a Virtual Machine to avoid those changes to your system.
:::

## Read results and logs

- You will find three local files under directory `/virtualclient/logs/`
```bash
logs
├── events-20221109.log
├── metrics-20221109.log
└── traces-20221109.log
```
- Metrics.log contains the Metrics captured by the benchmark. Columns `metricName`, `metricValue`, `metricUnit` contain some of the most important information
from a benchmark run.
```json {16,17,18,19}
{
    "timestamp": "2022-11-14T07:26:18.2717145+00:00",
    "level": "Information",
    "message": "OpenSSL.ScenarioResult",
    "agentId": "testuser",
    "appVersion": "1.6.0.0",
    "clientId": "testuser",
    "executionProfileName": "GET-STARTED-OPENSSL.json",
    "executionProfilePath": "/home/testuser/virtualclient/profiles/GET-STARTED-OPENSSL.json",
    "executionSystem": null,
    "experimentId": "6619e311-e3ee-4727-a082-dc61f1fbb44d",
    "metadata": {"experimentId":"6619e311-e3ee-4727-a082-dc61f1fbb44d","agentId":"testuser"},
    "metricCategorization": "",
    "metricDescription": "",
    "metricMetadata": {},
    "metricName": "sha256 16-byte",
    "metricRelativity": "HigherIsBetter",
    "metricUnit": "kilobytes/sec",
    "metricValue": 323830.14,
    "parameters": {"scenario":"SHA256","commandArguments":"speed -elapsed -seconds 10 sha256","packageName":"openssl","tags":"CPU,OpenSSL,Cryptography","profileIteration":1,"profileIterationStartTime":"2022-11-14T07:25:18.1731942Z"},
    "platformArchitecture": "linux-arm64",
    "scenarioArguments": "speed -multi 4 -elapsed -seconds 10 sha256",
    "scenarioEndTime": "2022-11-14T07:26:18.2470775Z",
    "scenarioName": "OpenSSL Speed",
    "scenarioStartTime": "2022-11-14T07:25:18.2251103Z",
    "systemInfo": ...,
    "tags": "CPU,OpenSSL,Cryptography",
    "etc": ...
}
```
- Traces contains the Messages that VirtualClient logs.
    - Messages like "BenchmarkStart", "BenchmarkStop"
    - Raw output from processes
    - More information logged by VC
- Events contains system informations or important system events.
    - Output from tools like uname, lscpu, lspci

:::tip Reading logs is tedious?
VC is designed for large scale perf testing. Check [Telemetry](./telemetry/telemetry.md) to see how to setup automatic data upload pipeline.
:::

## Congratulations !!
You just benchmarked your system with OpenSSL.

- To benchmark your system's crypotography performance holisticaly, we recommend the full profile for OpenSSL: [`PERF-CPU-OPENSSL.json`](https://github.com/microsoft/VirtualClient/blob/main/src/VirtualClient/VirtualClient.Main/profiles/PERF-CPU-OPENSSL.json)