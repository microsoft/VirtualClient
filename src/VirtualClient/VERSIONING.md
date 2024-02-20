# Versioning
The following sections describe the versioning process for projects/libraries within the Virtual Client platform/core repo.

Projects versioned using the version defined by the Official build pipeline. This version is defined in the in the [OneBranch.Official.yml](https://github.com/Azure/AzureVirtualClient/blob/main/.pipelines/OneBranch.Official.yml)
file found in the root of the repo in the **.pipeline** directory. The team only ever changes the 'major' or 'minor' versions in this
file. Furthermore,tThe team follows the 'semantic versioning' process to determine versions. You can learn more about semantic versioning at the 
link below.

* [Semantic Versioning](https://semver.org/)
* [Semantic Versioning in .NET](https://docs.microsoft.com/en-us/dotnet/core/versions/#semantic-versioning)

#### Semantic Versioning Process
For practical purposes, use the following distinctions when trying to determine what version should be used for this project where changes have
been made. This process is generally followed with most changes to the Virtual Client. However, it MUST be followed before any release of new
"common" libraries or changes to them.

<div style="font-size:10pt">

``` csharp
// Version Format:
{Major}.{Minor}.{Patch|Revision}

// Example: Version = 1.2.3
// Major Version = 1
// Minor Version = 2
// Patch Version = 3
```
</div>

* **Increment the major version for breaking changes**  
  If changes are made to existing classes that would be a breaking change to projects that reference the library, the **major** version in the 
  'OneBranch.Official.yml' should be incremented. When incrementing the **major** version, the **minor** should be zeroed out. If the version is 
  initially 1.1.\{revision\} for example, then it will become 2.0.\{revision\} afterwards.

  The following are a few examples of changes that require major version updates:
  * The signature of a public method on one of the interfaces or classes is changed.
  * The signature of a protected method on one of the interfaces or classes is changed.
  * The name of a class or interface is changed.
  * The namespace of a class or interface is changed.
  * Access modifiers for methods or members are changed.
  * A class or interface is removed from the project.
  * A new dependency is required for the project (e.g. a new reference library).

  <div style="font-size:10pt">

  ``` yaml
  - task: onebranch.pipeline.version@1 # generates automatic version. For other versioning options check https://aka.ms/obpipelines/versioning
    displayName: 'Setup Build Number'
    inputs:
      system: 'RevisionCounter'
      major: '1' <-------------- This must be incremented.
      minor: '0' <-------------- This must be zeroed out.
      exclude_commit: true
  ```
  </div>

* **Increment the minor version when new classes, interfaces or methods and members on them are added**  
  If changes are made that bring new features and functionality to the classes and interfaces within the project, then the minor version should be
  incremented.If the version is initially 1.1.\{revision\} for example, then it will become 1.2.\{revision\} afterwards.

  The following are a few examples of changes that require minor version updates:
  * A new class or interface is added to the project.
  * A new method or member is added to an existing class or interface.
  * A specific implementation is made to be significantly more efficient than before.

  Additionally, the minor version for the Official build must be incremented to match. This version is found in the 'OneBranch.Official.yml' file
  found in the root of the repo in the **.pipeline** directory.

  <div style="font-size:10pt">

  ``` yaml
  - task: onebranch.pipeline.version@1 # generates automatic version. For other versioning options check https://aka.ms/obpipelines/versioning
    displayName: 'Setup Build Number'
    inputs:
      system: 'RevisionCounter'
      major: '1'
      minor: '0' <-------------- This must be incremented.
      exclude_commit: true
  ```
  </div>

* **Increment the patch/revision version for bug fixes or improvements to existing implementations**  
  There is nothing that the developer needs to do here. It is the default behavior of the OneBranch official build to increment the patch/revision
  version on every build of the Virtual Client.

  The following are a few examples of changes that require minor version updates:
  * A bug is fixed within the logic of a method but no changes are made to the method signature.
  * The implementation within the logic of a method is refactored but no changes are made to the method signature.

* **Set the default build version to a major/minor which is higher than the version set in the pipeline YAML files above.
  In order to ensure the debugging experience for developers creating extensions for the Virtual Client, the default version
  should be set to a higher major/minor version than the one for the Official build. For example, if the Official build version
  in the YAML is set to a major/minor of 1.5.*, then the default version for the Virtual Client should be set to 1.6.*. The default
  version is set in the Module.props for the Virtual Client solution/directory.

  <div style="font-size:10pt">

  ``` xml
  <PropertyGroup>
      <PackagePreReleaseSuffix></PackagePreReleaseSuffix>
      <GenerateAssemblyInfo>true</GenerateAssemblyInfo>
      <AssemblyVersion>1.6.0.0</AssemblyVersion>
      <AssemblyVersion Condition="'$(VCBuildVersion)' != ''">$(VCBuildVersion)</AssemblyVersion>
  </PropertyGroup>
  ```
  </div>