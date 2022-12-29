# Workload Development Recommendations
The following documentation provides guidance on the practices and fundamentals that the CRC team recommends for teams who want to create workloads to
run as part of CRC Flighting system experiments. The overarching goal of these recommendations is to create a recipe for the creation of workloads that
can be easily developed and easily integrated into the CRC Flighting system via the Virtual Client workload provider.

## Terminology
The following pieces of terminology are used often to describe CRC Flighting system experiments and workloads.

* #### Workload 
  These are special agents/toolsets/software that CRC runs on blades or VMs as part of a flighting experiment that were designed to represent the experience of a 
  customer. The CRC team runs workloads to capture various reliability and performance metrics from the system while a payload/change is being applied to 
  the physical node/system underneath. These metrics, when compiled in large datasets, allow the CRC team to accurately analyze the real net impact of a
  given change on the overall customer experience. 

* #### CRC Flighting System
  The execution system that provides the automation to conduct firmware/hardware A/B experiments at-scale in the production Azure fleet.

* #### Virtual Client
  The Virtual Client is the customer-representative workload provider for the CRC Flighting system. It is an executable that abstracts the complexities of
  running workloads from the flighting execution system providing the integration point for new customer/partner workloads that will be ran in
  the flighting system. The Virtual Client additionally ensures standardization in capturing system performance information as well as telemetry that 
  is emitted as part of workloads running in CRC experiments.

* #### Addressable Fleet  
  The CRC team focuses on the addressable Azure fleet. Fleet nodes that are addressable exist in clusters that are fully online for use by either 
  internal Azure teams or public customers. Clusters are qualified as online when all of the cluster fabric services are enabled including the TiP 
  service. The cluster fabric services are required to be enabled in order to properly manage nodes within the cluster including the ability for 
  users/customers to request cloud resources via ARM (Azure Resource Manager).

* #### Compute Nodes 
  Compute nodes are designed to host VMs as a general rule. Many of the Azure services that run in the cloud and that we offer customers rely on VMs 
  in whole or in part to host the software that implements the services being provided. CRC largely focuses on compute nodes because they are the nodes
  directly used by customers and that have the most direct impact on their experience. 

* #### Utility Nodes 
  Utility nodes are designed primarily to host cluster fabric services use to manage the cluster itself. They do not host customer services or workloads.
  The CRC team does not currently focus on utility nodes. However, the team is actively working to extend fleet coverage targets to include utility nodes
  within the Cobalt semester. 

* #### Storage Nodes 
  Storage nodes are designed primarily to integrate with large arrays of physical disks (JBODs). They do not host customer services or workloads. Compute 
  nodes often use storage nodes to provide for customer storage necessities (e.g. storage of the VHD file). Storage nodes are not supported by either the 
  TiP service nor the ARM service and often run a different OS than compute nodes. Because of these distinctions, CRC cannot currently target storage nodes 
  for flighting validations. With that said, there are opportunities here in partnership with the TiP service team where validations on storage nodes would 
  be made possible. In the semesters that follow Cobalt, CRC may amend the current coverage strategy to perform some amount of validations on these SKUs 
  where it is deemed critical to the business. 

* #### General Purpose SKU 
  A general purpose SKU represents nodes that are used by customers for everyday compute needs. The vast majority of the Azure fleet are general purpose 
  hardware SKUs. 

* #### Special Purpose SKU 
  Special SKUs are nodes that have hardware components used to enable special computational capabilities for customers. These SKUs for example might have 
  multiple CPU sockets and many cores for high performance computing needs or GPU chips that are specially designed for advanced graphics rendering/gaming 
  capabilities. These SKUs make up a small percentage of the fleet; however, the customers using them pay a lot of money for these capabilities and an 
  interruption to the service on these SKUs typically causes a very substantial impact to the customer. Common special SKU categories include GPC, GPM, GPZ,
  HM, HPC, VHM, DWS, and FPG. 

* #### Payload 
  A payload is a change that is made to a physical node/system. For example, the deployment of a microcode firmware update is a change to nodes in the 
  fleet. In practice, payloads represent updates to the physical infrastructure of the Azure fleet that are often associated with required SLA guarantees 
  we offer Azure customers. They are deployed to systems while they are running and thus carry a significant risk to the interruption of service for Azure 
  customers. Historically, these changes have been associated with increases in annual interrupt rates (AIR) for Azure customers and are thus the fundamental subject of CRC validations. 

