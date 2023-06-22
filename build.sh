#!/bin/bash

while [[ "$#" -gt 0 ]]; do
    case $1 in
        --build-all)
            BUILD_ALL=true
            ;;
        *)
            ;;
    esac
    shift
done

PUBLISH_FLAGS="--self-contained -p:InvariantGlobalization=true"
ARCH=$(uname -m)

if [ "$ARCH" == "x86_64" ]; then
    ARCH="linux-x64"
elif [ "$ARCH" == "aarch64" ]; then
    ARCH="linux-arm64"
else
    echo "Unsupported architecture: $ARCH"
fi

echo "Building VirtualClient solution."
dotnet build src/VirtualClient/VirtualClient.sln -c Debug

if [ "$BUILD_ALL" = true ]; then
    echo "Publishing VirtualClient for all platforms."
    dotnet publish src/VirtualClient/VirtualClient.Main/VirtualClient.Main.csproj -r linux-x64 -c Debug $PUBLISH_FLAGS
    dotnet publish src/VirtualClient/VirtualClient.Main/VirtualClient.Main.csproj -r linux-arm64 -c Debug $PUBLISH_FLAGS
    dotnet publish src/VirtualClient/VirtualClient.Main/VirtualClient.Main.csproj -r win-x64 -c Debug $PUBLISH_FLAGS
    dotnet publish src/VirtualClient/VirtualClient.Main/VirtualClient.Main.csproj -r win-arm64 -c Debug $PUBLISH_FLAGS
else
    echo "Publishing VirtualClient for architecture: $ARCH"
    dotnet publish src/VirtualClient/VirtualClient.Main/VirtualClient.Main.csproj -r $ARCH -c Debug $PUBLISH_FLAGS
fi