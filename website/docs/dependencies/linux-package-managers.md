# Linux Distributions And Package Management
VirtualClient has the goal of supporting all platforms and specifics with relative ease. It should work, out of the box. 
Installing a package is easy. Installing packages on different platforms and package managers is not. 
VC encapsulates the complex logic of handling package installation on different distros into a single component that users can easily use in profiles.

[Package Manager Apt](https://wiki.debian.org/AptCLI)  
[Package Manager Dnf](https://github.com/rpm-software-management/dnf)  
[Package Manager Yum](http://yum.baseurl.org/)  
[Package Manager Zypper](https://en.opensuse.org/Portal:Zypper)  
[A good Third-party intro that I found](https://www.linode.com/docs/guides/linux-package-management-overview/)  
[Website to search for packages](https://pkgs.org/)  

## .NET Supported Linux Distributions
https://github.com/dotnet/core/blob/main/release-notes/6.0/supported-os.md

VirtualClient runs on .NET 6. It should run on any distribution supported by .NET 6.

### Supported package management and corresponding distro

| Package Manager  | Distributions                  |
|------------------|--------------------------------|
| apt              | Debian, Ubuntu                 |
| dnf   	       | CentOS/RHEL8, Mariner, Fedora  |
| yum              | CentOS/RHEL7                   |
| zypper           | OpenSUSE                       | 
| None             | Flatcar                        |

* Note: If your distro is supported, it doesn't mean all workload and profiles are properly defined and tested in your distro. 
The "SupportedDistribuitons" parameter will show the distribution that are tested by VC team and users. Please feel free to test in your environment and contribute to the profiles.
It is also possible that some workloads or dependencies just doesn't work on specific distros, even though VC and other workloads support them.

### Define LinuxPackageInstallation dependency
LinuxPackageInstallation is a dependency class that handles the complexity of installing different packages on different Linux distributions.  
TLDR: The packages that will install on your system are "Packages" + "Packages Specific to your package manager" + "Packages Specific to your distro".  
For example: if you are on Ubuntu. Packages = "Packages"+"Packages-Apt"+"Packages-Ubuntu".

### Support for packages that have the same name across distros
This is the simplest case. The package is well-known and all package manager have the same name for the package, in default repo. 
Life is simple.

```json
{
    "Type": "LinuxPackageInstallation",
    "Parameters": {
        "Packages": "make,openssl"
    }
}
```

### Support for packages that have different names across package managers
If the packages are directly available in the default repositories, users only need to fill in the corresponding package names, in the different package manager.
You need to repeat it for other package management because the name could be different in different package manager.
The following is example for libaio, which is used in FIO tests.

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

### Support for packages that have different names across different Linux distros
There are packages that has different names/paths in different distributions. It follows the convention of *Packages-Distro*.
The following example is for Atop. Apt and Zypper have them in default repository, however Redhat decided to put them in EPEL repo and not as default.  
However, please note that in actual installation, epel-release and atop can't be installed in the same command. 
Epel-release needs to be installed first, so this is separated into two LinuxPackageInstallation.

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

### Support for packages that require specific repositories
There are packages that are not available in the default repositories. Thus VC needs to add them to the system. Due to how package manager works, they are intrinsicly packageManager specific.
It follows the convention of *Repositories-PackageManager*. 

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

### Support for packages that require repositories specific to a given Linux distro
There are packages that are not available in the default repositories. Thus VC needs to add them to the system. What makes it worse is that those repository could be distro-version specific.
For these scenarios, LinuxPackageInstallation class supports definition of distro-specific extra repository. It follows the convention of *Repositories-Distro*.
Luckily so far VC does not use those packages in current workloads. However, those things do exist.

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

### Mapping: Common Packages used in VC

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
