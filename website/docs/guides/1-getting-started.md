---
id: getting-started
sidebar_position: 1
---

# Getting Started

In this document, we are going to run a "hello-world" version of VirtualClient: benchmark your system with CoreMark.

---

## Installation

#### *NuGet package*

- VirtualClient NuGet Package is at https://www.nuget.org/packages/VirtualClient
```powershell
PM> NuGet\Install-Package VirtualClient -Version 0.0.2
```
- You could optionally download directly from NuGet https://www.nuget.org/api/v2/package/VirtualClient/0.0.2
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

---

## Run a simple VC profile

- Execute this command
```bash
VirtualClient --profile=PERF-CPU-COREMARK.json --profile=MONITORS-NONE.json --iterations=1
```
- `--profile=PERF-CPU-COREMARK.json` tells VC to run a CoreMark benchmark
- VirtualClient has a default profile, `--profile=MONITORS-NONE.json` overrides that behavior in this one-time run.
- `--iteration=1` Tells VC to run this profile once. Default behavior is to run profiles repetatively until timeout.


:::caution
In this profile, VC will install gcc-9 and other development tools, and set gcc-9 as default compiler in your system.<br/>
If prefered, run in a Virtual Machine to avoid those changes to your system.
:::

## Read results and logs

- You will find three local files under directory `/vc/logs/`
```bash
logs
├── events-20221109.log
├── metrics-20221109.log
└── traces-20221109.log
```
- Metrics.log contains the Metrics captured by the benchmark. Columns `metricName`, `metricValue`, `metricUnit` contain some of the most important information
from a benchmark run.
```json {16,18,19}
{
    "timestamp": "2022-11-09T04:09:59.3573706+00:00",
    "level": "Information",
    "message": "CoreMark.ScenarioResult",
    "agentId": "ExampleClient",
    "appVersion": "1.6.0.0",
    "clientId": "ExampleClient",
    "executionProfileName": "PERF-CPU-COREMARK.json",
    "executionProfilePath": "/home/vcvmadmin/vc/profiles/PERF-CPU-COREMARK.json",
    "executionSystem": null,
    "experimentId": "3a225222-f834-4101-8a81-219a1f4e9587",
    "metadata": {"experimentId":"3a225222-f834-4101-8a81-219a1f4e9587","agentId":"ExampleClient"},
    "metricCategorization": "",
    "metricDescription": "",
    "metricMetadata": {},
    "metricName": "Iterations/Sec",
    "metricRelativity": "HigherIsBetter",
    "metricUnit": "iterations/sec",
    "metricValue": 93187.139894,
    "parameters": {"scenario":"ScoreSystem","packageName":"coremark","profileIteration":1,"profileIterationStartTime":"2022-11-09T04:09:22.3729518Z"},
    "platformArchitecture": "linux-arm64",
    "scenarioArguments": "XCFLAGS=\"-DMULTITHREAD=4 -DUSE_PTHREAD\" REBUILD=1 LFLAGS_END=-pthread",
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
You just benchmarked your system with CoreMark.