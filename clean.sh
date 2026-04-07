#!/bin/bash

EXIT_CODE=0
SCRIPT_DIR="$(dirname $(readlink -f "${BASH_SOURCE}"))"

Usage() {
    echo ""
    echo "Deletes build artifacts from the repo."
    echo ""
    echo "Usage:"
    echo "---------------------"
    echo "clean.sh"
    echo ""
    echo "Examples"
    echo "---------------------"
    echo "user@system:~/repo$ chmod +x *.sh"
    echo "user@system:~/repo$ ./clean.sh"
    echo ""
    Finish
}

Error() {
    EXIT_CODE=1
    End
}

End() {
    echo ""
    echo "Clean Stage Exit Code: $EXIT_CODE"
    echo ""
    Finish
}

Finish() {
    exit $EXIT_CODE
}

if [ "${1,,}" == "/?" ] || [ "${1,,}" == "-?" ] || [ "${1,,}" == "--help" ]; then
    Usage
fi

echo ""

for dir in $SCRIPT_DIR/out/*/
do
    echo "Clean: $dir"
    rm -rfd $dir

    result=$?
    if [ $result -ne 0 ]; then
        Error
    fi
done

for dir in $(find $SCRIPT_DIR/src -type d | grep obj$)/
do
    echo "Clean: $dir"
    rm -rfd $dir

     result=$?
     if [ $result -ne 0 ]; then
        Error
     fi
done

End