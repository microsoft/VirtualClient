# Using/Integrating Extensions
The Virtual Client platform supports a few different "extensions" models enabling developers to create feature sets and for users to integrate those into the platform
runtime. This document covers how to use and integrate extensions into the platform. Additional documentation exists that focuses on the development process itself.

* [Developing Extensions](../developing/0020-develop-extensions.md)

## What are Extensions
Extensions in a simple sense are extra feature sets for actions, monitors or dependency handlers that can be developed independently of the core Virtual Client platform
but that can be used by the platform. They enable teams and individuals to create these feature sets in their own repos on their own schedules/time.

## Extensions Models Supported
Developers and users of the Virtual Client platform have a few different options to consider when integrating extensions. The following describes a high-level overview
of each of them.

### .NET-Based Extensions
The .NET-based extensions model allows developers and users to provide binaries/.dlls written in a .NET language such as C# and custom profiles to a build of the 
Virtual Client runtime/executable. The Virtual Client then loads these binaries/.dlls at runtime as if they were a part of the Virtual Client runtime package itself.
This model is handy for teams who want to work in their own repo to create new actions, monitors and dependency handlers and custom profiles that reference these
new components. Reference the documentation noted above on 'Developing Extensions' for a deeper discussion on the development process involved.

The following section illustrates a few of the aspects of the concept.

``` bash
# The extensions are placed in the 'extensions' folder in the Virtual Client 
# application root directory.
#
# e.g.
# Virtual Client install on the system.
/home/user/virtualclient.1.15.0/linux-x64
/home/user/virtualclient.1.15.0/linux-x64/VirtualClient

# Extensions are installed in the 'extensions' folder within.
/home/user/virtualclient.1.15.0/linux-x64/extensions/

# Extensions binaries/.dlls go in the '../extensions folder
/home/user/virtualclient.1.15.0/linux-x64/extensions/Any.VirtualClient.Extensions.Actions.dll
/home/user/virtualclient.1.15.0/linux-x64/extensions/Any.VirtualClient.Extensions.Dependencies.dll
/home/user/virtualclient.1.15.0/linux-x64/extensions/Any.VirtualClient.Extensions.Monitors.dll

# Extensions profiles go in the '../extensions/profiles' folder.
/home/user/virtualclient.1.15.0/linux-x64/extensions/profiles
/home/user/virtualclient.1.15.0/linux-x64/extensions/profiles/MONITORS-CUSTOM-1.json
/home/user/virtualclient.1.15.0/linux-x64/extensions/profiles/PERF-CUSTOM-WORKLOAD-1.json
/home/user/virtualclient.1.15.0/linux-x64/extensions/profiles/PERF-CUSTOM-WORKLOAD-2.json
```

### Script-Based Extensions
The script-based extensions model allows developers and users to provide interpreted scripts written in languages such as Python or PowerShell to to a build of the
Virtual Client runtime/executable. Virtual Client has out-of-box support for running custom scripts and for defining the full workflow specifics within a custom profile.
This extensions model relies upon the scripts being writt

## Extensions Usage Requirements
The following section provides guidance on requirements to consider when integrating extensions into the Virtual Client.

### Versioning Requirements
<mark>This applies to the .NET-based extensions model only.</mark>

The VC Team follows a process of semantic versioning with the Virtual Client runtime application and framework libraries. The versions of the assemblies/.dlls/.exes 
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
