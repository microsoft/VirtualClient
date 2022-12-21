ARG REPO=mcr.microsoft.com/dotnet/runtime
# Windows nanoserver 20H2 image.
FROM ${REPO}:5.0.9-nanoserver-20H2

COPY out/bin/Debug/x64/VirtualClient.Main/net6.0/win-x64/publish/ C:/VirtualClient/