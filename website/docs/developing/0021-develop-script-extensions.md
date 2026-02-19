# Developing Script Extensions
The use of scripting languages (e.g. Python, PowerShell) is a popular choice for software and system engineers responsible for automating test coverage
on bare metal hardware systems. The Virtual Client platform provides runtime support for running scripts directly from the command line and additionally 
supports a controller/agent workflow for remote execution through SSH sessions on both Linux and Windows systems.

[Controller/Agent Overview](../guides/0021-controller-agent.md)

The following document provides a set of general guidelines to consider when developing script-based automation extensions so that they can be readily integrated
into the Virtual Client platform. 

## Script Extensions Usage
Before going into the details for script extensions development, it is helpful to have an idea of how they can be used in the
Virtual Client platform. The following sections illustrate how to integrate script packages into the Virtual Client platform for execution via the command line. Whereas the Virtual Client
platform often defines complex workflows using profiles, scripts can be executed directly without the need for a profile. Before getting into design and implementation
recommendations for script-based automation, it is helpful to get a sense of how they will be used in Virtual Client.

### Integrating Script Extensions
A package/folder containing scripts can be placed anywhere on the system, but the recommendation is to place them in the ```/packages``` folder for the Virtual Client
application. This is the folder that the application uses to download and host packages for all other toolsets.

``` bash
# e.g.
# Example On Windows Systems
C:\Users\AnyUser\VirtualClient\content\win-arm64\packages\custom_scripts.1.0.0
C:\Users\AnyUser\VirtualClient\content\win-arm64\packages\custom_scripts.1.0.0\binaries
C:\Users\AnyUser\VirtualClient\content\win-arm64\packages\custom_scripts.1.0.0\setup
C:\Users\AnyUser\VirtualClient\content\win-arm64\packages\custom_scripts.1.0.0\Invoke-FirmwareUpdate.ps1

# e.g.
# Example On Linux Systems
/home/anyuser/VirtualClient/content/linux-x64/packages/custom_scripts.1.0.0
/home/anyuser/VirtualClient/content/linux-x64/packages/custom_scripts.1.0.0/binaries
/home/anyuser/VirtualClient/content/linux-x64/packages/custom_scripts.1.0.0/setup
/home/anyuser/VirtualClient/content/linux-x64/packages/custom_scripts.1.0.0/update_firmware.py
```

### Executing Scripts in an Extensions Package
Scripts within a script extensions package/folder can be referenced and executed directly on the command line in Virtual Client. To reference
a script on the command line, the full script command should be surrounded in quotation marks. 

``` bash
# e.g.
# Windows Examples
# Scripts can be referenced directly on the command line.
VirtualClient.exe "pwsh C:\Users\AnyUser\VirtualClient\content\win-arm64\packages\custom_scripts.1.0.0\Invoke-FirmwareUpdate.ps1 -LogDirectory C:\Users\AnyUser\VirtualClient\content\win-arm64\logs\firmware_updates"

# Relative paths can be used as well and are relative to the Virtual Client application.
VirtualClient.exe "pwsh .\packages\custom_scripts.1.0.0\Invoke-FirmwareUpdate.ps1 -LogDirectory .\logs\firmware_updates"

# e.g.
# Linux Examples
VirtualClient "python C:\Users\AnyUser\VirtualClient\content\win-arm64\packages\custom_scripts.1.0.0\update_firmware.py --log-directory=/home/anyuser/VirtualClient/content/linux-arm64/logs/firmware_updates"

# Relative paths...
VirtualClient "python ./packages/custom_scripts.1.0.0/update_firmware.py --log-directory=./logs/firmware_updates"
```

Your scripts do not really even need to write log files. Virtual Client allows the user to request the output of any script or toolset ran from the command line
to be written to file by simply including the ```--log-to-file``` flag on the command line.

``` bash
# Windows Examples
VirtualClient.exe "pwsh .\packages\custom_scripts.1.0.0\Invoke-FirmwareUpdate.ps1 -LogDirectory .\logs\firmware_updates" --log-to-file

# Linux Examples
VirtualClient "python ./packages/custom_scripts.1.0.0/update_firmware.py --log-directory=./logs/firmware_updates" --log-to-file
```

## Script Extensions Design Principles
The following section defines a set of recommendations to apply when designing script-based automation.

<div>
<mark>
A very fundamental concept to consider when designing scripted automation is to focus on creating <b>self-contained packages</b>. Self-contained
packages can be simply copied/downloaded to a target system and executed successfully with no additional steps. This makes debugging/testing and integration
with automation systems much more seamless. Manual steps are problematic for repeatable and efficient scale executions.
</mark>
</div>
<br/>

* **Scripts Can be Executed Independently**  
  Scripts should be executable largely independent of other automation/orchestration systems. For example, a user should be 
  able to copy/download the test content to a system and run it with very minimal (if any) additional requirements. It is acceptable for the scripted
  automation to depend upon other libraries/modules or toolsets (a very common scenario in software/automation); however, these should be either included with the
  script package itself or easily installable. Similarly, if installation is required, it is advisable to include additional scripted automation to handle this
  requirement (e.g. scripts that install Linux packages).

