#!/bin/bash

if [ "${1,,}" == "/?" ] || [ "${1,,}" == "-?" ] || [ "${1,,}" == "--help" ]; then
    Usage
fi

ExitCode=0

echo ""
echo "[Restoring NuGet Packages]"
echo "--------------------------------------------------"
dotnet restore "$(dirname "$0")/src/VirtualClient/VirtualClient.sln" "$1"
result=$?
if [ $result -ne 0 ]; then
    Error
fi

End

Usage() {
    echo ""
    echo "Usage:"
    echo "---------------------"
    echo "$0 [--interactive]"
    echo ""
    echo "Examples:"
    echo "---------------------"
    echo "# Restore NuGet packages for all projects"
    echo "$0"
    echo ""
    echo "# Restore, allowing user to provide credentials"
    echo "$0 --interactive"
    Finish
}

Error() {
    ExitCode=$?
    End
}

End() {
    echo "Restore Stage Exit Code: $ExitCode"
    Finish
}

Finish() {
    exit $ExitCode
}
