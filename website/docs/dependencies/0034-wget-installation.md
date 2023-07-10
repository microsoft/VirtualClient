# Wget Installation
A dependency to assist installing packages from a remote location using the 'wget' toolset.

- [Wget Documentation](https://www.gnu.org/software/wget/manual/wget.html)

## Supported Platform/Architectures
* linux-x64
* linux-arm64
* win-x64
* win-arm64

## Profile Component Parameters
The following section describes the parameters used by the individual component in the profile.

| **Parameter** | **Required** | **Description**                                         |
|---------------|--------------|---------------------------------------------------------|
| PackageUri | Yes          | The URI to the package to download and install.    |
| SubPath   | No          | The subpath within the extracted folder to register as the true PackageName. Note: When extracted, some packages have subdirectories. |

## Example
The following section describes the parameters used by the individual component in the profile.

* [Profile Example](https://github.com/microsoft/VirtualClient/blob/main/src/VirtualClient/VirtualClient.Main/profiles/GET-STARTED-REDIS.json)

```json
{
    "Type": "WgetPackageInstallation",
    "Parameters": {
        "Scenario": "InstallRedisPackage",
        "PackageName": "redis",
        "PackageUri": "https://github.com/redis/redis/archive/refs/tags/6.2.1.tar.gz",
        "SubPath": "redis-6.2.1",
        "Notes": "Example path to package -> /packages/redis/redis-6.2.1"
    }
},
```