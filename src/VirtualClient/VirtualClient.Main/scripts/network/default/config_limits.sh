#!/bin/bash

EXIT_CODE=0
ENABLE_BUSY_POLL=false
EPHEMERAL_PORT_RANGE="10000 60000"
LIMITS_FILE="/etc/security/limits.conf"

Usage() {
    echo ""
    echo "Configures limits and range settings on the local system."
    echo ""
    echo "Options:"
    echo "---------------------"
    echo "--enable-busy-poll     Enables busy polling."
    echo "--port-range           Defines the ephemeral port range."
    echo ""
    echo "Usage:"
    echo "---------------------"
    echo "config_limits.sh [--enable-busy-poll] [--port-range='<start end>']"
    echo ""
    echo "Examples:"
    echo "---------------------"
    echo "config_limits.sh"
    echo "config_limits.sh --port-range='10000 60000'"
    echo "config_limits.sh --enable-busy-poll"
    echo "config_limits.sh --port-range='10000 60000' --enable-busy-poll"
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

# Configure open file descriptor limits
echo ""
echo "Configure '$LIMITS_FILE' settings..."

if grep -q "# VC Settings" "$LIMITS_FILE"; then
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

# Configure sysctl settings for network and file descriptor limits
# https://www.kernel.org/doc/html/latest/networking/ip-sysctl.html
#
echo ""
echo "Configure 'sysctl' settings..."

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
else
    sysctl -w net.core.busy_poll=0 || Error
    sysctl -w net.core.busy_read=0 || Error
fi

Finish