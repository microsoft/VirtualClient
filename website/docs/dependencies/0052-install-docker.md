# Docker Installation
Docker is a platform to help develop and ship applications. Should a workload require the download of a containerized application, this dependency can assist in docker installation and setup in preparation.

- [Docker Documentation](https://docs.docker.com/)

## Supported Platform/Architectures
* linux-x64
* linux-arm64

## Profile Component Parameters
The following section describes the parameters used by the individual component in the profile.

| **Parameter** | **Required** | **Description**                                         |
|---------------|--------------|---------------------------------------------------------|
| Version | No          | The version of Docker to download and install.    |

## Example
The following section describes the parameters used by the individual component in the profile.

* [Profile Example](https://github.com/microsoft/VirtualClient/blob/main/src/VirtualClient/VirtualClient.Main/profiles/PERF-GPU-SUPERBENCH.json)

```json
{
    "Type": "DockerInstallation",
    "Parameters": {
        "Scenario": "InstallDocker"
    }
},
```