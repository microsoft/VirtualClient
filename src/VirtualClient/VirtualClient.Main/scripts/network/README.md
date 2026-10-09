# Network Configuration Scripts
The scripts in this directory are used to automate configuring network settings on systems as part of Virtual Client executions.
They are generally packaged for upload to a storage account and downloadable by Virtual Client during execution. The goal is to enable
fast-adaptation on different systems to account for differences between one version of an operating system and another throughout time.
For example, the supported configuration requirements for Linux distributions tends to change frequently. By packaging the scripts in a
package for download by Virtual Client, the development team can more quickly adapt to new requirements.

## Methodology
The package structure contains a set of scripts that represent the 'default' configurations but allows for platform-specific script
implementations to be provided. Platform-specific implementations of the various scripts can be placed in a folder whose name matches
the combination of the `ID` and `VERSION_ID` property values in the `/etc/os-release` file on the system being configured. The name should
follow the format `{ID}-{VERSION-ID}` and be all lower-cased. The following examples illustrate the concept.

[Example os-release file definitions](https://github.com/microsoft/VirtualClient/tree/main/src/VirtualClient/TestResources/unix/os-release)

* **RedHat Examples**
  ``` bash
  # Given the following `/etc/os-release` file contents:
  NAME="Red Hat Enterprise Linux"
  VERSION="8.10 (Ootpa)"
  ID="rhel"
  ID_LIKE="fedora"
  VERSION_ID="8.10"
  PLATFORM_ID="platform:el8"
  PRETTY_NAME="Red Hat Enterprise Linux 8.10 (Ootpa)"
  ANSI_COLOR="0;31"
  CPE_NAME="cpe:/o:redhat:enterprise_linux:8::baseos"
  HOME_URL="https://www.redhat.com/"
  DOCUMENTATION_URL="https://access.redhat.com/documentation/en-us/red_hat_enterprise_linux/8"
  BUG_REPORT_URL="https://issues.redhat.com/"
 
  REDHAT_BUGZILLA_PRODUCT="Red Hat Enterprise Linux 8"
  REDHAT_BUGZILLA_PRODUCT_VERSION=8.10
  REDHAT_SUPPORT_PRODUCT="Red Hat Enterprise Linux"
  REDHAT_SUPPORT_PRODUCT_VERSION="8.10"

  # The platform-specific folder name for this system would be:
  rhel-8.10

  # Example of a platform-specific folder structure:
  /network
      /configure_network.sh
      /rhel-8.10
          /config_limits.sh
          /config_nftables.sh
          /config_sysctl.sh
  ```
  <br/>

  ``` bash
  # Given the following `/etc/os-release` file contents:
  NAME="Red Hat Enterprise Linux"
  VERSION="9.6 (Plow)"
  ID="rhel"
  ID_LIKE="fedora"
  VERSION_ID="9.6"
  PLATFORM_ID="platform:el9"
  PRETTY_NAME="Red Hat Enterprise Linux 9.6 (Plow)"
  ANSI_COLOR="0;31"
  LOGO="fedora-logo-icon"
  CPE_NAME="cpe:/o:redhat:enterprise_linux:9::baseos"
  HOME_URL="https://www.redhat.com/"
  DOCUMENTATION_URL="https://access.redhat.com/documentation/en-us/red_hat_enterprise_linux/9"
  BUG_REPORT_URL="https://issues.redhat.com/"
 
  REDHAT_BUGZILLA_PRODUCT="Red Hat Enterprise Linux 9"
  REDHAT_BUGZILLA_PRODUCT_VERSION=9.6
  REDHAT_SUPPORT_PRODUCT="Red Hat Enterprise Linux"
  REDHAT_SUPPORT_PRODUCT_VERSION="9.6"
 
  # The platform-specific folder name for this system would be:
  rhel-9.6

  # Example of a platform-specific folder structure:
  /network
      /configure_network.sh
      /rhel-9.6
          /config_limits.sh
          /config_nftables.sh
          /config_sysctl.sh
  ```

* **Ubuntu Examples**
    ``` bash
    # Given the following `/etc/os-release` file contents:
    NAME="Ubuntu"
    VERSION="18.04.6 LTS (Bionic Beaver)"
    ID=ubuntu
    ID_LIKE=debian
    PRETTY_NAME="Ubuntu 18.04.6 LTS"
    VERSION_ID="18.04"
    HOME_URL="https://www.ubuntu.com/"
    SUPPORT_URL="https://help.ubuntu.com/"
    BUG_REPORT_URL="https://bugs.launchpad.net/ubuntu/"
    PRIVACY_POLICY_URL="https://www.ubuntu.com/legal/terms-and-policies/privacy-policy"
    VERSION_CODENAME=bionic
    UBUNTU_CODENAME=bionic
    
    # The platform-specific folder name for this system would be:
    ubuntu-18.04
    
    # Example of a platform-specific folder structure:
    /network
        /configure_network.sh
        /ubuntu-18.04
            /config_limits.sh
            /config_iptables.sh
            /config_sysctl.sh
    ```
    <br/>

    ``` bash
    # Given the following `/etc/os-release` file contents:
    PRETTY_NAME="Ubuntu 24.04.3 LTS"
    NAME="Ubuntu"
    VERSION_ID="24.04"
    VERSION="24.04.3 LTS (Noble Numbat)"
    VERSION_CODENAME=noble
    ID=ubuntu
    ID_LIKE=debian
    HOME_URL="https://www.ubuntu.com/"
    SUPPORT_URL="https://help.ubuntu.com/"
    BUG_REPORT_URL="https://bugs.launchpad.net/ubuntu/"
    PRIVACY_POLICY_URL="https://www.ubuntu.com/legal/terms-and-policies/privacy-policy"
    UBUNTU_CODENAME=noble
    LOGO=ubuntu-logo
    
    # The platform-specific folder name for this system would be:
    ubuntu-24.04
    
    # Example of a platform-specific folder structure:
    /network
        /configure_network.sh
        /ubuntu-24.04
            /config_limits.sh
            /config_iptables.sh
            /config_sysctl.sh
    ```
    <br/>

    ``` bash
    # Given the following `/etc/os-release` file contents:
    PRETTY_NAME="Ubuntu 26.04 LTS"
    NAME="Ubuntu"
    VERSION_ID="26.04"
    VERSION="26.04 (Resolute Raccoon)"
    VERSION_CODENAME=resolute
    ID=ubuntu
    ID_LIKE=debian
    HOME_URL="https://www.ubuntu.com/"
    SUPPORT_URL="https://help.ubuntu.com/"
    BUG_REPORT_URL="https://bugs.launchpad.net/ubuntu/"
    PRIVACY_POLICY_URL="https://www.ubuntu.com/legal/terms-and-policies/privacy-policy"
    UBUNTU_CODENAME=resolute
    LOGO=ubuntu-logo
    
    # The platform-specific folder name for this system would be:
    ubuntu-26.04
    
    # Example of a platform-specific folder structure:
    /network
        /config_network.sh
        /ubuntu-26.04
            /config_limits.sh
            /config_nftables.sh
            /config_sysctl.sh
    ```