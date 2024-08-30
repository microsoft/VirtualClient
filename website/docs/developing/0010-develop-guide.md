# Developer Guide
Welcome to the Virtual Client development team! The Virtual Client is a .NET 8.0 command line application written in C# that offers both cross-platform and multi-architecture support. As such, the 
application can run on both Windows and Linux operating systems as well as on hardware with x64 and arm64 architecture CPUs/processors. The following documentation covers details, concepts and 
practices to consider when doing development work in the Virtual Client source code. The goal is to enable a developer new to the codebase to quickly understand the high level requirements and expectations before 
he/she commits to doing work extending the features of the Virtual Client platform.

## Preliminaries
Before beginning, it is helpful to understand some of the concepts and foundations involved in Virtual Client development. The following links provide
platform overview and design concepts. The remainder of this guide will use terms that are covered in these documents, so it is important to go through
these first.


If you are developing extensions to the Virtual Client platform in another repo, the following documentation can get you started.

* [Developing Virtual Client Extensions](./0020-develop-extensions.md)


After going through this developer guide, there are links code examples at the bottom of this document to get you hands-on experience.

## Terminology
For the sections that follow, the term '**component**' is used to describe a Virtual Client action, monitor or dependency. These will be concrete class implementations
referenced in Virtual Client profiles. In the example below, the 'OpenSslExecutor', 'DependencyPackageInstallation' and 'PerfCounterMonitor' are ALL components. All components
in the Virtual Client derive from the base class 'VirtualClientComponent'.


``` json
# In the example below, the OpenSslExecutor, DependencyPackageInstallation and PerfCounterMonitor are all
# generically called 'components'.
{
    "Description": "OpenSSL 3.0 CPU Performance Workload",
    "Parameters": { },
    "Actions": [
        {
            "Type": "OpenSslExecutor",
            "Parameters": {
                "Scenario": "MD5",
                "CommandArguments": "speed -elapsed -seconds 100 md5",
                "PackageName": "openssl",
                "Tags": "CPU,OpenSSL,Cryptography"
            }
        }
    ],
    "Dependencies": [
        {
            "Type": "DependencyPackageInstallation",
            "Parameters": {
                "Scenario": "InstallOpenSSLWorkloadPackage",
                "BlobContainer": "packages",
                "BlobName": "openssl.3.0.0.zip",
                "PackageName": "openssl",
                "Extract": true
            }
        }
    ]
}
```

``` csharp
// They all derive from the base class VirtualClientComponent

/// <summary>
/// The OpenSSL workload executor component.
/// </summary>
public class OpenSslExecutor : VirtualClientComponent

/// <summary>
/// The Azure blob package dependency installation component.
/// </summary>
public class DependencyPackageInstallation : VirtualClientComponent
```

## Practices and Principles
Before digging into the finer-grained details of development work in the Virtual Client codebase, it is important to understand the high-level practices
and principles the VC Team follows. The practices and principles the team follows are intended to promote rapid software feature development while ensuring
high quality designs and a codebase that can be sustainably maintained over time. These practices are well-vetted learned over many years in the "school of hard 
lessons learned". As such, it is important for developers new to the Virtual Client codebase to follow the practices when possible. That said the most
important thing is to keep things as simple as possible.

* **Keep it Simple**  
  Whereas it is difficult to keep everything simple all the time when implementing new logic, it is important to try. It helps to focus on the exact
  requirements when implementing a solution and to avoid allowing the scope to creep to things that are not actually needed. It is easy to have thoughts
  such as "what if we need this in the future", but this can easily lead to over-engineered solutions with difficult to maintain/extend code. The process 
  of unit testing (as noted below) is very helpful as a guide for writing just the right amount of code.

* **Enforce isolation with new actions/executors, monitors and dependencies**  
  When developing new actions/executors, monitors or dependencies in Virtual Client, it is important to try to isolate the logic
  of that component from other components. Common/shared dependencies used by all Virtual Client components are carefully implemented 
  in the 'Core' project; however, other components should be cautious to use shared logic that is NOT a part of the Core. This helps
  to prevent a problem with one component from accidentally impacting another. This is an especially important concept given how many
  distinct workload executors, monitors and dependencies exist in Virtual Client with the list constantly growing.

