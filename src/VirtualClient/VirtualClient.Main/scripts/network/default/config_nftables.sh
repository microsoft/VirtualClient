#!/bin/bash

EXIT_CODE=0

Usage() {
    echo ""
    echo "Configures 'nftables' settings on the local system."
    echo ""
    echo "Usage:"
    echo "---------------------"
    echo "config_nftables.sh"
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

nft -f - <<'EOF' || Error
flush ruleset

table inet firewall {
    chain raw_prerouting {
        type filter hook prerouting priority raw; policy accept;
        counter notrack
    }

    chain raw_output {
        type filter hook output priority raw; policy accept;
        counter notrack
    }

    chain filter_input {
        type filter hook input priority filter; policy accept;
        counter accept
    }

    chain filter_forward {
        type filter hook forward priority filter; policy accept;
        counter accept
    }

    chain filter_output {
        type filter hook output priority filter; policy accept;
        counter accept
    }
}
EOF

echo ""
nft list ruleset

Finish