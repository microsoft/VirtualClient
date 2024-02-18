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

if [ "${1,,}" == "/?" ] || [ "${1,,}" == "-?" ] || [ "${1,,}" == "--help" ]; then
    Usage
fi

echo ""
echo "[Running Tests]"
echo "--------------------------------------------------"

for file in $(find "$(dirname "$0")/src" -type f -name "*Tests.csproj"); do
    dotnet test -c Release "$file" --no-restore --no-build --filter "(Category=Unit)" --logger "console;verbosity=normal"
    result=$?
    if [ $result -ne 0 ]; then
        Error
    fi
done

End
