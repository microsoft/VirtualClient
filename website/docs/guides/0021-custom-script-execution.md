# Custom Script Execution
The following sections provide an overview on how to use the Virtual Client application as a runtime agent for the execution
of custom developed scripts (e.g. Python, PowerShell). This document focuses on script execution but does not cover the script 
development process. Reference the following documentation for recommendations on the implementation of script-based extensions 
encapsulated as `self-contained packages` for execution through Virtual Client.

[Script Development Guidelines](../developing/0021-develop-script-extensions.md)

## Step 1: Add Custom Packages/Scripts to the Folder Structure
Custom script packages should be copied/placed inside the `packages` folder in the Virtual Client executable/application directory.

``` bash
# Package directories:
{vc_directory}/linux-arm64/packages
{vc_directory}/linux-x64/packages
{vc_directory}/win-arm64/packages
{vc_directory}/win-x64/packages
```

## Step 2a: Execute Custom Scripts
Once your custom scripts/script packages have been added to the `packages` folder, they can be easily executed through Virtual Client.
Note that in the examples below, all relative paths provided on the command line will be treated as relative to the Virtual Client executable/application
directory.

``` bash
# Windows Examples
# ---------------------------
# Note that the "./packages" and "./logs" paths are relative to the Virtual Client executable/application directory.
# Also note that any forward-slashes in the paths are handled as backslashes on Windows systems. The use of forward-slashes
# is merely for consistency (in look and feel) across platforms on the command line.
C:\Users\AnyUser\VirtualClient\win-x64> VirtualClient.exe "./packages/custom-scripts.1.0.0/execute_openssl.py --log-dir ../logs/openssl_test"

# Linux Examples
# ---------------------------
~VirtualClient/linux-arm64$ chmod +x ./VirtualClient
~VirtualClient/linux-arm64$ ./VirtualClient "./packages/custom-scripts.1.0.0/execute_openssl.py --log-dir ./logs/openssl_test"
```

## Step 2b: Execute Custom Scripts Downloaded from a Package Store
Virtual Client supports the ability to download and execute custom scripts from packages (e.g. tar.gz, zip files) hosted in an Azure Storage Account. This is done through the use of the 
`--package-store` option on the command line. This is in fact how the application works when running many out-of-box profiles and workloads. The necessary
packages are downloaded from a storage account used to support the open source platform. Similarly, custom script packages can be downloaded
from a storage account and executed similarly to the above example. The difference is that the package name must be supplied on the command
line in a parameter called `Package` and the `--package-store` option must be used to specify the storage account from which to download the package.

[Azure Storage Account Integration](https://microsoft.github.io/VirtualClient/docs/guides/0600-integration-blob-storage/)

### Supported Parameters
In addition to the required `Package` parameter, the following describes additional parameters that can be defined on the command line.

| Parameter Name      | Description | Default Value |
|---------------------|-------------|---------------|
| LogFileName         | The name of the log file to which the script execution standard output/error will be written when the `--log-to-file` flag is provided on the command line. | |
| LogFolderName       | The name of the folder in which the log file will be created when the `--log-to-file` flag is provided on the command line. | |
| PackageContainer    | The name of the storage container in which the package is located. | packages |
| Scenario            | The name of the scenario to use for the execution of the package. This is used in telemetry for the results if the execution. | ExecuteScript |
| TelemetryFileFormat | A regular expression that defines the telemetry file format of files written by the scripts and that contain telemetry information (e.g. metrics, events) for capture. See the 'Metrics' and 'Events' schema sections in the [Data/Telemetry Support](https://microsoft.github.io/VirtualClient/docs/guides/0040-telemetry/#log-files) documentation. | Csv |

``` bash
# Windows Examples
# ---------------------------
# Note that the "./packages" and "./logs" paths are relative to the Virtual Client executable/application directory.
# Also note that any forward-slashes in the paths are handled as backslashes on Windows systems. The use of forward-slashes
# is merely for consistency (in look and feel) across platforms on the command line.
C:\Users\AnyUser\VirtualClient\win-x64> VirtualClient.exe "./packages/custom-scripts.1.0.0/execute_openssl.py --log-dir ../logs/openssl_test" --parameters="Package=custom-scripts.1.0.0.zip" --packages="https://anystorage.blob.core.windows.net/..." --log-to-file

# Linux Examples
# ---------------------------
~VirtualClient/linux-arm64$ chmod +x ./VirtualClient
~VirtualClient/linux-arm64$ ./VirtualClient "./packages/custom-scripts.1.0.0/execute_openssl.py --log-dir ../logs/openssl_test" --parameters="Package=custom-scripts.1.0.0.zip" --packages="https://anystorage.blob.core.windows.net/..." --log-to-file
```