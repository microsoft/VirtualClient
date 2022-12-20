# Dependency Packages
The following documentation covers the different package store options available in Virtual Client used for downloading and installing
dependencies on the system. Virtual Client supports NuGet feeds as well as Azure Blob stores for hosting dependency packages that need
to be downloaded to the system during the execution of a workload profile. The following sections describes how this works in the Virtual
Client.

There are a few different types of packages to keep in mind from a general sense: Virtual Client packages (vcpkg) and packages inherent to 
the OS platform and that are downloaded/installed on the system using common package managers (e.g. apt, debian, chocolatey). The documentation
below covers packages specifically created for direct integration with the Virtual Client.

## Virtual Client Packages (*.vcpkg)
Virtual Client packages used by the Virtual Client follow a strict folder schema which is similar to the NuGet package schema. NuGet is well-designed 
for the purpose of defining dependency packages and additionally has plenty of tooling support that can be easily integrated into the user local development 
process as well as the automated/Official build process.This folder schema allows the developer to add support for both cross-platform/OS and cross-architecture/CPU scenarios 
in a single package. This consistency makes it easier to integrate these packages into the Virtual Client for just about any type of dependency. It is recommended
that Virtual Client dependency packages be used whenever feasible because they offer the following benefits:

* **Dependency packages ensure consistency in the versions of workloads and monitors to run.**  
  Self-contained packages ensure that it will be the same EXACT software running every single time. This is important to prevent variations in software versions
  causing variations in the results/measurements emitted by the software. This is especially important when running VC for longer periods of time with data analysis
  at the end...minimizing variables!

* **They simplify the coding/development requirements.**  
  By pre-packaging dependencies, developers onboarding new features to the Virtual Client remove a significant amount of work that would otherwise need to be done
  in code. Writing additional code takes time and creates more places in the codes for bugs.

* **They make the runtime dependency installation process more reliable**  
  

* Virtual Client packages are additionally expected to be self-describing. To do this a 'vcpkg' definition
is added to the package folder at the root location (see below).

ALL custom packages created to be used by the Virtual Client are expected to follow the same folder schema protocol. Additionally, it is best to keep the names 
of the packages lower-cased (it makes cross-platform support easier).

### Package Definitions
All Virtual Client packages must have a definition file (.vcpkg) in the parent directory. A .vcpkg definition defines the name of the package, version 
information and metadata that can be used when implementing components in the Virtual Client that use the packages. As a general rule, the name of the
.vcpkg file should match the name defined inside of it. Additionally, both the file name and the 'name' within should be lower-cased. This helps avoid
casing issues when running in cross-platform scenarios.

<div style="font-size:10.5pt">

``` json
# Example 1: geekbench5.vcpkg
{
    "name": "geekbench5",
    "description": "GeekBench5 benchmark toolsets.",
    "metadata": {}
}

# Example 2: lshw.vcpkg
{
    "name": "lshw",
    "description": "Hardware lister for Linux toolset.",
    "version": "B.02.19.59 ",
    "metadata": {
        "commit": "https://github.com/lyonel/lshw/commit/996aaad9c760efa6b6ffef8518999ec226af049a"
    }
}

# Example 2: sqlbackupfiles.vcpkg
{
    "name": "sqlbackupfiles",
    "description": "SQL Server database backup files for restoring full databases on the system.",
    "metadata": {
        "databaseName": "tpch1000gcci",
        "databaseDataFileName": "tpch1000gcci_root",
        "databaseLogFileName": "tpch1000gcci_log"
    }
}
```
</div>

### Packages with Binaries/Scripts that Run Anywhere
Virtual Client supports certain types of workloads/dependencies that can essentially run on 'any' platform/architecture that the Virtual Client itself
runs on. For example, certain workloads use the Java runtime to operate. The binaries and scripts should be placed in the parent directory alongside
the .vcpkg file. The following examples illustrate the expected folder structure.

* /packageroot
  * <div style='color:#3DA4CA'>packagename.vcpkg</div>
  * <div style='color:#3DA4CA'>Workload or dependency binaries, scripts etc... that can run on any system.</div>

