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
| PackageName   | Yes          | The name/identifier used to reference the OpenJDK package downloaded during the preliminary dependency package installation step (e.g. 'javadevelopmentkit'...see example below). |
| Scenario      | No           | A name/identifier for the specific component in the profile. This is used for telemetry purposes only with components in dependency sections of the profile (i.e. cannot be used with --scenarios option on the command line).                                                      |

## Example
The following section describes the parameters used by the individual component in the profile.

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