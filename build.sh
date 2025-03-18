#!/bin/bash

EXIT_CODE=0
BUILD_CONFIGURATION="Release"
BUILD_FLAGS=""
BUILD_VERSION=""
SCRIPT_DIR="$(dirname $(readlink -f "${BASH_SOURCE}"))"

Usage() {
    echo ""
    echo "Builds the source code in the repo. "
    echo ""
    echo "Options:"
    echo "---------------------"
    echo "--trim   - Enables trimming for publish output."
    echo ""
    echo "Usage:"
    echo "---------------------"
    echo "build.sh [--trim]"
    echo ""
    echo "Examples"
    echo "---------------------"
    echo "user@system:~/repo$ ./build.sh"
    echo "user@system:~/repo$ ./build.sh --trim"
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
        *)
            echo "Unknown option: $1"
            Usage
            ;;
    esac
    shift
done

# The default build configuration is 'Release'.
BUILD_CONFIGURATION="Release"

# The default build configuration (e.g. Release) can be overridden 
# by the 'VCBuildConfiguration' environment variable
if [[ -v "VCBuildConfiguration" && -n "$VCBuildConfiguration" ]]; then
    BUILD_CONFIGURATION=$VCBuildConfiguration
fi

# The default build version is defined in the repo VERSION file.
BUILD_VERSION=$(cat "$SCRIPT_DIR/VERSION")

# The default build version can be overridden by the 'VCBuildVersion' environment variable
if [[ -v "VCBuildVersion" && -n "$VCBuildVersion" ]]; then
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
dotnet build "$SCRIPT_DIR/src/VirtualClient/VirtualClient.sln" -c $BUILD_CONFIGURATION \
-p:AssemblyVersion=%BUILD_VERSION

result=$?
if [ $result -ne 0 ]; then
    Error
fi

echo ""
echo "[Build Virtual Client: linux-x64]"
echo "----------------------------------------------------------------------"
dotnet publish "$SCRIPT_DIR/src/VirtualClient/VirtualClient.Main/VirtualClient.Main.csproj" -r linux-x64 -c $BUILD_CONFIGURATION --self-contained \
-p:AssemblyVersion=%BUILD_VERSION $BUILD_FLAGS

result=$?
if [ $result -ne 0 ]; then
    Error
fi

echo ""
echo "Build Virtual Client: linux-arm64]"
echo "----------------------------------------------------------------------"
dotnet publish "$SCRIPT_DIR/src/VirtualClient/VirtualClient.Main/VirtualClient.Main.csproj" -r linux-arm64 -c $BUILD_CONFIGURATION --self-contained \
-p:AssemblyVersion=%BUILD_VERSION $BUILD_FLAGS

result=$?
if [ $result -ne 0 ]; then
    Error
fi

echo ""
echo "Build Virtual Client: win-x64]"
echo "----------------------------------------------------------------------"
dotnet publish "$SCRIPT_DIR/src/VirtualClient/VirtualClient.Main/VirtualClient.Main.csproj" -r win-x64 -c $BUILD_CONFIGURATION --self-contained \
-p:AssemblyVersion=%BUILD_VERSION $BUILD_FLAGS

result=$?
if [ $result -ne 0 ]; then
    Error
fi

echo ""
echo "[Build Virtual Client: win-arm64]"
echo "----------------------------------------------------------------------"
dotnet publish "$SCRIPT_DIR/src/VirtualClient/VirtualClient.Main/VirtualClient.Main.csproj" -r win-arm64 -c $BUILD_CONFIGURATION --self-contained \
-p:AssemblyVersion=%BUILD_VERSION $BUILD_FLAGS

result=$?
if [ $result -ne 0 ]; then
    Error
fi
done

End
