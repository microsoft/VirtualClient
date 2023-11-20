ARG REPO=mcr.microsoft.com/dotnet/runtime
# Ubuntu 20.04 image.
FROM ${REPO}:5.0.9-focal-amd64

COPY out/bin/Debug/x64/VirtualClient.Main/net8.0/linux-x64/publish/. ./VirtualClient/