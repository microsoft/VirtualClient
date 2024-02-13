#!/bin/bash

ExitCode=0

Usage() {
    echo ""
    echo "Usage:"
    echo "---------------------"
    echo "$0"
    Finish
}

Error() {
    ExitCode=$?
    End
}

End() {
    echo "Test Stage Exit Code: $ExitCode"
    Finish
}

Finish() {
    exit $ExitCode
}

if [ "${1,,}" == "/?" ] || [ "${1,,}" == "-?" ] || [ "${1,,}" == "--help" ]; then
    Usage
fi

echo ""
echo "[Running Tests]"
echo "--------------------------------------------------"

for file in $(find "$(dirname "$0")/src" -type f -name "*Tests.csproj"); do
    test_project="${file%.*}.dll"  # Change the file extension from .csproj to .dll
    dotnet test -c Release "$test_project" --no-restore --no-build --filter "(Category=Unit|Category=Functional)" --logger "console;verbosity=normal"
    result=$?
    if [ $result -ne 0 ]; then
        Error
    fi
done

End