<div/>

  <div style="font-size:10.5pt">

  ```
  # Example Package Structure 1
  # ------------------------------------------------------
  # Package = specpower2008.0.0.zip
  # Contains the SPEC Power scripts and toolsets that can run on any system (using the Java Runtime/SDK).

  # Contents of package folder
  /specpower2008.0.0/ccs
  /specpower2008.0.0/ssj
  /specpower2008.0.0/specpower2008.vcpkg

  # Contents of the specpower2008.vcpkg file
  {
    "name": "specpower2008",
    "description": "SPEC Power 2008 workload toolsets.",
    "version": "2008",
    "metadata": {}
  }

  # Example Package Structure 2
  # ------------------------------------------------------
  # Package = tpch-1tb.zip
  # Contains SQL database backup files that can be used on any system where SQL Server runs.

  # Contents of package folder
  /tpch-1tb/sqlbackupfiles.vcpkg
  /tpch-1tb/TPCH-1TB_001.bak
  /tpch-1tb/TPCH-1TB_002.bak
  /tpch-1tb/TPCH-1TB_003.bak
  /tpch-1tb/TPCH-1TB_004.bak
  /tpch-1tb/TPCH-1TB_005.bak
  /tpch-1tb/TPCH-1TB_006.bak
  /tpch-1tb/TPCH-1TB_007.bak
  /tpch-1tb/TPCH-1TB_008.bak
  /tpch-1tb/TPCH-1TB_009.bak
  /tpch-1tb/TPCH-1TB_010.bak

  # Contents of the sqlbackupfiles.vcpkg file
  {
    "name": "sqlbackupfiles",
    "description": "SQL Server database backup files for restoring full databases on the system.",
    "metadata": {
        "databaseName": "tpch1000gcci",
        "databaseDataFileName": "tpch1000gcci_root",
        "databaseLogFileName": "tpch1000gcci_log"
    }
  }
  ```
  </div>

### Packages with Support for Different Platform/Architectures
Virtual Client also supports binaries/scripts that are compiled specific to a set of platform/architectures. For these type of packages, the
folder structure should match the platform/architectures that are supported.

* /packageroot
  * <div style='color:#3DA4CA'>packagename.vcpkg</div>
  * /linux-arm64
    * <div style='color:#3DA4CA'>Contains workload or dependency binaries, scripts etc... that can run on Linux/ARM64 systems.</div>
  * /linux-x64
    * <div style='color:#3DA4CA'>Contains workload or dependency binaries, scripts etc... that can run on Linux/x64/amd64 systems.</div>
  * /win-arm64
    * <div style='color:#3DA4CA'>Contains workload or dependency binaries, scripts etc... that can run on Windows/ARM64 systems.</div>
  * /win-x64
    * <div style='color:#3DA4CA'>Contains workload or dependency binaries, scripts etc... that can run on Windows/x64/amd64 systems.</div>

<div/>

  <div style="font-size:10.5pt">

  ```
  # Example Package Structure 1
  # ------------------------------------------------------
  # Package = geekbench5.1.0.0.zip

  # Contents of package folder
  /geekbench5.1.0.0/geekbench5.vcpkg
  /geekbench5.1.0.0/linux-arm64/GeekBench5.preferences
  /geekbench5.1.0.0/linux-arm64/geekbench.plar
  /geekbench5.1.0.0/linux-arm64/geekbench_aarch64
  /geekbench5.1.0.0/linux-arm64/geekbench5
  /geekbench5.1.0.0/linux-x64/GeekBench5.preferences
  /geekbench5.1.0.0/linux-x64/geekbench.plar
  /geekbench5.1.0.0/linux-x64/geekbench_x84_64
  /geekbench5.1.0.0/linux-x64/geekbench5
  /geekbench5.1.0.0/win-arm64/GeekBench5.preferences
  /geekbench5.1.0.0/win-arm64/geekbench.plar
  /geekbench5.1.0.0/win-arm64/geekbench_aarch64.exe
  /geekbench5.1.0.0/win-arm64/geekbench5
  /geekbench5.1.0.0/win-x64/GeekBench5.preferences
  /geekbench5.1.0.0/win-x64/geekbench.plar
  /geekbench5.1.0.0/win-x64/geekbench_x84_64.exe
  /geekbench5.1.0.0/win-x64/geekbench5

  # Contents of the geekbench5.vcpkg file
  {
    "name": "geekbench5",
    "description": "GeekBench5 benchmark toolsets.",
    "metadata": {}
  }
  ```

  </div>

## What Packages Are Required
The packages that are used as part of a Virtual Client profile are defined in the 'Dependencies' section of the profile. If custom package locations
are used, the package name <u>MUST match the name in the profile dependencies</u> (e.g. 'geekbench5' in the example below).

<div style="font-size:10.5pt">

``` json
# Try to keep package names lower-cased ALWAYS

"Dependencies": [
    {
        "Type": "DependencyPackageInstallation",
        "Parameters": {
            "BlobContainer": "packages",
            "BlobName": "geekbench5.1.2.0.zip",
            "PackageName": "geekbench5",
            "Extract": true
        }
    }
]
```
</div>

