#!/bin/bash

EXIT_CODE=0
SCRIPT_DIR="$(dirname $(readlink -f "${BASH_SOURCE}"))"
BUILD_CONFIGURATION=""
BUILD_VERSION=""
PACKAGE_SUFFIX=""
SUFFIX_FOUND=""

Usage() {
    echo ""
    echo "Builds packages from the artifacts/output of the build process."
    echo ""
    echo "Options:"
    echo "---------------------"
    echo "--suffix  - Defines a suffix to place on the package names. Valid values include: alpha, beta."
    echo ""
    echo "Usage:"
    echo "---------------------"
    echo "build-packages.sh [--suffix <alpha|beta>]"
    echo ""
    echo "Examples"
    echo "---------------------"
    echo "# Use defaults"
    echo "user@system:~/repo$ chmod +x *.sh"
    echo "user@system:~/repo$ ./build.sh"
    echo "user@system:~/repo$ ./build-packages.sh"
    echo ""
    echo "# Set package suffix"
    echo "user@system:~/repo$ chmod +x *.sh"
    echo "user@system:~/repo$ ./build.sh"
    echo "user@system:~/repo$ ./build-packages.sh --suffix beta"
    echo ""
    echo "# Set specific version and configuration"
    echo "user@system:~/repo$ export VCBuildVersion=\"1.16.25\""
    echo "user@system:~/repo$ export VCBuildConfiguration=\"Debug\""
    echo "user@system:~/repo$ chmod +x *.sh"
    echo "user@system:~/repo$ ./build.sh"
    echo "user@system:~/repo$ ./build-packages.sh"
    echo ""
    Finish
}

Error() {
    EXIT_CODE=1
    End
}

End() {
    echo ""
    echo "Packaging Stage Exit Code: $EXIT_CODE"
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

for ((i=1; i<=$#; i++)); do
    arg="${!i}"

    # alpha or beta
    if [ "$arg" != "--suffix" ]; then
        PACKAGE_SUFFIX=$(echo "$arg" | sed 's/^-*//')
    fi
done

# The default build version is defined in the repo VERSION file.
BUILD_VERSION=$(cat "$SCRIPT_DIR/VERSION")

# The default build version can be overridden by the 'VCBuildVersion' 
# environment variable
if [[ -v "VCBuildVersion" && -n "$VCBuildVersion" ]]; then
    BUILD_VERSION="$VCBuildVersion"
fi

# The default build configuration is 'Release'.
BUILD_CONFIGURATION="Release"

# The default build configuration (e.g. Release) can be overridden 
# by the 'VCBuildConfiguration' environment variable
if [[ -v "VCBuildConfiguration" && -n "$VCBuildConfiguration" ]]; then
    BUILD_CONFIGURATION=$VCBuildConfiguration
fi

PACKAGE_VERSION=$BUILD_VERSION
if [ "$PACKAGE_SUFFIX" != "" ]; then 
    PACKAGE_VERSION="$BUILD_VERSION-$PACKAGE_SUFFIX"
fi

echo ""
echo "**********************************************************************"
echo "Build Version   : $BUILD_VERSION"
echo "Repo Root       : $SCRIPT_DIR"
echo "Configuration   : $BUILD_CONFIGURATION"
echo "Package Version : $PACKAGE_VERSION"
echo "**********************************************************************"
echo""

PACKAGES_PROJECT_DIR="$SCRIPT_DIR/src/VirtualClient/VirtualClient.Packaging"
PACKAGES_PROJECT="$SCRIPT_DIR/src/VirtualClient/VirtualClient.Packaging/VirtualClient.Packaging.csproj"

# The packages project itself is not meant to produce a binary/.dll and thus is not built. However, to ensure
# the requisite NuGet package assets file exist in the local 'obj' folder, we need to perform a restore.
dotnet restore "$PACKAGES_PROJECT" --force
result=$?
if [ $result -ne 0 ]; then
    Error
fi

echo ""
echo "[Create NuGet Package] VirtualClient.linux-arm64.$PACKAGE_VERSION"
echo "----------------------------------------------------------"
dotnet pack "$PACKAGES_PROJECT" --force --no-restore --no-build -c $BUILD_CONFIGURATION \
-p:Version=$PACKAGE_VERSION  -p:NuspecFile="$PACKAGES_PROJECT_DIR/nuspec/VirtualClient.linux-arm64.nuspec"

result=$?
if [ $result -ne 0 ]; then
    Error
fi

echo ""
echo "[Create NuGet Package] VirtualClient.linux-x64.$PACKAGE_VERSION"
echo "----------------------------------------------------------"
dotnet pack "$PACKAGES_PROJECT" --force --no-restore --no-build -c $BUILD_CONFIGURATION \
-p:Version=$PACKAGE_VERSION  -p:NuspecFile="$PACKAGES_PROJECT_DIR/nuspec/VirtualClient.linux-x64.nuspec"

result=$?
if [ $result -ne 0 ]; then
    Error
fi

echo ""
echo "[Create NuGet Package] VirtualClient.win-arm64.$PACKAGE_VERSION"
echo "----------------------------------------------------------"
dotnet pack "$PACKAGES_PROJECT" --force --no-restore --no-build -c $BUILD_CONFIGURATION \
-p:Version=$PACKAGE_VERSION  -p:NuspecFile="$PACKAGES_PROJECT_DIR/nuspec/VirtualClient.win-arm64.nuspec"

result=$?
if [ $result -ne 0 ]; then
    Error
fi

echo ""
echo "[Create NuGet Package] VirtualClient.win-x64.$PACKAGE_VERSION"
echo "----------------------------------------------------------"
dotnet pack "$PACKAGES_PROJECT" --force --no-restore --no-build -c $BUILD_CONFIGURATION \
-p:Version=$PACKAGE_VERSION  -p:NuspecFile="$PACKAGES_PROJECT_DIR/nuspec/VirtualClient.win-x64.nuspec"

result=$?
if [ $result -ne 0 ]; then
    Error
fi

echo ""
echo "[Create NuGet Package] VirtualClient.Framework.$PACKAGE_VERSION"
echo "----------------------------------------------------------"
dotnet pack "$PACKAGES_PROJECT" --force --no-restore --no-build -c $BUILD_CONFIGURATION \
-p:Version=$PACKAGE_VERSION  -p:NuspecFile="$PACKAGES_PROJECT_DIR/nuspec/VirtualClient.Framework.nuspec"

result=$?
if [ $result -ne 0 ]; then
    Error
fi

echo ""
echo "[Create NuGet Package] VirtualClient.TestFramework.$PACKAGE_VERSION"
echo "----------------------------------------------------------"
dotnet pack "$PACKAGES_PROJECT" --force --no-restore --no-build -c $BUILD_CONFIGURATION \
-p:Version=$PACKAGE_VERSION  -p:NuspecFile="$PACKAGES_PROJECT_DIR/nuspec/VirtualClient.TestFramework.nuspec"

result=$?
if [ $result -ne 0 ]; then
    Error
fi

End