* **Scripts Can be Easily Integrated Into Scale Automation**  
  Scripts should be easy to integrate with scale automation/orchestration systems. In general, scripted automation SHOULD NOT
  be dependent upon automation/orchestration systems to work correctly as this would be in conflict with the tenet noted above. Much of the process of designing
  easy-to-integrate script-based automation involves following command line 101 practices. Scripts designed as command line toolsets are typically easy to
  execute either manually or in scale automation systems (unattended). They additionally allow required information to be easily provided to the application via 
  the command line or as environment variables. The latter two aspects are very common and easy to implement.

* **Scripts Should Produce Easy-to-Access Results**  
  Whether the script-based automation is executed by a user manually on a system or across 100s of systems by scale automation, the results should be easy
  to access. Useful information should be provided in the standard output and standard error of the terminal/console. Another common option is to emit the information 
  to a file on the system (i.e. log file). This is not as easy to integrate as standard output/error but is acceptable.

## Script Extensions Implementation Guidelines
The following section applies the design principles noted above to some of the implementation details. These recommendations below aim at
facilitating the creation of lightweight script automation that is relatively easy to write.

* **Scripts Should be Written for Command Line Toolset/Terminal Integration**  
  There are few (if any) options more simple to write that are also easy to use than those designed for command line integration (e.g. PowerShell, Python).
  Scripts should be written for command line usage wherever possible.

* **Scripts Should Follow Command Line Fundamentals/Principles**  
  To ensure script-based automation is both easy to use by individual users and is also easy to integrate into scale automation, developers should follow command line
  101 principles. The following are the key principles involved:

  * **Return a Relevant Exit Code**  
    Scripts on the command line should return an exit code of 0 to represent a successful execution/outcome. A non-zero exit code should be returned
    to represent a non-successful execution/outcome. Good script-based automation tools use different non-zero exit codes to represent exact reasons for the operation
    failing.

  * **Provide Useful Information in Standard Output**  
    Scripts should provide information useful to a user (or scale automation) in standard output (e.g. console print screen). This makes it easy
    for a user of the script automation to understand exactly what happened and ideally to be able to make a decision on the outcome by simply reading
    the output. Furthermore, the use of standard output is one of the easiest output interface for integration into scale automation (e.g. redirected to a log file).

  * **Provide Error Information in Standard Error**  
    Scripts should provide information on errors that occur in standard error. The error information should be ideally written
    in layman's terms so that it is easy for the user to understand the context of the problem (i.e. "an error occurred" is not generally a good error statement).
    Furthermore, the use of standard error is one of the easiest output interface for integration into scale automation (e.g. redirected to a log file).

  * **Prefer Options/Parameters on the Command Line**  
    Most script terminals and command line tools support or require options/parameters be supplied on the command line. This is the easiest way to pass information into a
    command line tool and is thus preferred over other options. Environment variables are also a reasonable choice as a second option; however, they are not
    as easily discoverable for users and require more setup requirements for scale automation. They are thus less preferred. Providing information to
    command line tools via a file/settings file is not generally a good choice because it reduces the flexibility of the usage for the user of the tool.
    Lightweight command line tools generally avoid file-based options for providing information to the tool in preference for options/parameters directly on
    the command line itself. File-based options additionally run more risk of exposing secrets when information within the file contains
    things such as passwords, access tokens. This is a violation of Azure's security standards having secrets in plain text.

    ``` bash
    # Command line tools should ideally pass in necessary information to the command 
    # on the command line.
    PS ~/custom_scripts.1.0.0> ./Invoke-FirmwareUpdate.ps1 -ImagePath ./firmware/GB32M34.bin -LogDirectory ./logs/firmware
    ```

* **Log Directories Should be Supplied Not Implied**  
  If the script emits log files, the log file directory should be definable by the user or scale automation on the command line. This is generally
  advisable over the use of an environment variable for the reasons noted above. It is also important that the tool support both full and relative paths for
  any file system path references. It is additionally advisable that log files include a timestamp in the name of the file. This ensures uniqueness
  of each log file and avoids files being overwritten when the command line tool is executed multiple times on the same system. It is also advisable
  to use ISO8601 (i.e. universal round-trip) date/time format as it is both easy to read and sorts chronologically in natural text (e.g. folder explorer) scenarios.

  https://www.iso.org/iso-8601-date-and-time-format.html

  ``` bash
  # Command line tools should support full paths
  PS ~/custom_scripts.1.0.0> ./Invoke-FirmwareUpdate.ps1 -LogDirectory /home/anyuser/content.gb200.1.0.0/logs/firmware

  # Command line tools should support relative paths
  PS ~/custom_scripts.1.0.0> ./Invoke-FirmwareUpdate.ps1 -LogDirectory ./logs/firmware

  # Command line tools should generally produce timestamped log files to ensure uniqueness
  # across multiple executions on the same system. Use "universal round-trip" format.
  PS ~/custom_scripts.1.0.0> ./Invoke-FirmwareUpdate.ps1 -LogDirectory ./logs/firmware -> 2025-04-17T-09-30-00-235Z-firmware-update.log
  ```

