#!/bin/bash

ExitCode=0
SCRIPT_DIR="$(dirname $(readlink -f "${BASH_SOURCE}"))"
BUILD_CONFIGURATION="Release"

Usage() {
    echo ""
    echo "Discovers and runs tests (unit + functional) defined in the build output/artifacts."
    echo ""
    echo "Options:"
    echo "---------------------"
    echo "--debug  - Flag requests tests for build configuration 'Debug' vs. the default 'Release'."
    echo ""
    echo "Usage:"
    echo "---------------------"
    echo "build-test.sh [--debug]"
    echo ""
    echo "Examples"
    echo "---------------------"
    echo "user@system:~/repo$ chmod +x *.sh"
    echo "user@system:~/repo$ ./build-test.sh"
    echo ""
    echo "user@system:~/repo$ chmod +x *.sh"
    echo "user@system:~/repo$ ./build-test.sh --debug"
    echo ""
    echo "user@system:~/repo$ export VCBuildVersion=\"1.16.25\""
    echo "user@system:~/repo$ ./build-test.sh --debug"
    echo ""
    Finish
}

Error() {
    ExitCode=1
    End
}

End() {
    echo "Test Stage Exit Code: $ExitCode"
    Finish
}

Finish() {
    exit $ExitCode
}

for ((i=1; i<=$#; i++)); do
    arg="${!i}"
  
    if [ "$arg" == "/?" ] || [ "$arg" == "-?" ] || [ "$arg" == "--help" ]; then
        Usage
    fi
done

for ((i=1; i<=$#; i++)); do
    arg="${!i}"

    # Build Configurations:
    # 1) Release (Default)
    # 2) Debug
    #
    # Pass in the --debug flag to use 'Debug' build configuration
    if [ "$arg" == "--debug" ]; then
        BUILD_CONFIGURATION="Debug"
    fi
done

echo ""
echo "**********************************************************************"
echo "Repo Root       : $SCRIPT_DIR"
echo "Configuration   : $BUILD_CONFIGURATION"
echo "Tests Directory : $SCRIPT_DIR/out/bin/$BUILD_CONFIGURATION"
echo "**********************************************************************"

echo ""
echo "[Running Tests]"
echo "--------------------------------------------------"

for file in $(find "$(dirname "$0")/src" -type f -name "*Tests.csproj"); do
    dotnet test -c $BUILD_CONFIGURATION "$file" --no-restore --no-build --filter "(Category=Unit)" --logger "console;verbosity=normal"
    result=$?
    if [ $result -ne 0 ]; then
        Error
    fi
done

End
