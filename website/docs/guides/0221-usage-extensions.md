# Using/Integrating Extensions
The Virtual Client platform supports a few different "extensions" models enabling developers to create feature sets and for users to integrate those into the platform
runtime. This document covers how to use and integrate extensions into the platform. Additional documentation exists that focuses on the development process itself.

* [Developing Extensions](../developing/0020-develop-extensions.md)

## What are Extensions
Extensions in a simple sense are extra feature sets for actions, monitors or dependency handlers that can be developed independently of the core Virtual Client platform
but that can be used by the platform. They enable teams and individuals to create these feature sets in their own repos on their own schedules/time. Developers and users of 
the Virtual Client platform have a few different options to consider when integrating extensions. The sections that follow describes how these extensions
models work and how to use them.

## .NET-Based/Managed Code Extensions
The .NET-based extensions model allows developers and users to provide binaries/.dlls written in a .NET language such as C# and custom profiles to a build of the 
Virtual Client runtime/executable. The Virtual Client then loads these binaries/.dlls at runtime as if they were a part of the Virtual Client runtime package itself.
This model is handy for teams who want to work in their own repo to create new actions, monitors and dependency handlers and custom profiles that reference these
new components. Reference the documentation noted above on `Developing Extensions` for a deeper discussion on the development process involved.

### Versioning 
The VC Team follows a process of semantic versioning with the Virtual Client .NET-based runtime application and framework libraries. The versions of the assemblies/.dlls/.exes 
can be used to determine which version of the Virtual Client runtime/executable (e.g. from NuGet packages) should be used. Use the following instructions to determine
the Virtual Client runtime platform version to use.

* **The version of the Virtual Client runtime/.exe must be >= to the framework version referenced by the extensions**  
  When developing Virtual Client extensions, a version of the `VirtualClient.Framework` package is referenced. This framework package will have a version (e.g. 1.15.0). 
  The binaries/.dlls built and that contain the extensions features have thus taken a "contract" against this specific version of Virtual Client. In order to integrate
  the extensions binaries/.dlls into the Virtual Client, the version of the Virtual Client must be greater than or equal to the version of the `VirtualClient.Framework` 
  package used to compile the extensions projects.

  For example, suppose the extensions projects were compiled against VirtualClient.Framework version 1.15.0. These extensions can be ran by any version of Virtual Client >=
  to 1.15.0 (e.g. 1.15.0, 1.15.1, 1.15.10 etc...). However, these extensions COULD NOT be used with Virtual Client version 1.14.0.

* **Use a version of Virtual Client runtime/.exe show major and minor version matches the framework version referenced by the extensions**  
  To ensure compatibility it is generally recommended that the version of the Virtual Client runtime/.exe used to integrate extensions have the same
  major and minor version as the `VirtualClient.Framework` package version referenced by the projects in the extensions repo. The following table illustrates
  a few examples as a reference.

  <mark>In general, backwards compatibility for Virtual Client releases is maintained within a {{major}}.{{minor}} range only (e.g. 1.14.0, 1.14.1, 1.14.2, ~~1.15.0~~). The Virtual Client team attempts to honor this most of the time; however, no guarantees are made.</mark>

  https://semver.org/

  ``` bash
  # Semantic Version Format:
  {major_version}.{minor_version}.{patch}
  ```

  Note that the versions of the runtime are for illustratory purposes only and do not necessarily represent actual version  
  of the runtime.

  | VirtualClient.Framework Version (for Extensions) | Virtual Client Runtime Version Recommendations |
  |--------------------------------------------------|------------------------------------------------|
  | 1.14.0                                           | 1.14.0, 1.14.1, 1.14.2...but less than ~~1.15.0~~ |
  | 1.14.5-beta (version = 1.14.5)                   | 1.14.5-beta (version = 1.14.5), 1.14.6-beta (version = 1.14.6), 1.14.7-beta (version = 1.14.7)...but less than ~~1.15.0~~ |
  | 1.15.5                                           | 1.15.5, 1.15.6-beta (version = 1.15.6), 1.15.7-beta (version = 1.15.7), 1.15.8...but less than ~~1.16.0~~     |

