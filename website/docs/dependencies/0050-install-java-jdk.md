# Install Java JDK
The Java JDK Virtual Client uses is [Microsoft Build of OpenJDK](https://docs.microsoft.com/en-us/java/openjdk/download).

:::info
This dependency derived from DependencyPackageInstallation [`dependency package installation`](./0001-install-vc-packages.md) for downloading and installing JDK via blob container.Check example below.
:::

- [MSFT OpenJDK Installation Guide](https://docs.microsoft.com/en-us/java/openjdk/install)
- This dependency operates in two modes. Initial approach is downloading OpenJDK package from the blob container, installing it and configuring 
  the environment variable "JAVA_HOME" to the directory of installed Open JDK path. The other approach is to copy preferred Open JDK version or your own JDK to the packages folder and 
  configure the environment variable "JAVA_HOME" to the copied path. 
- The configured environment variable is used within the code/logic to execute Java binaries from the installed JDK.

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

## Installing packages using blob container

The specified JDK package is downloaded and installed/extracted using the parameters blob container and blob name.If you prefer to use another version or replace the downloaded version with 
new version, replace the BlobName with preferred version (refer example below) and delete the previous version in packages folder (for instance VirtualClient\content\win-x64\packages).

### Profile Component Parameters
The following section describes the parameters used by the component in the profile profile for installing packages using blob container.

| **Parameter** | **Required** | **Description**                                                                                                 |
|---------------|--------------|-----------------------------------------------------------------------------------------------------------------|
| BlobContainer | Yes          | The name of the Azure storage account blob container where the package exists (e.g. packages).                  |
| BlobName      | Yes          | The name of the file/blob to download within the Azure storage account to download (e.g. microsoft-jdk-17.0.9.zip).    |
| PackageName   | Yes          | The name/identifier used to reference the OpenJDK package downloaded during the preliminary dependency package installation step (e.g. 'javadevelopmentkit'...see example below). |
| Scenario      | No           | A name/identifier for the specific component in the profile. This is used for telemetry purposes only with components in dependency sections of the profile (i.e. cannot be used with --scenarios option on the command line). |
| Extract       | No           | Default = true. True to instruct the Virtual Client that the package is an archive (e.g. .zip, .tgz) and to extract it. False if the file is a standalone file and should be left exactly as-is after download. |


### Example
The following section describes the parameters used by the component in the profile for installing packages using blob container.

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

## Installing packages without blob container
If you prefer to use different versions of OpenJDK or your own JDK instead of downloading and installing from blob container, please adhere to the following steps.
- Remove any existing JDK packages in the platform-specific packages folder (for instance VirtualClient\content\win-x64\packages) before pasting another JDK version or your own JDK.
- Inside the packages folder, create a new folder named corporationname-jdk-version (for instance microsoft-jdk-21.0.1 or oracle-jdk-21.0.2 ).
- Within the newly created jdk folder, create a file named javadevelopmentkit.vcpkg and include the parameters specified in the example below.
- Create a subfolder inside the jdk folder with name as platformspecific-architecture (for instance win-x64/win-arm64/linux-x64/linux-arm64); 
  the subfolder path should be created as ( for instance  VirtualClient\content\win-x64\packages\microsoft-jdk-12.0.1\win-x64).
- Copy all extracted files from your own JDK or different JDK versions into the platform specific subfolder path (for instance  VirtualClient\content\win-x64\packages\microsoft-jdk-12.0.1\win-x64).

### Example
The following section describes the parameters to be added in javadevelopmentkit.vcpkg (This is required only if you are using your own JDK or copying a different version into packages folder 
instead of downloading/installing via blob container)

```
# Example File 1: Path of file - VirtualClient\content\win-x64\packages\microsoft-jdk-21.0.1\javadevelopmentkit.vcpkg
{
    "name": "javadevelopmentkit",
    "description": "Microsoft Java Development Kit (JDK) and runtime.",
    "version": "21.0.1",
    "metadata": {
    }
}

# Example File 2: Path of file - VirtualClient\content\win-arm64\packages\oracle-jdk-21.0.2\javadevelopmentkit.vcpkg
{
    "name": "javadevelopmentkit",
    "description": "Oracle Java Development Kit (JDK).",
    "version": "21.0.2",
    "metadata": {

    }
}

```

### Profile Component Parameters
The following section describes the parameters used by the component in the profile for installing packages without blob container.

| **Parameter** | **Required** | **Description**                                                                                                 |
|---------------|--------------|-----------------------------------------------------------------------------------------------------------------|
| PackageName   | Yes          | The name/identifier used to reference the OpenJDK package downloaded during the preliminary dependency package installation step (e.g. 'javadevelopmentkit'...see example below). |
| Scenario      | No           | A name/identifier for the specific component in the profile. This is used for telemetry purposes only with components in dependency sections of the profile (i.e. cannot be used with --scenarios option on the command line). |

### Example
The following section describes the parameters used by the component in the profile for installing packages without blob container.

  ```json
    {
      "Type": "JDKPackageDependencyInstallation",
      "Parameters": {
        "Scenario": "InstallJDKPackage",
        "PackageName": "javadevelopmentkit"
      }
    }
  ```