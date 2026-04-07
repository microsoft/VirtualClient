# Controller/Agent Overview
The following sections provide an overview on how to use the Virtual Client application as an agent for orchestration
of workflows from a single test controller/jumpbox system. This is especially useful for hardware testing and qualification processes whereby the
user would like to execute "test case" automation from a single controller system targeting N-number of remote "systems under test".

This document focuses on script execution but does not cover the script development process. Reference the following documentation for guidelines on the 
implementation of `script-based extensions` packaged as `self-contained packages` for execution through Virtual Client as a runtime platform.

[Script Development Guidelines](../developing/0021-develop-script-extensions.md)

## Considerations
Some hardware testing scenarios have systems that do not have direct internet connectivity. These offline scenarios are limiting due to the lack of internet 
connectivity impacting the ability to leverage common automation practices such as installation of software, packages and toolsets. Virtual Client provides 
a limited amount support for these scenarios where dependencies (e.g. toolsets, drivers) can be installed in a local area network from the controller. However, 
it is recommended that the user consider ```Internet Connection Sharing``` options where possible where direct internet connectivity is not an option. This enables 
each system-under-test on the local area network to use the network adapter on the test controller for internet access. The same features are also available for Linux 
test controller systems.

* https://pureinfotech.com/share-internet-connection-windows-10/
* https://www.xmodulo.com/internet-connection-sharing-iptables-linux.html

## Prerequisites
the following are required in order to use the Virtual Client as a test controller.