### How to Use .NET-Based/Managed Code Extensions
When developing .NET-based extensions, the user/developer will create a simple folder structure with the extensions binaries/.dlls and profiles in it. The folder structure
is covered more in-depth in the `Developing Extensions` documentation noted at the top. The following section describes how to incorporate .NET-based extensions.

* **Place Extensions Folder in Default 'packages' Location**  
  The default way to incorporate extensions into the Virtual Client runtime is to add the extensions package into the Virtual Client default 'packages' folder.
  Extensions themselves have a special folder structure that is consistent with other packages (e.g. workloads, monitors) that are incorporated into Virtual Client.
  This makes it easy to incorporate the extensions because they can just be copied into the 'packages' folder. In fact the extensions packages can even be placed
  in the 'packages' folder as a .zip/archive file (e.g. any.virtualclient.extensions.1.0.0.zip) and Virtual Client will extract them on startup.

  ``` bash
  # e.g.
  # Virtual Client is installed (or exists) on the system.
  ~/virtualclient.1.15.0/linux-x64
  ~/virtualclient.1.15.0/linux-x64/VirtualClient

  # Extensions packages/folders are placed in the 'packages' folder within.
  ~/virtualclient.1.15.0/linux-x64/packages
  ~/virtualclient.1.15.0/linux-x64/packages/any.virtualclient.extensions.1.0.0
  ~/virtualclient.1.15.0/linux-x64/packages/any.virtualclient.extensions.1.0.0/any.virtualclient.extensions.1.0.0.vcpkg

  # Each extensions package may contain 1 or more platform/architecture folders.
  ~/virtualclient.1.15.0/linux-x64/packages/any.virtualclient.extensions.1.0.0/linux-arm64
  ~/virtualclient.1.15.0/linux-x64/packages/any.virtualclient.extensions.1.0.0/linux-x64
  ~/virtualclient.1.15.0/linux-x64/packages/any.virtualclient.extensions.1.0.0/win-arm64
  ~/virtualclient.1.15.0/linux-x64/packages/any.virtualclient.extensions.1.0.0/win-x64

  # Binary extensions within each supported platform/architecture folder.
  ~/virtualclient.1.15.0/linux-x64/packages/any.virtualclient.extensions.1.0.0/linux-x64/Any.VirtualClient.Extensions.Actions.dll
  ~/virtualclient.1.15.0/linux-x64/packages/any.virtualclient.extensions.1.0.0/linux-x64/Any.VirtualClient.Extensions.Dependencies.dll
  ~/virtualclient.1.15.0/linux-x64/packages/any.virtualclient.extensions.1.0.0/linux-x64/Any.VirtualClient.Extensions.Monitors.dll

   # Profile extensions within each supported platform/architecture folder
   # in a 'profiles' sub-folder.
  ~/virtualclient.1.15.0/linux-x64/packages/any.virtualclient.extensions.1.0.0/linux-x64/profiles
  ~/virtualclient.1.15.0/linux-x64/packages/any.virtualclient.extensions.1.0.0/linux-x64/profiles/MONITORS-CUSTOM-1.json
  ~/virtualclient.1.15.0/linux-x64/packages/any.virtualclient.extensions.1.0.0/linux-x64/profiles/PERF-CUSTOM-WORKLOAD-1.json
  ~/virtualclient.1.15.0/linux-x64/packages/any.virtualclient.extensions.1.0.0/linux-x64/profiles/PERF-CUSTOM-WORKLOAD-2.json

  # Virtual Client will search for extensions in the default 'packages' folder location.
  ~/virtualclient.1.15.0/linux-x64$ VirtualClient --profile=PERF-CUSTOM-WORKLOAD-1.json ...
  ```

