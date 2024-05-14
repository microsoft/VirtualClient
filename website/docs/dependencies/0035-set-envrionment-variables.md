# Set Environment Variables
Set one or multiple environment variables on the system.

## Supported Platform/Architectures
* linux-x64
* linux-arm64
* win-x64
* win-arm64

## Profile Component Parameters
The following section describes the parameters used by the individual component in the profile.

| **Parameter** | **Required** | **Description**                                         |
|---------------|--------------|---------------------------------------------------------|
| EnvironmentVariables   | No          | Semicolon delimtered key value pairs with equal sign. Example: "Varaible1=A;Variable2=B"  |

## Example
The following section describes the parameters used by the individual component in the profile.


```json
{
    "Type": "SetEnvironmentVariable",
    "Parameters": {
        "Scenario": "SetEnvironmentVariable",
        "EnvironmentVariables": "Varaible1=A;Variable2=B",
    }
}
```