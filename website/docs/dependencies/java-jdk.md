---
id: java-jdk
---
# Java JDK Installation
The Java JDK Virtual Client uses is [Microsoft Build of OpenJDK](https://docs.microsoft.com/en-us/java/openjdk/download). If you wish to use other versions of OpenJDK, you need to use your own package store.

:::info
This dependency does not download the JDK itself. It is usually done with [`Dependency Package Installation`](./dependency-package.md).
:::

- [MSFT OpenJDK Installation Guide](https://docs.microsoft.com/en-us/java/openjdk/install)
- This dependency does not download the binary. It only finds the OpenJDK path and sets the Environment variable "JAVA_HOME" to that directory.

## Parameters
| **Parameter** | **Required** | **Description**                                                                                                 |
|---------------|--------------|-----------------------------------------------------------------------------------------------------------------|
| PackageName   | Yes          | Reference the OpenJDK packagename downloaded with DependencyPackageInstallation.                                |
| Scenario      | No           | Name for telemetry purpose. Does not change functionality.                                                      |



## Examples

```json {12-16}
{
    "Type": "DependencyPackageInstallation",
    "Parameters": {
        "Scenario": "InstallJavaSDKPackage",
        "BlobContainer": "packages",
        "BlobName": "microsoft-jdk-17.0.3.zip",
        "PackageName": "javadevelopmentkit",
        "Extract": true
    }
},
{
    "Type": "JavaDevelopmentKitInstallation",
    "Parameters": {
        "Scenario": "InstallJavaSDK",
        "PackageName": "javadevelopmentkit"
    }
}
```

### Supported runtimes
win-x64, win-arm64, linux-x64, linux-arm64