## Goals for Creating a Good Workload
The following sections describes some of the goals involved in the creation of a good workload. At a high-level, a good workload:

  * #### Runs Consistently Reliable Tests
    The test that the workload runs should be consistently reliable. If the test itself cannot consistently produce results time and time again then
    it produces too much variation (noise) for it to be used to accurately gauge the state of a system under test. This is about code quality and defensive
    programming in many ways.

  * #### Runs Consistently Repeatable Tests
    The tests that the workload runs should be repeatable in the characteristics of the results. This means that the same test ran on the same node/VM
    over and over should produce similar results given nothing has changed on the system under test. This is not to say that the results will be identical
    but that the standard deviation of the results would be generally small given all other things being equal on the system.

  * #### Produces Objective Results
    The tests that the workload runs should produce definitive and objective results. The results should not make estimations as a general rule but should
    instead attempt to measure facets of the system that are based on hardened and vetted patterns, practices and software. For example, on a Windows system
    the performance counter and ETW sub-systems are highly refined and reliable. A workload can confidently rely upon performance counters to measure many performance
    characteristics of the system in a precise manner.

  * #### Produces Structured Results
    The results that a good workload produces, will be in a consistently structured format. This makes it easy for the results to be parsed for meaningful
    insights and aggregations.

  * #### Is Easy to Integrate with Other Systems
    A good workload should be easy to use thus making it easy to integrate into the larger execution system. This reduces the overhead required for
    teams to onboard new and valueable test scenarios.

## Workload Recipe
Given the goals noted above, the following section provides a general recipe that can be followed when creating a workload for easy integrated into
the CRC Flighting system. Provided the recipe is followed, it is typically very quick for the CRC team to onboard new workload scenarios which
can then produce valuable results at-scale in A/B experiments.

* #### Create the workload as a simple command-line tool
  * [Option 1]: Create a command line executable. 
    * All platforms support executables and there is a lot of of pre-existing OS/systems integration support.
    * Command line executables can typically be compiled to support multiple OS platforms (Windows vs. Linux) as well as CPU architectures.
    * Command line executables are easy to port around (e.g. copy, download etc...).
    * This is the preferred option.
    <br/><br/>

  * [Option 2]: Create the workload as a PowerShell Core/7 script or module.
    * PowerShell Core/7 is an industrial strength scripting language that can run on both Windows and Linux operating systems.
    * PowerShell has support for multiple CPU architectures.
    * PowerShell scripts and modules are easy to port around (e.g. copy, download etc...).
    <br/><br/>

  * Workloads that require GUI support should be avoided.
    * They are not easy to integrate into automation workflows and may not even run in certain server OS environments.
    <br/><br/>

  * Driver-like workloads  
    For certain workloads that need to run at the Kernel level, it may not be possible to have a traditional executable script to execute the test
    logic. For these scenarios, it is important that the driver/workload utilize well-known/vetted sub-systems (e.g. performance counters, ETW) in order
    to produce results. The more challenging aspect of these type of workloads is the deployment requirements.

* #### The workload should be written in a cross-platform programming/scripting language
  Whereas it is not always a requirement for certain scenarios, the CRC Flighting system tests on both Windows and Linux systems. A good workload can run
  on either of these systems. Indeed a large portion of the VMs used by Azure customers are Linux VMs.

  * [Option 1]: Write the workload application in native programming language such as C++.  
    * Native code applications can be compiled to target multiple OS platforms and CPU architectures.
    * Native code applications can be signed as part of Azure Official builds so that they can be run on the Azure physical host itself.
    * The Virtual Client already runs native code executables so there is a precedent in place.
    * This is a preferred option.
    <br/><br/>

  * [Option 2]: Write the workload application in .NET Core/5.0.  
    * The .NET Core/5.0 framework enables applications to be written in a managed coding language such as C# that can be compiled to run on multiple OS 
      platforms and CPU architectures.
    * .NET Core/5.0 applications can typically be developed at speed significantly faster than with native programming languages.
    * .NET Core/5.0 applications can be signed as part of Azure Official builds so that they can be run on the Azure physical host itself.
    * Many team members in Azure are proficient in managed coding skillsets and thus can contribute to ongoing development needs.
    * The CRC Flighting system and the Virtual Client itself are primarily written in .NET Core/5.0/C# and runs on both VMs (Windows and Linux) as well as Azure hosts (including ARM64 hosts).
    * The Virtual Client already runs managed code executables so there is a precedent in place.
    * This is a preferred option.
    <br/><br/>

  * [Option 3]: Write the workload application in PowerShell Core/7.  
    * PowerShell Core/7 scripts and modules can run on both Windows and Linux systems.
    * PowersShell Core/7 scripts and modules can be signed as part of Azure Official builds so that they can be run on the Azure physical host itself.
    * The Virtual Client already runs workloads that have bootstrapping/setup needs that are handled with PowerShell scripts so there is a precedent in place.
    <br/><br/>

  * [Option 4]: Write the workload application in Python.
    * Python has a portal execution interpreter/runtime that provides support for both Windows and Linux systems.
    * There may be restrictions that prevent running Python on Azure physical hosts.

