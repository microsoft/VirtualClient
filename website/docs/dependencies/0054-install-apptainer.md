# Apptainer Installation
Apptainer is a secure, portable, and easy-to-use container system that provides absolute trust and security. It is widely used across industry and academia in many areas of work.

- [Apptainer Documentation](https://apptainer.org/)

## Supported Platform/Architectures
* linux-x64
* linux-arm64

## Profile Component Parameters
The following section describes the parameters used by the individual component in the profile.

| **Parameter** | **Required** | **Description**                                         | **Default** |
|---------------|--------------|---------------------------------------------------------|--------------|
| Version | No          | The version of apptainer image to download and install.    | 1.1.6 |

## Example
The following section describes the parameters used by the individual component in the profile.

* [Profile Example](https://github.com/microsoft/VirtualClient/blob/main/src/VirtualClient/VirtualClient.Main/profiles/PERF-GPU-METASEQ-NVIDIA.json)

```json
{
    "Type": "ApptainerInstallation",
    "Parameters": {
        "Scenario": "InstallApptainer",
        "Version": "1.1.6"
    }
},
```