* **Define an Alternate Location for the Extensions Folder Location**  
  Virtual Client supports allowing a user to define an alternate/secondary location to discover extensions. To define an alternate location for discovering extensions packages, 
  define the path to the directory where the packages exist using a `VC_PACKAGES_PATH` environment variable. Multiple alternate locations can be defined (similar to the PATH 
  environment variable) by delimiting the paths with a semi-colon (e.g. /home/user/extensions_packages1;/home/user/extensions_packages1). During startup, the runtime will search 
  for packages in both the default 'packages' directory as well as in the alternate location.

  <mark>
  Note that there may not be duplicate binaries or profiles (by name) in the target location. If duplicates (by name) are found, a runtime error will occur.
  </mark>

  ``` bash
  # e.g.
  # Suppose that ackages exist in an alternate location.
  /home/user/extensions_packages

  # Extensions packages/folders are placed in this 'extensions_packages' location.
  /home/user/extensions_packages/any.virtualclient.extensions.1.0.0
  /home/user/extensions_packages/any.virtualclient.extensions.1.0.0/any.virtualclient.extensions.1.0.0.vcpkg

  # Each extensions package may contain 1 or more platform/architecture folders.
  /home/user/extensions_packages/any.virtualclient.extensions.1.0.0/linux-arm64
  /home/user/extensions_packages/any.virtualclient.extensions.1.0.0/linux-x64
  /home/user/extensions_packages/any.virtualclient.extensions.1.0.0/win-arm64
  /home/user/extensions_packages/any.virtualclient.extensions.1.0.0/win-x64

  # Binary extensions within each supported platform/architecture folder.
  /home/user/extensions_packages/any.virtualclient.extensions.1.0.0/linux-x64/Any.VirtualClient.Extensions.Actions.dll
  /home/user/extensions_packages/any.virtualclient.extensions.1.0.0/linux-x64/Any.VirtualClient.Extensions.Dependencies.dll
  /home/user/extensions_packages/any.virtualclient.extensions.1.0.0/linux-x64/Any.VirtualClient.Extensions.Monitors.dll

   # Profile extensions within each supported platform/architecture folder
   # in a 'profiles' sub-folder.
  /home/user/extensions_packages/any.virtualclient.extensions.1.0.0/linux-x64/profiles
  /home/user/extensions_packages/any.virtualclient.extensions.1.0.0/linux-x64/profiles/MONITORS-CUSTOM-1.json
  /home/user/extensions_packages/any.virtualclient.extensions.1.0.0/linux-x64/profiles/PERF-CUSTOM-WORKLOAD-1.json

  # Virtual Client can be instructed to search for extensions in this alternate
  # location in addition to the default 'packages' folder location.
  ~/virtualclient.1.15.0/linux-x64$ export VC_PACKAGES_PATH=/home/user/extensions_packages
  ~/virtualclient.1.15.0/linux-x64$ VirtualClient --profile=PERF-CUSTOM-WORKLOAD-1.json ...
  ```

* **Define an Alternate Location for the Extensions Binaries/.dlls Location**  
  Virtual Client supports allowing a user to define an alternate/secondary location to discover extensions binaries/.dlls specifically. To define an alternate location for 
  discovering extensions binaries/.dlls, define the path to the directory where the binaries exist using a `VC_LIBRARY_PATH` environment variable. During startup,
  the runtime will search for packages in both the default 'packages' directory as well as in the alternate location. Multiple alternate locations can be defined 
  (similar to the PATH environment variable) by delimiting the paths with a semi-colon (e.g. /home/user/extensions_binaries1;/home/user/extensions_binaries1).  
  This is particularly helpful in testing/debugging scenarios where developers are compiling extensions binaries on the same system enabling a fast inner-development 
  loop.
  
  <mark>
  Note that there may not be duplicate binaries (by name) in the target location. If duplicates (by name) are found, a runtime error will occur.
  </mark>

  ``` bash
  # e.g.
  # Suppose that the binaries exist in an alternate location (e.g. a build output location).
  /home/user/repos/any.team.extensions/out/bin/Debug/x64/Any.Team.VirtualClient.Extensions/net8.0/linux-x64/publish

  # Virtual Client can be instructed to search for extensions binaries in this alternate
  # location in addition to the default 'packages' folder location.
  ~/virtualclient.1.15.0/linux-x64$ export VC_LIBRARY_PATH=/home/user/repos/any.team.extensions/out/bin/Debug/x64/Any.Team.VirtualClient.Extensions/net8.0/linux-x64/publish
  ~/virtualclient.1.15.0/linux-x64$ VirtualClient --profile=/home/user/repos/any.team.extensions/src/Any.Team.VirtualClient.Extensions/profiles/PERF-CUSTOM-WORKLOAD-1.json ...
  ```

