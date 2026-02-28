#!/bin/bash

EXIT_CODE=0
SCRIPT_DIR="$(dirname $(readlink -f "${BASH_SOURCE[0]}"))"

# Network configuration flags
DISABLE_FIREWALL=false
ENABLE_BUSY_POLL=false
NO_FILE_LIMIT=1048575
EPHEMERAL_PORT_RANGE="10000 60000"
LIMITS_FILE="/etc/security/limits.conf"
SEARCH_TERM="# VC Settings Begin"

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
    echo "./config_network.sh [--disable-firewall] [--enable-busy-poll] [--port-range=<start end>]"
    echo ""
    echo "Examples:"
    echo "---------------------"
    echo "./config_network.sh"
    echo "./config_network.sh --port-range=\"10000 60000\""
    echo "./config_network.sh --disable-firewall"
    echo "./config_network.sh --enable-busy-poll"
    echo "./config_network.sh --port-range=\"10000 60000\" --disable-firewall --enable-busy-poll"
    echo ""
    Finish
}

Error() {
    EXIT_CODE=1
    End
}

End() {
    echo ""
    echo "Exit Code: $EXIT_CODE"
    echo ""
    Finish
}

Finish() {
    exit $EXIT_CODE
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
                # Normalize quotes and lowercase
                DISABLE="${DISABLE,,}"
                DISABLE="${DISABLE%\"}"
                DISABLE="${DISABLE#\"}"

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
                # Normalize quotes and lowercase
                ENABLE="${ENABLE,,}"
                ENABLE="${ENABLE%\"}"
                ENABLE="${ENABLE#\"}"

                if [[ "$ENABLE" == "true" ]]; then
                    ENABLE_BUSY_POLL=true
                fi
            fi
            ;;
        --port-range=*)
            # Extract everything after '='
            EPHEMERAL_PORT_RANGE="${1#*=}"

            # Split into two numbers
            PORT_RANGE_START=$(echo "$EPHEMERAL_PORT_RANGE" | awk '{print $1}')
            PORT_RANGE_END=$(echo "$EPHEMERAL_PORT_RANGE" | awk '{print $2}')

            # Validate both values exist
            if [[ -z "$PORT_RANGE_START" || -z "$PORT_RANGE_END" ]]; then
                echo "ERROR: --port-range requires two values, e.g. --port-range=\"10000 60000\""
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
echo "CONFIGURE NETWORK"
echo "**********************************************************************"
echo "Disable Firewall     : $DISABLE_FIREWALL"
echo "Enable Busy Poll     : $ENABLE_BUSY_POLL"
echo "Ephemeral Port Range : $EPHEMERAL_PORT_RANGE"
echo "Script Directory     : $SCRIPT_DIR"
echo "**********************************************************************"
echo ""

echo "-------------------------------"
echo "SET /etc/security/limits.conf"
echo "-------------------------------"
# 1. Check if the marker already exists in the file
if grep -q "$SEARCH_TERM" "$LIMITS_FILE"; then
    echo "Settings already exist..."
else
    echo "Apply settings to '$LIMITS_FILE'."

    # 2. Append the block to the end of the file using sudo and tee
    # The -a flag in tee stands for 'append'
    sudo tee -a "$LIMITS_FILE" > /dev/null <<"EOF"
# VC Settings Begin
* soft    nofile    1048575
* hard    nofile    1048575
# VC Settings End
EOF

fi

echo ""
echo "-------------------------------"
echo "SET SYSCTL SETTINGS"
echo "-------------------------------"
# https://www.kernel.org/doc/html/latest/networking/ip-sysctl.html
#
# increase the maximum number of file descriptors.
sysctl -w fs.file-max=1048575 || Error

# TIME_WAIT work-around
sysctl -w net.ipv4.tcp_tw_reuse=1 || Error

# increase ephemeral ports
sysctl -w net.ipv4.ip_local_port_range="$EPHEMERAL_PORT_RANGE" || Error

# disable SYN cookies (for network workloads)
sysctl -w net.ipv4.tcp_syncookies=0 || Error

# increase SYN backlog (for network workloads)
sysctl -w net.ipv4.tcp_max_syn_backlog=2048 || Error

# disable reverse path filtering (for network workloads)
sysctl -w net.ipv4.conf.all.rp_filter=0 || Error

# disable connection tracking
sysctl -w net.netfilter.nf_conntrack_max=0 || Error

# Busy poll settings
if [[ "$ENABLE_BUSY_POLL" == true ]]; then
     sysctl -w net.core.busy_poll=50 || Error
     sysctl -w net.core.busy_read=50 || Error
fi

# Network firewall settings
if [[ "$DISABLE_FIREWALL" == true ]]; then
    echo ""
    echo "-------------------------------"
    echo "SET FIREWALL RULES"
    echo "-------------------------------"

    nft flush ruleset
    systemctl restart nftables

     # https://linux.die.net/man/8/iptables
     # disable connection tracking
     iptables -t raw -I OUTPUT -j NOTRACK || Error
     iptables -t raw -I PREROUTING -j NOTRACK || Error

     # accept all inbound, outbound and forwarding traffic
     iptables -P INPUT ACCEPT || Error
     iptables -P OUTPUT ACCEPT || Error
     iptables -P FORWARD ACCEPT || Error

     # flush/accept new firewall settings
     iptables --flush || Error

     iptables -S
     echo ""
     iptables -t raw -L -v -n
     echo ""
     iptables -L -v -n
fi

End