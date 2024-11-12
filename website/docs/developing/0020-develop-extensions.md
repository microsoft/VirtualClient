# Developing Extensions
The following sections cover the fundamentals to consider when developing extensions to the Virtual Client. Extensions refers to profiles or component
binaries/.dlls containing actions, monitors, dependency handlers etc... that are developed in a separate repo/location than the Virtual Client platform/core
repo. Extensions allow development teams to add features to the Virtual Client runtime platform that are specialized towards their team's needs and charter.
Before getting started, it is helpful to familiarize yourself with the Virtual Client platform design and concepts.

* [General Developer Guide](./0010-develop-guide.md)
* [Integrating Extensions at Runtime](../guides/0221-usage-extensions.md)

The following example extensions repo can be used for reference to the details described within this guide.

* [CRC-VirtualClient-Examples Git Repo](https://msazure.visualstudio.com/One/_git/CRC-VirtualClient-Examples)

## Platform Libraries


* #### NuGet Packages/Libraries
  * **VirtualClient.Framework**  
    Contains the fundamental classes and interfaces required to develop actions, monitors and dependency handlers for the Virtual Client.

  * **VirtualClient.TestFramework**  
    Contains classes and interfaces that ease the task of writing unit and functional tests for extensions codebase.

  ``` xml
  <Project Sdk="Microsoft.NET.Sdk">

    <PropertyGroup>
        <TargetFramework>net8.0</TargetFramework>
        <RootNamespace>CRC.VirtualClient.Extensions</RootNamespace>
    </PropertyGroup>

    <ItemGroup>
        <PackageReference Include="VirtualClient.Framework" Version="1.0.1900.660" />
    </ItemGroup>

    <Import Project="$([MSBuild]::GetDirectoryNameOfFileAbove($(MSBuildThisFileDirectory), Module.props))\Module.props" />
  </Project>
  ```

## Platform Requirements
The Virtual Client is a .NET 8.0 application. Assemblies containing extensions to the Virtual Client must likewise be built against the .NET 8.0 framework
SDK. Assemblies compiled for the Virtual Client must have the term 'VirtualClient' in them. It is recommended that the following format be used when
naming your assemblies. This will help to avoid any conflicts with extensions produced by other teams.


```
# Format:
{TeamName}.VirtualClient.Extensions.{ComponentTypesWithin}.dll

# Examples:
CRC.VirtualClient.Extensions.Actions.dll
CRC.VirtualClient.Extensions.Monitors.dll
CRC.VirtualClient.Extensions.Dependencies.dll
```

In addition, the following attribute must be added to a code file (typically an AssemblyInfo.cs file) within the each project that contains components
that when compiled can be used in the Virtual Client.


``` csharp
# Contents of an AssemblyInfo.cs file.
[assembly: VirtualClient.Contracts.VirtualClientComponentAssembly]
```

## Versioning Requirements
The following section provides requirements to follow when considering versions of the platform libraries to reference. The VC Team follows a process of
semantic versioning with the Virtual Client runtime application and framework libraries. The versions of the assemblies/.dlls/.exes can be used to determine
which version of the NuGet packages should be used. The following resource explains the versioning process.

It is recommended that you follow the same "semantic versioning" strategy as is followed for the Virtual Client platform. The following examples 
illustrate versions of extensions (when compiled) that would be expected to work with a specific Virtual Client release. It is really only the
'major' and 'minor' versions that are important here. The following table illustrates the general idea.

| VirtualClient.Framework Library Version  | Supported VirtualClient.exe Versions  | Version to use for Extensions Library Builds | Examples |
|------------------------------------------|---------------------------------------|----------------------------------------------|----------|
| 1.0.\* (e.g. 1.0.1900.660)               | 1.0.\* (e.g. 1.0.1930.665)            | 1.0.\* (e.g. 1.0.0.0)                        | VirtualClient.exe version 1.0.1900.660 will support framework library versions from 1.0.0.0 to < 1.1.0.0. The framework versions will almost always have a version similar to the runtime .exe version; however, this illustrates the idea of the version range supported. 
| 1.1.\* (e.g. 1.1.1950.120)               | 1.1.\* (e.g. 1.1.1960.125)            | 1.1.\* (e.g. 1.1.0.0)                        | VirtualClient.exe version 1.1.1950.120 will support framework library versions from 1.1.0.0 to < 1.2.0.0.
| 2.0.\* (e.g. 2.0.2000.500)               | 2.0.\* (e.g. 2.0.2001.505)            | 2.0.\* (e.g. 2.0.0.0)                        | VirtualClient.exe version 2.0.2000.500 will support framework library versions from 2.0.0.0 to < 2.1.0.0.
| 2.1.\* (e.g. 2.1.2100.100)               | 2.1.\* (e.g. 2.1.2200.300)            | 2.1.\* (e.g. 2.1.0.0)                        | VirtualClient.exe version 2.1.2100.100 will support framework library versions from 2.1.0.0 to < 2.2.0.0.

## Packaging Requirements
Virtual Client extensions are loaded into runtime execution via dependency packages similarly to the way that other types of dependencies
(e.g. workload binaries, scripts etc...) are integrated. A Virtual Client dependency package is nothing more than a structured .zip file
that contains the files required for a particular dependency. Dependency packages allow Virtual Client to support a number of different scenarios
including those that are disconnected from the internet. Additionally, they enable each version of the Virtual Client to operate using exact/known
binaries, scripts etc... This is an important concept both for producing repeatable results as well as for simplifying deployment and setup requirements.


For extensions packages, the following illustrates the expected folder structure and contents. Note that you do not have to compile your libraries
specifically for the platform/architectures noted below. Compilation against 'AnyCPU' is sufficient to keep things simple. An extensions package
will have the following content.

* **Package Definition File**  
  A package definition is a simple JSON file that is placed in the root directory of the package that defines the name and description of the 
  package, its version and any metadata properties desired. In this way, Virtual Client packages are "self-describing". This file should typically be
  named the same as the 'name' property within its contents and MUST have a **.vcpkg** extension. Additionally, the 'metadata' should contain a property
  named **extensions** that is set to true.


  ``` json
  # Example contents of the package definition file named 'crcvcextensions.vcpkg'.
  {
    "name": "crcvcextensions",
    "description": "VC Team Virtual Client extensions.",
    "version": "1.0.1",
    "metadata": {
        "extensions": true
    }
  }
  ```

* **Profile Extensions**  
  Profile extensions are not required. If they exist, the files should be placed inside of the appropriate folder for each platform/architecture supported 
  (e.g. win-x64, linux-x64) in a folder named **profiles**.

* **Binary/.dll Extensions**  
   Binary extensions are not required. If they exist, the binary/.dll extensions should be placed inside of the appropriate folder for each platform/architecture supported 
  (e.g. win-x64, linux-x64).

  The following illustrates the folder structure expected for an extensions package called 'crc.vc.extensions'.


  ```bash
   -------------------------------------------------------------------
   # Folder Structure
   -------------------------------------------------------------------
   /crc.vc.extensions
      crcvcextensions.vcpkg
          A Virtual Client package (.vcpkg) definition that has a metadata property called 'extensions' set to true. This file should exist
          in the root directory of the package.
                
      /linux-arm64
            {binaries}
                Any additional binaries that can be used on Linux/ARM64 systems are placed directly in the linux-arm64 folder.
                
            /profiles
                Folder contains any additional profiles that can be used on Linux/ARM64 systems.

      /linux-x64
            {binaries}
                Any additional binaries that can be used on Linux/x64 systems are placed directly in the linux-x64 folder.
                
            /profiles
                Folder contains any additional profiles that can be used on Linux/x64 (Intel, AMD) systems.
             
      /win-arm64
            {binaries}
                Any additional binaries that can be used on Windows/ARM64 systems are placed directly in the win-arm64 folder.
                
            /profiles
                Folder contains any additional profiles that can be used on Windows/ARM64 systems.
          
      /win-x64
            {binaries}
                Any additional binaries that can be used on Windows/x64 systems are placed directly in the win-x64 folder.
                
            /profiles
                Folder contains any additional profiles that can be used on Windows/x64 (Intel, AMD) systems.

   -------------------------------------------------------------------
   # Example 
   -------------------------------------------------------------------
   /crc.vc.extensions
   /crc.vc.extensions/crcvcextensions.vcpkg

   # binaries...
   /crc.vc.extensions/linux-arm64/CRC.VirtualClient.Extensions.Actions.dll
   /crc.vc.extensions/linux-arm64/CRC.VirtualClient.Extensions.Actions.pdb
   /crc.vc.extensions/linux-x64/CRC.VirtualClient.Extensions.Actions.dll
   /crc.vc.extensions/linux-x64/CRC.VirtualClient.Extensions.Actions.pdb
   /crc.vc.extensions/win-arm64/CRC.VirtualClient.Extensions.Actions.dll
   /crc.vc.extensions/win-arm64/CRC.VirtualClient.Extensions.Actions.pdb
   /crc.vc.extensions/win-x64/CRC.VirtualClient.Extensions.Actions.dll
   /crc.vc.extensions/win-x64/CRC.VirtualClient.Extensions.Actions.pdb

   # profiles...
   /crc.vc.extensions/linux-arm64/profiles/EXAMPLE-WORKLOAD-PROFILE.json
   /crc.vc.extensions/linux-x64/profiles/EXAMPLE-WORKLOAD-PROFILE.json
   /crc.vc.extensions/win-arm64/profiles/EXAMPLE-WORKLOAD-PROFILE.json
   /crc.vc.extensions/win-x64/profiles/EXAMPLE-WORKLOAD-PROFILE.json
  ```

## How To Use/Integrate Extensions
Once extensions have been developed and an extensions package exists, they can be used in the Virtual Client runtime. See the following documentation
for details on how to integrate extensions into the runtime.

### Place Extensions Directly in Virtual Client Application Folder
The simplest way to integrate extensions binaries, profiles or packages into Virtual Client is to copy the files directly to the Virtual Client application
directory (e.g. /virtualclient, /virtualclient/profiles, /virtualclient/packages).

* Extensions binaries/.dlls can be placed directly in the root directory of the Virtual Client application (i.e. right beside the VirtualClient executable).  

  ``` bash
  # Given an installation location /home/users/virtualclient, the binaries/.dlls 
  # can be copied into the root directory directly.
  #
  /home/users/virtualclient/Custom.VirtualClient.Extensions.Actions.dll
  /home/users/virtualclient/Custom.VirtualClient.Extensions.Dependencies.dll
  /home/users/virtualclient/Custom.VirtualClient.Extensions.Monitors.dll
  ```

* Extensions profiles can be placed directly in the Virtual Client 'profiles' directory alongside the existing/out-of-box profiles.

  ``` bash
  # Given an installation location /home/users/virtualclient, the profiles 
  # can be copied into the 'profiles' directory directly.
  #
  /home/users/virtualclient/profiles/PERF-CUSTOM-WORKLOAD-A.json
  /home/users/virtualclient/profiles/PERF-CUSTOM-WORKLOAD-B.json
  ```

* Extensions packages can be placed directly in the Virtual Client 'packages' directory.
  
  ``` bash
  # Given an installation location /home/users/virtualclient, the packages 
  # can be copied into the 'packages' directory directly.
  /home/users/virtualclient/packages/customworkload_a.1.0.0.zip
  /home/users/virtualclient/packages/customworkload_b.1.0.0.zip
  ```

### Reference Extensions from Other Locations on the System
Virtual Client enables the user/developer to define additional locations to look for extensions during execution startup. To do so, the user/developer can set
custom environment variables on the system to provide Virtual Client with the alternate locations. Virtual Client will look for dependencies in both the default
locations (normal operation) as well as within these alternate locations.

#### Supported Environment Variables
The following environment variables can be used to define alternate locations for dependencies:

* **VC_LIBRARY_PATH**  
  Defines 1 or more path locations where extensions assemblies/.dlls exist and that should be loaded at runtime. Multiple directory paths can be defined separated
  by a semi-colon ';' character (similar to the Windows and Linux `PATH` environment variable). Note that Virtual Client will search the immediate directory only
  for extension assemblies/.dlls. Recursive subdirectory searches are not supported.

  ``` bash
  # Example Folder Contents:
  # /VirtualClient.Extensions.Actions
  #      /VirtualClient.Extensions.Actions.dll
  #      /VirtualClient.Extensions.Actions.pdb
  #
  # /VirtualClient.Extensions.Monitors
  #      /VirtualClient.Extensions.Monitors.dll
  #      /VirtualClient.Extensions.Monitors.pdb
  #
  # On Windows systems
  C:\VirtualClient> set VC_LIBRARY_PATH=C:\Extensions\VirtualClient.Extensions.Actions
  C:\VirtualClient> set VC_LIBRARY_PATH=C:\Extensions\VirtualClient.Extensions.Actions;C:\Extensions\VirtualClient.Extensions.Monitors

  # On Linux systems.
  /home/user/virtualclient$ export VC_LIBRARY_PATH=/home/user/Extensions/VirtualClient.Extensions.Actions
  /home/user/virtualclient$ export VC_LIBRARY_PATH=/home/user/Extensions/VirtualClient.Extensions.Actions;/home/user/Extensions/VirtualClient.Extensions.Monitors
  ```

* **VC_PACKAGES_DIR**  
  Defines the directory path where Virtual Client packages (including extensions packages) exist and to where packages should be downloaded. This overrides the
  default packages location (e.g. /packages) such that it will not be used for package searches or downloads. The must be a single directory. Recursive subdirectory searches are not supported.

  ``` bash
  # Example Folder Contents:
  # /custom_packages
  #      /workload_a.1.0.0.zip
  #      /workload_b.1.0.0.zip
  #      /workload_c.1.0.0.zip
  #      /workload_d.1.0.0.zip
  
  # On Windows systems
  C:\VirtualClient> set VC_PACKAGES_DIR=C:\custom_packages

  # On Linux systems.
  /home/user/virtualclient$ export VC_PACKAGES_DIR=/home/user/custom_packages
  ```

### Downloaded Extensions from a Package Store
The default for most Virtual Client scenarios is to download extensions from a package store. The **VirtualClient bootstrap** command can be used to download
extensions from a package store and install them.

```bash
# Package/Blob Store Structure
/container=packages/blob=crc.vc.extensions.zip

# Execute bootstrap command to download and install the extensions
C:\Users\Any\VirtualClient> VirtualClient.exe bootstrap --package=crc.vc.extensions.zip --name=crcvcextensions --packages="{Connection|SAS URI}"
 
# Execute an extensions profile
C:\Users\Any\VirtualClient> VirtualClient.exe --profile=EXAMPLE-WORKLOAD-PROFILE.json --timeout=1440 --packages="{Connection|SAS URI}"
```

### A Custom-Defined Bootstrap Profile is Used
The developer can choose to use a custom profile for bootstrapping/installing extensions as well.

``` json
# Profile = BOOTSTRAP-EXTENSIONS.json
{
  "Description": "Installs extensions from a package store.",
  "Dependencies": [
      {
          "Type": "DependencyPackageInstallation",
          "Parameters": {
              "Scenario": "InstallCRCExtensionsPackage",
              "BlobContainer": "packages",
              "BlobName": "crc.vc.extensions.zip",
              "PackageName": "crcvcextensions",
              "Extract": true
          }
      }
  ]
}
```
  
  ...Then you can use it! Note that the profile can exist in another directory location and be referenced by the path to the file (full or relative path).


  ```bash
  # Package/Blob Store Structure
  /container=packages/blob=crc.vc.extensions.zip

  # 1) Execute Bootstrap Command
  /VirtualClient/VirtualClient.exe --profile=S:\Some\Other\Folder\BOOTSTRAP-EXTENSIONS.json --dependencies --packages="{BlobStoreConnectionString|SAS URI}"

  # 2) Execute Extensions Profile
  /VirtualClient/VirtualClient.exe --profile=EXAMPLE-WORKLOAD-PROFILE.json --timeout=1440
  ```

## How To Debug Extensions in Visual Studio
This next section is going to cover the topic of debugging Virtual Client extensions. It is very helpful at times when doing development work to have
the ability to run the Virtual Client runtime executable while enabling the ability to step through the code line by line. For this section, we will be
looking at how to do this using the Visual Studio IDE and facilities that it has to make debugging easier. Make sure to review the section "Debugging Virtual Client Code"
at the bottom of the [General Developer Guide](./0010-develop-guide.md) for more information on debugging.

* **Debug Using Unit/Functional Tests**  
  This option is documented in the general developer guide. The technique is the same for debugging extensions as it is for any other component.

* **Debug by Running a Custom Profile #1**  
  The preliminary setup for this option is the same as what is documented in the general developer guide in the section on debugging at the bottom. There are a few extra steps here 
  where the developer must provide the Visual Studio runtime with a hint as to where to find the extensions binaries. We will be essentially setting up Visual Studio to run an instance of
  the VirtualClient.exe on the system and supplying it with the location of the extensions binaries.

  Note that in this scenario, we are executing the debugging scenario from Visual Studio in the extensions project. It is a good idea (for consistency) to reference a 
  "just-built" version of the Virtual Client runtime executable in many cases. This is typically done by cloning the Virtual Client platform repo, building it and referencing the 
  VirtualClient.exe from the built output location (e.g. /\{repoDir\}/out/bin/Debug/x64/VirtualClient.Main/net8.0/VirtualClient.exe).

  ``` json
  # A custom profile is created and placed on the file system somewhere (typically somewhere outside of the source directory). In this profile, the
  # custom action/executor component is added to the actions.
  {
    "Description": "Debug Custom Workload Executor",
    "Actions": [
        {
            "Type": "CustomWorkloadExecutor",
            "Parameters": {
                "Scenario": "Scenario1",
                "Duration": "00:00:10",
                "ExampleParameter1": "AnyValue1",
                "ExampleParameter2": 4567,
                "PackageName": "exampleworkload",
                "Tags": "Test,VC"
            }
        }
    ],
    "Dependencies": [
        {
            "Type": "DependencyPackageInstallation",
            "Parameters": {
                "Scenario": "InstallExampleWorkloadPackage",
                "BlobContainer": "packages",
                "BlobName": "exampleworkload.1.0.0.zip",
                "PackageName": "exampleworkload",
                "Extract": true
            }
        }
    ]
  }
  ```


  The Virtual Client platform allows the developer to define a custom environment variable **VC_LIBRARY_PATH** to provide an extra location 
  to search for binaries that contain Virtual Client components. This environment variable should be set to the build output path for your extensions.

  ```
  e.g.

  # Example output directory for extensions
  S:\one\crc-virtualclient-examples\out\bin\Release\AnyCPU\CRC.VirtualClient.Extensions.Actions\net8.0
  ```

**Setup Visual Studio for debugging:**

  1. Set the solution configuration to **Debug** at the top of the Visual Studio IDE window.

  2. Set the extensions project containing the code to debug as the startup project. To do so, right-click on the project in the Solution Explorer and select 
    **Set as Startup Project** from the context menu.

  3. Right-click on the VirtualClient.Main project and open the **Debug** options. Set the following information.
     * Launch = Executable

     * Executable = \{PathToVirtualClientExe\}  
       (e.g. ```S:\one\crc-air-workloads\out\bin\Debug\x64\VirtualClient.Main\net8.0\VirtualClient.exe```)

     * Application arguments = \{VirtualClientCommandLine\}   
       (e.g. `--profile=S:\one\debugging\DEBUG-EXAMPLE-WORKLOAD.json --profile=MONITORS.NONE.json --packages="https://virtualclient..."`)

     * Environment variables = Add the `VC_LIBRARY_PATH` variable and the path to your built extensions binaries.  
       (e.g. `VC_LIBRARY_PATH = S:\one\crc-virtualclient-examples\out\bin\Debug\x64\CRC.VirtualClient.Extensions.Actions\net8.0`)

  4. Place a breakpoint in the code where you like (e.g. in the InitializeAsync or ExecuteAsync methods of your component).

  5. Click the play/continue button at the top-center of the Visual Studio IDE window (or press the F5 key).
  