# Install .NET SDK
The .NET SDK is a set of libraries and tools that allow developers to create .NET applications and libraries. The dependency implementation in the
Virtual Client allows the user to define the version of the .NET SDK. In practice this is typically a .NET 6.0+ version of the SDK by default.

- [SDK Documentation](https://learn.microsoft.com/en-us/dotnet/core/sdk)

## Supported Platform/Architectures
* linux-x64
* linux-arm64
* win-x64
* win-arm64

## Profile Component Parameters
The following section describes the parameters used by the individual component in the profile.

| **Parameter** | **Required** | **Description**                                         |
|---------------|--------------|---------------------------------------------------------|
| DotNetVersion | Yes          | The version of the .NET SDK to download and install.    | 
| PackageName   | Yes          | The name/identifier used to reference the .NET SDK package download location by other Virtual Client components (e.g. 'dotnetsdk'...see example below). |
| Scenario      | No           | A name/identifier for the specific component in the profile. This is used for telemetry purposes only with components in dependency sections of the profile (i.e. cannot be used with --scenarios option on the command line).                                                      |

## Example
The following section describes the parameters used by the individual component in the profile.

* [Profile Example](https://github.com/microsoft/VirtualClient/blob/main/src/VirtualClient/VirtualClient.Main/profiles/PERF-ASPNETBENCH.json)

  ```json
  {
      "Type": "DotNetInstallation",
      "Parameters": {
          "Scenario": "InstallDotNetSdk",
          "DotNetVersion": "$.Parameters.DotNetVersion",
          "PackageName": "dotnetsdk"
      }
  }
  ```