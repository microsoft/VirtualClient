# Testing Methodology
The following sections provide insights into a testing methodology that can be applied to testing workload executors in the Virtual Client
codebase. The goal here is to shine a light on the basic techniques applied and the mechanics they drive when testing Virtual Client logic.

Before reading the documentation below, familiarize yourself with the general principles involved in writing tests for components in an
application or system codebase.


## Testing Framework Libraries
The following open source libraries are used to enable rich and robust testing outcomes in the Virtual Client codebase.

* **[AutoFixture](https://github.com/AutoFixture/AutoFixture/wiki)**  
  AutoFixture is a library/framework that is used to create mock/fake objects that are used in testing scenarios. The ability to create valid mock objects in test
  code is a very common requirement. The AutoFixture framework makes mock object setup a triviality.

  Within the Virtual Client codebase, a C# [extension method approach](https://github.com/Azure/AzureVirtualClient/blob/main/src/VirtualClient/VirtualClient.TestFramework/FixtureExtensions.cs) 
  in conjunction with AutoFixture to define the setup of a wide range of different mock objects in a single, consolidated place minimizing the need for that
  setup in all of the tests. 

* **[Moq](https://github.com/Moq/moq4/wiki/Quickstart)**  
  The Moq library provides a robust yet simple framework allowing developers to setup mock behaviors when testing. The MockFixture noted below uses the Moq framework 
  extensively to setup and change behaviors for core dependencies of Virtual Client components/classes. This enables a wide range of scenarios and code handling mechanics to
  be tested.

## Testing Fixtures
The Virtual Client solution uses a few different types of testing fixtures to simplify the requirements for setting up dependency behaviors required to
test workload executor and supporting classes. Testing fixtures enable a reduction in the code required to setup mock/test behaviors by encapsulating the logic
in a single place or to use C# extension methods to do the same.

* **[MockFixture](https://github.com/Azure/AzureVirtualClient/blob/main/src/VirtualClient/VirtualClient.TestFramework/MockFixture.cs)**  
  The MockFixture provides/encapsulates all of the dependencies required by Virtual Client workload executors implemented as mock objects using the Moq 
  framework. Mock fixtures help to minimize lines of code required to properly test classes and components in a few different ways. Firstly, the developer does
  not have to duplicate all of the mock class/interface dependencies over and over in individual test classes. Almost every dependency needed is on the MockFixture
  class. The MockFixture is also used as to create reusable C# extensions methods. These extensions method allow the developer to perform setup and verifications
  of test code more simply.

* **[DependencyFixture](https://github.com/Azure/AzureVirtualClient/blob/main/src/VirtualClient/VirtualClient.TestFramework/DependencyFixture.cs)**  
  The DependencyFixture provides/encapsulates all of the dependencies required by Virtual Client workload executors implemented to work the same way
  as live/real dependencies except using only in-memory backing. In-memory backing means that the dependency keeps track of its assets/objects purely
  in-memory. For example, the real package management dependency in Virtual Client uses the file system to reference and manage workload packages. The
  in-memory version of it supplied by the DependencyFixture keeps the references to the packages and to the file system all in-memory (i.e. no actual interaction
  with the real file system). The DependencyFixture is often used with functional tests in the Virtual Client codebase. This is because functional tests are 
  focused on the end-to-end behavioral correctness of the code flow (e.g. the execution of all components in a single profile) and the DependencyFixture more
  closely mimics certain "real-life" environment/system behavior.

## Test Setup Mechanics
Each of the tests in the Virtual Client codebase (unit as well as functional) uses one or more of the testing fixtures noted above. This simplifes the process of testing code
by instilling repeatable patterns and reducing redundancy/duplication in setup requirements. The ultimate goals are as follows:

* Remove code from individual tests and test methods that is used purely to setup mock/fake behaviors.
* Improve the readability of the tests.
* Reduces the learning curve for new engineers needing to write tests.
* Increases the speed at which the team can do the work to write new code and test it thoroughly.

Towards meet these goals, we use a set of general techniques to ensure the testing fixtures can be used easily at the same time as they are flexible enough to
cover a wide range of scenario setups. The following list describes some of the techniques involved.

* Testing fixtures should have ALL dependencies necessary for testing Virtual Client components as properties. This prevents the duplication of these as member
  variables in test classes. See the 'MockFixture' class noted above for examples.

* Testing fixtures should have helper methods that make it easy to accomplish the setup of very common mock/fake behaviors on dependencies. One of the most common
  ways we do this is to implement C# extension methods. This can significantly reduce the code required in the test classes and methods and improve readability.
  See the [MockSetupExtensions](https://github.com/Azure/AzureVirtualClient/blob/main/src/VirtualClient/VirtualClient.TestExtensions/MockSetupExtensions.cs) 
  class for examples.

The best way to illustrate the ideas is to showcase the usage patterns. The following links provide good examples of using the various testing fixtures to establish robust
tests for a given class or component. For developers using the Visual Studio IDE, it is easy to debug the code. Simply set a breakpoint in any one of the test methods, 
right-click on the test and select "Debug Test(s)" to see the mechanics in motion.

* [Example Using MockFixture Mechanics](https://github.com/Azure/AzureVirtualClient/blob/main/src/VirtualClient/VirtualClient.Actions.UnitTests/Example2WorkloadExecutorTests_MockFixture.cs)  
  Example of how to write unit or functional tests using the Moq framework and MockFixture class as the foundation for the testing fixture and setup mechanics. 

* [Example Using DependencyFixture Mechanics](https://github.com/Azure/AzureVirtualClient/blob/main/src/VirtualClient/VirtualClient.Actions.UnitTests/Example2WorkloadExecutorTests_DependencyFixture.cs)  
  Example of how to write unit or functional tests using the in-memory dependency implementations as the foundation for the testing fixture 
  and setup mechanics.

## Cross-Platform Testing
When doing development of Virtual Client components, you will often need to consider debugging in cross-platform scenarios (e.g. Windows and Linux). The following
section provides some useful information to help you do development where you need to validate on different OS platforms.

### Useful Linux Operations
The following section illustrates some of the more interesting commands/operations you can use when testing on Linux systems.

#### SSH and SCP for Testing on Linux
SSH is a common tool used to login to Linux systems. When using VMs, it can be very handy in test scenarios to simply set a password on your
VMs to make establishing SSH sessions easier. SCP is a common tool for copying files and directories from your local system to the VM and vice-versa.
These toolsets are both commonly installed as part of the Windows 10+ operating system.

<div style="font-size:10.5pt">

```
# Establish and SSH connection to the system. The SSH toolset will as for a password.
ssh vcvmadmin@10.1.0.1

# Once the SSH session is established, you can copy a directory from the local system to the VM. You will open a different command
# line console for this SCP command than the one used for the SSH connection above.
C:\Users\You>scp S:\one\repo\out\bin\Debug\VirtualClient.Main\net6.0\linux-x64\publish vcvmadmin@10.1.0.1:/home/vcvmadmin/VirtualClient
```
</div>