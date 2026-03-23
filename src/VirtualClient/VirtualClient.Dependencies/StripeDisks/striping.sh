#!/bin/bash
set -euo pipefail

# =============================================================================
# Disk Striping (RAID 0) Script
# Combines listdisks.py and striping.py functionality into a single bash script.
#
# Filter Disk Types:
#   1 = OnlySameModelAsOSDisk
#   2 = OnlyDifferentModelOfOSDisk (default)
#   3 = NoOSDisk (all disks except the OS disk)
#   4 = AllDisks
# =============================================================================

DEVICE_PATH="/dev/md0"
SIZE_GREATER_THAN=256
DISK_COUNT=0
MOUNT_DIRECTORY="/mnt/raid0"
FSTYPE="ext4"
FILTER_DISK_TYPE=2  # OnlyDifferentModelOfOSDisk
REMOVE=false
REMOVE_QUIT=false

# --- Disk listing functions ---

get_os_disk_partition() {
    # Find the device backing the root filesystem
    local root_dev
    root_dev=$(findmnt -n -o SOURCE / 2>/dev/null || true)
    if [[ -z "$root_dev" ]]; then
        root_dev=$(df / 2>/dev/null | awk 'NR==2{print $1}')
    fi
    echo "$root_dev"
}

get_os_disk() {
    # Resolve the partition device to its parent whole-disk device
    local part_dev
    part_dev=$(get_os_disk_partition)
    if [[ -z "$part_dev" ]]; then
        echo ""
        return
    fi

    # Resolve symlinks (e.g. /dev/mapper/... -> /dev/sdX)
    part_dev=$(readlink -f "$part_dev")

    # Strip partition number to get the whole disk: /dev/sda1 -> /dev/sda, /dev/nvme0n1p1 -> /dev/nvme0n1
    local disk
    disk=$(lsblk -no PKNAME "$part_dev" 2>/dev/null | head -1)
    if [[ -n "$disk" ]]; then
        echo "/dev/$disk"
    else
        echo ""
    fi
}

get_os_disk_model() {
    local os_disk
    os_disk=$(get_os_disk)
    if [[ -z "$os_disk" ]]; then
        echo ""
        return
    fi
    local disk_name
    disk_name=$(basename "$os_disk")
    # Try udevadm first
    local model
    model=$(udevadm info --query=property --name="$os_disk" 2>/dev/null | grep '^ID_MODEL=' | cut -d= -f2)
    if [[ -z "$model" ]]; then
        # Fallback to /sys
        model=$(cat "/sys/block/$disk_name/device/model" 2>/dev/null | xargs)
    fi
    echo "$model"
}

get_disk_size_gb() {
    # Returns disk size in GB for a given /dev/xxx device
    local dev="$1"
    local disk_name
    disk_name=$(basename "$dev")
    local size_sectors
    size_sectors=$(cat "/sys/block/$disk_name/size" 2>/dev/null || echo 0)
    # size is in 512-byte sectors
    echo "$size_sectors" | awk '{printf "%.2f", ($1 * 512) / (1024^3)}'
}

get_disk_model() {
    local dev="$1"
    local model
    model=$(udevadm info --query=property --name="$dev" 2>/dev/null | grep '^ID_MODEL=' | cut -d= -f2)
    if [[ -z "$model" ]]; then
        local disk_name
        disk_name=$(basename "$dev")
        model=$(cat "/sys/block/$disk_name/device/model" 2>/dev/null | xargs)
    fi
    echo "$model"
}

