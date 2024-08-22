# VC Packages
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
process as well as the automated/Official build process. This folder schema allows the developer to add support for both cross-platform/OS and cross-architecture/CPU scenarios 
in a single package. This consistency makes it easier to integrate these packages into the Virtual Client for just about any type of dependency. It is recommended
that Virtual Client dependency packages be used whenever feasible because they offer the following benefits:

* **VC Packages ensure consistency in the versions of workloads and monitors to run.**  
  Self-contained packages ensure that it will be the same EXACT software running every single time. This is important to prevent variations in software versions
  causing variations in the results/measurements emitted by the software. This is especially important when running VC for longer periods of time with data analysis
  at the end...minimizing variables!

* **VC Packages simplify the coding/development requirements.**  
  By pre-packaging dependencies, developers onboarding new features to the Virtual Client remove a significant amount of work that would otherwise need to be done
  in code. Writing additional code takes time and creates more places in the codes for bugs.

* **VC Packages help make the runtime dependency installation process more reliable**  
  Additional reliability at runtime is created when placing pre-packaged dependencies in storage locations for which the Virtual Client team or the individual developer own.
  This is because it reduces the number of dependencies on which the application must rely in order to install the dependencies it needs. Were the dependencies to be installed
  from a third-party location, the application is subject to the availability of those resource locations without an expedient recourse. The Virtual Client team for example uses Azure
  storage accounts to host dependency packages because they are highly reliable and can be easily replicated in the case of availability issues/outages.

* **VC Packages enable support for "disconnected" scenarios**  
  Dependency packages can be included with the Virtual Client application (within the /packages directory). If the packages are included, the Virtual Client does
  not have to download the dependencies because they are already on the system. This allows the user to run the Virtual Client in scenarios whereby there may not be
  an internet connection (e.g. private/local network scenarios).

## Package Definitions
All Virtual Client packages must have a definition file (.vcpkg) in the parent directory. A .vcpkg definition defines the name of the package, version 
information and metadata that can be used when implementing components in the Virtual Client that use the packages. As a general rule, the name of the
.vcpkg file should match the name defined inside of it. Additionally, both the file name and the 'name' within <b>should be lower-cased</b>. This helps avoid
casing issues when running in cross-platform scenarios (e.g. Linux, Windows). This file makes Virtual Client packages self-describing.

<div class="code-section">

```
# Example File 1: geekbench5.vcpkg
{
    "name": "geekbench5",
    "description": "GeekBench5 benchmark toolsets.",
    "metadata": {}
}

# Example File 2: lshw.vcpkg
{
    "name": "lshw",
    "description": "Hardware lister for Linux toolset.",
    "version": "B.02.19.59 ",
    "metadata": {
        "commit": "https://github.com/lyonel/lshw/commit/996aaad9c760efa6b6ffef8518999ec226af049a"
    }
}

# Examplele 3: sqlbackupfiles.vcpkg
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

## Package Naming Conventions
With package folder as they exist on the file system, it is best practice to ensure they are always lower-cased. As a general rule this
helps to avoid issues with path conventions between Windows and Unix systems where the former is NOT case-sensitive but the latter IS case-sensitive. It
is simply easier to go with a pattern that works on both by lower-casing your package directory names. You will see this convention in practice in the
examples below.

## Packages with Binaries/Scripts that Run Anywhere
Virtual Client supports certain types of workloads/dependencies that can essentially run on 'any' platform/architecture that the Virtual Client itself
runs on. For example, certain workloads use the Java runtime to operate. The binaries and scripts should be placed in the parent directory alongside
the .vcpkg file. The following examples illustrate the expected folder structure.

* /packageroot
  * packagename.vcpkg
  * Workload or dependency binaries, scripts etc... that can run on any system.
  <br/><br/>

  <div class="code-section">

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

## Packages with Support for Different Platform/Architectures
Virtual Client also supports binaries/scripts that are compiled specific to a set of platform/architectures. For these type of packages, the
folder structure should match the platform/architectures that are supported. The following examples illustrate the expected folder structure.

* /packageroot
  * packagename.vcpkg
  * /linux-arm64
    * Contains workload or dependency binaries, scripts etc... that can run on Linux/ARM64 systems.
  * /linux-x64
    * Contains workload or dependency binaries, scripts etc... that can run on Linux/x64/amd64 systems.
  * /win-arm64
    * Contains workload or dependency binaries, scripts etc... that can run on Windows/ARM64 systems.
  * /win-x64
    * Contains workload or dependency binaries, scripts etc... that can run on Windows/x64/amd64 systems.
  <br/><br/>

  <div class="code-section">

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

## Zipping Up Your Packages
Once you have created the folder structure for your package and have placed all of the binaries, scripts, etc... into the folders where you like,
the next step is to zip it up so that it can be uploaded to a package store location (or to a shared folder location on your system or network).
There is a handy PowerShell script in source that makes it very easy and ensures the folder structure within the .zip file created remains exactly
as you defined it. If you have cloned the Virtual Client repo, you will already have the script. You can download it or copy the contents into your own
file if not. Note that this requires PowerShell 7.

:::note
> Why a Script?  
> Windows out-of-box "Send to Compressed (zipped) folder" places your directory in a zip file whose name is the same. This duplication of hierarchy is
> not desirable which is why the VC team uses the custom PowerShell script noted below
:::

* [Create-ZipFile.ps1](https://github.com/microsoft/VirtualClient/blob/main/src/VirtualClient/Create-ZipFile.ps1)

``` powershell
# Open the PowerShell 7 console and execute the command to zip your package.
C:\source\repos\virtualclient\src\VirtualClient> .\Create-ZipFile "C:\Users\Any\Desktop\customworkload.1.2.3" "C:\Users\Any\Desktop\customworkload.1.2.3.zip"
```

## What Packages Are Required
The packages that are used as part of a Virtual Client profile are defined in the 'Dependencies' section of the profile. If custom package locations
are used, the package name <u>MUST match the name in the profile dependencies</u> (e.g. 'geekbench5' in the example below).

<div class="code-section">

```
# Package names should be lower-cased ALWAYS

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

