ARG REPO=mcr.microsoft.com/dotnet/runtime
# Ubuntu 22.04 image.
FROM ${REPO}:8.0-jammy-amd64
# VirtualClient dependencies.
RUN apt-get update -y && apt-get install -y lsb-release sudo wget gnupg

COPY out/bin/Release/x64/VirtualClient.Main/net8.0/linux-x64/. ./VirtualClient/
