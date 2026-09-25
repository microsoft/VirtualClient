#!/bin/bash

EXIT_CODE=0
SCRIPT_DIR="$(dirname $(readlink -f "${BASH_SOURCE[0]}"))"
OS_PLATFORM=""

# Network configuration flags
DISABLE_FIREWALL=false
ENABLE_BUSY_POLL=false
EPHEMERAL_PORT_RANGE="10000 60000"

Usage() {
    echo ""
    echo "Sets network settings on the local system."
    echo ""
    echo "Options:"
    echo "---------------------"
    echo "--disable-firewall     Disables the network firewall."
    echo "--enable-busy-poll     Enables busy polling."
    echo "--port-range           Defines the ephemeral port range."
    echo ""
    echo "Usage:"
    echo "---------------------"
    echo "config_network.sh [--disable-firewall] [--enable-busy-poll] [--port-range='<start end>']"
    echo ""
    echo "Examples:"
    echo "---------------------"
    echo "./config_network.sh"
    echo "./config_network.sh --port-range='10000 60000'"
    echo "./config_network.sh --disable-firewall"
    echo "./config_network.sh --enable-busy-poll"
    echo "./config_network.sh --port-range='10000 60000' --disable-firewall --enable-busy-poll"
    echo ""
    Finish
}

Error() {
    EXIT_CODE=1
    Finish
}

Finish() {
    echo ""
    echo "Exit Code: $EXIT_CODE"
    echo ""
    exit $EXIT_CODE
}

Set_Platform() {
    # Source the os-release file
    if [ -f /etc/os-release ]; then
        . /etc/os-release
    else
        echo "Error: /etc/os-release not found." >&2
        Error
    fi

    # Convert values to lowercase using Bash parameter expansion (,,)
    # (Sourcing already automatically strips any surrounding quotes)
    os_id="${ID,,}"
    os_version="${VERSION_ID,,}"

    # Set the OS platform
    #
    # e.g.
    # azurelinux-3.0, azurelinux-4.0
    # centos-7, centos-8
    # debian-10, debian-11
    # fedora-34, fedora-35
    # opensuse-leap-16.0, opensuse-42.3
    # rhel-8.10, rhel-9.6, rhel-10.0
    # ubuntu-20.04, ubuntu-22.04, ubuntu-24.04, ubuntu-26.04
    OS_PLATFORM="$os_id-$os_version"
}

Configure_Limits() {
    # If a platform-specific implementation exists, use it (e.g. /network/ubuntu-26.04/config_limits.sh).
    # Otherwise use the default implementation.
    if [ -f "$SCRIPT_DIR/$OS_PLATFORM/config_limits.sh" ]; then
        echo "Config = '<SCRIPT_DIR>/$OS_PLATFORM/config_limits.sh'"
        bash "$SCRIPT_DIR/$OS_PLATFORM/config_limits.sh" --port-range="$EPHEMERAL_PORT_RANGE" --enable-busy-poll=$ENABLE_BUSY_POLL || Error
    else
        echo "Config = '<SCRIPT_DIR>/default/config_limits.sh'"
        bash "$SCRIPT_DIR/default/config_limits.sh" --port-range="$EPHEMERAL_PORT_RANGE" --enable-busy-poll=$ENABLE_BUSY_POLL || Error
    fi
}

Configure_Iptables() {
    # If a platform-specific implementation exists, use it (e.g. /network/ubuntu-18.04/config_iptables.sh).
    # Otherwise use the default implementation.
    if [ -f "$SCRIPT_DIR/$OS_PLATFORM/config_iptables.sh" ]; then
        echo "Config = '<SCRIPT_DIR>/$OS_PLATFORM/config_iptables.sh'"
        bash "$SCRIPT_DIR/$OS_PLATFORM/config_iptables.sh" || Error
    else
        echo "Config = '<SCRIPT_DIR>/default/config_iptables.sh'"
        bash "$SCRIPT_DIR/default/config_iptables.sh" || Error
    fi
}