* #### The workload should be parameterized
  In order to ensure that the workload can support all necessary scenarios and configuration requirements, the workload should allow all important
  settings to be provided on the command line.

* #### The workload should be designed to produce a consistent steady-state test
  As mentioned in the goals above, the workload should produce consistent and reliable results. The best workloads run a "steady-state" test. A steady state
  test will run the exact same execution workflow each time given the same settings are provided. The tests should not be based on algorithms that cannot produce
  a steady state time and time again. Tests should also avoid testing aspects of the system that are themselves highly variable. Highly variable algorithms or aspects
  will inevitably produce highly variable results. These results will make it difficult to ascertain differences in the system performance when changes
  are made to the system under test.

* #### The workload should run the exact same test every time given exact settings
  Following up on the bullet point above, a workload should run the exact same test every time given a set of specific settings. The tests ran should
  vary only when the settings supplied on the command line have changed.

* #### The workload should produce highly structured results
  
  * [Option 1]: Output the results in JSON-format.
    * JSON-formatted results are very easy to parse thus making them very easy to integrate. 
    * There is a lot of support for JSON in most native and managed programming languages.
    * This is the preferred option.
    <br/><br/>

  * [Option 2]: Output the results in XML-format.
    * XML-formatted results are easy to parse thus making them easy to integrate.
    * There is a lot of support for XML in most native and managed programming languages.
    <br/><br/>

  * [Option 3]: Output the results in CSV(comma-separated value)-format.
    * CSV-formatted results are easy to parse thus making them easy to integrate.
    * There is a lot of support for CSV in most native and managed programming languages.
    <br/><br/>

  * [Option 4]: Output the results as fixed-width column format.
    * Fixed-width column results are fairly common and relatively easy to parse.
    * It is best if the results columns are a consistent fixed-width across the board.
    <br/><br/>

  * [Option 5]: Use system performance counter subsystems.
    * Performance counters provide high-precision measurements of system performance and are easy to create on most OS systems.
    * The use of performance counters removes the need to write out any specific results.
    * The Virtual Client has good native support for capturing performance counters at high-precision.
    <br/><br/>

  * Ideally, results should be provided in standard/console output or returned in a structured object (e.g. PowerShell script scenarios).
    <br/><br/>

  * It is also ok to write results to a file, but standard/console output is preferred.

* #### The workload should handle any setup or dependency installation requirements where possible
  There is a balance to strike here. For cases where the setup of dependencies can be handled by the workload itself, they should be. The workload
  developer is the expert in the requirements. For certain workloads in the Virtual Client, the dependencies are packaged along with the workload itself.
  This is perfectly reasonable and often times ideal to ensure exact dependencies are used with the workload. For scenarios where dependencies cannot
  be easily packaged, have a conversation with the CRC team. There are common dependency download and installation scenarios that are better integrated into
  the platform than the workload because other workloads share the same/similar requirement.

* #### The workload should be provided as a versioned package
  In order to support solid ongoing development and servicing practices, workload applications should be versioned. Additionally, the different
  versions of the workload should be provided via a package store.

  * [Option 1]: Provide the workload as a NuGet package
    * NuGet packages are very easy to integrate into development environments and build pipelines.
    * NuGet packages are easy to integrate into Virtual Client runtime scenarios.
    * NuGet packages are easy to create.
    * Azure DevOps provides easy ability to create new/custom NuGet feeds for internal package sharing.
    * This is the preferred option.
    <br/><br/>

  * [Option 2]: Provide the workload as an Azure Blob package
    * Azure Blob store packages are easy to integrate into Virtual Client runtime scenarios.
    * Azure Blob storage accounts are easy to create.
    * Even though the package will be stored in Azure Blob, the structure of the package should still
      follow NuGet schema recommendations.

  <br/>
  
  See [Dependency Packages](./0040-dependency-packages.md) for a more detailed description of how packages are 
  expected to be defined for use with Virtual Client operations.