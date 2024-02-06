# Run Custom Scripts with Virtual Client
The following section covers the steps to use custom scripts and test suites with Virtual Client, by just authoring custom profiles, thereby 
without any code change in Virtual Client. Virtual Client provides multiple "Action" components that can be used to build a custom profile 
and run any script/test-suite. Virtual Client would have some expectations from the custom script/test-suite in terms of folder 
structure for the script, logging and metrics parsing, which are covered in subsequent sections.

The document includes an example in the last section that shows the end to end steps of running a custom script/test-suite 
with Virtual Client.

## Execution and Logging Requirements with the Test Suite/Script 
  * **Directory Structure**
    The script/test-suite, when being executed through Virtula Client would be expected to follow a standard directory structure that 
    is used by all VC Workloads. VC supports four OS-platforms (win-x64, linux-x64, win-arm64 and linux-arm64). The script/test suites 
    should be placed in the suitabe directory. These will then be zipped to form the wokrload package, and can be uploaded on blob 
    package store, if the DownloadDependencyProvider needs to be used to download the package during VC Execution.
    * Example directory structure:
      ```
      - workloadPackage 
        - linux-arm64
          - <linux-arm64 based scripts>
        - linux-x64
          - <linux-x64 based scripts>
        - win-x64
          - <win-x64 based scripts>
        - win-arm64
          - <win-arm64 based scripts>
      ```

  * **Logging Requirements**
    All the execution logs that need to captured/uploaded to content Store, need to be provided as a parameter to the Action Component
    (Refer to examples in below sections). The parameter supports logs in a sub-folder within the workload package, or string pattern 
    of respective log files.  

  * **Capturing Metrics**
    If the metrics are required to be captured for the test execution and sent to telemetry, then the script/tool would be expected to 
    have a parser that parses the required metrics from it’s logs and have them enumerated in a standard metrics JSON file 
    (called test-metrics.json) within the workload package folder. Virtual Client would be capturing the metrics from the JSON and
    using it to process further for sending it over telemetry and attaching required metadata to it. 
    * Example test-metrics.json file:
      ``` json
      {
          "metric1": "0",
          "metric2": "1.45",
          "metric3": "1279854282929.09"
      }
      ```

