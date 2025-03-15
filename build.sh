#!/bin/bash

EXIT_CODE=0
BUILD_CONFIGURATION=""
BUILD_FLAGS="-p:PublishTrimmed=true"
BUILD_VERSION=""
SCRIPT_DIR="$(dirname $(readlink -f "${BASH_SOURCE}"))"

Usage() {
    echo ""
    echo "Builds the source code in the repo."
    echo ""
    echo "Usage:"
    echo "---------------------"
    echo "build.sh"
    echo ""
    echo "Examples"
    echo "---------------------"
    echo "# Use defaults"
    echo "user@system:~/repo$ chmod +x *.sh"
    echo "user@system:~/repo$ ./build.sh"
    echo ""
    echo "# Set specific version and configuration"
    echo "user@system:~/repo$ export VCBuildVersion=\"1.16.25\""
    echo "user@system:~/repo$ export VCBuildConfiguration=\"Debug\""
    echo "user@system:~/repo$ chmod +x *.sh"
    echo "user@system:~/repo$ ./build.sh"
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

for ((i=1; i<=$#; i++)); do
    arg="${!i}"
  
    if [ "$arg" == "/?" ] || [ "$arg" == "-?" ] || [ "$arg" == "--help" ]; then
        Usage
    fi
done

# The default build version is defined in the repo VERSION file.
BUILD_VERSION=$(cat "$SCRIPT_DIR/VERSION")

# The default build version can be overridden by the 'VCBuildVersion' 
# environment variable
if [[ -v "VCBuildVersion" && -n "$VCBuildVersion" ]]; then
    BUILD_VERSION=$VCBuildVersion
fi

# The default build configuration is 'Release'.
BUILD_CONFIGURATION="Release"

# The default build configuration (e.g. Release) can be overridden 
# by the 'VCBuildConfiguration' environment variable
if [[ -v "VCBuildConfiguration" && -n "$VCBuildConfiguration" ]]; then
    BUILD_CONFIGURATION=$VCBuildConfiguration
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
-p:AssemblyVersion=$BUILD_VERSION

result=$?
if [ $result -ne 0 ]; then
    Error
fi

echo ""
echo "[Build Virtual Client: linux-x64]"
echo "----------------------------------------------------------------------"
dotnet publish "$SCRIPT_DIR/src/VirtualClient/VirtualClient.Main/VirtualClient.Main.csproj" -r linux-x64 -c $BUILD_CONFIGURATION --self-contained \
-p:AssemblyVersion=$BUILD_VERSION $BUILD_FLAGS

result=$?
if [ $result -ne 0 ]; then
    Error
fi

echo ""
echo "Build Virtual Client: linux-arm64]"
echo "----------------------------------------------------------------------"
dotnet publish "$SCRIPT_DIR/src/VirtualClient/VirtualClient.Main/VirtualClient.Main.csproj" -r linux-arm64 -c $BUILD_CONFIGURATION --self-contained \
-p:AssemblyVersion=$BUILD_VERSION $BUILD_FLAGS

result=$?
if [ $result -ne 0 ]; then
    Error
fi

echo ""
echo "Build Virtual Client: win-x64]"
echo "----------------------------------------------------------------------"
dotnet publish "$SCRIPT_DIR/src/VirtualClient/VirtualClient.Main/VirtualClient.Main.csproj" -r win-x64 -c $BUILD_CONFIGURATION --self-contained \
-p:AssemblyVersion=$BUILD_VERSION $BUILD_FLAGS

result=$?
if [ $result -ne 0 ]; then
    Error
fi

echo ""
echo "[Build Virtual Client: win-arm64]"
echo "----------------------------------------------------------------------"
dotnet publish "$SCRIPT_DIR/src/VirtualClient/VirtualClient.Main/VirtualClient.Main.csproj" -r win-arm64 -c $BUILD_CONFIGURATION --self-contained \
-p:AssemblyVersion=$BUILD_VERSION $BUILD_FLAGS

result=$?
if [ $result -ne 0 ]; then
    Error
fi

End