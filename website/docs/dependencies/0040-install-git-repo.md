# Install/Clone a Git Repo
Virtual Client git clones a public repository into VC `packages` directory.

:::info
Some Windows versions do not have `Git` installed by default. You will need to install `Git` first via the `Chocolatey` package manager.
:::

* [Install Chocolatey](./0020-install-chocolatey.md)
* [Install Chocolatey Packages](./0030-install-chocolatey-packages.md)

## Supported Platform/Architectures
* linux-x64
* linux-arm64
* win-x64
* win-arm64

## Profile Component Parameters
The following section describes the parameters used by the individual component in the profile.

| **Parameter** | **Required** | **Description**                                                                                                 |
|---------------|--------------|-----------------------------------------------------------------------------------------------------------------|
| PackageName   | Yes          | The logical name of the that will be registered with the Virtual Client runtime to represent the packages directory into which the repo was cloned. Other profile components can use this name to reference/discover the repo and its location. |
| RepoUri       | Yes          | The full URI to the Git repository to download/clone into the packages directory.                               |
| Scenario      | No           | A name/identifier for the specific component in the profile. This is used for telemetry purposes only with components in dependency sections of the profile (i.e. cannot be used with --scenarios option on the command line).                  |


## Example
In this example, VC clones https://github.com/eembc/coremark.git into the runtime packages directory

* [Profile Example](https://github.com/microsoft/VirtualClient/blob/main/src/VirtualClient/VirtualClient.Main/profiles/PERF-CPU-COREMARK.json)

  <div class="code-section">

  ``` json
  {
      "Type": "GitRepoClone",
      "Parameters": {
          "RepoUri": "https://github.com/eembc/coremark.git",
          "PackageName": "coremark"
      }
  }
  ```
  </div>

## Example
In this example, VC installs Chocolatey, installs Git on the system and then clones https://github.com/aspnet/Benchmarks.git into the runtime packages directory

* [Profile Example with Chocolatey](https://github.com/microsoft/VirtualClient/blob/main/src/VirtualClient/VirtualClient.Main/profiles/PERF-ASPNETBENCH.json)

  <div class="code-section">

  ``` json
  {
      "Type": "ChocolateyInstallation",
      "Parameters": {
          "Scenario": "InstallChocolatey",
          "PackageName": "chocolatey"
      }
  },
  {
      "Type": "ChocolateyPackageInstallation",
      "Parameters": {
          "Scenario": "InstallGitOnWindows",
          "PackageName": "chocolatey",
          "Packages": "git"
      }
  },
  {
      "Type": "GitRepoClone",
      "Parameters": {
          "Scenario": "CloneAspNetBenchmarksRepo",
          "RepoUri": "https://github.com/aspnet/Benchmarks.git",
          "PackageName": "aspnetbenchmarks"
      }
  }
  ```
  </div>