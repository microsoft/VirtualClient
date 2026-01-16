# Install Linux Packages
Virtual Client has a dependency component that can be added to a workload or monitor profile to install dependency packages from a package store. The following section illustrates the
details for integrating this into the profile. The platform has a goal to supporting all platforms and specifics with relative ease (i.e. it should work out-of-the-box). 
Installing a package is easy. Installing packages on different platforms and package managers is not. 

The platform encapsulates the complex logic of handling package installation on different distros into a single component that users can easily 
use in profiles.

- [Package Manager Apt](https://wiki.debian.org/AptCLI)  
- [Package Manager Dnf](https://github.com/rpm-software-management/dnf)  
- [Package Manager Yum](http://yum.baseurl.org/)  
- [Package Manager Zypper](https://en.opensuse.org/Portal:Zypper)  
- [A Good Third-Party Intro](https://www.linode.com/docs/guides/linux-package-management-overview/)  
- [Search for Packages](https://pkgs.org/)  

## Supported Platform/Architectures
* linux-x64
* linux-arm64

## .NET Supported Linux Distributions
VirtualClient runs on .NET 8. It should run on any distribution supported by .NET 8.

* [Supported Distros](https://github.com/dotnet/core/blob/main/release-notes/8.0/supported-os.md)

## Supported Package Managers and Distros
The following table shows the list of package managers and the Linux distros on which they are typically supported by default. Note that this list does not infer that 
all Virtual Client workloads and profiles have been properly tested and confirmed working across all of these package managers and distros but is intended only
to help users of the application make basic determinations on the likelihood that a particular profile will operate correctly on a given distro. It is entirely possible that 
some workloads, monitors or dependencies simply will not work on specific distros.To make this easier for users to determine whether a given profile was tested and confirmed
to work on a particular distro, the profile may contain a **SupportedDistributions** parameter indicating this.

| Package Manager  | Distributions                  |
|------------------|--------------------------------|
| apt              | Debian, Ubuntu                 |
| dnf              | CentOS/RHEL8, Mariner, Fedora  |
| yum              | CentOS/RHEL7                   |
| zypper           | OpenSUSE                       | 
| None             | Flatcar                        |


## Define LinuxPackageInstallation dependency
LinuxPackageInstallation is a dependency class that handles the complexity of installing different packages on different Linux distributions. The package installation scenarios given
different Linux distros are as follows:

- Packages with the same name across all distros.
- Packages with specific names depending upon the distro.
- Packages with specific names depending upon the package manager.

## Example: Packages that have the same name across distros
This is the simplest case. The package is well-known and all package managers use the same name for the package.

* [Profile Example](https://github.com/microsoft/VirtualClient/blob/main/src/VirtualClient/VirtualClient.Main/profiles/PERF-GRAPH500.json)

  <div class="code-section">

  ```json
  {
      "Type": "LinuxPackageInstallation",
      "Parameters": {
          "Scenario": "InstallRequiredLinuxPackages",
          "Packages": "make,mpich"
      }
  }
  ```
  </div>

## Example: Packages that have different names across package managers
If the packages are directly available in the default repositories, users only need to fill in the corresponding package names, in the different package manager.
You need to repeat it for other package management because the name could be different in different package manager.
The following is example for libaio, which is used in FIO tests.

* [Profile Example](https://github.com/microsoft/VirtualClient/blob/main/src/VirtualClient/VirtualClient.Main/profiles/PERF-GPU-SUPERBENCH.json)

  <div class="code-section">

  ```json
  {
      "Type": "LinuxPackageInstallation",
      "Parameters": {
          "Packages-Apt": "libaio1,libaio-dev",
          "Packages-Dnf": "libaio,libaio-devel",
          "Packages-Yum": "libaio,libaio-devel",
          "Packages-Zypper": "libaio1,libaio-dev"
      }
  }
  ```
  </div>

## Example: Packages that have different names across different Linux distros
There are packages that has different names/paths in different distributions. It follows the convention of *Packages-Distro*.
The following example is for Atop. Apt and Zypper have them in default repository, however Redhat decided to put them in EPEL repo and not as default.  
However, please note that in actual installation, epel-release and atop can't be installed in the same command. 
Epel-release needs to be installed first, so this is separated into two LinuxPackageInstallation.

* [Profile Example](https://github.com/microsoft/VirtualClient/blob/main/src/VirtualClient/VirtualClient.Main/profiles/MONITORS-DEFAULT.json)

  <div class="code-section">

  ```json
  {
      "Type": "LinuxPackageInstallation",
      "Parameters": {
          "Packages-RHEL8": "https://dl.fedoraproject.org/pub/epel/epel-release-latest-8.noarch.rpm",
          "Packages-RHEL7": "https://dl.fedoraproject.org/pub/epel/epel-release-latest-7.noarch.rpm",
          "Packages-CentOS7": "epel-release",
          "Packages-Apt": "atop",
          "Packages-Dnf": "atop",
          "Packages-Yum": "atop",
          "Packages-Zypper": "atop"
      }
  }
  ```
  </div>

## Example: Packages that require specific repositories
There are packages that are not available in the default repositories. Thus VC needs to add them to the system. Due to how package manager works, they are intrinsicly packageManager specific.
It follows the convention of *Repositories-PackageManager*. 

* [Profile Example](https://github.com/microsoft/VirtualClient/blob/main/src/VirtualClient/VirtualClient.Main/profiles/PERF-NETWORK-DEATHSTARBENCH.json)

  <div class="code-section">

  ```json
  {
      "Type": "LinuxPackageInstallation",
      "Parameters": {
          "Repositories-Apt": "ppa:ubuntu-toolchain-r/test",
          "Packages-Apt": "gcc-10",
          "Packages-Dnf": "gcc-toolset-10",
          "Packages-Yum": "gcc-toolset-10",
          "Packages-Zypper": "gcc10"
      }
  }
  ```
  </div>

## Example: Packages that require repositories specific to a given Linux distro
There are packages that are not available in the default repositories. Thus VC needs to add them to the system. What makes it worse is that those repository could be distro-version specific.
For these scenarios, LinuxPackageInstallation class supports definition of distro-specific extra repository. It follows the convention of *Repositories-Distro*.
Luckily so far VC does not use those packages in current workloads. However, those things do exist.

<div class="code-section">

```json
{
    "Type": "LinuxPackageInstallation",
    "Parameters": {
        "Repositories-RHEL8": "RHEL8.repo, IWillUpdateWhenIHaveARealExample",
        "Repositories-RHEL7": "RHEL7.repo, IWillUpdateWhenIHaveARealExample",
        "Packages-Apt": "somePackage",
        "Packages-Dnf": "somePackage",
        "Packages-Yum": "somePackage",
        "Packages-Zypper": "somePackage"
    }
}
```
</div>

## Mapping: Common Packages Used in Virtual Client Profiles

| Package       | apt           | dnf                 | yum                 | zypper |
|---------------|---------------|---------------------|---------------------|--------|
| atop          | atop          | atop(extra package) | atop(extra package) | atop  |
| automake      | automake      | automake            | automake            | automake |
| bison         | bison         | bison               | bison               | bison |
| byacc         | byacc         | byacc               | byacc               | byacc |
| cmake         | cmake         | cmake               | cmake               | cmake |
| gcc           | gcc           | gcc                 | gcc                 | gcc |
| gcc-11        | gcc-11        | gcc-toolset-11      | centos-release-scl-rh,devtoolset-11-gcc      | gcc11 |
| libaio        | libaio1       | libaio              | libaio              | libaio1 |
| libaio-dev    | libaio-dev    | libaio-devel        | libaio-devel        | libaio-dev |
| make          | make          | make                | make                | make  |
| nvidia-driver | nvidia-common | nvidia-driver       | nvidia-driver       | [N/A](https://software.opensuse.org/package/nvidia-driver) |
| mpich         | mpich         | mpich               | mpich               | mpich  |
| python3-pip   | python3-pip   | python3-pip         | python3-pip         | python3-pip |
| sshpass       | sshpass       | sshpass             | sshpass             | sshpass |
| stress-ng     | stress-ng     | stress-ng           | stress-ng           | stress-ng     |