## Package Search Locations and Priority
The Virtual Client application supports both packages that exist on the file system as well as the ability to download packages from a store as is
described in the sections that follow. In some cases, it is desirable to deploy packages with the Virtual Client application. The following details 
describes the order by which Virtual Client searches for packages and the requirements of each search. Note that packages downloaded from a package
store will be downloaded to the 'packages' folder and registered (version included).

<div style="color:red">
<div style="font-weight:600">IMPORTANT</div>
With package folder as they exist on the file system, it is best practice to ensure they are always lower-cased. As a general rule this
helps to avoid issues with path conventions between Windows and Unix systems where the former is NOT case-sensitive but the latter IS case-sensitive. It
is simply easier to go with a pattern that works on both by lower-casing your package directory names. You will see this convention in practice in the
examples below.
</div>

* **1) Registered package locations**  
  The Virtual Client allows a developer to register a package/dependency path at runtime. There are cases where this is useful or convenient
  for certain scenarios (e.g. installers, installations). A registered package will have a corresponding .vcpkgreg document/definition in the
  'packages' directory of the Virtual Client application.

  <div style="font-size:10.5pt">

  ```
  e.g.
  # Location Registered at Runtime: C:\any\custom\package\registration\location\customtoolset\1.0.0

  C:\any\custom\package\registration\location\customtoolset.1.0.0
  C:\any\custom\package\registration\location\customtoolset.1.0.0\linux-x64
  C:\any\custom\package\registration\location\customtoolset.1.0.0\linux-arm64
  C:\any\custom\package\registration\location\customtoolset.1.0.0\win-x64
  C:\any\custom\package\registration\location\customtoolset.1.0.0\win-arm64
  ```

  ``` json
  # Example of the corresponding registration document in the 'packages' directory
  # C:\VirtualClient\state\customtoolset.vcpkgreg
  {
      "name": "CustomToolset",
      "description": "Custom toolset to use with other workload binaries.",
      "path": "C:\any\custom\package\registration\location\customtoolset.1.0.0",
      "archivePath": null,
      "specifics": {}
  }
  ```
  </div>

* **2) Search the folder location defined by a user-defined environment variable**  
  A user of the Virtual Client can define an environment variable called <span style="font-weight:600">VCDependenciesPath</span>. This directory will be used
  to discover packages with the highest priority. If a package is not defined here, the Virtual Client will look for the package in the 
  locations noted below. If a package is found in this location, Virtual Client will not search for other locations. The package found
  here will be used.

  <div style="background-color:#f1d235;padding:5pt">
  <div style="font-weight:600">Required Convention:</div>
  Package parent directory names should ALWAYS be lower-cased (e.g. geekbench5 vs. <span style="text-decoration:line-through">Geekbench5</span>).
  </div>
  <br/>

  <div style="font-size:10.5pt">

  ```
  e.g.
  set VCDependenciesPath=C:\any\custom\packages\location

  C:\any\custom\packages\location\geekbench5
  C:\any\custom\packages\location\geekbench5\linux-x64
  C:\any\custom\packages\location\geekbench5\linux-arm64
  C:\any\custom\packages\location\geekbench5\win-x64
  C:\any\custom\packages\location\geekbench5\win-arm64
  ```
  </div>

* **3) Search 'packages' folder location.**  
  The default package store location is the 'packages' folder within the Virtual Client parent directory. Packages can be placed here
  (or downloaded...see below) into this location. The package .vcpkg definition will be used to register the package in the 'packages'
  directory. You can see that a package has been registered by the existence of a .vcpkgreg file in the same directory.

  <div style="background-color:#f1d235;padding:5pt">
  <div style="font-weight:600">Required Convention:</div>
  Package parent directory names should ALWAYS be lower-cased (e.g. geekbench5 vs. <span style="text-decoration:line-through">Geekbench5</span>).
  </div>
  <br/>

  <div style="font-size:10.5pt">

  ```
  e.g.
  C:\VirtualClient\packages\geekbench5.1.0.0\linux-x64
  C:\VirtualClient\packages\geekbench5.1.0.0\linux-arm64
  C:\VirtualClient\packages\geekbench5.1.0.0\win-x64
  C:\VirtualClient\packages\geekbench5.1.0.0\win-arm64
  C:\VirtualClient\packages\geekbench5.vcpkgreg
  ```
  </div>

* **4) Packages downloaded to the 'packages' folder**
If Virtual Client does not find a package pre-existing on the system in the locations noted above, it will download the package into the 'packages' directory.
The package will be registered with a .vcpkgreg file in the same directory.

  <div style="background-color:#f1d235;padding:5pt">
  <div style="font-weight:600">Convention:</div>
  Packages downloaded will ALWAY have parent directory names lower-cased (e.g. geekbench5 vs. <span style="text-decoration:line-through">Geekbench5</span>).
  </div>
  <br/>

  <div style="font-size:10.5pt">

  ```
  # Package Name = geekbench5.1.0.0.zip

  # Would be downloaded to the following location.
  C:\VirtualClient\packages\geekbench5.1.0.0\linux-x64
  C:\VirtualClient\packages\geekbench5.1.0.0\linux-arm64
  C:\VirtualClient\packages\geekbench5.1.0.0\win-x64
  C:\VirtualClient\packages\geekbench5.1.0.0\win-arm64
  C:\VirtualClient\packages\geekbench5.vcpkgreg
  ```
  </div>

