# Workload Onboarding Process
The following documentation provides guidance on the steps that the Virtual Client requires to onboard workloads to platform. The steps
defined are not exactly the same every time; however, this gives a developer the general idea of what to expect.

## Step 1: Research and Understand the Workload Software
* Research the proper use of the workload software to fully understand how to run it correctly on the system. If a subject matter expert is available, 
  connect with them to get perspective on the workload software.
* Collect and keep track of documentation, download locations, licenses etc... related to the workload software. This is particularly important if the
  workload software is not already pre-built but instead must be built/compiled from a repo by the developer.
* Run the workload software yourself (on a range of systems if possible) and gather examples of the results/output.
* Inspect the results/output to determine the list of important metrics that should be captured from them. Sometimes the results are not emitted by the workload
  but are instead read from the system on which it is running (e.g. performance counters).
* Try to get all questions that you have answered such that you have a sense of truly understanding what the workload software does and how to operate
  it for trustworthy results on the system.

## Step 2: Create VC Packages and Uploading to Storage
It is recommended that any workload software that can be packaged in a Virtual Client package (*.vcpkg) is packaged this way. There are a host of benefits
to packaging workloads and dependencies in easy-to-consume Virtual Client packages.

* [VC Packages](./0040-vc-packages.md)
* [Storage Account Support](../guides/0600-integration-blob-storage.md)

## Step 3: Create Parsers and Unit Tests
* Create a parser class file with name \<Workload_name>ResultsParser.cs(e.g. WebFundamentalsResultsParser.cs) in project VirtualClient.Parser.
* Write unit tests for the parser created in project VirtualClient.Parser.UnitTests with the name \<Workload_name>ResultsParserTests.cs(e.g. WebFundamentalsResultsParserTests.cs)
* To store all the required input files for the tests, create a folder with the name of workload in the examples folder in VirtualClient.Parser.UnitTests project.
* Update VirtualClient.Parser.UnitTests.csproj file with the details of example files.

```xml
    <None Update="Examples\WebFundamentals\WebFundamentalsInvalidExample.xml">
        <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
    </None>
    <None Update="Examples\WebFundamentals\WebFundamentalsInvalidMetricCountExample.xml">
        <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
    </None>
    <None Update="Examples\WebFundamentals\WebFundamentalsExample.xml">
        <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
    </None>
```

## Step 4: Update Package Manager and Unit Tests
* Update PackageManager.cs of project VirtualClient.Core to include the details of all the packages required to be downloaded.
* Update PackageManagerTests.cs of project VirtualClient.Core.UnitTests to include the unit tests of the changes.

## Step 5: Profile Creation
* Create json file with name PERF-\<PERF_CRITERION>-\<Workload_Name>.json (eg. PERF-WEB-WEBFUNDAMENTALS.json) in profiles folder of VirtualClient.Main project.
* This file contains all the dependencies that are required by the workload.
* Dependencies are included in the project VortualClient.Dependencies.
* Update VirtualClient.Main.csproj file and VirtualClient.Actions.FunctionalTests.csproj file with the json file created.

## Step 6: Create Executor and Unit Tests
* Create a folder with the name of workload in VirtualClient.Actions project which will contain all the files related to executor.
  * **Single-system Workloads**  
    * [Example Workload Executor](https://github.com/microsoft/VirtualClient/tree/main/src/VirtualClient/VirtualClient.Actions/Examples)
    * [OpenSSL Workload Executor](https://github.com/microsoft/VirtualClient/tree/main/src/VirtualClient/VirtualClient.Actions/OpenSSL)
    * [OpenFOAM Workload Executor](https://github.com/microsoft/VirtualClient/blob/main/src/VirtualClient/VirtualClient.Actions/OpenFOAM/OpenFOAMExecutor.cs)

  * **Multi-system, Client/Server Workloads**  
    * [Example Workload Executor](https://github.com/microsoft/VirtualClient/tree/main/src/VirtualClient/VirtualClient.Actions/Examples/ClientServer)
    * [Redis Workload Executor](https://github.com/microsoft/VirtualClient/tree/main/src/VirtualClient/VirtualClient.Actions/Redis)
    * [Memcached Workload Executor](https://github.com/microsoft/VirtualClient/tree/main/src/VirtualClient/VirtualClient.Actions/Memcached)

## Step 7: Dependencies Creation
* In case, Workload requires one time set-up on VM then that can be added as a dependency in the VC.
* Add \<Dependency_Name>.cs(IISInstallation.cs) file in VirtualClient.Dependencies project.
* Add its unit tests in project VirtualClient.Dependencies.UnitTests project.
* This dependency can be added in profile file created for workload in VirtualClient.Main.

## Step 8: Running the Workload on Azure VMs and Verify data in Telemetry
* Open command prompt and follow these commands to create NuGet package

```bash
cd /src/VirtualClient
build-vc.cmd
build-packages-vc.cmd 1.0.0  // 1.0.0 is version and it could be any based on choice
```

* Package will be found in location VirtualClient\out\packages with the name VirtualClient.[version].nupkg. Eg. VirtualClient.1.0.0.nupkg
* Copy the NuGet package to the VM on which, workload needs to be run.
* Please refer [Virtual Client Documentation](https://github.com/microsoft/VirtualClient/blob/main/src/VirtualClient) for details on how to run workload on Azure VM using Virtual Client package.
* Verify the results in telemetry
* PR can be created now for review and checkin.

## Step 9: Documentation Updation
* **Workload Binaries and Scripts Locations**
This documentation should exist in the VirtualClient.Packaging folder and should follow the naming convention + format for the example below. 
This includes information on exactly where to get the binaries/scripts etc. required to create a workload package (including custom build/compile instructions). 
This should also and especially include information on pieces of the workload package that are NOT in source control (e.g. where is it at if not in source control). 
An example of this is the SPEC CPU workload requirements for an *.iso file. This file is over 2 GB in size and we do not keep it in source control. It exists ONLY in the package store.

* **Workload Details, Profiles and Metrics**
  A standard pattern is in place for describing the details of the workload (what it is and what it does) as well as the different profiles that are offered to run that workload as well as what type of metrics to we capture when the workload is run. 
  This is very important for users of the Virtual Client to understand fine-grained details about these workloads and profile scenarios. This information is divided into 3 parts/documents. Use the examples below for reference.
  
  * [Example - OpenSSL Overview/Details](../workloads/openssl/openssl.md)
  * [Example - OpenSSL Workload Profiles](../workloads/openssl/openssl-profiles.md)

