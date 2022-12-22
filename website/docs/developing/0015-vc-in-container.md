# Running VC in container

---
## Installing Docker on your Windows Dev machine
To run Docker on your presumeably Windows Dev Machine, you need to install both Docker and WSL. Reboots might be required.
1. [Docker desktop for Windows](https://hub.docker.com/editions/community/docker-ce-desktop-windows)
2. [Install Windows Subsystem for Linux](https://docs.microsoft.com/en-us/windows/wsl/install-win10#manual-installation-steps)

---
## Building VC Container
VC containers uses [official .NET Runtime docker images](https://hub.docker.com/_/microsoft-dotnet-runtime/) as base images. Specific tags are used match the OS/Architecture for the container. 
For example, mcr.microsoft.com/dotnet/runtime:5.0.9-focal-arm64v8 will pull up the Ubuntu20.04-ARM64 image with .NET5.0.9. Please refer to the Official .NET Docker repository page if tags need to be updated in Dockerfile.

### Command to build
The command to build docker container is ["docker build"](https://docs.docker.com/engine/reference/commandline/build/).

**IMPORTANT**: Always tag your docker container using "-t" and ":", "-t test-win-x64:1.0.1.1" will mark "name=test-win-x64" and "tag=1.0.1.1". 
If tag after column sign is not supplied, it will be assigned a default tag "latest", which could overwrite production image if pushed accidentally.

```
$ docker build -f src\VirtualClient\VirtualClient.Packaging\dockerfiles\win-x64.dockerfile -t test-win-x64:1.0.1.1 E:\source\VirtualClient
$ docker build -f src\VirtualClient\VirtualClient.Packaging\dockerfiles\linux-x64.dockerfile -t test-linux-x64:1.0.1.1 E:\source\VirtualClient
```

### [Switch between Windows and Linux](https://docs.microsoft.com/en-us/windows/wsl/install-win10#manual-installation-steps)
The container can only run as the Host operating system. On your Windows dev machine, Docker runs Linux containers on the WSL backend, and runs Windows containers on your host operating system.
As a result, the docker CLI needs to switch mode if you want to test Windows containers and then Linux containers.
From the Docker Desktop menu, you can toggle which daemon (Linux or Windows) the Docker CLI talks to. 
Select Switch to Windows containers to use Windows containers, or select Switch to Linux containers to use Linux containers (the default).


### Supported Platforms
* Linux x64
* Linux arm64
* Windows x64
* ~~Windows arm64~~ (Could be supported soon)

It is important to know that Container shares the OS with the host, so sometimes the container will not be able to run on your host even though both are running as win-x64.
WindowsServerCore2022-Container is not able to run on Windows10-Host for example.

---


---
## Run VC Container
The command to run docker container is ["docker run"](https://docs.docker.com/engine/reference/commandline/run/)

```
$ docker run -d -p 3000:80 test-win-x64:1.0.1.1 C:\VirtualClient\VirtualClient.exe <Your VC Command>
$ docker run -d -p 3000:80 test-linux-x64:1.0.1.1 VirtualClient/VirtualClient <Your VC Command>
```