### Workload Package Stores
The Virtual Client uses an Azure Blob store in order to maintain packages that contain workload binaries and dependencies. This enables the CRC team to
keep the size of the Virtual Client down to a minimum. Indeed some of the workloads and their dependencies have files sizes so large they would exceed the
maximum allowable size of a NuGet package (in Azure DevOps). In order to enable Virtual Client to download packages from the Azure Blob store, the access token
must be supplied on the command line. The following section describes how to do that.

* [Workload Packages Blob Store](https://ms.portal.azure.com/#@microsoft.onmicrosoft.com/resource/subscriptions/e234e682-48fa-4666-a66d-87d198bff8ca/resourceGroups/rg-crc-virtualclient/providers/Microsoft.Storage/storageAccounts/virtualclientstorage/containersList)
* [Workload Packages NuGet Feed](https://msazure.visualstudio.com/One/_packaging?_a=feed&feed=CRC) 

Please contact the CRC team if you do not have questions or need access to the packages stores.

### Providing Package Store Parameters to Virtual Client
There are scenarios where a secret must be supplied to the Virtual Client in order to access or download dependencies to the system as
part of a workload profile execution. In these scenarios, it is typical for the secret to be referenced in the profile component definition
itself with a placeholder used to define the value. The actual value of that secret is then passed into the Virtual Client on the command line
using the '--parameters' option. Parameters can always be overridden on the command-line in this way. In fact, this applies to any set of Virtual Client 
component parameters whether they are a secret or not. It is the responsibility of the user of Virtual Client (or the execution system) to supply
any required parameters to the Virtual Client that are expected to be overridden. The documentation for each profile will cover the specifics
for parameters that are required.

**Blob Store Packages**  
If the workload package is stored in an Azure Blob store, the following example shows how to provide the required account key
to the Virtual Client in order to authenticate.

<div style="font-size:10.5pt">

``` script
// Preferred Option: Supply the package store connection string or SAS URI like so:
VirtualClient.exe --profile=PERF-CPU-OPENSSL.json --system=Juno --timeout=1440 --packageStore={BlobStoreConnectionString|SAS URI}
```
</div>

### Azure Blob Storage
The preferred package store option for the Virtual Client is an Azure storage account blob store. Azure blob stores can host very large
block blob packages and have high performance when downloading packages to the system. Additionally, Azure blob stores simplify package download
requirements by allowing containers in the blob store to be defined with a "blob anonymous read" access policy. For packages that do not
contain any sensitive binaries or information, this is the ideal option because it does not require the Virtual Client or user of the application
to supply any access keys. Most of the package dependencies used by the Virtual Client contain binaries and content that is publicly available
and thus can be hosted in the blob store with "blob anonymous read" enabled. The Virtual Client supports containers that require access keys
to be provided in order to download the packages as well.

* [Dependency Packages Blob Store](https://ms.portal.azure.com/#@microsoft.onmicrosoft.com/resource/subscriptions/e234e682-48fa-4666-a66d-87d198bff8ca/resourceGroups/rg-crc-virtualclient/providers/Microsoft.Storage/storageAccounts/virtualclientstorage/containersList)  
  (For access to the Package blob store, contact [crc_vc_fte@microsoft.com](mailto:crc_vc_fte@microsoft.com))

##### Defining a Package Dependency  
To define a package dependency for a given workload, insert a 'DependencyPackageInstallation' dependency in the workload profile. The package store
connection string or SAS URI must be supplied on the command line as noted above.

<div style="font-size:10.5pt">

``` json
{
    "Description": "OpenSSL 3.0 CPU Performance Workload",
    "Actions": [
        {
            "Type": "OpenSslExecutor",
            "Parameters": {
                "CommandArguments": "speed -elapsed -evp md5 -seconds 100",
                "PackageName": "openssl",
                "Tags": "CPU,OpenSSL,Cryptography"
            }
        }
    ],
    "Dependencies": [
        {
            "Type": "DependencyPackageInstallation",
            "Parameters": {
                "BlobContainer": "packages",
                "BlobName": "openssl.1.1.0.zip",
                "PackageName": "openssl",
                "Extract": true
            }
        }
    ]
}
```
</div>