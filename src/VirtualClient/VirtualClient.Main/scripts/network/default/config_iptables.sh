#!/bin/bash

EXIT_CODE=0

Usage() {
    echo ""
    echo "Configures 'iptables' settings on the local system."
    echo ""
    echo "Usage:"
    echo "---------------------"
    echo "config_iptables.sh"
    echo ""
    Finish
}

Error() {
    EXIT_CODE=1
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
        *)
            echo "Unknown option: $1"
            Usage
            ;;
    esac
    shift
done

# https://linux.die.net/man/8/iptables
# flush firewall settings
iptables --flush || Error

# disable connection tracking
iptables -t raw -I OUTPUT -j NOTRACK || Error
iptables -t raw -I PREROUTING -j NOTRACK || Error

# accept all inbound, outbound and forwarding traffic
iptables -P INPUT ACCEPT || Error
iptables -P OUTPUT ACCEPT || Error
iptables -P FORWARD ACCEPT || Error

iptables -S
echo ""
iptables -t raw -L -v -n
echo ""
iptables -L -v -n

Finish