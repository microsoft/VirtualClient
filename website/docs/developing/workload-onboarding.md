# Workload Onboarding LifeCycle
The following documentation provides guidance on the steps that the Virtual Client requires to onboard workloads to VirtualClient platform.

#### **Step1: Workload Understanding**
* Connect with SME(Subject Matter Expert) and understand about the workload, collect documentations and packages related to it, and finalize the 
list of expected metrics which will be pushed to telemetry.
* Run the workload as per mentioned in the documentation and verify the parameters of expected metrics.
* Get all the doubts and discrepancies clarified by setting a meeting with SME again.

#### **Step2: Dependency Packages Creation and Uploading**

* **NuGet Package Creation Steps**
    * Go to VirtualClient.Packaging project and create a folder with the name of workload in each of the supporting architectures folder.
    * Copy all the required files based on architecture in the newly created folders.
    * Create a [NuGet specification file](https://docs.microsoft.com/en-us/nuget/reference/nuspec) in VirtualClient.Packaging project with the name <workload_name>.nuspec. 
      * Examples:
        *  [WebFundamentals.nuspec](../VirtualClient.Packaging/WebFundamentals.nuspec)
        *  [DotnetRuntime.nuspec](../VirtualClient.Packaging/DotnetRuntime.nuspec)

    * File contains details related to NuGet Package formed along with all the locations of workload(win-x64, win-arm64, lin-x64 etc.).
    * Update src/VirtualClient/build-packages-workloads.cmd to accommodate changes for the new workload.

```bash
// Changes look like these
 echo:
    echo [Creating NuGet Package: WebFundamentals Workload]
    echo --------------------------------------------------
    call dotnet pack %PackagesProject% --force --no-restore --no-build -c Debug -p:NuspecFile=WebFundamentals.nuspec && echo: || Goto :Error
```

* 
    * Open Command Prompt and follow these commands to create NuGet package

```bash
cd /src/VirtualClient
build-vc.cmd
build-packages-workloads.cmd 
```

 * Package will be found in location repo/out/packages with the name [Workload_name].[version].nupkg. e.g. DotNetRuntime.1.0.0.nupkg
 * This package needs to be uploaded on either of these two locations:

 * If size of one or more files involved in the workload dependencies is large then it can directly be uploaded on Blob Store without creating NuGet package for it.

#### **Step3: Create Parser and Unit Tests**
* Create a parser class file with name <Workload_name>ResultsParser.cs(e.g. WebFundamentalsResultsParser.cs) in project VirtualClient.Parser.
* Write unit tests for the parser created in project VirtualClient.Parser.UnitTests with the name <Workload_name>ResultsParserTests.cs(e.g. WebFundamentalsResultsParserTests.cs)
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

#### Step4: Update Package Manager and Unit Tests
* Update PackageManager.cs of project VirtualClient.Core to incluide the details of all the packages required to be downloaded.
* Update PackageManagerTests.cs of project VirtualClient.Core.UnitTests to include the unit tests of the changes.

#### Step5: Profile Creation
* Create json file with name PERF-<PERF_CRITERION>-<Workload_Name>.json (eg. PERF-WEB-WEBFUNDAMENTALS.json) in profiles folder of VirtualClient.Main project.
* This file contains all the dependencies that are required by the workload.
* Dependencies are included in the project VortualClient.Dependencies.
* Update VirtualClient.Main.csproj file and VirtualClient.Actions.FunctionalTests.csproj file with the json file created.

#### **Step6: Create Executor and Unit Tests**
* Create a folder with the name of workload in VirtualClient.Actions project which will contain all the files related to executor.
* For Client-Server Executor, please refer 
    * [WebFundamentals Client Executor](../VirtualClient.Actions/WebFundamentals/WebFundamentalsClientExecutor.cs)
    * [WebFundamentals Server Executor](../VirtualClient.Actions/WebFundamentals/WebFundamentalsServerExecutor.cs)
    * [WebFundamentals Executor](../VirtualClient.Actions/WebFundamentals/WebFundamentalsExecutor.cs) 
    * [SQLCloudDB Client Executor](../VirtualClient.Actions/SQLCloudDB/CloudDBClientExecutor.cs)
    * [SQLCloudDB Server Executor](../VirtualClient.Actions/SQLCloudDB/CloudDBClientExecutor.cs)
    * [SQLCloudDB Executor](../VirtualClient.Actions/SQLCloudDB/CloudDBClientExecutor.cs)
* For Single VM Executor, please refer [DotnetRuntime](../VirtualClient.Actions/DotnetRuntime/DotnetRuntimeExecutor.cs) workload.

#### **Step7: Dependencies Creation**
* In case, Workload requires one time set-up on VM then that can be added as a dependency in the VC.
* Add <Depency_Name>.cs(IISInstallation.cs) file in VirtualClient.Dependencies project.
* Add its unit tests in project VirtualClient.Dependencies.UnitTests project.
* This dependency can be added in profile file created for workload in VirtualClient.Main.

#### **Step8: Running the Workload on Azure VMs and Verify data in Telemetry**
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

#### **Step9: Documentation Updation**
* **Workload Binaries and Scripts Locations**
This documentation should exist in the VirtualClient.Packaging folder and should follow the naming convention + format for the example below. 
This includes information on exactly where to get the binaries/scripts etc. required to create a workload package (including custom build/compile instructions). 
This should also and especially include information on pieces of the workload package that are NOT in source control (e.g. where is it at if not in source control). 
An example of this is the SPEC CPU workload requirements for an *.iso file. This file is over 2 GB in size and we do not keep it in source control. It exists ONLY in the package store.
        * Example: [Notes-OPENSSL.md](../../src/VirtualClient/VirtualClient.Packaging/Notes-OPENSSL.md)
* **Workload Details, Profiles and Metrics**
A standard pattern is in place for describing the details of the workload (what it is and what it does) as well as the different profiles that are offered to run that workload as well as what type of metrics to we capture when the workload is run. 
This is very important for users of the Virtual Client to understand fine-grained details about these workloads and profile scenarios. This information is divided into 3 parts/documents. Use the examples below for reference.
    * Examples: 
        * [OpenSSL.md](../workloads/openssl/openssl.md)
        * [OpenSSLMetrics.md](../workloads/openssl/openssl-metrics.md)
        * [OpenSSLProfiles.md](../workloads/openssl/openssl-profiles.md)

