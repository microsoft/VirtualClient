# Execute Command
A dependency to execute a command on the system, in a specified working directory.

## Supported Platform/Architectures
* linux-x64
* linux-arm64
* win-x64
* win-arm64

## Profile Component Parameters
The following section describes the parameters used by the individual component in the profile.

| **Parameter** | **Required** | **Description**                                         |
|---------------|--------------|---------------------------------------------------------|
| Command | Yes          | The command(s) to execute. Multiple commands should be delimited using the '&amp;&amp;' characters. |
| WorkingDirectory   | No          | The working directory from which the command should be executed.  |

## Example
The following section describes the parameters used by the individual component in the profile. Note: If the 'PackageName' parameter is defined, this parameter will take precedence over the WorkingDirectory parameter. Otherwise, the directory where the package is installed for the 'PackageName' parameter will be used as the working directory.

* [Profile Example](https://github.com/microsoft/VirtualClient/blob/main/src/VirtualClient/VirtualClient.Main/profiles/PERF-REDIS.json)

```json
{
    "Type": "ExecuteCommand",
    "Parameters": {
        "Scenario": "CompileMemtier",
        "SupportedPlatforms": "linux-x64,linux-arm64",
        "Command": "git checkout 1.4.0&&autoreconf -ivf&&bash -c './configure'&&make",
        "WorkingDirectory": "{PackagePath:memtier}"
    }
}
```