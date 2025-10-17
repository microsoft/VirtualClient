---
id: getting-started
sidebar_position: 1
---

# Getting Started
The sections below will help the user install the Virtual Client

In this document, we are going to run a "hello world" version of VirtualClient: benchmark your system's crypotography performance, with OpenSSL Speed, using 
SHA256 algorithm.

## Installation

Virtual Client is a self-contained .NET 9 application, so "Installation" really just means copying the Virtual Client package onto your system. It runs out-of-box on 
[all operating systems supported by .NET 9](https://github.com/dotnet/core/blob/main/release-notes/9.0/supported-os.md).

### Zip Package
You can find latest production quality release zip packages in [GitHub Releases](https://github.com/microsoft/VirtualClient/releases).

### NuGet Packages
All builds and releases of Virtual Client are published as NuGet packages for each platform-architecture (e.g. linux-arm64, linux-x64, win-arm64, win-x64).  

* **[linux-arm64]**  
  Builds that run on Linux operating systems with ARM64 processors.  
  https://www.nuget.org/packages/VirtualClient.linux-arm64

* **linux-x64**  
  Builds that run on Linux operating systems with Intel/AMD X64 processors.  
  https://www.nuget.org/packages/VirtualClient.linux-x64

* **win-arm64**  
  Builds that run on Windows operating systems with ARM64 processors.  
  https://www.nuget.org/packages/VirtualClient.win-arm64

* **win-x64**  
  Builds that run on Windows operating systems with Intel/AMD X64 processors.  
  https://www.nuget.org/packages/VirtualClient.win-x64

You can download in a browser directly from NuGet by defining the version and copying any of the following URIs to the browser address bar. 
* https://www.nuget.org/api/v2/package/VirtualClient.linux-arm64/{version}
* https://www.nuget.org/api/v2/package/VirtualClient.linux-x64/{version}
* https://www.nuget.org/api/v2/package/VirtualClient.win-arm64/{version}
* https://www.nuget.org/api/v2/package/VirtualClient.win-arm64/{version}

### NuGet/Zip Package Contents
The Virtual Client NuGet package (*.nupkg) is just a zip file. On Windows systems, you can simply change the extension to .zip and extract. You can also unzip with
programs such as 'unzip' or '7zip'.

- The Virtual Client application/executable can be found in the path locations as follows for the appropriate platform-architecture

  ``` treeview
  VirtualClient.linux-arm64/
  ├── content/
  |   ├── linux-arm64/
  |   |   └── VirtualClient
  └── etc.

  VirtualClient.linux-x64/
  ├── content/
  |   ├── linux-x64/
  |   |   └── VirtualClient
  └── etc.

  VirtualClient.win-arm64/
  ├── content/
  |   └── win-arm64/
  |       └── VirtualClient.exe
  └── etc.

  VirtualClient.win-x64/
  ├── content/
  |   └── win-x64/
  |       └── VirtualClient.exe
  └── etc.
  ```

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

## Building the Source Code
If preferable, the Virtual Client source code can be built on your local system. This is useful for picking up the absolute latest changes
to the source code and for testing changes locally. Before attempting to build the Virtual Client repo, ensure the fo

* [Install the .NET SDK 9.0.x](https://dotnet.microsoft.com/en-us/download/dotnet/9.0)
* Build artifacts are output to the following locations. The `/out/bin` directory is where all compiled binaries/executables are output. The `/out/obj` directory
  will contain intermediate files used during compilation. The `/out/packages` directory will contain any packages that are created during build + packaging 
  processes.
  
  ``` treeview
  {rootdir}/
  ├── out/
  |   ├── bin/
  |   |   └── Debug|Release/
  |   |       └── AnyCPU/
  |   |       └── x64/
  |   |       └── ARM64/
  |   ├── obj/
  |   ├── packages/
  ```

### Building On a Windows System
The following section provides instructions for building on Windows systems.

* In the repo root directory, run the following commands:

  ``` script
  # Build the repo
  C:\repos\virtualclient> build.cmd

  # Build packages (e.g. NuGet) from the build artifacts
  C:\repos\virtualclient> build-packages.cmd

  ```
* The build process will create ready-to-run builds of the Virtual Client for all supported platforms and architectures. Virtual Client executable and binaries can be found in the repo 'out' directory in corresponding architecture/runtimes folder locations. 

  ``` bash
  {rootdir}\out\bin\Release\ARM64\VirtualClient.Main\net9.0\linux-arm64\VirtualClient
  {rootdir}\out\bin\Release\ARM64\VirtualClient.Main\net9.0\win-arm64\VirtualClient.exe
  {rootdir}\out\bin\Release\x64\VirtualClient.Main\net9.0\linux-x64\VirtualClient
  {rootdir}\out\bin\Release\x64\VirtualClient.Main\net9.0\win-x64\VirtualClient.exe
  ```
* VirtualClient is a self-contained .NET application. The application can be run from the build output locations noted above or copied to another
  system. When simply copy the contents of the folder for the specific platform-architecture (shown above) to the system on which you want to run. 
  Note that when running on Linux systems, the 'VirtualClient' binary will need to be attributed as executable (e.g. ```chmod +x /home/user/virtualclient/linux-x64/VirtualClient```).

### Building On a Linux System
The following section provides instructions for building on Linux systems.

:::info
You need to install .NET SDK for building VirtualClient locally. You could use command like `sudo snap install dotnet-sdk` or refer to [.NET documentation](https://learn.microsoft.com/en-us/dotnet/core/install/linux).
:::

* In the repo root directory, run the following commands:

  ``` script
  # Build the repo for all platforms (linux-x64,linux-arm64,win-x64,win-arm64)
  ~/repo/VirtualClient$ chmod +x ./build.sh
  ~/repo/VirtualClient$ ./build.sh
  ```

## Run a Simple Profile
The following section illustrates how to run a simple profile on your system. This profile will run the OpenSSL workload on your system for a brief period of time. This workload
evaluates the performance of the CPU(s) on your system but will not cause it any harm.

:::caution
When running this profile on your system, the Virtual Client will download OpenSSL binaries onto your system, under `/virtualclient/packages/openssl.3.0.0/`.
If you prefer to keep your system unchanged, consider running the Virtual Client on a different system/VM.
:::

* Execute the following command to run an example VC profile. This profile runs an OpenSSL workload.

  [`--profile=GET-STARTED-OPENSSL.json`](https://github.com/microsoft/VirtualClient/blob/main/src/VirtualClient/VirtualClient.Main/profiles/GET-STARTED-OPENSSL.json)

  ```bash
  # --profile=GET-STARTED-OPENSSL.json - instructs VC to run a simple version of the full OpenSSL benchmark targeting the SHA256 algorithm.
  # --logger=csv - instructs VC to emit metrics to a CSV file.
  # --log-to-file - instructs VC to write the output of toolsets and process to log files.

  sudo ./VirtualClient --profile=GET-STARTED-OPENSSL.json --logger=csv --log-to-file
  ```

* Virtual Client also supports referencing remote profiles that are accessible via a simple URL:
  
  ``` bash
  sudo ./VirtualClient --profile=https://raw.githubusercontent.com/microsoft/VirtualClient/main/src/VirtualClient/VirtualClient.Main/profiles/GET-STARTED-OPENSSL.json
  ```

## Results and Logs
Each Virtual Client workload or monitor will emit results of some kind. Key decision making information will often be parsed and emitted in a structured "metrics" form.
Metrics are typically numeric values that represent measurements for key/important performance and reliability aspects of the workload and the system on which it is running. 

Virtual Client emits metrics as well as informational log files to a `/logs` folder inside of the application directory by default.

```bash
logs/
├── metrics.csv
└── openssl/
    └── 2025-10-16-102345435627-sha256.log
    └── 2025-10-16-102822729761-sha256.log
```

:::tip
VC is designed to support larger scale operations integrating with big data storage resources use to analyze of large data sets. See the documentation 
below to learn how you can incorporate "big data" Azure resources.
:::

## Congratulations !!
You are ready to begin using Virtual Client to run a range of different supported industry standard benchmarks and customer representative workloads. The following
links provide details for natural next steps.

* [Supported Workloads](https://microsoft.github.io/VirtualClient/docs/category/workloads/)
* [Supported Monitors](https://microsoft.github.io/VirtualClient/docs/category/monitors/)
* [Profiles](./0011-profiles.md)
* [Command Line Options](./0010-command-line.md)
* [Usage Examples](./0200-usage-examples.md)
* [Telemetry/Data](./0040-telemetry.md)
* [Azure Event Hubs Telemetry Integration](./0600-integration-blob-storage.md)
* [Azure Storage Account Integration](./0610-integration-event-hub.md)