## Package Search + Download Locations
The Virtual Client application supports both packages that exist on the file system as well as the ability to download packages from a store as is
described in the sections that follow. In some cases, it is desirable to deploy packages with the Virtual Client application. The following details 
describes the order by which Virtual Client searches for packages and the requirements of each search. Note that packages downloaded from a package
store will be downloaded to the 'packages' folder and registered (version included).

* **1) Default Search Location**  
  The default package store location is the 'packages' folder within the Virtual Client parent directory. Packages can be placed here
  (or downloaded) into this location. The package .vcpkg definition will be used to register the package in the 'packages'
  directory. You can see that a package has been registered by the existence of a .vcpkgreg file in the same directory.

  <div class="code-section">

  ```
  # e.g.
  C:\VirtualClient\packages\geekbench5.1.0.0\linux-x64
  C:\VirtualClient\packages\geekbench5.1.0.0\linux-arm64
  C:\VirtualClient\packages\geekbench5.1.0.0\win-x64
  C:\VirtualClient\packages\geekbench5.1.0.0\win-arm64
  C:\VirtualClient\packages\geekbench5.vcpkgreg
  ```
  </div>

* **2) Search the folder location defined by a user-defined environment variable**  
  A user of the Virtual Client can define an environment variable called `VC_PACKAGES_DIR` to override the default packages location. The directory defined
  will be used (instead of the default 'packages' folder location) to discover packages when defined. Similarly to the default packages location, 
  package .vcpkg definitions will be used to register the package in this directory. You can see that a package has been registered by the existence of a 
  .vcpkgreg file in the same directory.

  <div class="code-section">

  ```
  # e.g.
  # Windows example
  set VC_PACKAGES_DIR=C:\any\custom\packages\location

  C:\any\custom\packages\location\geekbench5
  C:\any\custom\packages\location\geekbench5\geekbench5.vcpkg
  C:\any\custom\packages\location\geekbench5\linux-x64
  C:\any\custom\packages\location\geekbench5\linux-arm64
  C:\any\custom\packages\location\geekbench5\win-x64
  C:\any\custom\packages\location\geekbench5\win-arm64

  # Linux example
  export VC_PACKAGES_DIR=/home/user/any/custom/packages/location

  /home/user/any/custom/packages/location/geekbench5
  /home/user/any/custom/packages/location/geekbench5/geekbench5.vcpkg
  /home/user/any/custom/packages/location/geekbench5/linux-x64
  /home/user/any/custom/packages/location/geekbench5/linux-arm64
  /home/user/any/custom/packages/location/geekbench5/win-x64
  /home/user/any/custom/packages/location/geekbench5/win-arm64
  ```
  </div>

## Package Stores
The Virtual Client team uses Azure storage accounts to maintain packages that contain workload binaries and dependencies. This enables the Virtual Client team to
keep the size of the Virtual Client down to a minimum for deployment purposes while also making it easy for the application to download/integrate dependencies. Indeed 
some of the workloads and their dependencies have files sizes that exceed multiple gigabytes. Virtual Client allows the user to provide a connection string or SAS token/URI
on the command line so that the packages can be downloaded. The following section describes how to do that. 

Please contact the Virtual Client team if you do not have questions or need access to the packages stores.

**Support for Azure Storage Accounts**  
If the workload package is stored in an Azure storage account store, the following example shows how to provide the required connection string or
SAS token/URI to the Virtual Client for the container in which the package/blob exists.
to the Virtual Client in order to authenticate.

<div class="code-section">

``` script
# Supply the package store connection string or SAS URI like so:
VirtualClient.exe --profile=PERF-CPU-OPENSSL.json --system=Azure --timeout=1440 --packageStore={BlobStoreConnectionString|SAS URI}
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

#### Defining a Package Dependency  
To define a package dependency for a given workload, insert a 'DependencyPackageInstallation' dependency in the workload profile. The package store
connection string or SAS URI must be supplied on the command line as noted above.

<div class="code-section">

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