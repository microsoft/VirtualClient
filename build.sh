#!/bin/bash

EXIT_CODE=0
BUILD_CONFIGURATION="Release"
BUILD_FLAGS=""
BUILD_VERSION=""
SCRIPT_DIR="$(dirname $(readlink -f "${BASH_SOURCE[0]}"))"

# Runtime build flags
BUILD_ALL=true
BUILD_LINUX_X64=false
BUILD_LINUX_ARM64=false
BUILD_WIN_X64=false
BUILD_WIN_ARM64=false

Usage() {
    echo ""
    echo "Builds the source code in the repo."
    echo ""
    echo "Options:"
    echo "---------------------"
    echo "--trim         Enables trimming for publish output."
    echo "--linux-x64    Build only for linux-x64 runtime."
    echo "--linux-arm64  Build only for linux-arm64 runtime."
    echo "--win-x64      Build only for win-x64 runtime."
    echo "--win-arm64    Build only for win-arm64 runtime."
    echo ""
    echo "Usage:"
    echo "---------------------"
    echo "./build.sh [--trim] [--linux-x64] [--linux-arm64] [--win-x64] [--win-arm64]"
    echo ""
    echo "Examples:"
    echo "---------------------"
    echo "./build.sh"
    echo "./build.sh --win-x64 --trim"
    echo "./build.sh --linux-arm64"
    echo ""
    Finish
}

Error() {
    EXIT_CODE=1
    End
}

End() {
    echo ""
    echo "Build Stage Exit Code: $EXIT_CODE"
    echo ""
    Finish
}

Finish() {
    exit $EXIT_CODE
}

# Parse arguments
while [[ $# -gt 0 ]]; do
    case "${1,,}" in
        "/?"|"-?"|"--help")
            Usage
            ;;
        "--trim")
            BUILD_FLAGS="-p:PublishTrimmed=true"
            ;;
        "--linux-x64")
            BUILD_LINUX_X64=true
            BUILD_ALL=false
            ;;
        "--linux-arm64")
            BUILD_LINUX_ARM64=true
            BUILD_ALL=false
            ;;
        "--win-x64")
            BUILD_WIN_X64=true
            BUILD_ALL=false
            ;;
        "--win-arm64")
            BUILD_WIN_ARM64=true
            BUILD_ALL=false
            ;;
        *)
            echo "Unknown option: $1"
            Usage
            ;;
    esac
    shift
done

# Set defaults if no runtime is explicitly selected
if [[ "BUILD_ALL" == true ]]; then
    BUILD_LINUX_X64=true
    BUILD_LINUX_ARM64=true
    BUILD_WIN_X64=true
    BUILD_WIN_ARM64=true
fi

# Build configuration override
if [[ -n "$VCBuildConfiguration" ]]; then
    BUILD_CONFIGURATION=$VCBuildConfiguration
fi

# Build version override
if [[ -f "$SCRIPT_DIR/VERSION" ]]; then
    BUILD_VERSION=$(cat "$SCRIPT_DIR/VERSION" | sed 's/^[[:space:]]*//;s/[[:space:]]*$//')
fi

if [[ -n "$VCBuildVersion" ]]; then
    BUILD_VERSION=$VCBuildVersion
fi

echo ""
echo "**********************************************************************"
echo "Build Version : $BUILD_VERSION"
echo "Repo Root     : $SCRIPT_DIR"
echo "Configuration : $BUILD_CONFIGURATION"
echo "Flags         : $BUILD_FLAGS"
echo "**********************************************************************"

echo ""
echo "[Build Solution]"
echo "----------------------------------------------------------------------"
echo ""
echo "!!!"
echo "!!! Note that the solution is be built with 'Debug' configuration in order to support a better debugging experience for extensions development."
echo "!!!"
echo ""
dotnet build "$SCRIPT_DIR/src/VirtualClient/VirtualClient.sln" -c Debug -v Detailed \
-p:AssemblyVersion=$BUILD_VERSION || Error

# Runtime-specific builds
if [[ "$BUILD_LINUX_X64" == true ]]; then
    echo ""
    echo "[Build Virtual Client: linux-x64]"
    echo "----------------------------------------------------------------------"
    dotnet publish "$SCRIPT_DIR/src/VirtualClient/VirtualClient.Main/VirtualClient.Main.csproj" -r linux-x64 -c $BUILD_CONFIGURATION -v Detailed --self-contained \
    -p:AssemblyVersion=$BUILD_VERSION -p:InvariantGlobalization=true $BUILD_FLAGS || Error
fi

if [[ "$BUILD_LINUX_ARM64" == true ]]; then
    echo ""
    echo "[Build Virtual Client: linux-arm64]"
    echo "----------------------------------------------------------------------"
    dotnet publish "$SCRIPT_DIR/src/VirtualClient/VirtualClient.Main/VirtualClient.Main.csproj" -r linux-arm64 -c $BUILD_CONFIGURATION -v Detailed --self-contained \
    -p:AssemblyVersion=$BUILD_VERSION -p:InvariantGlobalization=true $BUILD_FLAGS || Error
fi

if [[ "$BUILD_WIN_X64" == true ]]; then
    echo ""
    echo "[Build Virtual Client: win-x64]"
    echo "----------------------------------------------------------------------"
    dotnet publish "$SCRIPT_DIR/src/VirtualClient/VirtualClient.Main/VirtualClient.Main.csproj" -r win-x64 -c $BUILD_CONFIGURATION -v Detailed --self-contained \
    -p:AssemblyVersion=$BUILD_VERSION $BUILD_FLAGS || Error
fi

if [[ "$BUILD_WIN_ARM64" == true ]]; then
    echo ""
    echo "[Build Virtual Client: win-arm64]"
    echo "----------------------------------------------------------------------"
    dotnet publish "$SCRIPT_DIR/src/VirtualClient/VirtualClient.Main/VirtualClient.Main.csproj" -r win-arm64 -c $BUILD_CONFIGURATION -v Detailed --self-contained \
    -p:AssemblyVersion=$BUILD_VERSION $BUILD_FLAGS || Error
fi

End