## Generic Executor Components for Scripts
  [Example Profile](https://github.com/microsoft/VirtualClient/blob/main/src/VirtualClient/VirtualClient.Main/profiles/EXAMPLE-EXECUTE-SCRIPT.json) 

  The following executor Components can be used to build profiles for executing the custom scripts and test-suites in Virtual Client.
  
  * **ScriptExecutor**
    This Action can be used to execute bash, cmd, bat, shell scripts and executables.
    ``` json
    Example:
    "Actions": [
        {
            "Type": "ScriptExecutor",
            "Parameters": {
                "Scenario": "Name_Of_Scenario",
                "CommandLine": "argument1 argument2",
                "ScriptPath": "script.sh",
                "LogPaths":  "*.log;*.txt;logSubFolder\\",
                "ToolName":  "Name_Of_Tool",
                "PackageName":  "exampleWorkload",
                "FailFast":  false,
                "Tags": "Test,VC,Script"
            }
        }
    ]
    ```

  * **PowershellExecutor**
    This Action can be used to execute Powershell Scripts in Windows. 
  * Please note Virtual Client should be run in elevated mode when using this component, as it sets the execution Policy to be bypass.
    ``` json
    Example:
    "Actions": [
        {
            "Type": "PowershellExecutor",
            "Parameters": {
                "Scenario": "Name_Of_Scenario",
                "CommandLine": "argument1 argument2",
                "ScriptPath": "script.ps1",
                "LogPaths":  "*.log;*.txt;logSubFolder\\",
                "ToolName":  "Name_Of_Tool",
                "PackageName":  "exampleWorkload",
                "FailFast":  false,
                "Tags": "Test,VC,Script"
            }
        }
    ]
    ```

  * **PythonExecutor**
    This Action can be used to execute Python Scripts in Windows and Linux. The Dependency Components in Virtual Client can be used to 
    install python on the system. The following example shows the dependency components for Linux and Windows, one of them can be used 
    as per the platform.
    ``` json
    Example:
    "Actions": [
        {
            "Type": "PythonExecutor",
            "Parameters": {
                "Scenario": "Name_Of_Scenario",
                "CommandLine": "argument1 argument2",
                "ScriptPath": "script.py",
                "LogPaths":  "*.log;*.txt;logSubFolder\\",
                "ToolName":  "Name_Of_Tool",
                "PackageName":  "exampleWorkload",
                "UsePython3": true,
                "FailFast":  false,
                "Tags": "Test,VC,Script"
            }
        }
    ]
    
    Python installation in Linux:
    "Dependencies": [
        {
            "Type": "LinuxPackageInstallation",
            "Parameters": {
                "Scenario": "InstallLinuxPackages",
                "Packages": "python3,python3-pip"
            }
        }
    ]
    
    Python Installation in Windows using chocolatey
    "Dependencies": [
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
                "Scenario": "InstallCygwin",
                "PackageName": "chocolatey",
                "Packages": "python3"
            }
        }
    ]
    ```

## Profile Parameters
The following parameters are supported by the Executor Classes that can be modified in a custom profile to run the scripts.

  |   Parameter  | Purpose | Acceptable Type |
  |--------------|---------|------------------|
  |  Scenario    | Name of the scenario | String |
  |  ToolName    | Name of the Script/Tool being executed | String |
  |  CommandLine | The command line arguments to be used with the script/executable | String |
  |  ScriptPath  | Relative Path of the script inside the workload package that needs to be invoked. | String |
  |  LogPaths    | A list of file/folder paths separated by semicolons ";". Refer to the Logging Section for more info. | String (List of Log files/folders separted by semicolon) |  
  |  PackageName | Name of the workload package built for running the script. If the workload package is being downloaded from blob package store, this needs to match with the package name defined in DependencyPackageInstallation. | String |
  |  FailFast    | Flag indicates that the application should exit immediately on first/any errors regardless of their severity. | Boolean  |
  |  UsePython3  | (Only valid for PythonExecutor) A true value indicates use of "python3" as environment variable to execute python, a false value will use "python" as the environment variable. | Boolean (Default is true) |


## Logging and Metrics

  * ### Logging
    The “LogPaths” parameter of the generic executor determines the semicolon ";" separated logs sub-folders and log files. 
  
      * For the sub-folder, the relative path to the sub-folder within the package directory needs to be provided. 
        Then all files within the directory will be swept, and they will be uploaded to content store (if the contentstore --cs parameter
        is provided in Virtual Client) and will be moved to the central logs directory in Virtual Client.
      
      * For the filePaths, each relative file path can be a pattern that will be matched within the workload Package directory. For example,
        a pattern of *.txt will cover all txt files within the workload package directory and they will be swept, uploaded to content
        store (if contentStore --cs parameter is provided) and will be moved to central logs directory.
      
      Example Central Logs Directory (for a win-x64 system):
        (VirtualClient root folder) > content > win-x64 > logs > toolName > scenarioName_TIMESTAMP 

  * ### Metrics
    The metrics capturing for the script/test-suite by Virtual Client would depend on the metric Parsing requirement. The script/test-suite
    would be expected to parse the required metrics and save it in a json format in a file called "test-metrics.json", 
    saved in the workload package root folder. Thus, the actual metric parsing logic here is a responsibility of the test suite/script. 
    It will be expected to generate a metrics json file with key-value pairs, where each key represents a metric name and the 
    value is the metric Value. Virtual Client will capture these metrics and will be sending it to telemetry for further actions. 


## Example Custom Profile

#### Pre-Reads:
  * [Virtual Client Profiles](https://microsoft.github.io/VirtualClient/docs/guides/0011-profiles/): Learn about profiles in Virtual Client
  * [Virtual Client Developer Guide](https://microsoft.github.io/VirtualClient/docs/developing/0010-develop-guide/): Refer to the "Debug in Visual Studio by Running a Custom Profile" Section to know more about developing Custom Profiles.
  * [Virtual Client Workload Package](https://microsoft.github.io/VirtualClient/docs/guides/0030-commercial-workloads/): Learn about creating workload packages and using it with DependencyPackageInstallation

#### Example
  Let us consider an example where a script called stress-memory is to be used with Virtual Client. There are two versions of the script 
  for Windows and Linux, stress-memory.cmd and stress-memory.sh respectively. The same binaries are for x64 and arm64 platforms.
  They take an argument for timeout (-t) with value in seconds and -v is to be added for verbose logging.
  
  They produce multiple log files
    * traces-TIMESTAMP.txt is produced in a folder called "traces" 
    * summary-TIMESTAMP.txt is produced in the root folder.
    * There are two configuration files called config-1.json and config-2.json, both in the root folder. 
    * Also, the scripts parse the logs and generate metrics and store it in a file test-metrics.json in the root folder. 
    
  All the 5 files are to be captured for logging.
  
  The Steps to followed for above example are:

  1. Build a workload package for the scripts. The following directory structure for the zipped package can be followed:
     ```
     - stressmemory.1.0.zip 
        - linux-arm64
          - stress-memory.sh
        - linux-x64
          - stress-memory.sh
        - win-x64
          - stress-memory.cmd
        - win-arm64
          - stress-memory.cmd
      ```

  2. The stressmemory.1.0.zip should be uploaded to the package store for downloading the workload package during execution.
  3. A custom profile ```(EXAMPLE-CUSTOM-PROFILE.json)``` needs to be authored and needs to be placed in the respective "profiles" directory.
     
     Custom Profile for above example (win-x64)

     ``` json
         {
            "Description": "Memory Stress Script Executor",
            "MinimumExecutionInterval": "00:10:00",
            "Metadata": {
                "SupportedPlatforms": "win-x64,win-arm64"
            },
            "Parameters": {
                "CommandLine": "-t 600 -v",
                "ScriptPath": "stress-memory.cmd",
                "LogPaths":  "traces\\;summary-*.txt;*.json",
                "ToolName":  "stressscript",
                "PackageName":  "stresspackage",
                "FailFast":  false
            },
            "Actions": [
                {
                    "Type": "ScriptExecutor",
                    "Parameters": {
                        "Scenario": "memory_stress_scenario",
                        "CommandLine": "$.Parameters.CommandLine",
                        "ScriptPath": "$.Parameters.ScriptPath",
                        "LogPaths": "$.Parameters.LogPaths",
                        "ToolName": "$.Parameters.ToolName",
                        "PackageName": "$.Parameters.PackageName",
                        "FailFast":  "$.Parameters.FailFast",
                        "Tags": "Test,VC,Script"
                    }
                }
            ],
            "Dependencies": [
                {
                    "Type": "DependencyPackageInstallation",
                    "Parameters": {
                        "Scenario": "InstallWorkloadPackage",
                        "BlobContainer": "packages",
                        "BlobName": "stressmemory.1.0.zip",
                        "PackageName": "$.Parameters.PackageName",
                        "Extract": true
                    }
                }
            ]
        }
     ```

  4. The Virtual Client can be executed with the following command (assuming win-x64):
     ```VirtualClient.exe --profile=EXAMPLE-CUSTOM-PROFILE.json --packages="https://virtualclient..." --debug --i=1```
  5. Once the execution is complete,
     * The metrics would be available at: 
       ```<VirtualClientRootFolder>\content\win-x64\logs\metrics.csv```
     * The 5 files that were required to be captured, would be availabe at:
       ```<VirtualClientRootFolder>\content\win-x64\logs\stressscript\memory_stress_scenario_yyyyMMddHHmmss\```