list_disks() {
    # Usage: list_disks <min_size_gb> <diskcount> <filter_type>
    # Outputs eligible disk device paths, one per line
    local min_size_gb="${1:-256}"
    local diskcount="${2:-0}"
    local filter_type="${3:-4}"  # default AllDisks

    local os_disk=""
    local os_disk_model=""

    if [[ "$filter_type" -eq 4 ]]; then
        echo "Listing all disks..." >&2
    else
        case "$filter_type" in
            1) echo "Filtering disks by model: OnlySameModelAsOSDisk" >&2 ;;
            2) echo "Filtering disks by model: OnlyDifferentModelOfOSDisk" >&2 ;;
            3) echo "Filtering disks: NoOSDisk" >&2 ;;
        esac

        os_disk=$(get_os_disk)
        if [[ -z "$os_disk" ]]; then
            echo "No OS disk found." >&2
            return
        fi

        os_disk_model=$(get_os_disk_model)
        if [[ -n "$os_disk_model" ]]; then
            echo "OS Disk Model: $os_disk_model" >&2
        else
            echo "OS Disk Model: Not found" >&2
            return
        fi
    fi

    local disks=()

    for dev in /sys/block/*; do
        local dev_name
        dev_name=$(basename "$dev")
        local dev_path="/dev/$dev_name"

        # Only consider whole disks (skip partitions, loop, ram, etc.)
        local dev_type
        dev_type=$(cat "$dev/queue/rotational" 2>/dev/null || echo "skip")
        if [[ "$dev_type" == "skip" ]]; then
            continue
        fi

        # Check it's a real disk (not loop, ram, etc.)
        case "$dev_name" in
            loop*|ram*|dm-*|sr*|fd*) continue ;;
        esac

        local size_gb
        size_gb=$(get_disk_size_gb "$dev_path")

        # Skip disks smaller than 1 GB or min_size_gb
        if (( $(echo "$size_gb < 1" | bc -l) )); then
            continue
        fi
        if (( $(echo "$size_gb < $min_size_gb" | bc -l) )); then
            continue
        fi

        # Skip OS disk (for all filter types except AllDisks)
        if [[ -n "$os_disk" && "$dev_path" == "$os_disk" ]]; then
            continue
        fi

        # Filter by model comparison
        if [[ "$filter_type" -ne 4 ]]; then
            local disk_model
            disk_model=$(get_disk_model "$dev_path")

            if [[ "$filter_type" -eq 1 && "$disk_model" != "$os_disk_model" ]]; then
                continue
            fi
            if [[ "$filter_type" -eq 2 && "$disk_model" == "$os_disk_model" ]]; then
                continue
            fi
            # filter_type 3 (NoOSDisk): already skipped OS disk above, accept all others
        fi

        disks+=("$dev_path")
    done

    # If diskcount > 0 and we have more disks, sort by size descending and take top N
    if [[ "$diskcount" -gt 0 && "${#disks[@]}" -gt "$diskcount" ]]; then
        local sorted
        sorted=$(for d in "${disks[@]}"; do
            echo "$(get_disk_size_gb "$d") $d"
        done | sort -rn | head -n "$diskcount" | awk '{print $2}')
        echo "$sorted"
    else
        for d in "${disks[@]}"; do
            echo "$d"
        done
    fi
}

# --- RAID / striping functions ---

print_virtual_disk_info() {
    local mount_point="${1:-/dev/md0}"
    echo "Listing virtual disks:"
    if ! lsblk "$mount_point" -o NAME,TYPE,SIZE 2>/dev/null; then
        echo "Error listing virtual disks or no virtual disks found."
    fi
}

create_filesystem() {
    local device="${1:-/dev/md0}"
    local fstype="${2:-ext4}"
    local mount_dir="${3:-/mnt/raid0}"

    echo "Create file system at $device with type $fstype and mount it at $mount_dir."

    if [[ -z "$device" || -z "$fstype" ]]; then
        echo "Error: Device and filesystem type must be specified." >&2
        exit 1
    fi
    if [[ -z "$mount_dir" ]]; then
        echo "Error: Mount directory must be specified." >&2
        exit 1
    fi

    sudo "mkfs.$fstype" "$device"
    echo "$fstype filesystem created successfully on $device."

    sudo mkdir -p "$mount_dir"
    sudo mount "$device" "$mount_dir"
    echo "$device mounted at $mount_dir."
}

create_raid0() {
    # Usage: create_raid0 <mount_point> <device1> <device2> [...]
    local mount_point="$1"
    shift
    local devices=("$@")

    echo "Creating RAID 0 array with devices: ${devices[*]}"

    if [[ "${#devices[@]}" -eq 0 ]]; then
        echo "Error: No devices provided for RAID 0 array." >&2
        exit 1
    fi

    if [[ "${#devices[@]}" -lt 2 ]]; then
        echo "Error: At least two devices are required to create a RAID 0 array." >&2
        exit 1
    fi

    local cmd=(sudo mdadm --create "$mount_point" --level=0 "--raid-devices=${#devices[@]}" "${devices[@]}")
    echo "Running: ${cmd[*]}"
    "${cmd[@]}"

    echo "RAID 0 array created successfully at $mount_point with devices: ${devices[*]}"
    print_virtual_disk_info "$mount_point"
}

is_device_in_raid() {
    local device="$1"
    local output
    output=$(sudo mdadm --examine "$device" 2>&1) || return 1

    if echo "$output" | grep -q "No md superblock detected"; then
        return 1
    fi
    if echo "$output" | grep -qE "Raid Level|Array UUID"; then
        return 0
    fi
    return 1
}

get_device_paths() {
    local min_size_gb="${1:-256}"
    local diskcount="${2:-0}"
    local filter_type="${3:-2}"

    local all_disks
    all_disks=$(list_disks "$min_size_gb" "$diskcount" "$filter_type")

    if [[ -z "$all_disks" ]]; then
        echo "Error: No suitable disks found for RAID 0." >&2
        exit 1
    fi

    local eligible=()
    while IFS= read -r dev; do
        if is_device_in_raid "$dev"; then
            echo "Warning: $dev is already part of a RAID array. It will not be used for RAID 0." >&2
        else
            eligible+=("$dev")
        fi
    done <<< "$all_disks"

    echo "Disks to be used for RAID 0: ${eligible[*]}" >&2
    for d in "${eligible[@]}"; do
        echo "$d"
    done
}

get_raid0_list_disks() {
    local mount_point="${1:-/dev/md0}"
    echo "Getting list of disks in RAID 0 array at $mount_point..." >&2

    local output
    output=$(sudo mdadm --detail "$mount_point" 2>&1)
    if [[ $? -ne 0 ]]; then
        echo "Failed to get details of RAID array $mount_point: $output" >&2
        return 1
    fi

    echo "$output" | while IFS= read -r line; do
        line=$(echo "$line" | xargs)
        # Skip lines containing the mount_point itself as a label
        if echo "$line" | grep -q "$mount_point"; then
            continue
        fi
        # Extract /dev/* paths
        if echo "$line" | grep -qo '/dev/[^ ]*'; then
            echo "$line" | grep -o '/dev/[^ ]*'
        fi
    done
}

clear_superblock() {
    local disks=("$@")
    echo "Clearing superblocks from disks: ${disks[*]}"
    if sudo mdadm --zero-superblock "${disks[@]}"; then
        echo "Superblocks cleared successfully."
    else
        echo "Failed to clear superblocks." >&2
        exit 1
    fi
}

clear_raid0() {
    local mount_point="${1:-/dev/md0}"
    echo "Clearing existing RAID 0 array at $mount_point..."

    local raid_disk_paths=()
    local detail_output
    if ! detail_output=$(get_raid0_list_disks "$mount_point" 2>&1); then
        echo "$detail_output"
        echo "Clearing superblock from all disks..."
        local all_disks
        all_disks=$(list_disks 0 0 4)
        if [[ -n "$all_disks" ]]; then
            local disk_arr=()
            while IFS= read -r d; do disk_arr+=("$d"); done <<< "$all_disks"
            clear_superblock "${disk_arr[@]}"
        fi
        return
    fi

    # Read disk paths from detail output
    while IFS= read -r d; do
        [[ -n "$d" ]] && raid_disk_paths+=("$d")
    done <<< "$(get_raid0_list_disks "$mount_point")"

    if sudo mdadm --stop "$mount_point" && sudo mdadm --remove "$mount_point" 2>/dev/null; then
        echo "RAID 0 array at $mount_point cleared successfully."
    else
        echo "Failed to clear RAID 0 array at $mount_point. Ensure it is not in use." >&2
    fi

    if [[ "${#raid_disk_paths[@]}" -gt 0 ]]; then
        clear_superblock "${raid_disk_paths[@]}"
    fi
}

# --- Usage / help ---

usage() {
    cat <<EOF
Usage: $(basename "$0") [OPTIONS]

Disk striping (RAID 0) utility.

Options:
  --devicePath PATH       RAID device path for the striped volume (default: /dev/md0)
  --sizeGreaterThan GB    Minimum disk size in GB for striping (default: 256)
  --diskCount N           Number of disks to use for striping; 0 means all (default: 0)
  --mountDirectory DIR    Directory to mount the filesystem (default: /mnt/raid0)
  --fstype TYPE           Filesystem type (default: ext4)
  --onlylocaldisk         Search only for local disks (same model as OS disk)
  --remove                Remove an existing RAID 0 array before creating a new one
  --remove-quit           Remove an existing RAID 0 array and quit
  --help                  Show this help message

Filter Disk Types:
  By default, disks with a DIFFERENT model than the OS disk are selected (remote/data disks).
  Use --onlylocaldisk to select disks with the SAME model as the OS disk instead.
EOF
    exit 0
}

# --- Argument parsing ---

while [[ $# -gt 0 ]]; do
    case "$1" in
        --devicePath)       DEVICE_PATH="$2"; shift 2 ;;
        --sizeGreaterThan)  SIZE_GREATER_THAN="$2"; shift 2 ;;
        --diskCount)        DISK_COUNT="$2"; shift 2 ;;
        --mountDirectory)   MOUNT_DIRECTORY="$2"; shift 2 ;;
        --fstype)           FSTYPE="$2"; shift 2 ;;
        --onlylocaldisk)    FILTER_DISK_TYPE=1; shift ;;
        --remove)           REMOVE=true; shift ;;
        --remove-quit)      REMOVE_QUIT=true; shift ;;
        --help|-h)          usage ;;
        *) echo "Unknown option: $1" >&2; usage ;;
    esac
done

# --- Main logic ---

if [[ "$REMOVE" == true || "$REMOVE_QUIT" == true ]]; then
    clear_raid0 "$DEVICE_PATH"
    if [[ "$REMOVE_QUIT" == true ]]; then
        echo "Exiting after removing RAID 0 array."
        exit 0
    fi
fi

echo "Using RAID device: $DEVICE_PATH"
echo "Minimum disk size (GB): $SIZE_GREATER_THAN"
echo "Disk count: $DISK_COUNT"
echo "Using directory: $MOUNT_DIRECTORY"
echo "Filesystem type: $FSTYPE"

if [[ "$FILTER_DISK_TYPE" -eq 1 ]]; then
    echo "Searching only for local disks."
else
    echo "Searching only for remote disks."
fi

# Collect eligible device paths
mapfile -t DEVICE_PATHS < <(get_device_paths "$SIZE_GREATER_THAN" "$DISK_COUNT" "$FILTER_DISK_TYPE")

# Check if the RAID device path is in the device list
for dp in "${DEVICE_PATHS[@]}"; do
    if [[ "$dp" == "$DEVICE_PATH" ]]; then
        echo "Warning: $DEVICE_PATH is in the list of devices. It will not be used for RAID 0. Try again using --remove."
        exit 0
    fi
done

if [[ "${#DEVICE_PATHS[@]}" -eq 0 ]]; then
    echo "Error: No eligible disks found for striping." >&2
    exit 1
fi

if [[ "${#DEVICE_PATHS[@]}" -eq 1 ]]; then
    echo "Only one disk found (${DEVICE_PATHS[0]}). Skipping RAID 0 creation and mounting directly."
    create_filesystem "${DEVICE_PATHS[0]}" "$FSTYPE" "$MOUNT_DIRECTORY"
else
    create_raid0 "$DEVICE_PATH" "${DEVICE_PATHS[@]}"
    create_filesystem "$DEVICE_PATH" "$FSTYPE" "$MOUNT_DIRECTORY"
fi