### Script-Based Extensions
The script-based extensions model allows developers and users to provide interpreted scripts written in languages such as Python or PowerShell to to a build of the
Virtual Client runtime/executable. Virtual Client has out-of-box support for running custom scripts and for defining the full workflow specifics within a custom profile.
This extensions model does not typically require any work in the Virtual Client codebase. Users/developers place their extensions in a Virtual Client package the same as
other packages and include any content or binaries (if required) to support the script executions. The folder structure for extensions is covered more in-depth in the 
`Developing Extensions` documentation noted at the top.

Regarding the subject of binaries required to support scripts, the Virtual Client team offers versions of certain script-based/interpreted languages such as PowerShell 7/Core
and Python. These packages can be referenced in a Virtual Client profile and downloaded to support the execution of scripts that depend upon them. The following section describes 
how to incorporate script-based extensions.

* **Place Script Extensions Folder in Default 'packages' Location**  
  The default way to incorporate script-based extensions into the Virtual Client runtime is to add the extensions package into the Virtual Client default 'packages' folder.
  Extensions themselves have a special folder structure that is consistent with other packages (e.g. workloads, monitors) that are incorporated into Virtual Client.
  This makes it easy to incorporate the extensions because they can just be copied into the 'packages' folder. In fact the script extensions packages can even be placed
  in the 'packages' folder as a .zip/archive file (e.g. any.script.extensions.1.0.0.zip) and Virtual Client will extract them on startup.

  ``` bash
  # e.g.
  # Virtual Client is installed (or exists) on the system.
  ~/virtualclient.1.15.0/linux-x64
  ~/virtualclient.1.15.0/linux-x64/VirtualClient

  # Script extensions packages/folders are placed in the 'packages' folder within.
  ~/virtualclient.1.15.0/linux-x64/packages
  ~/virtualclient.1.15.0/linux-x64/packages/any.script.extensions.1.0.0
  ~/virtualclient.1.15.0/linux-x64/packages/any.script.extensions.1.0.0/any.script.extensions.1.0.0.vcpkg

  # Each script extensions package may contain 1 or more scripts (and supporting binaries).
  ~/virtualclient.1.15.0/linux-x64/packages/any.script.extensions.1.0.0/linux-x64/execute.py
  ~/virtualclient.1.15.0/linux-x64/packages/any.script.extensions.1.0.0/linux-x64/some_executable

  # There may be different scripts (and supporting binaries) per platform/architecture. This facility is typically 
  # employed to support applications compiled to run on different OS platforms such as Windows and Linux as well 
  # as on different CPU architectures such as x64 and ARM64. However, there are times when this separation is needed 
  # to support cross-platform/cross-architecture for scripts also.
  ~/virtualclient.1.15.0/linux-x64/packages/any.script.extensions.1.0.0/linux-x64/execute.py
  ~/virtualclient.1.15.0/linux-x64/packages/any.script.extensions.1.0.0/linux-x64/some_executable

  C:\virtualclient.1.15.0\win-x64\packages\any.script.extensions.1.0.0/win-x64/execute.py
  C:\virtualclient.1.15.0\win-x64\packages\any.script.extensions.1.0.0/win-x64/some_executable.exe

   # Profile extensions within each supported platform/architecture folder
   # in a 'profiles' sub-folder.
  ~/virtualclient.1.15.0/linux-x64/packages/any.script.extensions.1.0.0/linux-x64/profiles
  ~/virtualclient.1.15.0/linux-x64/packages/any.script.extensions.1.0.0/linux-x64/profiles/SCRIPT-WORKLOAD-1.json
  ~/virtualclient.1.15.0/linux-x64/packages/any.script.extensions.1.0.0/linux-x64/profiles/SCRIPT-WORKLOAD-2.json
  ```

  Script-based extensions can be incorporated into profiles directly. These profiles can be defined in the script-based extensions
  package similarly to .NET-based extensions packages (as illustrated above).

  ``` json
  
  {
    "Description": "Profile to execute script-based extensions.",
    "Metadata": {
        "SupportedPlatforms": "linux-x64,linux-arm64,win-x64,win-arm64"
    },
    "Parameters": { },
    "Actions": [
    	{
            "Type": "PowerShellExecutor",
            "Parameters": {
				"Scenario": "ExecuteWorkload1",
                "ScriptPath": "{PackagePath/Platform:any.script.extensions1}/Execute.ps1",
                "PackageName": "powershell7"
          }
        },
		{
            "Type": "PythonExecutor",
            "Parameters": {
				"Scenario": "ExecuteWorkload2",
                "ScriptPath": "{PackagePath/Platform:any.script.extensions2}/install.py",
                "PackageName": "python3"
          }
        }
    ],
    "Dependencies": [
	   {
            "Type": "DependencyPackageInstallation",
            "Parameters": {
                "Scenario": "DownloadPowerShell7Package",
                "BlobContainer": "packages",
                "BlobName": "powershell.7.1.3.zip",
                "PackageName": "powershell7",
                "Extract": true
            }
        },
		{
            "Type": "DependencyPackageInstallation",
            "Parameters": {
                "Scenario": "DownloadPythonPackage",
                "BlobContainer": "packages",
                "BlobName": "python.3.10.5.zip",
                "PackageName": "python3",
                "Extract": true
            }
        },
		{
            "Type": "DependencyPackageInstallation",
            "Parameters": {
                "Scenario": "DownloadScriptExtensionsPackage1",
                "BlobContainer": "packages",
                "BlobName": "any.script.extensions1.1.0.0.zip",
                "PackageName": "any.script.extensions1",
                "Extract": true
            }
        },
		{
            "Type": "DependencyPackageInstallation",
            "Parameters": {
                "Scenario": "DownloadScriptExtensionsPackage2",
                "BlobContainer": "packages",
                "BlobName": "any.script.extensions2.1.0.0",
                "PackageName": "any.script.extensions2",
                "Extract": true
            }
        }
    ]
  }

  ```

