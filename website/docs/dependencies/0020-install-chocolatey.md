# Install Chocolatey
Virtual Client has a dependency component that can be added to a workload or monitor profile to install dependency packages from a package store. The following section illustrates the
details for integrating this into the profile.

- [Chocolatey Official Page](https://chocolatey.org/)
- [Chocolatey Packages](https://community.chocolatey.org/packages)

### Supported Platform/Architectures
* win-x64
* win-arm64

### Component Parameters
The following section describes the parameters used by the individual component in the profile.

| **Parameter** | **Required** | **Description**                                                                                                 |
|---------------|--------------|-----------------------------------------------------------------------------------------------------------------|
| PackageName   | Yes          | The logical name of the package that will be registered with the Virtual Client runtime. This name can be used by other profile components to reference the installation parent directory location for Chocolatey. |
| Scenario      | No           | A name/identifier for the specific component in the profile. This is used for telemetry purposes only with components in dependency sections of the profile (i.e. cannot be used with --scenarios option on the command line). |


### Example
The following sections provides examples for how to integrate the component into a profile.

* [Profile Example](https://github.com/microsoft/VirtualClient/blob/main/src/VirtualClient/VirtualClient.Main/profiles/PERF-CPU-LAPACK.json)

  <div class="code-section">

  ```json
  {
      "Type": "ChocolateyInstallation",
      "Parameters": {
          "Scenario": "InstallChocolatey",
          "PackageName": "choco"
      }
  }
  ```
  </div>