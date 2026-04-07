# Getting Started (Docker)

In this document, we are going to run OpenSSL workload in a Docker Container.

## Build container
:::info
VirtualClient is planning on setting up a public container repository. At this moment you need to build VC docker images locally.
:::

VirtualClient keeps the `DockerFile` in `src\VirtualClient\VirtualClient.Packaging\dockerfiles\` directory. The following is an example command to build a docker image. You need to:
1. Build VirtualClient using `build.cmd` or `dotnet build`.
2. Build docker container using `docker build`.

```bash
build.cmd
docker build -f src\VirtualClient\VirtualClient.Packaging\dockerfiles\win-x64.dockerfile -t test-win-x64:1.0.1.3 E:\Source\Github\VirtualClient
```

The build process could take a couple minutes.
```bash
E:\Source\Github\VirtualClient>docker build -f src\VirtualClient\VirtualClient.Packaging\dockerfiles\win-x64.dockerfile -t test-win-x64:1.0.1.3 E:\Source\Github\VirtualClient
Sending build context to Docker daemon  2.788GB
Step 1/3 : ARG REPO=mcr.microsoft.com/windows/nanoserver
Step 2/3 : FROM ${REPO}:ltsc2022
ltsc2022: Pulling from windows/nanoserver
38952155e2cd: Pull complete
Digest: sha256:79fb1372fc5b3aeeca73603d5eadd0a8fb7d4f0b77bd29498696c03bb6de1fdf
Status: Downloaded newer image for mcr.microsoft.com/windows/nanoserver:ltsc2022
 ---> 0db1879370e5
Step 3/3 : COPY out/bin/Release/x64/VirtualClient.Main/net9.0/win-x64/publish/ C:/VirtualClient/
 ---> 7c2fe2466138
Successfully built 7c2fe2466138
Successfully tagged test-win-x64:1.0.1.3

Use 'docker scan' to run Snyk tests against images to find vulnerabilities and learn how to fix them
```

## Start container
For Windows, the Dockerfile copied VC binaries to C:\VirtualClient. You can invoke a docker container using the following commands.

```bash
>docker run -d -p 3000:80 test-win-x64:1.0.1.3 C:\VirtualClient\VirtualClient.exe --profile=PERF-CPU-OPENSSL.json 
bad3c2a2fe95a3135264dc70ee63f89df7e1deb7875b3a0104b3231e248feaac
```

## Read console logs
You can check the container console logs to see if the workloads is running as expected.

```bash
>docker logs bad3c2a2fe95a3135264dc70ee63f89df7e1deb7875b3a0104b3231e248feaac
[12/22/2022 10:36:14 AM] Profile: Initialize
[12/22/2022 10:36:14 AM] Profile: Install Dependencies
[12/22/2022 10:36:14 AM] Profile: Dependency = DependencyPackageInstallation (scenario=InstallOpenSSLWorkloadPackage)
[12/22/2022 10:36:25 AM] Profile: Execute Monitors
[12/22/2022 10:36:25 AM] Profile: Monitor = PerfCounterMonitor (scenario=CaptureCounters)
[12/22/2022 10:36:25 AM] Profile: Execute Actions
[12/22/2022 10:36:25 AM] Profile: Action = OpenSslExecutor (scenario=MD5)
[12/22/2022 10:46:25 AM] Profile: Action = OpenSslExecutor (scenario=SHA1)
```

:::info
Since you can't easily access the VC metric files in containers, it is recommended to setup [Telemetry](./0040-telemetry.md) to get automatic data upload.
:::

## Congratulations !!
You just benchmarked your system's container performance using VC in Docker.
