# Install MySQL
Virtual Client has a dependency component that can be added to a workload or monitor profile to install MySQL on the system. The following section illustrates the
details for integrating this into the profile.

## Supported Platform/Architectures
* linux-x64
* linux-arm64
* win-x64
* win-arm64

## Profile Component Parameters
Virtual Client uses either Chocolatey (on Windows systems) or a Linux package manager (on Linux systems) to install MySQL. Refer to the following documentation
describing these components.

* [Install Chocolatey](./0020-install-chocolatey.md)
* [Install Chocolatey Packages](./0030-install-chocolatey-packages.md)
* [Install Linux Packages](./0010-install-linux-packages.md)

## Example
The following sections provides examples for how to integrate the component into a profile on Linux systems.

* [Profile Example](https://github.com/microsoft/VirtualClient/blob/main/src/VirtualClient/VirtualClient.Main/profiles/PERF-MYSQL-SYSBENCH-OLTP.json)

  <div class="code-section">

  ```
  # Linux system integration
  {
      "Type": "LinuxPackageInstallation",
      "Parameters": {
          "Scenario": "InstallRequiredLinuxPackages",
          "Packages": "make, automake, libtool, pkg-config",
          "Packages-Apt": "libaio-dev, libmysqlclient-dev, libssl-dev",
          "Packages-Dnf": "libaio-devel, mariadb-devel, openssl-devel",
          "Packages-Yum": "libaio-devel, mariadb-devel, openssl-devel",
          "Packages-Zypper": "libaio-dev, libmysqlclient-devel, openssl-devel",
          "Role": "Client"
      }
  },
  {
      "Type": "LinuxPackageInstallation",
      "Parameters": {
          "Scenario": "InstallRequiredLinuxPackages",
          "Packages": "mysql-server",
          "Role": "Server"
      }
  }
  ```
  </div>