* **Define an Alternate Location for the Script Extensions Folder Location**  
  Another way to integrate script-based extension is to define an alternate/secondary location to discover extensions. To define an alternate location for discovering 
  extensions packages, define the path to the directory where the packages exist using a `VC_PACKAGES_PATH` environment variable. Multiple alternate locations can be defined 
  (similar to the PATH environment variable) by delimiting the paths with a semi-colon (e.g. /home/user/extensions_packages1;/home/user/extensions_packages1). 
  During startup, the runtime will search for packages in both the default 'packages' directory as well as in the alternate location.

  ``` bash
  # e.g.
  # Suppose that the script extensions packages exist in an alternate location.
  /home/user/script_extensions_packages

  # Script extensions packages/folders are placed in this 'script_extensions_packages' location.
  /home/user/script_extensions_packages/any.script.extensions.1.0.0
  /home/user/script_extensions_packages/any.script.extensions.1.0.0/any.script.extensions.1.0.0.vcpkg

  # Each script extensions package may contain 1 or more scripts (and supporting binaries).
  /home/user/script_extensions_packages/any.script.extensions.1.0.0/linux-x64/execute.py
  /home/user/script_extensions_packages/any.script.extensions.1.0.0/linux-x64/some_executable

  # There may be different scripts (and supporting binaries) per platform/architecture. This facility is typically 
  # employed to support applications compiled to run on different OS platforms such as Windows and Linux as well 
  # as on different CPU architectures such as x64 and ARM64. However, there are times when this separation is needed 
  # to support cross-platform/cross-architecture for scripts also.
  /home/user/script_extensions_packages/any.script.extensions.1.0.0/linux-x64/execute.py
  /home/user/script_extensions_packages/any.script.extensions.1.0.0/linux-x64/some_executable

   # Profile extensions within each supported platform/architecture folder
   # in a 'profiles' sub-folder.
  /home/user/script_extensions_packages/any.script.extensions.1.0.0/linux-x64/profiles
  /home/user/script_extensions_packages/any.script.extensions.1.0.0/linux-x64/profiles/SCRIPT-WORKLOAD-1.json
  /home/user/script_extensions_packages/any.script.extensions.1.0.0/linux-x64/profiles/SCRIPT-WORKLOAD-2.json
  ```