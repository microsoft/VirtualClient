#!/bin/bash

ExitCode=0
SCRIPT_DIR="$(dirname $(readlink -f "${BASH_SOURCE}"))"
BUILD_CONFIGURATION="Debug"

Usage() {
    echo ""
    echo "Discovers and runs tests (unit + functional) defined in the build output/artifacts."
    echo ""
    echo "Usage:"
    echo "---------------------"
    echo "build-test.sh"
    echo ""
    echo "Examples"
    echo "---------------------"
    echo "# Use defaults"
    echo "user@system:~/repo$ chmod +x *.sh"
    echo "user@system:~/repo$ ./build.sh"
    echo "user@system:~/repo$ ./build-test.sh"
    echo ""
    echo "# Set specific version and configuration"
    echo "user@system:~/repo$ export VCBuildVersion=\"1.16.25\""
    echo "user@system:~/repo$ export VCBuildConfiguration=\"Debug\""
    echo "user@system:~/repo$ ./build.sh"
    echo "user@system:~/repo$ ./build-test.sh"
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
    dotnet test -c Debug "$file" --no-restore --no-build --filter "(Category=Unit)" --logger "console;verbosity=normal"
    result=$?
    if [ $result -ne 0 ]; then
        Error
    fi
done

End