Configure_Nftables() {
    # If a platform-specific implementation exists, use it (e.g. /network/ubuntu-18.04/config_nftables.sh).
    # Otherwise use the default implementation.
    if [ -f "$SCRIPT_DIR/$OS_PLATFORM/config_nftables.sh" ]; then
        echo "Config = '<SCRIPT_DIR>/$OS_PLATFORM/config_nftables.sh'"
        bash "$SCRIPT_DIR/$OS_PLATFORM/config_nftables.sh" || Error
    else
        echo "Config = '<SCRIPT_DIR>/default/config_nftables.sh'"
        bash "$SCRIPT_DIR/default/config_nftables.sh" || Error
    fi
}

Disable_Firewalld() {
    # Check if firewalld service exists/is installed
    if systemctl list-unit-files firewalld.service | grep -q firewalld.service; then
        echo "Stop 'firewalld' service..."

        # Stop firewalld
        sudo systemctl stop firewalld
    fi
}

# Parse arguments
# Supports the following patterns for boolean flags:
# --disable-network
# --disable-network=true
# --disable-network="true"
#
while [[ $# -gt 0 ]]; do
    case "${1,,}" in
        "/?"|"-?"|"--help")
            Usage
            ;;
        --disable-firewall|--disable-firewall=*)
            # Extract value after '=' (if present)
            DISABLE="${1#*=}"

            # If no value was provided, treat it as true
            # (i.e. used as a flag -> --disable-firewall)
            if [[ "$DISABLE" == "$1" ]]; then
                DISABLE_FIREWALL=true
            else
                DISABLE="${DISABLE,,}"
                DISABLE="${DISABLE//[\"\']/}" # Strip single and double quotes

                if [[ "$DISABLE" == "true" ]]; then
                    DISABLE_FIREWALL=true
                fi
            fi
            ;;
        --enable-busy-poll|--enable-busy-poll=*)
            # Extract value after '=' (if present)
            ENABLE="${1#*=}"

            # If no value was provided, treat it as true
            # (i.e. used as a flag -> --enable-busy-poll)
            if [[ "$ENABLE" == "$1" ]]; then
                ENABLE_BUSY_POLL=true
            else
                ENABLE="${ENABLE,,}"
                ENABLE="${ENABLE//[\"\']/}" # Strip single and double quotes

                if [[ "$ENABLE" == "true" ]]; then
                    ENABLE_BUSY_POLL=true
                fi
            fi
            ;;
        --port-range=*)
            # Extract everything after '='
            EPHEMERAL_PORT_RANGE="${1#*=}"
            EPHEMERAL_PORT_RANGE="${EPHEMERAL_PORT_RANGE//[\"\']/}" # Strip single and double quotes

            # Split into two numbers
            PORT_RANGE_START=$(echo "$EPHEMERAL_PORT_RANGE" | awk '{print $1}')
            PORT_RANGE_END=$(echo "$EPHEMERAL_PORT_RANGE" | awk '{print $2}')

            # Validate both values exist
            if [[ -z "$PORT_RANGE_START" || -z "$PORT_RANGE_END" ]]; then
                echo "ERROR: --port-range requires two values, e.g. --port-range='10000 60000'"
                Error
            fi
            ;;
        *)
            echo "Unknown option: $1"
            Usage
            ;;
    esac
    shift
done

echo ""
echo "**********************************************************************"
echo "CONFIGURE NETWORK"
echo ""
echo "Disable Firewall     : $DISABLE_FIREWALL"
echo "Enable Busy Poll     : $ENABLE_BUSY_POLL"
echo "Ephemeral Port Range : $EPHEMERAL_PORT_RANGE"
echo "Script Directory     : $SCRIPT_DIR"
echo "**********************************************************************"

Set_Platform

echo ""
echo "-------------------------------"
echo "DETERMINE PLATFORM"
echo "-------------------------------"
echo "Platform = $OS_PLATFORM"

echo ""
echo "-------------------------------"
echo "SET LIMITS"
echo "-------------------------------"
Configure_Limits

# Network firewall settings
if [[ "$DISABLE_FIREWALL" == true ]]; then
    echo ""
    echo "-------------------------------"
    echo "SET FIREWALL RULES"
    echo "-------------------------------"
    Disable_Firewalld

    if command -v nft &> /dev/null; then
        Configure_Nftables
    elif command -v iptables &> /dev/null; then
        Configure_Iptables
    fi
fi

Finish