* The test controller system MUST have internet access in order to support download of dependencies.
* Target systems MUST be network-accessible from the test controller system (e.g. same LAN). It is also recommeded that the target systems have some amount of direct internet connectivity in order to make system setup/configuration easier. See the ```Considerations``` section above.
* SSH support MUST be installed/enabled on each target system. The support is native to Linux but requires a feature to be installed on Windows.

  [Get started with OpenSSH for Windows](https://learn.microsoft.com/en-us/windows-server/administration/openssh/openssh_install_firstuse?tabs=gui&pivots=windows-server-2025)

* Scripting language runtimes are installed (e.g. PowerShell, Python). Note that the VC controller provides a feature to install either PowerShell or Python on
  a target system if desirable (see below). That said, the language runtimes can be installed in any way preferable.

* Linux User/Execution Account Requirements:  
  A large portion of test automation requires elevated privileges to access parts of the Linux system. This often requires
  user accounts for automation having the ability to run elevated. There are a few ways that this is typically done:

  * **Run as "root"**  
    Whereas this is not generally recommended, the user or execution CAN login as the **root** user. One of the most common use cases involves installing
    services/daemons configured to run as the **root** user. When running as the **root** user account, commands executed will have root privileges and do 
    not require execution using "sudo".
  
    **Run as User Account with Root Privileges**  
    A more typical scenario is to run under a specific account (e.g. user_admin) that has been configured to have root privileges. The following section describes
    how to configure an account to have root privileges.

    <mark>Note that commands requiring elevated permissions (e.g. dmidecode -t bios) must be executed using "sudo" so that they
    are running with superuser privileges.</mark>

    ``` bash
    # Option 1: Configure a Single User Account
    # --------------------------------------------
    # With this option, we configure a single user account. Note that you MUST be executing 
    # the following commands when logged in as the "root" account or with an account that has "root" 
    # privileges.
  
    # Given there is a user account named "user_admin"...
    #
    # 1) Provide the account with membership to the "sudo" group
    sudo usermod -aG sudo user_admin

    # 2a) Add a file to the /etc/sudoers.d directory with the following content. This enables
    #    the execution of sudo commands without requiring a password. This is very important
    #    for unattended automation scenarios where there will be no one there to provide it.
    #
    # Run as "root"
    su -

    # Create the permissions file. You can name it whatever you like but it is best to 
    # ensure it is specific to the 1 user account (to allow for other accounts in the future).
    echo "user_admin  ALL=(ALL) NOPASSWD: ALL" > /etc/sudoers.d/user_admin_permissions

    # Refresh group membership
    newgrp sudo

    # If the change does not take affect, check the user permissions to ensure there aren't any other
    # rules (in the sudoers.d) folder overriding the change. The rules are displayed in the order at which
    # they are applied. The very last rule should match the one entered above if everything is defined
    # correctly (e.g. user_admin  ALL=(ALL) NOPASSWD: ALL). If not, find the sudoers.d file with the
    # offending rule and update it to match.
    sudo -l -U user_admin
    ```

## Targeting Systems-Under-Test
In the sections below, you will note the use of the ```--agent-ssh``` option in the command line examples. This option allows the 
user to define the SSH session/connection information including target name or IP, the login user account and the login password.

``` bash
# Format:
{user}@{name_or_ip_address};{password}

# IPv4 Address Example:
anyuser@192.168.1.15;abc123456

# IPv6 Address Example:
anyuser@2001:0db8:85a3:0000:0000:8a2e:0370:7334;abc123456

# Host/DNS Name Example:
anyuser@rack1_u07;abc123456
```

## Step 1: Download/Copy the Agent to the Test Controller System
The Virtual Client can be downloaded from the following storage account locations internally.

[Download Location](https://ms.portal.azure.com/#view/Microsoft_Azure_Storage/ContainerMenuBlade/~/overview/storageAccountId/%2Fsubscriptions%2F94f4f5c5-3526-4f0d-83e5-2e7946a41b75%2FresourceGroups%2Fvirtualclient%2Fproviders%2FMicrosoft.Storage%2FstorageAccounts%2Fvirtualclient/path/packages-share/etag/%220x8DD3B05B90489EA%22/defaultId//publicAccessVal/Blob)

Note that Controller/Agent packages have builds of the agent for every platform-architecture necessary to support cloud hardware systems. These different
platform-architectures are required because the operating system and CPU architecture on the controller (jumpbox) may not match with the same on
the target systems (systems-under-test).

An agent package will have the following structure:

``` bash
# A folder where all logs/log files will be saved during 
# controller operations
{agent_package}/logs

# A folder where all packages should be placed.
{agent_package}/packages

# Build of the agent for running on Linux/ARM64 systems
# (e.g. Ubuntu 22.04 + Cobalt 200, GB200).
{agent_package}/linux-arm64

# Build of the agent for running on Linux/X64 systems
# (e.g. Ubuntu 22.04 + Intel, AMD).
{agent_package}/linux-x64

# Build of the agent for running on Windows/ARM64 systems
# (e.g. Windows 11 + Cobalt 200, GB200).
{agent_package}/win-arm64

# Build of the agent for running on Windows/X64 systems
# (e.g. Windows 11 + Intel, AMD).
{agent_package}/win-x64
```

## Step 2: Install the Agent on Target System
In order to enable the execution of scripts and content on a SUT, the agent package must be copied to and installed on the SUT.

``` bash
# Windows Examples
# ---------------------------
# Install the agent package on the target system. 
C:\Users\AnyUser\VirtualClient\win-x64> VirtualClient.exe --profile=agent\INSTALL-AGENT.json --agent-ssh=anyuser@192.168.1.15;abc123456

# ...On multiple target systems
C:\Users\AnyUser\VirtualClient\win-x64> VirtualClient.exe --profile=agent\INSTALL-AGENT.json --agent-ssh=anyuser@192.168.1.15;abc123456 --agent-ssh=anyuser@192.168.1.16;abc123456

# Linux Examples
# ---------------------------
~VirtualClient/linux-arm64$ chmod +x ./VirtualClient
~VirtualClient/linux-arm64$ ./VirtualClient --profile=agent/INSTALL-AGENT.json --agent-ssh=anyuser@192.168.1.15;abc123456

~VirtualClient/linux-arm64$ chmod +x ./VirtualClient
~VirtualClient/linux-arm64$ ./VirtualClient --profile=agent/INSTALL-AGENT.json --agent-ssh=anyuser@192.168.1.15;abc123456 --agent-ssh=anyuser@192.168.1.16;abc123456
```

## Step 3: Add Custom Packages/Scripts to the Agent
If you have custom packages containing scripts and toolsets/binaries to run on either the test controller system or target systems-under-test, these packages
should be copied/placed inside the agent package itself within the ```packages``` folder for the platform-architecture to match the test controller system.

``` bash
# The packages directory is a peer directory of the agent
# directories. Place your packages here.
{agent_package}/packages

# Agent builds for each platform-architecture.
{agent_package}/linux-arm64
{agent_package}/linux-x64
{agent_package}/win-arm64
{agent_package}/win-x64
```

## Step 4: Install Custom Packages/Scripts on Target Systems
If at any point, the user wants to add additional custom scripts for execution on the test controller, the SUT or both, simply add the script
packages to the appropriate agent ```packages``` folder in Step #2 above. Then install the packages on the target system:

``` bash
# Windows Examples
# ---------------------------
# Install the agent package on the target system. 
C:\Users\AnyUser\VirtualClient\win-x64> VirtualClient.exe --profile=agent\INSTALL-PACKAGES.json --agent-ssh=anyuser@192.168.1.15;abc123456

# ...On multiple target systems
C:\Users\AnyUser\VirtualClient\win-x64> VirtualClient.exe --profile=agent\INSTALL-PACKAGES.json --agent-ssh=anyuser@192.168.1.15;abc123456 --agent-ssh=anyuser@192.168.1.16;abc123456
  
# Linux Examples
# ---------------------------
~VirtualClient/linux-arm64$ chmod +x ./VirtualClient
~VirtualClient/linux-arm64$ ./VirtualClient --profile=agent/INSTALL-PACKAGES.json --agent-ssh=anyuser@192.168.1.15;abc123456

~VirtualClient/linux-arm64$ chmod +x ./VirtualClient
~VirtualClient/linux-arm64$ ./VirtualClient --profile=agent/INSTALL-PACKAGES.json --agent-ssh=anyuser@192.168.1.15;abc123456 --agent-ssh=anyuser@192.168.1.16;abc123456
```

## Step 5a: Execute Custom Scripts
Once your custom scripts/script packages have been installed on the target SUT, you can execute them through the agent
by supplying the ```remote``` subcommand on the command line along with the SSH target information. When the "remote-execute"
subcommand is provided, the agent command line defined is executed on the target/remote system (vs. on the controller).

``` bash
# Windows Examples
# ---------------------------
# Execute scripts on the target systems. Any log output should be written to a subfolder
# in the target agent's "logs" folder so that the log files can be copied back to the test
# controller system.
C:\Users\AnyUser\VirtualClient\win-x64> VirtualClient.exe remote "../packages/custom-scripts.1.0.0/execute_openssl.py --log-dir ../logs/openssl_test" --agent-ssh=anyuser@192.168.1.15;abc123456 --agent-ssh=anyuser@192.168.1.16;abc123456

# Linux Examples
# ---------------------------
~VirtualClient/linux-arm64$ chmod +x ./VirtualClient
~VirtualClient/linux-arm64$ ./VirtualClient remote "../packages/custom-scripts.1.0.0/execute_openssl.py --log-dir ../logs/openssl_test" --agent-ssh=anyuser@192.168.1.15;abc123456 --agent-ssh=anyuser@192.168.1.16;abc123456
```

## Step 5b: Execute Out-of-Box Profiles
Similarly to executing custom scripts, the ```remote``` subcommand can be used to execute out-of-box workload profiles on the target/remote system that support offline scenarios. 
Note that profiles supporting offline scenarios (e.g. target systems having no internet access) will often have the prefix "OFFLINE" in the name (e.g. OFFLINE-PERF-IO-FIO.json).


``` bash
# Execute out-of-box profiles supported for offline scenarios.
C:\Users\AnyUser\VirtualClient\win-x64> VirtualClient.exe remote --profile=PERF-IO-FIO.json --agent-ssh=anyuser@192.168.1.15;abc123456 --agent-ssh=anyuser@192.168.1.16;abc123456
```

## Step 6: Inspect Logs
The agent will copy logs from the system-under-test after each execution of a command on the target system. Logs are written to the
```logs``` folder at the root of the agent package/folder.

``` bash
# The logs directory is a peer directory of the agent directories. 
# Logs will be copied from each target system to this directory in the
# format shown below.
{agent_package}/logs/{host_name_or_ip}/{experiment_id}

# e.g.
{agent_package}/logs/192.168.1.15/78Cd98de-f5b9-4753-943a-2d47171e1428/metrics.csv
{agent_package}/logs/192.168.1.15/78Cd98de-f5b9-4753-943a-2d47171e1428/openssl/2025-05-23t10-00-00-sha1.log
{agent_package}/logs/192.168.1.15/78Cd98de-f5b9-4753-943a-2d47171e1428/openssl/2025-05-23t11-00-00-sha256.log
{agent_package}/logs/192.168.1.15/78Cd98de-f5b9-4753-943a-2d47171e1428/openssl/2025-05-23t12-00-00-sha512.log

{agent_package}/logs/192.168.1.16/042a6687-7f58-442e-8e9b-7c724d26867c/metrics.csv
{agent_package}/logs/192.168.1.16/042a6687-7f58-442e-8e9b-7c724d26867c/openssl/2025-05-23t10-01-00-sha1.log
{agent_package}/logs/192.168.1.16/042a6687-7f58-442e-8e9b-7c724d26867c/openssl/2025-05-23t11-02-00-sha256.log
{agent_package}/logs/192.168.1.16/042a6687-7f58-442e-8e9b-7c724d26867c/openssl/2025-05-23t12-03-00-sha512.log
```

## Orchestrating Agent Workflows
The Virtual Client must support workflows that include executing steps on the controller system as well as a set of target systems. Users may
prefer to orchestrate execute workflows 1 command/command line at a time. Users may additionally prefer to use out-of-box agent profiles that wrap up 
more advanced workflows between controllers and target systems in a single profile.

**Example: Orchestrating Workflows 1 Command at a Time**  
The following example shows how to orchestrate a workflow one command line execution at a time. Pay particular attention to ensure the
command provided bears in mind the platform-architecture of the system on which in will run. For example, the controller might be a Windows
system while the target system is a Linux system. In this scenario, a command to run on the target system should use forward-slashes (vs. back-slashes)
in paths.

``` bash
# Example: 
# Assume the controller system is a win-x64 system. 
# Assume the target system is a linux-arm64 system.
# ---------------------------------------------------
# Capture information from the rack manager (RSCM) interface.
C:\Users\AnyUser\VirtualClient\win-x64> VirtualClient.exe "python ../packages/custom-scripts.1.0.0/log_rscm_info.py --rm=root@192.168.1.10:pw"

# Capture pre-test information from a target system-under-test. This runs on the target agent system.
C:\Users\AnyUser\VirtualClient\win-x64> VirtualClient.exe remote "python ../packages/custom-scripts.1.0.0/log_pretest_info.py" --agent-ssh=anyuser@192.168.1.15;abc123456

# AC cycle the target system-under-test through the rack manager (RSCM) interface.
C:\Users\AnyUser\VirtualClient\win-x64> VirtualClient.exe "python ../packages/custom-scripts.1.0.0/ac_cycle_system.py --rm=root@192.168.1.10:pw --slot=2"

# AC cycle the target system-under-test through the rack manager (RSCM) interface.
C:\Users\AnyUser\VirtualClient\win-x64> VirtualClient.exe "python ../packages/custom-scripts.1.0.0/run_quickstress.py --rm=root@192.168.1.10:pw --slot=2"

# Capture post-test information from a target system-under-test. This runs on the target agent system.
C:\Users\AnyUser\VirtualClient\win-x64> VirtualClient.exe remote "python ../packages/custom-scripts.1.0.0/log_posttest_info.py" --agent-ssh=anyuser@192.168.1.15;abc123456

# Example: 
# Assume the controller system is a win-x64 system. 
# Assume the target system is a win-arm64 system.
# ---------------------------------------------------
# Capture information from the rack manager (RSCM) interface.
C:\Users\AnyUser\VirtualClient\win-x64> VirtualClient.exe "python ..\packages\custom-scripts.1.0.0\log_rscm_info.py --rm=root@192.168.1.10:pw"

# Capture pre-test information from a target system-under-test. This runs on the target agent system.
C:\Users\AnyUser\VirtualClient\win-x64> VirtualClient.exe remote "python ..\packages\custom-scripts.1.0.0\log_pretest_info.py" --agent-ssh=anyuser@192.168.1.15;abc123456

# AC cycle the target system-under-test through the rack manager (RSCM) interface.
C:\Users\AnyUser\VirtualClient\win-x64> VirtualClient.exe "python ..\packages\custom-scripts.1.0.0\ac_cycle_system.py --rm=root@192.168.1.10:pw --slot=2"

# AC cycle the target system-under-test through the rack manager (RSCM) interface.
C:\Users\AnyUser\VirtualClient\win-x64> VirtualClient.exe "python ..\packages\custom-scripts.1.0.0\run_quickstress.py --rm=root@192.168.1.10:pw --slot=2"

# Capture post-test information from a target system-under-test. This runs on the target agent system.
C:\Users\AnyUser\VirtualClient\win-x64> VirtualClient.exe remote "python ..\packages\custom-scripts.1.0.0\log_posttest_info.py" --agent-ssh=anyuser@192.168.1.15;abc123456
```

**Example: Orchestrating Workflows Using Out-of-Box Profiles**  

``` bash
VirtualClient.exe --profile=QUAL-AC-CYCLE-WORKFLOW.json --agent-ssh=anyuser@192.168.1.15;abc123456 --parameters="SshRscm=%RACK_SCM_SSH%"
```

``` json
{
    "Description": "Provides example of a controller/agent workflow with steps that execute on both controller and target systems.",
    "Metadata": {
        "SupportedPlatforms": "linux-x64,linux-arm64,win-x64,win-arm64"
    },
    "Parameters": {
        "SshRscm": null
    },
    "Actions": [
        {
            "Type": "RemoteAgentExecutor",
            "Parameters": {
                "Scenario": "ExecutePreTestOnTargetSystem",
                "Command": "--profile=QUAL-PRETEST.json --iterations=1",
                "Notes": "Executes the VC profile/command on the target system(s) as defined by the --agent-ssh options on the command line."
            }
        },
        {
            "Type": "ExecuteCommand",
            "Parameters": {
                "Scenario": "CycleTargetSystem",
                "Command": "python ..\\packages\\custom-scripts.1.0.0\\ac_cycle_system.py --rm={SshRscm} --slot=2",
                "SshRscm": "$.Parameters.SshRscm",
                "Notes": "Executes the command on the current/controller system."
            }
        },
        {
            "Type": "RemoteAgentExecutor",
            "Parameters": {
                "Scenario": "ExecuteQuickStressOnTargetSystem",
                "Command": "--profile=QUAL-QUICKSTRESS.json --iterations=1",
                "Notes": "Executes the command on the target system(s) as defined by the --agent-ssh options on the command line."
            }
        },
        {
            "Type": "RemoteAgentExecutor",
            "Parameters": {
                "Scenario": "ExecutePostTestOnTargetSystem",
                "Command": "--profile=QUAL-POSTTEST.json --iterations=1",
                "Notes": "Executes the VC profile/command on the target system(s) as defined by the --agent-ssh options on the command line."
            }
        }
    ]
}
```