## Script Extensions Packaging Guidelines
The following section covers packaging standards for the creating of script-based automation packages. As noted above, the primary goal of these standards
is to produce a **self-contained package**. A self-contained package can be copied/downloaded to a system under test and the scripts within executed
with minimal or no additional setup requirements. There are cases where 1st and 3rd party toolsets/binaries will need to be downloaded to the system
and this topic will be covered at the end of this section.

The term `package` used in this section refers to a folder structure containing all test content and dependencies. A package can be easily
zipped and copied around or uploaded to Azure Storage Accounts etc... for use in scale automation scenarios. Packages are a fundamental part of any
script-based extensions development process. The following illustrates an example of the recommendation for the folder structure of a package:

``` bash
/custom_scripts.1.0.0
    /binaries
    /docs
    /setup

    /Invoke-FirmwareUpdate.ps1
    /update_firmware.py
```

* **Include Scripts in the Package Root Directory**  
  Scripts should generally be added to the root directory of the package. This ensures that scripts can easily reference other scripts
  for import or execution. It does not matter which language the scripts are written in (e.g. PowerShell, Python). That said, it is additionally
  useful to organize same-language scripts in subdirectories if desired.

  The following illustrates the folder structure of the test content package:

  ``` bash
  # Test content scripts should generally go in the root of the package.
  /custom_scripts.1.0.0
      - Common.psm1
      - Firmware.Linux.psm1
      - Firmware.Windows.psm1
      - Custom.Linux.psd1
      - Custom.Windows.psd1

  # The use of subdirectories to contain "same-language" files is fine as well.
  /custom_scripts.1.0.0
      /powershell
          - Common.psm1
          - Firmware.Linux.psm1
          - Firmware.Windows.psm1
          - Custom.Linux.psd1
          - Custom.Windows.psd1
      /python
          - update_ssd_firmware.py
          - update_nvme_firmware.py
          - update_fpga_firmware.py
  ```

* **Include 1st and 3rd Party Toolsets in the Package**  
  When reasonable in overall size (e.g. < 10 MB), include any 1st or 3rd party toolsets used/referenced by the test content
  in a `binaries` subdirectory within the package. Follow the conventions illustrated below to encapsulate the toolsets within appropriate
  "platform-specific" folders. This makes it visually easy to discern which toolsets can be used on which OS platforms and architectures
  and is a well-vetted pattern from the industry at-large.

  <div>
  <mark>
  Note that it is also common to pre-install toolsets when possible especially on Linux systems. This reduces the overhead
  of having to carry toolsets/binaries in the test content package. One good option is to include scripts in the package 
  <b>/setup</b> folder that can be used to install the toolsets. Users or automation can easily run these scripts once before then
  running scripts within the same package.
  </mark>
  </div>
  <br/>

  ``` bash
  # Toolsets and binaries used/referenced by the test content scripts should be contained
  # within platform-specific subdirectories within the package.
  /custom_scripts.1.0.0

      # Toolsets go in 'binaries' subdirectory
      /binaries

          # Toolsets for the specific platform/architecture (e.g. linux-x64) go in
          # a subdirectory named for that platform/architecture.
          /customutil
              # Toolsets that work on Linux OS and ARM64 CPU architectures.
              /linux-arm64
                  - customutil

              # Toolsets that work on Linux OS and X64 CPU architectures.
              /linux-x64
                  - customutil

          /ipmiutil
              # Toolsets that work on Windows OS and ARM64 CPU architectures.
              /win-arm64
                  - ipmiutil.exe

              # Toolsets that work on Windows OS and X64 CPU architectures.
              /win-x64
                  - ipmiutil.exe
  ```

* **Include Documentation in the Package**  
  Each hardware program comes with special considerations. Documentation related to the specific program should be included in either the root
  directory for the package (e.g. README.md) or in a `docs` subdirectory within the package. Any non-automated setup requirments should be clearly
  documented.

  ``` bash
  # Documentation providing context and usage information should be contained within
  # a 'docs' subdirectory.
  /custom_scripts.1.0.0
      /docs
          - README.md
          - SETUP_REQUIREMENTS.md
          - USAGE.md
  ```

* **Include System Setup Scripts in the Package**  
  With some hardware program qualification scenarios, there are 1-time/initial setup requirements that are automated with scripts. For example, in certain
  scenarios, it is highly desirable to pre-install certain toolsets (e.g. install ipmiutil from a Linux package manager). These toolsets, once installed, greatly simplify the
  integration/referencing of the toolsets within the test content scripts. Scripts that are used for 1-time/initial system setup should be included in a
  `setup` subdirectory. Any installer applications (e.g. MSI, debian packages) should be included in this directory as well.

  ``` bash
  # System setup scripts (and related content) should be contained within 
  # a 'setup' subdirectory.
  /content.gb200.1.0.0
      /setup
          - install-packages.sh
          - install-pwsh.sh
          - install-python.sh
          - install-docker.sh
          - install-debug-toolsets.sh
          - debug-toolsets.deb
  ```