* **Functionality that is shared by all components should be part of the 'Core' project**  
  There are dependencies that are required by most components in the Virtual Client. The implmentations of these hold a high quality bar
  because they are so fundamental to the operations of the Virtual Client application as a whole. Most of the necessary dependencies have been
  implemented. However if a new dependency is required in Virtual Client that is to be shared across all providers, it should be implemented
  in the 'Core' project. Note that something is not "common" because it might be in the future. A component is common when it is used by many
  other components across the codebase. For example, the package manager is used by almost every Virtual Client component. As a general rule,
  a new component should not be added to the 'Core' project until it is definitively used by more than 2 or 3 distinct other components. 

  * [VirtualClient.Core Project](https://github.com/microsoft/VirtualClient/blob/main/src/VirtualClient/VirtualClient.Core)

* **Unit tests are fundamental to development quality-at-speed.**  
  One of the fundamental goals of the VC Team is to enable high quality development at-speed. To accomplish this as the size
  and complexity of the codebase grows, it is very important to have good unit tests in place for each component or supporting
  class. The process of writing unit tests acts as a concrete guide in the development process forcing the developer to think
  deeply about the code he/she is writing. This is NOT meant to slow the developer down but instead helps to ensure high quality
  design with minimal code...it is a particularly good process for this goal. A favorite motto of the team is: "you have to go slow to
  go fast". This means that the developer will invest more time up front in the development process to ensure faster development down
  the road. This happens because the code is often well-thought out without being over-engineered and there are programmatic tests
  in place protecting the correct functioning of the logic for the future. There are plenty of good examples and patterns in the source
  code to follow.

  * [Testing Guide](./0090-testing.md)

* **Functional tests are required for new profiles.**  
  Functional tests are similar to unit tests except that they focus on the correct integration of all components. When the VC Team creates new
  workload or monitoring profiles, functional tests are also created. Whereas unit tests focus on classes meeting all important "specifications",
  functional tests focus on the interactions between components being correct. There are plenty of good examples and patterns in source code
  to follow.

* **New profiles (or components within them) must be documented**  
  Each time the VC Team onboards a new workload, monitor or dependency it is documented in the repo within the 'VirtualClient.Documentation'
  folder. Good documentation is an important part of the quality bar that the team holds ensuring that users/customers can always learn more
  about the Virtual Client. There are patterns in place within the documentation to guide developers through the process.

* **Components should be implemented to support ALL possible platform/architectures**  
  The Virtual Client is generally designed to run in as many scenarios as possible. To do so, a developer has to consider whether the component
  they are onboarding can run on Windows or Linux, x64 (Intel, AMD) or ARM64 architecture. Many times the .NET framework itself provides for the
  ability to run cross-OS platform/cross-CPU architecture. However, there are times when the logic must implement support in a slightly different
  way. It is always a goal to implement new features and components such that they cover ALL possible scenarios. By doing so, the value of the
  Virtual Client to users/partners/customers is increased because it can cover more of the scenarios that are important to them.

* **Components should be written to be idempotent**  
  The term "Idempotent" in this context refers to the ability of a component to be ran any number of times back-to-back without changing the
  desired outcome. For example, a component that is responsible for installing a particular dependency should not change the state of that dependency
  once installed nor should it fail on a second, third etc... run. A component that is responsible for running a workload or test should be able
  to successfully run that workload or test on each and every subsequent run. In practice, this sometimes mean avoiding doing work that has already
  been performed (e.g. installing a given dependency) and cleaning up either at the beginning or end of the component execution.

  The Virtual Client core platform has a state manager that allows components to preserve state information (in files on the local system) so that they
  can be referenced later or on subsequent runs. This is useful for example to save off information that indicates some specific operation was performed
  that could fail if attempted a second time in succession.

## Considerations When Getting Started
As a developer is starting to think about contributing to the Virtual Client codebase, there are a few things that are helpful to consider as a guide. 
The following sections provide some high-level ideas to help form a plan. The sections below are written to account for the onboarding of an entirely
new component (workload, monitor) to the Virtual Client so as to cover the space thoroughly.

* **Familiarize yourself with the platform concepts**  
  It is important to understand the fundamental concepts for the Virtual Client before beginning. If you did not go through the platform overview
  and design documentation at the top, please do so before continuing.

* **Identify your dependencies**  
  One of the very first things to do when getting started with a new component implementation is to identify all of the dependencies that your
  component will have. This will inform whether you need to package those as a Virtual Client package or install them in other ways. For example:

  * Workload scripts or binaries.
  * Dependencies/packages that have to be installed for workload scripts or binaries (e.g. apt, debian, chocolatey).
  * Configurations/settings for the system/OS (e.g. registry keys, TCP ephemeral ports).

* **Identify what type of component you are implementing: action, monitor or dependency**  
  The Virtual Client has 3 key types of components in source: actions, dependencies and monitors. Components that are "Fundamental" belong in the Virtual Client
  platform/core repo. Components that are "Domain-Specific" belong in a separate repo. Developers will reference a few key libraries from the Virtual Client platform/core
  repo (specifically Contracts and Core libraries published as NuGet packages) for integration.

  * **Actions**  
    Actions are used to implement components that execute workloads/tests or scripts. These represent the benchmarks, validation tests or customer-representative work that is
    happening on the system. For example, the 'OpenSslExecutor' referenced at the top of this document is a component that executes a workload that tests the CPU on the system
    called 'OpenSSL SPeed'. It is an industry-standard cryptography benchmarking workload.

  * **Dependencies**  
    Dependencies are used to implement components that setup or install dependencies on the system required by actions and monitors in a profile. For example,
    the 'DependencyPackageInstallation' component referenced at the top of this document is a component that downloads Virtual Client packages from an Azure storage account 
    to the system at runtime. 

  * **Monitors**  
    Monitors are used to implement components that run in the background and capture important information from the system under load. For example, the 'PerfCounterMonitor' 
    referenced at the top of this document is a component that captures performance counters from the system while workloads/tests are running.

* **Learn how the workload/test, monitor or dependency works**  
  When onboarding a new workload/test or monitor, time MUST be spent learning how it works. This will allow the developer to make and informed decision on how the
  application should run as part of defining a Virtual Client profile in which it is integrated. This is very, very important to the goals of the Virtual Client. The user of the 
  platform should NOT have to know very much about the  or be an expert in it in order to take advantage of the expertise built into Virtual Client on their behalf. In fact, this principle
  is fundamental to the idea of the Virtual Client and the primary reason why profiles exist. A profile represents the expertise of the developers that integrated
  components into the Virtual Client. This includes the expertise of other teams of subject matter experts consulted during the discovery work. At the end of the day,
  a user of the Virtual Client should be able to take advantage of the expertise of many when running a workload and should have trust in the information that is
  produced. This depends on you, the developer doing the hard due diligence work to truly understand what the workload does and how to run it effectively.

  Team members typically use virtual machines in a subscription for exploratory work to learn how a particular application being onboarded works. Contact the team
  using information in the general documentation at the top of this document if you need virtual machine support.

* **Create Virtual Client packages (.vcpkg) for certain dependencies**  
  Many dependencies for the Virtual Client such as a workload or monitor will be packaged in a Virtual Client (.vcpkg) package. Developers do not necessarily
  need to create custom VC packages for everything. However, it is common practice to create packages for the Virtual Client containing required dependencies
  (e.g. workloads, Java runtime, PowerShell, Python) so that they can be put in a package/blob store for download at runtime. Virtual Client profiles are designed
  to download and install required dependencies. This is not required, but it is convenient and helps to make Virtual Client easy to deploy into a wide range of
  environments/scenarios.

  Another benefit of placing dependencies in custom Virtual Client packages is that it enables Virtual Client to support "disconnected" scenarios. These
  are scenarios where the Virtual Client will need to run on a system that does not have a network connection. For example, a set of workloads might require
  the Python3 framework to run despite the disconnected scenario. It cannot be downloaded at runtime. In this example, the workloads would be placed in a package 
  and the Python3 framework libraries in a separate package (for reusability with other workloads in the future). These packages would be deployed with the Virtual 
  Client in the 'packages' folder so that there is no need for Virtual Client to download them.
  

* **Design your workload or monitor profile**  
  Virtual Client profiles represent the interface to the user for your workloads/tests, monitors and dependencies. They represent the expertise that a developer gained
  throughout the discovery process for a new component being onboarded to the Virtual Client. It is common for example for a new workload/test to have a single profile
  that a user can reference on the command line to run that workload/test in one or more ways that represent a holistic scenario. A profile should define a scenario that
  is both whole (in terms of breadth of coverage on a system) as well as trustworthy (in terms of quality of coverage on the system). As noted above, the user should not
  need to be an expert in the workload/test or monitor in order to take advantage of the expertise within.

  As was covered in the platform design documentation at the top, Virtual client profiles are divided into 2 types of profiles: workload/test profiles and monitoring profiles.
  This allows the user to run different workloads/tests on a system with different monitors as they require. 

  ```
  VirtualClient.exe --profile=PERF-CPU-OPENSSL.json --profile=MONITORS-DEFAULT.json --timeout=1440 --system=Demo
  ```
  
  * [Existing Profiles](https://github.com/microsoft/VirtualClient/blob/main/src/VirtualClient/VirtualClient.Main/profiles)

## Platform/Core Projects and Libraries
Before beginning the discussion of development/programming aspects of the Virtual Client, the following section covers the important projects that
exist within the Virtual Client platform/core codebase. It is helpful to understand what components are implemented in each of these projects.

* **VirtualClient.Contracts**  
  This project contains all of the fundamental data contract/model and POCO (plain old C# object) classes that are used by Virtual Client components. This
  project additionally contains the logging extensions that are used to create the structured/schematized telemetry foundation that ensures data
  emitted by the Virtual Client follows consistent patterns and is easily/readily consumable for data analysis.

* **VirtualClient.Core**  
  This project contains all of the important shared/core dependency interfaces and implementations  used by all Virtual Client components
  (e.g. workload executors, monitors and dependency installers/handlers). This is the "common" class library if you will.

* **VirtualClient.Actions**  
  This project contains "Fundamental" workload/test executor implementations. The components within this project can be seen referenced in the
  'Actions' section of related workload profiles.

  This project also contains classes/implementations of various results/raw text parsers that are used in conjuction with workload/test executors
  to read important information/data (e.g. metrics) from the output of workloads. The parsing of results is complex enough to
  keep the implementation separate from the workload executors and to ensure reusability for different implementations of a related
  workload executor.

* **VirtualClient.Api**  
  This project contains the Virtual Client API. This is a self-hosted REST API that allows instances of the Virtual Client running on
  different systems to communicate with each other. This is important for workloads that require multi-system/tier topologies to conduct 
  the workload operations (e.g. client/server interactions).

* **VirtualClient.Dependencies**  
  This project contains "Fundamental" dependency installer/handler classes/implementations. The components within this project can be seen referenced in the
  'Dependencies' section of related workload or monitoring profiles.

* **VirtualClient.Monitors**  
  This project contains "Fundamental" background monitor implementations. The components within this project can be seen referenced in the
  'Monitors' section of related monitoring profiles.

  This project also contains classes/implementations of various results/raw text parsers that are used in conjuction with monitors
  to read important information/data (e.g. metrics) from the output of monitors. The parsing of results is complex enough to
  keep the implementation separate from the monitors and to ensure reusability for different implementations of a related
  monitor.

* **VirtualClient.Packaging**  
  This project contains workload binaries, scripts etc... required to build certain workload and dependency packages for use with the
  Virtual Client.

* **VirtualClient.TestExtensions**  
  This project contains classes/implementations that are used to test Virtual Client codebase components. Mock fixtures for example are used
  extensively to reduce duplication and to simplify the setup of different mocks and behaviors in Virtual Client unit and functional tests. 
  The test assets in the Virtual Client codebase are as equally important as the code for which they are testing. The Virtual Client codebase
  utilizes the classes/implementations in this project to enable faster velocity in writing new tests and to ensure consistent patterns
  of testing throughout.

## VirtualClientComponent
The 'VirtualClientComponent' class is the fundamental base class for all components (e.g. workload/test executors, monitors and dependency installers/handlers) within
the codebase. This class allows certain very common requirements to be consolidated in a single place.

* **Common/Shared Dependencies**  
  Common dependencies (e.g. package manager, disk manager, file system manager) are passed into the constructors of all components via
  an IServiceCollection instance. This is the principle of dependency injection which creates flexibility for both runtime as well as test
  time executions of code paths. These coded dependencies are created at the start of the Virtual Client and are available to every component
  that runs regardless of when it runs so it is very easy to access the shared platform runtime interfaces.

* **Component Parameters**  
  All parameters that are either defined on the component itself in a workload or monitoring profile or that were passed in on the command line are passed 
  into the constructor of the component. This allows the component to operate based on parameters defined in the profile or for those that were overridden by
  the user on the command line.

## Common/Shared Dependencies
All workload executors, monitors and dependency installers/handlers share a common set of requirements. These requirements are encapsulated into a set of core interfaces
and implementations in the Virtual Client. The following section describes each of the most important categories of shared dependencies as well as their
implementations. As described above, these commons/shared dependencies are passed into the constructor of all workload executor, monitors and dependency
handlers for use. The interfaces and implementations below exist in the 'VirtualClient.Core' project as noted above.

Note that the use of interfaces is part of a "program to interfaces" design principle. Whereas the incorporation of interfaces in a codebase increases the
learning curve for developers trying to understand the flow, it is important to keep abstractions to a minimum. As a general rule, the VC Team tries to have a single
interface and implementation for a given dependency. This allows flexibility for different "live" scenarios as well as for deep/robust ability to test the code
for functional correctness.

* **API Clients**  
  Certain workload scenarios require multiple systems to operate (e.g. networking workloads, client/server). These workloads have a requirement to communicate
  with each other to be able to synchronize client-side executions with server-side expectations. The Virtual Client uses an [environment layout](../guides/0020-client-server.md) provided on
  the command line to determine the IP addresses of other instances. API client creation and management is encapsulated in the following interfaces/classes:

  * IApiClientManager
  * ApiClientManager

* **Blob Store Upload/Download Requirements**  
  The Virtual Client supports the ability to upload and download files/content from an Azure storage account blob store (and other cloud blob stores in the future). Blob store interactions 
  are encapsulated in the following interfaces/classes:

  * IBlobManager
  * BlobManager

* **Disk Management**  
  Certain workloads available in the Virtual Client require the ability to read information from the system about disks attached as well as to initialize/format
  the disks. For example, the FIO workload is designed to test the disks on the system for I/O performance. With Azure virtual machines, managed/remoted disks are
  typically attached in a raw state uninitialized and unformatted. They must be prepped with a file system before any I/O tests can be ran using them. Disk management
  features are encapsulated in the following interfaces/classes:

  * IDiskManager
  * DiskManager
  * UnixDiskManager
  * WindowsDiskManager

* **Firewall Management**  
  Some workloads require changes to the firewall on the system in order to operate. This might include opening ports (e.g. TCP, UDP) or enabling certain applications
  to execute freely. Firewall management features are encapsulated in the following interfaces/classes:

  * IFirewallManager
  * FirewallManager
  * UnixFirewallManager
  * WindowsFirewallManager

* **Package Management**  
  With the sheer number of different workloads available in the Virtual Client, there are a lot of different workload and dependency packages that are required. The application
  delegates the responsibility for downloading, extracting and keeping track of all of the various packages using a package manager. The package management foundation
  supports the ability to download packages from both NuGet feeds as well as from Azure storage account blob stores. Package management features are encapsulated in the
  following interfaces/classes.

  * IPackageManager
  * PackageManager

* **Process Management**  
  The Virtual Client runtime platform execute operating system processes often as part of just about every workload/test executor, monitor or dependency installer/handler. In addition
  there are times when processes need to be launched with elevated privileges. The responsibility for creating and managing processes within the runtime is encapsulated in the
  following interfaces/classes.


  * ProcessManager
  * UnixProcessManager
  * WindowsProcessManager
  * IProcessProxy
  * ProcessProxy

* **State Management**  
  Certain scenarios require the ability to preserve state information in between operations. For example, there are operations that make configuration settings changes to the
  system and then require a reboot. When the Virtual Client is restarted, it needs to know what previous requirements were completed. State management is also very important
  for workloads that require multiple systems (client/server) to enable information to be passed back and forth between one instance and another of the Virtual Client. State management
  features are encapsulated in the following interfaces/classes:

  * IStateManager
  * StateManager

* **System Management**  
  Because of there are quite a few different dependency interfaces/classes that are required to support the needs of executing a wide range of workloads on the system, all common
  dependencies noted above are consolidated together into a single abstraction to simplify the discovery of what is available. Additionally, there are a few requirements that
  are extensions to behaviors that use the dependencies above that are a part of the system management abstraction. The following interfaces/abstractions provide for the
  common system management features:

  * ISystemInfo
  * ISystemManagement
  * SystemManagement

* **Telemetry Management**  
  Telemetry is a critical part of the Virtual Client runtime platform operation. Lots and lots of telemetry is emitted and is used for every data-related requirement on the system.
  The class implementations used in the Virtual Client ALL follow the .NET team's logging patterns using the 'ILogger' recommendations.

  * [.NET ILogger interface/pattern](https://docs.microsoft.com/en-us/dotnet/core/extensions/logging?tabs=command-line). 


  The fundamental classes/interfaces used to emit telemetry in the Virtual Client are as follows:

  * VirtualClientLoggingExtensions
  * VirtualClientTelemetryExtensions

  * EventContext
  * ConsoleLogger
  * AppInsightsTelemetryLogger
  * EventHubTelemetryLogger
  * SerilogFileLogger

## Coding Practices
The following section describes some of the high-level practices and recommendations to follow when working in the Virtual Client codebase. This is not an
exhaustive list but does illustrate things that are "fundamental" to development in the codebase.

* **Use the async/await pattern**  
  The C# programming language (as of version 6.0) has great support for asynchronous programming. Asynchronous programming allows for the application to be
  far more efficient in the usage of system primitive resources for I/O-bound or CPU-bound operations. The Virtual Client runtime platform itself MUST run as
  efficiently as possible. This is especially the case when it is running a resource-sensitive benchmark, workload or test. This is because the resources that the
  Virtual Client itself uses in order to operate as a runtime platform on the system affect the resources available t othe workload and can cause "noise" in the
  data that is emitted. For example, were the Virtual Client itself to use too many CPU/process resources/cycles when running a workload that is designed to
  benchmark the performance of the CPU/processor, the performance results of the benchmark could be skewed to be less accurate.

  This pattern is used pervasively throughout the Virtual Client codebase so there are plenty of examples. You can familiarize yourself with asynchronous
  programming concepts and techniques in Microsoft public documentation.

  * [Asynchronous Programming in C#](https://docs.microsoft.com/en-us/dotnet/csharp/async)

* **Date/Time values should ALWAYS be in universal time (UTC)**  
  When referencing any date/time objects, always represent them in UTC (e.g. DateTime.UtcNow). This is especially important when emitting data. The Virtual Client
  runs all over the planet. It is much easier to convert a date in UTC form to any other timezone form. It is much harder to do this the other way around when you do
  not know what timezone a date reference was in originally. Suffice to say, just use UTC times everywhere always!

* **Write unit tests at the same time as writing your components**  
  Unit tests programmatically validate the functional correctness of the important behaviors of a given class. Unit testing as a process is extremely important to 
  the development practices in the Virtual Client. The process of writing unit tests at the same time as writing new components allows the developer a concrete way to check 
  the quality of the design as well as the code for a new component. In fact, there is no faster way to test new code than a set of unit tests. This is increasingly so over time
  as new coded features are added to existing classes. VC Team members follow a hybrid "behavior-driven development" process where unit tests are written incrementally
  at the same time as writing the new component code itself. These tests are named as if they are a "specifications" document. Indeed the names of the test methods in the 
  codebase might surprise folks at times being rather long and descriptive at times. This is very purposeful. Unit tests ARE a specification of a class that is being
  developed and should thus read like a list of specs. Correspondingly specifications MUST cover every single discreet/singular behavior that is critical to the correct 
  functioning of a given class.

  Over time as the codebase grows, unit tests act as protection for the quality of the application as a whole. Indeed 80% or better of functional regressions and bugs can be
  minimized or eliminated simply by having good unit tests in place. This in turn allows developers to rapidly prototype and implement new features in the Virtual Client
  platform (i.e. high quality development at-speed).

* **Use the platform test/mock fixtures to help you write unit tests**  
  The Virtual Client platform/core repo has a set of mock fixtures and extensions in the 'VirtualClient.TestExtensions' folder that make it easier to do setup and validations in unit
  tests. The patterns are pervasive throughout the unit test projects within the platform/core repo. These mock fixtures greatly simplify the process of writing unit tests
  at the same time as they help to keep your unit test class code cleaner, simpler and more readable.

* **Use the core dependencies interface 'ISystemManagement' to integrate with the runtime platform as well as the operating system on which you are running**  
  Every workload/test executor, monitor or dependency needs to access something from the runtime platform or on the operating system. For example, the developer may
  need to access the file system, get information about disks on the system, create processes to run a workload or find the location of packages on the system. Core
  code dependencies are implemented behind a set of common interfaces as noted above. The 'ISystemManagement' interface contains/exposes ALL of them in a single place
  (e.g. ISystemManagement.DiskManager, ISystemManagement.PackageManager). Because ALL shared dependencies are accessed using these interfaces, the entire codebase can
  be thoroughly tested. This is key to keeping the design and functional quality bars high in Virtual Client. The following interfaces are available on the ISystemManagement
  interface and can be used:

  * Use the 'IApiClientManager' instance for API client creation and operations.
    * (i.e. ISystemManagement.ApiClientManager)<br/><br/>
  * Use the 'IFileSystem' instance for file system operations.
    * (i.e. ISystemManagement.FileSystem)<br/><br/>
  * Use the 'IDiskManager' instance for disk operations.
    * (i.e. ISystemManagement.DiskManager)<br/><br/>
  * Use the 'IFirewallManager' instance for firewall operations.
    * (i.e. ISystemManagement.FirewallManager)<br/><br/>
  * Use the 'IPackageManager' instance for package download, extraction and locating operations.
    * (i.e. ISystemManagement.PackageManager)<br/><br/>
  * Use the 'ProcessManager' instance for operating system process creation and execution operations.
    * (i.e. ISystemManagement.ProcessManager)<br/><br/>
  * Use the 'IStateManager' instance for local state preservation operations.
    * (i.e. ISystemManagement.StateManager)<br/><br/>
  * use the 'PlatformSpecifics' instance for folder/file path creation/combining operations.
    * (i.e. ISystemManagement.PlatformSpecifics)
  
  <br/>
  

  ``` csharp
  public class OpenSslExecutor : VirtualClientComponent
  {
      public OpenSslExecutor(IServiceCollection dependencies, IDictionary<string, IConvertible> parameters)
      {
           // ISystemManagement has all shared/core dependencies.
           ISystemManagement systemManagement = dependencies.GetService<ISystemManagement>();
           IFileSystem fileSystem = systemManagement.FileSystem;
           IPackageManager packageManager = systemManagement.PackageManager;

           // Or...every interfaced instance on ISystemManagement is also available in the dependencies
           // supplied to every Virtual Client component constructor.
           IFileSystem fileSystem = dependencies.GetService<IFileSystem>();
           IPackageManager packageManager = dependencies.GetService<IPackageManager>();
      }
  }
  ```
  

* **Keep your components isolated from other components as much as possible**  
  Each workload/test executor, monitor or dependency installers/handler should focus on one thing and do that well. By that definition, it is natural to assert that the logic within
  that component should be isolated from other components. Over time the number of workload executors, monitors and dependency installers/handlers grows and it becomes harder
  and harder to protect the runtime platform application from regressions and bugs. One way to help accomplish this this is to be methodical about how code for one component
  can affect another component. If the 2 components do not share any code, then it is much less likely that a regression or bug in 1 of them causes the same in the other. The
  shared/core dependencies exist to make this possible given the reality that very little code in an application can be 100% isolated. However, we purposefully minimize the shared
  code between components. We instead harden the shared/core dependencies and enforce their use within workload/test executors, monitors and dependencies. The following are a
  few examples of how you keep your components isolated.

  * Avoid shared base classes in your components beyond the base 'VirtualClientComponent' class.
  
  * Contrary to what you may have learned, do not try to make everything "common/shared" code just because a few components need the same thing. Common code paths represent
    places where a single bug can break many things. Hence the bar for defining what makes something "common" must be kept high. As a general rule, common/shared code should be reserved
    for logic that is used by at least 20% of all components within the platform/core. Keep the logic isolated in your own components until that point or until otherwise instructed.

  * Keep constants and enums defined within your component. Same as above, they are NOT common until demonstrably proven to be using the same general rule.

* **Keep your package names lower-cased**  
  Windows paths, folder and file names are not case-sensitive. However on Unix/Linux they are. Packages are often downloaded to the system by the Virtual Client and then
  referenced in code by path and by name. To make cross-platform coding easier, just keep the package names lower-cased because this will work on both operating system
  platforms. This includes the name of the package file itself (e.g. diskspd.1.2.3.zip -> package name = diskspd).

* **Keep the names of any folder or files created programmatically lower-cased**  
  For the same reason as the bullet point above, it is best to keep any programmatically created folder or file names lower-cased.

* **Use the PlatformSpecifics.Combine() instead of Path.Combine() for referencing or combining paths**  
  Do not use Path.Combine(). Use ISystemManagement.PlatformSpecifics.Combine() instead. Virtual Client operates on both Windows and Unix/Linux systems. The format of paths on these two operating system platforms differs.
  On Windows backslashes are used (e.g. C:\any\path\to\something). On Unix/Linux forward slashes are used (e.g. /home/user/any/path/to/something).
  Whereas the .NET framework typically handles combining paths (e.g. Path.Combine), do not use this in the code. Related to the bullet point above 
  on unit tests, this allows tests to be written that can target either Windows or Unix/Linux that can be ran on a Windows system for example while 
  maintaining the semantics of the other platform. This is especially important for testing components that run on either Windows or Unix/Linux.

* **Always add useful context information to telemetry events/event context**  
  The Virtual Client emits a lot of very good telemetry and this is very helpful especially for cases were debugging after it has ran is necessary. The best way to help
  a user with debugging issues is to ensure proper context-specific information is included in the telemetry events emitted. All Virtual Client components emit telemetry
  and are passed an 'EventContext' object from the base VirtualClientComponent class. This object allows the developer to add additional context to the telemetry that will
  be emitted. Imagine you will have to debug a problem. What information would be helpful? Include that type of information in the context.

  ``` csharp
  // For example:
  // It is usefult to capture context-specific information related to a process being executed 
  // (the command, arguments, exit codes, standard output, standard error etc...)
  EventContext relatedContext = telemetryContext.Clone()
      .AddContext("command", workload.Command)
      .AddContext("commandArguments", workload.CommandArguments);

  return this.Logger.LogMessageAsync($"{nameof(FioExecutor)}.ExecuteProcess", relatedContext, async () =>
  {
     try
     {
         using (IProcessProxy process = this.processManager.CreateProcess(pathToExe, commandLineArguments))
         {
            await process.StartAndWaitAsync(cancellationToken).ConfigureAwait(false);
            if (!cancellationToken.IsCancellationRequested)
            {
                // Capture the process outcome details
                this.Logger.LogWorkloadProcessDetails<OpenSslExecutor>(process, relatedContext);

                process.ThrowIfErrored<WorkloadException>(ProcessProxy.DefaultSuccessCodes, errorReason: ErrorReason.WorkloadFailed);
            }
         }
     }
     catch (Exception exc)
     {
         // Always capture unexpected exception/error information
         relatedContext.AddError(exc);
         throw;
     }
  });
  ```
  

* **Throw useful exceptions**  
  Whenever developing new components, there will be certain scenarios that can happen where exceptions are explicitly thrown. Exceptions represent cases where
  a set of expectations required for the application to run correctly cannot be met. This is such an important aspect of programming in the Virtual Client that
  it has its own developer guidance documentation. Follow the recommendations in the documentation to ensure high quality exceptions and error information is
  always provided to users of the Virtual Client.

  * [Virtual Client Error Handling Developer Guide](./0070-error-handling.md)

## General Code Flow
The following section provides information on the general flow of the code for a Virtual Client component. This is helpful to understand when developing new
components for the platform.

### The Component Base Class
Firstly, all components in the Virtual Client whether action, monitor or dependency handler derive/inherit from the base class **VirtualClientComponent**. Thus, when
implementing any new components, the developer should inherit from this class. The base class has a single constructor that must be implemented in the
new component class. 


``` csharp
public class CustomWorkloadExecutor : VirtualClientComponent
{
    public CustomWorkloadExecutor(IServiceCollection dependencies, IDictionary<string, IConvertible> parameters = null)
        : base(dependencies, parameters)
    {
    }
}
```


This constructor takes in the following parameters:

* **IServiceCollection**  
  Provides all shared/common dependencies required by Virtual Client components (see section above). These core dependencies can be used to interoperate 
  with the Virtual Client core runtime platform as well as the system on which it is running.

* **IDictionary\<string, IConvertible>**  
  Provides the parameters defined in the profile for the action, monitor or dependency handler step to the class instance.

### Component Code/Method Flow
The following methods are executed in the order specified for each and every component in the Virtual Client platform. Of the methods listed, only the ExecuteAsync
method is required to be implemented. The other methods are optional and may be overridden in the new component to meet the needs of the developer implementation.

* **IsSupported**  
  Method is executed to determin whether or not the component should be executed on the system. Reasons why a component might not be valid/supported
  for a given system include:
  * The component or its dependencies cannot run on the current platform/architecture (e.g. win-arm64, linux-arm64).
  * The component or its dependencies cannot run on the current distro of the operating system (e.g. Ubuntu, Redhat).

* **InitializeAsync**  
  Method allows the developer to perform initial/preliminary validations and to set local member variables/properties that will be used when the 
  component ExecuteAsync() method is called. Some common things that are implemented in this method include:
  * Checks to ensure the component has the dependencies it needs to succeed (e.g. dependency packages, system settings)
  * Setting member variables on the class instance that can be used later during the ExecuteAsync() call.

* **Validate**  
  Method allows the developer to validate the component and parameters that were passed to the component in the constructor. This happens after
  the initialization step to allow for any parameters that have "calculated" or replacement values to be evaluated. The developer should call the
  "EvaluateParametersAsync" method to apply any well-known placeholders to the parameters. See the documentation on [profiles](https://microsoft.github.io/VirtualClient/docs/guides/0011-profiles/)
  for more information on parameter references and well-known parameter values.

* **ExecuteAsync**  
  Method is where the developer should perform the main body of work for the component. For example, this method may execute a workload binary
  or test on the system. Additionally, this is the step where measurements/metrics captured from running workload or script binaries are parsed
  and captured.

* **CleanupAsync**  
  Method allows the developer to perform any cleanup operations. For example, certain workload binaries leave log files as output on the file system.
  It is a good idea to always cleanup any artifacts created during the operation of the component. This helps to ensure idempotency with components that
  run in the Virtual Client runtime.

## Trace Logging and Telemetry
All logging in the Virtual Client is telemetry. Telemetry differs from traditional free-form logging because it is typically more highly structured. Even trace 
logging in the Virtual Client is structured telemetry. When writing code in the Virtual Client, a few different extension methods are provided that should be used to ensure that 
logging is routed correctly. There are 3 different categories of telemetry in the Virtual Client each with its own logging extension method(s).

* **Trace Logs Telemetry**  
  This category includes seneric application trace messages. These are used primarily for debugging purposes make it easy for the user to see exactly what the application is 
  doing. Trace messages are especially important for situations where operations are failing in the Virtual Client. In most cases, errors are automatically captured by the 
  logging mechanics of the Virtual Client. Having the error messages and callstack available is very helpful for determining root causes of issues.

  There are 2 different levels of importance for trace messages in the Virtual Client. A "trace message" will always be LogLevel.Trace. These messages are typically used for 
  verbose output and will display in the console output ONLY when the "--debug" flag is used on the command line.

  ``` csharp
  this.Logging.LogTraceMessage($"AnyComponent.PerformOperation", telemetryContext);

  // A corresponding asynchronous method implementation exists as well.
  await this.Logging.LogTraceMessageAsync($"AnyComponent.PerformOperation", telemetryContext)
      .ConfigureAwait(false);
  ```

  The second type of trace message is the application informational message. The messages are typically LogLevel.Information but may be any log level. This type of trace message 
  is always output to console output and to other targets.

  ``` csharp
  this.Logging.LogMessage($"AnyComponent.PerformOperation", LogLevel.Information, telemetryContext);
  this.Logging.LogMessage($"AnyComponent.SomeTypeOfError", LogLevel.Error, telemetryContext);

  // A corresponding asynchronous method implementation exists as well.
  await this.Logging.LogMessageAsync($"AnyComponent.PerformOperation", LogLevel.Information, telemetryContext)
      .ConfigureAwait(false);

  // Error information is easy to capture as well using this logging extension method.
  try
  {
       // Some kind of logic that could result in exceptions happening.
  }
  catch (Exception exc)
  {
      this.Logging.LogMessage("AnyComponent.SomeOperationError", LogLevel.Error, telemetryContext.AddError(exc));
  }
  ```

  The developer will often see a particular LogMessage/LogMessageAsync extension method being used that wraps an entire block of 
  code. This extension provides additional functionality desirable when logging information including capturing the time (in milliseconds)
  that the logic in the block of code took to execute and automatically handling + capturing error information.

  ``` csharp
  return this.Logging.LogMessageAsync($"AnyComponent.PerformOperation", telemetryContext, async () =>
  {
      // A block of 1 or more lines of code inside of here to perform some set of operations.
      // This logging extension provides some nice features around logging:
      //
      // 1) A "Start" event is written capturing a timestamp at the beginning of the logic block operations
      //   (e.g. AnyComponent.PerformOperationStart).
      //
      // 2) A "Stop" event is written capturing a timestamp at the end of the logic block operations (e.g. AnyComponent.PerformOpertionStop). 
      //    This event will contain a propery 'durationMs' in the telemetry message context/custom dimensions that defines the length of
      //    time in milliseconds the logic took. This can be helpful when analyzing the performance of logic later on
      //    without needing to perform date/time math.
      //
      // 3) Any exceptions/errors that are throw will be automatically captured and the error messages + callstack will be added to
      //    an "Error" message (e.g. AnyComponent.PerformOpertionError).
      //
      // All of this functionality is wrapped up in the extension method which allows for consistency in telemetry event names and
      // error handling while significantly reducing "noise" in the code related to telemetry logic.
  });
  ```
  
  The Virtual Client framework additionally has a logging extension method for cases where the developer wants to treat exceptions/errors as
  traditional free-form logging vs. structured logging.

  ``` csharp
  try
  {
       // Some kind of logic that could result in exceptions happening.
  }
  catch (Exception exc)
  {
      this.Logging.LogErrorMessage(exc, telemetryContext);
  }

  ```

* **Metrics/Measurements Telemetry**  
  One of the primary goals of the Virtual Client runtime is to capture and structure metrics/measurements from the output of workloads, tests
  and monitors. To ensure consistency in the structure of metrics/measurements, a logging extension method is provided for the purpose.

  ``` csharp
  private void CaptureMetrics(IProcessProxy process, DateTime startTime, DateTime endTime, EventContext telemetryContext)
  {
      string workloadResults = process.StandardOutput.ToString();
      ExampleWorkloadMetricsParser resultsParser = new ExampleWorkloadMetricsParser(workloadResults);
      IList<Metric> workloadMetrics = resultsParser.Parse();

      this.Logger.LogMetrics(
          toolName: "ExampleWorkload",
          scenarioName: "some_unique_scenario_for_the_workload",
          scenarioStartTime: startTime,
          scenarioEndTime: endTime,
          metrics: workloadMetrics,
          metricCategorization: null,
          scenarioArguments: "ExampleWorkload.exe --any=command --line=arguments",
          this.Tags,
          telemetryContext);
  }
  ```

* **System Events Telemetry**  
  The Virtual Client also has a logging extension designed for capturing important information or events from the system on which it is running. This extension
  allows the developer to capture this information and to ensure it is routed together for distinction in telemetry storage resources.

  ``` csharp
  // The only requirement for the dictionary values (e.g. the object instances) is that
  // it is JSON-serializable.
  IDictionary<string, object> eventLogEntries = this.GetEventLogEntries(eventId: 21);
  this.Logger.LogSystemEvents("AnyMonitor.CaptureEventLogs", eventLogEntries, telemetryContext)
  ```

* **Metadata Contract**
  The Virtual Client framework provides a simple model for enabling developers to impart a "metadata contract" in the telemetry that is output from
  the application. The metadata contract facilitates a consistent schema within telemetry events to include context information about the host, operating system,
  hardware and workload/monitor scenarios. In fact certain information about the host, operating system, hardware and profile workload/monitor is included in
  the output of the Virtual Client by default.

  * [Metadata Contract Details and Examples](../guides/0040-telemetry.md)  
    Familiarize yourself with the different categories of metadata available (e.g. default, dependencies, host, runtime, scenario).

  * Persisted/Global Metadata  
    The metadata contract feature allows users to persist metadata that will be included with the telemetry emitted by every
    component within a Virtual Client profile (e.g. actions, monitors, and dependencies). Persisted metadata will be combined with
    component-specific metadata (described below) when emitting telemetry within that component.

    ``` csharp
     // Persist metadata properties throughout the entire execution runtime of the Virtual Client.
     MetadataContract.Persist("company", "Microsoft", MetadataContractCategory.Default);
     MetadataContract.Persist("package_openssl", "openssl.3.0.0.zip", MetadataContractCategory.Dependencies);
     MetadataContract.Persist("hostType", "Physical Blade", MetadataContractCategory.Host);
     MetadataContract.Persist("", "Microsoft", MetadataContractCategory.Runtime);
     MetadataContract.Persist("category", "CPU Performance", MetadataContractCategory.Scenario);
    ```
  * Component-Specific Metadata  
    The metadata contract feature supports the ability for the developer to define information specific to a given component within a Virtual Client
    profile. This metadata is merged with the persisted/global metadata described above and emitted with the telemetry for the specific component.

    ``` csharp
     // Each component within Virtual Client has a "MetadataContract" property to which the developer can add metadata specific to the
     // execution context of that component at runtime (e.g. OpenSslExecutor).
     this.MetadataContract.Add("company", "Microsoft", MetadataContractCategory.Default);
     this.MetadataContract.Add("package_openssl", "openssl.3.0.0.zip", MetadataContractCategory.Dependencies);
     this.MetadataContract.Add("hostType", "Physical Blade", MetadataContractCategory.Host);
     this.MetadataContract.Add("", "Microsoft", MetadataContractCategory.Runtime);
     this.MetadataContract.Add("category", "CPU Performance", MetadataContractCategory.Scenario);
    ```


## Telemetry Loggers
The Virtual Client uses a set of different types of loggers each implementing the .NET ILogger interface:

* **Console Logger**  
  Used to write telemetry messages to the console standard output and error streams (e.g. on-screen logging). Verbose output on-screen is enabled in the
  Virtual Client by supplying the "--debug" flag on the command line.

* **File Logger**  
  Used to write telemetry messages to log files. Virtual Client writes 3 different types of log files that can all be found in the application "logs" directory.
  The 3 different log files include those that contain general trace logging and messages, those that include workload and system metrics information only and those that
  contain system performance counter measurements and information.

* **Event Hub Logger**  
  Used to upload telemetry messages to an Azure Event Hubs namespace. In practice the Virtual Client uses 3 or 4 different Event Hubs within the namespace to section
  off the data by its category (e.g. traces vs. metrics). Azure provides out-of-box support for a number of different "big data" storage resources that can ingest the telemetry
  from the Event Hubs into storage. For example the VC Team connects Azure Data Explorer/Kusto clusters to the Event Hub so that the telemetry emitted to the Event Hub is being
  automatically ingested into the cluster databases.

## Code Examples
Sometimes the very best documentation for developers to learn is hands-on coding. The VC Team has included a set of code examples that illustrate some of the
implementation concepts. Good news! If you are at this point, your manager can no longer give you a hard time talking about "implementation details".
The devil is in them from this point forward, so enjoy!! :) Each of the examples below can be ran right at the developer desktop for breakpoint/debugging 
euphoria. We typically use the Visual Studio IDE due to its robust support for developer "inner-loop" needs.

* **Example Implementations**  
  The following examples illustrate some of the basic implementation concepts for Virtual Client workload/test executors and monitors. These examples are
  used in an example workload profile. The examples can be run at the desktop within Visual Studio for live debugging.

  * [Example Workload + Monitoring Profile](https://github.com/microsoft/VirtualClient/blob/main/src/VirtualClient/VirtualClient.Main/profiles/EXAMPLE-WORKLOAD.json)  
    This profile references the example workload executor below as well as the example monitor. This profile also has a dependency installer/handler in it
    to download a workload package from a blob store. This profile can be run at the desktop to see how things work. Note that you will need a SAS URI to the
    VC Team storage account where the workload packages exist. Virtual Client will be downloading dependency packages from here. Contact the VC Team to get 
    a SAS URI for your needs.

    * To Debug: In Visual Studio:
      * Set the 'VirtualClient.Main' project as the 'Startup project'.
      * Right-click on this project and select 'Properties' from the menu.
      * In the 'Debug' section, put the following in for the 'Application arguments':
        * --profile=EXAMPLE-WORKLOAD.json --timeout=1440 --packages=\<YourBlobStoreSASUri>
  
  * [Example Workload/Test Executor](https://github.com/microsoft/VirtualClient/blob/main/src/VirtualClient/VirtualClient.Actions/Examples/ExampleWorkloadExecutor.cs)  
    Provides a coded example for how to write a basic workload executor.

  * [Workload/Test Executor and Background Profiling Monitor](https://github.com/microsoft/VirtualClient/blob/main/src/VirtualClient/VirtualClient.Monitors/ExampleProfilingMonitor.cs)  
    Provides a coded example of how to write a monitor that can be used for background profiling operations (both Interval-based as well as On-Demand).


  * [Example Client/Server Executor](https://github.com/microsoft/VirtualClient/blob/main/src/VirtualClient/VirtualClient.Actions/Examples/ClientServer)  
    Provides a coded example for how to write advanced, client/server workload executors.

* **Examples of Unit Tests**  
  The following examples illustrate some of the unit testing concepts in the Virtual Client codebase.

  * [Example Action/Executor](https://github.com/microsoft/VirtualClient/blob/main/src/VirtualClient/VirtualClient.Actions.UnitTests/Example2WorkloadExecutor.cs)

  * **Example Tests using MockFixture**  
    Provides examples of basic unit testing concepts along with the use of the MockFixture helper class.

    * [Example Tests](https://github.com/microsoft/VirtualClient/blob/main/src/VirtualClient/VirtualClient.Actions.UnitTests/Example2WorkloadExecutorTests_MockFixture.cs)

  * **Example Tests using DependencyFixture**  
    Provides examples of basic unit testing concepts along with the use of the DependencyFixture helper class. This class differs from the MockFixture
    in that it uses in-memory implementations of the Virtual Client platform core dependencies. It is typically used for functional testing in Virtual Client
    projects.

    * [Example Tests](https://github.com/microsoft/VirtualClient/blob/main/src/VirtualClient/VirtualClient.Actions.UnitTests/Example2WorkloadExecutorTests_DependencyFixture.cs)

## Debugging Virtual Client Code
The sections below document some of the ways in which the developer can debug components in the Virtual Client. The Visual Studio IDE is used for these examples because of its
robust support for developer inner-loop processes including support for debugging.

### Debug in Visual Studio Using Unit/Functional Tests
There is no faster way to get a debugger attached to your code than via an Nunit test. The Visual Studio 'Test Explorer' makes it very
easy to put a break point in the code of a test method, to right-click in the test and then to select 'Debug Test(s)' from the context
menu. From that point Visual Studio will build your source code and will execute the test with a debugger attached. The VC Team often 
has 3 different types of test projects in source (i.e. unit, functional, integration) that are used for testing the code with various
goals and these are all just as easy to use for debugging purposes.

Once you have written a unit/functional test in Visual Studio:
* Place a breakpoint at the beginning of the test method.
* Right-click in the test.
* Select "Debug Test(s)" from the context menu.

Visual Studio will build the source code, attach a debugger to the test execution runner and will run the test code. The debugger will hit your breakpoint in the test. From that
point forward, you can step through your code as normal (e.g. F5, F10, F11 debug stepping).

#### Debug in Visual Studio by Running a Custom Profile
The developer may sometimes want to run the full Virtual Client runtime executable to debug his/her code. This is often the case when the developer wants to test the code live and
without any mocks. To do this, it is easiest to create a custom profile in a directory on the file system that contains actions, monitors, dependencies definitions related to the class
the developer wants to debug. If the profile is for debugging purposes only, it is best to create it in a directory outside of source control and to place ONLY the components
in the profile required for correct operations. For example, the developer may be creating a new action/workload executor and wants to test it. This action/workload executor may 
require a dependency package exist or be downloaded at runtime. The developer can either place the dependency package in the build output directory for the Virtual Client or can 
put an appropriate DependencyPackageInstallation component in the profile so that it can be downloaded (assuming the dependency package exists in the package store). The following
examples illustrate how to do this.

* **Custom Profile Option #1**  
  In this example, a custom profile will be used to debug an action/executor on which the developer is working (e.g. ExampleWorkloadExecutor below). This action/executor requires
  a specific dependency package containing workload binaries to be downloaded. A custom profile will be used to incorporate the action/executor and the workload dependency package.

  ``` json
  # A custom profile is created and placed on the file system somewhere (typically somewhere outside of the source directory). In this profile, the
  # custom action/executor component is added to the actions and the dependency package installation component is added to the dependencies.
  {
    "Description": "Debug Example Workload Executor",
    "Actions": [
        {
            "Type": "ExampleWorkloadExecutor",
            "Parameters": {
                "Scenario": "Scenario1",
                "CommandLine": "Workload duration=00:01:00",
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
  

  Once the profile is created, setup Visual Studio for debugging
  * Set the solution configuration to **Debug** at the top of the Visual Studio IDE window.

  * Set the **VirtualClient.Main** project as the startup project. To do so, right-click on the project in the Solution Explorer and select 
    **Set as Startup Project** from the context menu.

  * Right-click on the VirtualClient.Main project and open the **Debug** options. Set the following information.
    * Application arguments = \{VirtualClientCommandLine\}    
      (e.g. `--profile=S:\one\debugging\DEBUG-EXAMPLE-WORKLOAD.json --profile=MONITORS-NONE.json --packages="https://virtualclient..."`)

  * Place a breakpoint in the code where you like (e.g. in the InitializeAsync or ExecuteAsync methods of your component).

  * Click the play/continue button at the top-center of the Visual Studio IDE window (or press the F5 key).


* **Custom Profile Option #2**  
  This option is the same as option #1 above except that the developer will copy the workload dependency package to the appropriate directory in the 
  Virtual Client build output directory ahead of time. Once the package is in this directory, the Virtual Client does not need to download it and thus
  the custom profile can be simplified by removing the "Dependencies" section as illustrated below.

  ``` json
  # A custom profile is created and placed on the file system somewhere (typically somewhere outside of the source directory). In this profile, the
  # custom action/executor component is added to the actions.
  {
    "Description": "Debug Example Workload Executor",
    "Actions": [
        {
            "Type": "ExampleWorkloadExecutor",
            "Parameters": {
                "Scenario": "Scenario1",
                "CommandLine": "Workload duration=00:01:00",
                "ExampleParameter1": "AnyValue1",
                "ExampleParameter2": 4567,
                "PackageName": "exampleworkload",
                "Tags": "Test,VC"
            }
        }
    ]
  }
  ```
  
  The workload dependencies package must be copied into the **\{repo_directory\}/out/bin/Release/x64/VirtualClient.Main/packages**
  directory before beginning to debug.

  ```
  e.g.
  S:\one\virtualclient\out\bin\Release\VirtualClient.Main\packages\exampleworkload.1.0.0.zip
  ```
  
  Once the profile is created, setup Visual Studio for debugging
  * Set the solution configuration to **Debug** at the top of the Visual Studio IDE window.

  * Set the **VirtualClient.Main** project as the startup project. To do so, right-click on the project in the Solution Explorer and select 
    **Set as Startup Project** from the context menu.

  * Right-click on the VirtualClient.Main project and open the **Debug** options. Set the following information.
     * Application arguments = \{VirtualClientCommandLine\}    
       (e.g. `--profile=S:\one\debugging\DEBUG-EXAMPLE-WORKLOAD.json --profile=MONITORS-NONE.json`)  

  * Place a breakpoint in the code where you like (e.g. in the InitializeAsync or ExecuteAsync methods of your component).

  * Click the play/continue button at the top-center of the Visual Studio IDE window (or press the F5 key).

* **Custom Profile Option #3**  
  This option is the same as option #2 above except that we will set an environment variable to the path/directory location of the workload dependency
  package on the system.

  ``` json
  # A custom profile is created and placed on the file system somewhere (typically somewhere outside of the source directory). In this profile, the
  # custom action/executor component is added to the actions.
  {
    "Description": "Debug Example Workload Executor",
    "Actions": [
        {
            "Type": "ExampleWorkloadExecutor",
            "Parameters": {
                "Scenario": "Scenario1",
                "CommandLine": "Workload duration=00:01:00",
                "ExampleParameter1": "AnyValue1",
                "ExampleParameter2": 4567,
                "PackageName": "exampleworkload",
                "Tags": "Test,VC"
            }
        }
    ]
  }
  ```
  
  The workload dependencies package exists in a directory on the system already. We set the environment variable **VC_PACKAGES_DIR** to this path/directory location 
  before beginning to debug.

  ```
  e.g.

  # Workload dependency package exists in a folder on the file system. We will set the 'VC_PACKAGES_DIR' environment 
  # variable to this location.
  S:\one\debugging\packages\exampleworkload.1.0.0.zip
  ```

  Once the profile is created, setup Visual Studio for debugging
  * Set the solution configuration to **Debug** at the top of the Visual Studio IDE window.

  * Set the **VirtualClient.Main** project as the startup project. To do so, right-click on the project in the Solution Explorer and select 
    **Set as Startup Project** from the context menu.

  * Right-click on the VirtualClient.Main project and open the **Debug** options. Set the following information.
    * Application arguments = \{VirtualClientCommandLine\}   
      (e.g. `--profile=S:\one\debugging\DEBUG-EXAMPLE-WORKLOAD.json --profile=MONITORS-NONE.json`)

    * Environment variables = Add the `VC_PACKAGES_DIR` variable and the path to your package directory.  
      (e.g. `VC_PACKAGES_DIR = S:\one\debugging\packages`)

  * Place a breakpoint in the code where you like (e.g. in the InitializeAsync or ExecuteAsync methods of your component).

  * Click the play/continue button at the top-center of the Visual Studio IDE window (or press the F5 key).