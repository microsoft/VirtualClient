---
id: getting-started
sidebar_position: 1
---

# Getting Started
The sections below will help the user install the Virtual Client

In this document, we are going to run a "hello world" version of VirtualClient: benchmark your system's crypotography performance, with OpenSSL Speed, using 
SHA256 algorithm.

## Installation

Virtual Client is a self-contained .NET 8 application, so "Installation" really just means copying the Virtual Client package onto your system. It runs out-of-box on 
[all operating systems supported by .NET 8](https://github.com/dotnet/core/blob/main/release-notes/8.0/supported-os.md).

### Debian / Ubuntu (deb)
VirtualClient is published to Microsoft package store. Use the following command to install. You can then call VirtualClient from this path `/usr/bin/virtualclient` (which is 
typically one of the default paths in the Linux `$PATH` environment variable). This is a symbolic link. The actual package is typically installed at the path `/opt/virtualclient`.

```bash
# example: ubuntu
declare os_distro=$(if command -v lsb_release &> /dev/null; then lsb_release -is | tr '[:upper:]' '[:lower:]' ; else grep -oP '(?<=^ID=).+' /etc/os-release ; fi)
# example: 20.04
declare os_version=$(if command -v lsb_release &> /dev/null; then lsb_release -rs; else grep -oP '(?<=^VERSION_ID=).+' /etc/os-release | tr -d '"'; fi)
# Add Microsoft deb repo
curl -sSL -O https://packages.microsoft.com/config/$os_distro/$os_version/packages-microsoft-prod.deb
sudo dpkg -i packages-microsoft-prod.deb
rm packages-microsoft-prod.deb
sudo apt-get update
sudo apt-get install virtualclient
```

### Red Hat-based distributions (rpm)
We maintain deb package for releases. Use the following command to install. You can then call VirtualClient from this path `/usr/bin/virtualclient`
(which is typically one of the default paths in the Linux `$PATH` environment variable). This is a symbolic link. The actual package is typically installed at the path `/opt/virtualclient`.

```bash
# example: ubuntu
declare os_distro=$(if command -v lsb_release &> /dev/null; then lsb_release -is | tr '[:upper:]' '[:lower:]' ; else grep -oP '(?<=^ID=).+' /etc/os-release ; fi)
# example: 20.04
declare os_version=$(if command -v lsb_release &> /dev/null; then lsb_release -rs; else grep -oP '(?<=^VERSION_ID=).+' /etc/os-release | tr -d '"'; fi)
# Add Microsoft deb repo
curl -sSL -O https://packages.microsoft.com/config/$os_distro/$os_version/packages-microsoft-prod.rpm
sudo rpm -i packages-microsoft-prod.rpm
rm rpm -i packages-microsoft-prod.rpm
sudo dnf update
sudo dnf install virtualclient
```

### Zip Package
You can find zip files in the latest [GitHub Releases](https://github.com/microsoft/VirtualClient/releases).

### NuGet Packages
- The Virtual Client is published as a NuGet package at the following location:  
  https://www.nuget.org/packages/VirtualClient

- You can download from a browser directly from NuGet using the following link and replacing the version:  
  https://www.nuget.org/api/v2/package/VirtualClient/{Version}

- You can also install the Virtual Client using PowerShell Core/7:

  ```powershell
  # Example
  PM> NuGet\Install-Package VirtualClient -Version 1.12.0
  ```

- If you are on a Windows system, you can download from the command line using the NuGet.exe:
  - https://www.nuget.org/downloads

  ``` bash
  # Example
  C:\Users\Any> NuGet.exe Install VirtualClient -OutputDirectory C:\Users\Any\nuget\packages -NoCache -Version 1.12.0 -Source nuget.org
  ```

### NuGet/Zip Package Contents
The Virtual Client NuGet package (*.nupkg) is just a zip file. On Windows systems, you can simply change the extension to .zip and extract. You can also unzip with
programs such as 'unzip' or '7zip'.

- The Virtual Client application/executable can be found in the path locations as follows:

  ``` treeview
  VirtualClient/
  ├── content/
  |   ├── linux-arm64/
  |   |   └── VirtualClient
  |   ├── linux-x64/
  |   |   └── VirtualClient
  |   ├── win-arm64/
  |   |   └── VirtualClient.exe
  |   └── win-x64/
  |       └── VirtualClient.exe
  └── etc.
  ```

## Building the Source Code
If preferable, the Virtual Client source code can be built on your local system. This is useful for picking up the absolute latest changes
to the source code and for testing changes locally. Before attempting to build the Virtual Client repo, ensure the fo

- [Install the .NET SDK 9.0.x](https://dotnet.microsoft.com/en-us/download/dotnet/9.0)
- Build artifacts are output to the following locations. The 'bin' directory is where all compiled binaries/executables are output. The 'obj' directory
  will contain intermediate files used during compilation. The 'packages' directory will contain any packages that are created during build + packaging 
  processes.
  
  ``` treeview
  {rootdir}/
  ├── out/
  |   ├── bin
  |   |   └── Debug|Release
  |   |       └── AnyCPU
  |   |       └── x64
  |   |       └── ARM64
  |   ├── obj
  |   ├── packages
  ```

### Building On a Windows System
The following section provides instructions for building on Windows systems.

- In the repo root directory, run the following commands:

  ``` script
  # Build the repo
  C:\repos\virtualclient> build.cmd

  # Build packages (e.g. NuGet) from the build artifacts
  C:\repos\virtualclient> build-packages.cmd

  ```
- The build process will create ready-to-run builds of the Virtual Client for all supported platforms and architectures. Virtual Client executable and binaries can be found in the repo 'out' directory in corresponding architecture/runtimes folder locations. 

  ```bash
  {rootdir}\out\bin\Release\ARM64\VirtualClient.Main\net9.0\linux-arm64\publish\VirtualClient
  {rootdir}\out\bin\Release\ARM64\VirtualClient.Main\net9.0\win-arm64\publish\VirtualClient.exe
  {rootdir}\out\bin\Release\x64\VirtualClient.Main\net9.0\linux-x64\publish\VirtualClient
  {rootdir}\out\bin\Release\x64\VirtualClient.Main\net9.0\win-x64\publish\VirtualClient.exe
  ```
- VirtualClient is a self-contained .NET application. The application can be run from the build output locations noted above or copied to another
  system. When simply copy the contents of the `/publish/` folder specific to the platform/architecture to the system on which you want to run. 
  Note that when running on Linux systems, the 'VirtualClient' binary will need to be attributed as executable (e.g. ```chmod +x /home/user/virtualclient/linux-x64/publish/VirtualClient```)

### Building On a Linux System
The following section provides instructions for building on Linux systems.

:::info
You need to install .NET SDK for building VirtualClient locally. You could use command like `sudo snap install dotnet-sdk` or refer to [.NET documentation](https://learn.microsoft.com/en-us/dotnet/core/install/linux).
:::

- In the repo root directory, run the following commands:
  ``` script
  # Build the repo with current platform (one of linux-x64 or linux-arm64)
  ~/build/VirtualClient$ ./build.sh

  # Build the repo for all platforms (linux-x64,linux-arm64,win-x64,win-arm64)
  ~/build/VirtualClient$ ./build.sh --build-all
  ```

## Run a Simple Profile
The following section illustrates how to run a simple profile on your system. This profile will run the OpenSSL workload on your system for a brief period of time. This workload
evaluates the performance of the CPU(s) on your system but will not cause it any harm.

:::caution
When running this profile on your system, the Virtual Client will download OpenSSL binaries onto your system, under `/virtualclient/packages/openssl.3.0.0/`.
If you prefer to keep your system unchanged, consider running the Virtual Client on a different system/VM.
:::

- Execute the following command to run an example VC profile. This profile runs an OpenSSL workload.

  ```bash
  sudo ./VirtualClient --profile=GET-STARTED-OPENSSL.json
  ```
- [`--profile=GET-STARTED-OPENSSL.json`](https://github.com/microsoft/VirtualClient/blob/main/src/VirtualClient/VirtualClient.Main/profiles/GET-STARTED-OPENSSL.json) tells VC to run a stripped down version of OpenSSL benchmark. With SHA256 algorithm.
  - VC supports remote profile, you can reference a url to a json file.
  - `--profile=https://raw.githubusercontent.com/microsoft/VirtualClient/main/src/VirtualClient/VirtualClient.Main/profiles/GET-STARTED-OPENSSL.json` is equavilent to `--profile=GET-STARTED-OPENSSL.json`.


## Results and Logs
Each Virtual Client workload or monitor will emit results of some kind. The most important parts of the results will be parsed out of them to form structured "metrics".
Metrics are numeric values that represent measurements for key/important performance and reliability aspects of the workload and the system on which it is running. 

:::tip Reading logs is tedious?
VC is designed for large scale operations by allowing you to integrate with data storage resources designed for analysis of large data sets. See the documentation on [Data/Telemetry](./0040-telemetry.md) 
to see how to incorporate "big data" resources.
:::

- You will find four or more local files under directory `/virtualclient/logs/`
```bash
logs
├── events-20221109.log
├── metrics-20221109.log
├── metrics.csv
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

## Congratulations !!
You just benchmarked your system with OpenSSL.

- To benchmark your system's crypotography performance holisticaly, we recommend the full profile for OpenSSL: [`PERF-CPU-OPENSSL.json`](https://github.com/microsoft/VirtualClient/blob/main/src/VirtualClient/VirtualClient.Main/profiles/PERF-CPU-OPENSSL.json)
