# Install Java JDK
The Java JDK Virtual Client uses is [Microsoft Build of OpenJDK](https://docs.microsoft.com/en-us/java/openjdk/download). If you wish to use other 
versions of OpenJDK, you need to use your own package store.

:::info
This dependency does not download the JDK itself. The JDK package can be installed using the basic [`dependency package installation`](./0001-install-vc-packages.md). Check example below.
:::

- [MSFT OpenJDK Installation Guide](https://docs.microsoft.com/en-us/java/openjdk/install)
- This dependency does not download the binary. It only finds the OpenJDK path and sets the Environment variable "JAVA_HOME" to that directory. This environment
  variable is used within the code/logic to execute Java binaries from the installed JDK.

## Supported Platform/Architectures
* linux-x64
* linux-arm64
* win-x64
* win-arm64

## JDK version supported
* microsoft-jdk-21.0.1
* microsoft-jdk-17.0.9
* microsoft-jdk-17.0.5
* microsoft-jdk-17.0.3
* microsoft-jdk-17.0.2
* microsoft-jdk-16.0.2
* microsoft-jdk-11.0.19

The JDK packaging process is currently manual. Please reachout to `virtualclient@microsoft.com`, or raise a GitHub issue, to request a particular JDK build.

## Profile Component Parameters
The following section describes the parameters used by the individual component in the profile.

| **Parameter** | **Required** | **Description**                                                                                                 |
|---------------|--------------|-----------------------------------------------------------------------------------------------------------------|
| BlobContainer | Yes          | The name of the Azure storage account blob container where the package exists (e.g. packages).                  |
| BlobName      | Yes          | The name of the file/blob to download within the Azure storage account to download (e.g. microsoft-jdk-17.0.9.zip).    |
| PackageName   | Yes          | The name/identifier used to reference the OpenJDK package downloaded during the preliminary dependency package installation step (e.g. 'javadevelopmentkit'...see example below). |
| Scenario      | No           | A name/identifier for the specific component in the profile. This is used for telemetry purposes only with components in dependency sections of the profile (i.e. cannot be used with --scenarios option on the command line). |
| Extract       | No           | Default = true. True to instruct the Virtual Client that the package is an archive (e.g. .zip, .tgz) and to extract it. False if the file is a standalone file and should be left exactly as-is after download. |

## Example
The following section describes the parameters used by the component in the profile.

* [Profile Example](https://github.com/microsoft/VirtualClient/blob/main/src/VirtualClient/VirtualClient.Main/profiles/PERF-SPECJVM.json)


  ```json
    {
      "Type": "JDKPackageDependencyInstallation",
      "Parameters": {
        "Scenario": "InstallJDKPackage",
        "BlobContainer": "packages",
        "BlobName": "microsoft-jdk-17.0.9.zip",
        "PackageName": "javadevelopmentkit",
        "Extract": true
      }
    }
  ```