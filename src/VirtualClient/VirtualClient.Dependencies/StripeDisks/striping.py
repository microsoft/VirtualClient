import subprocess
import argparse
from listdisks import list_disks, FilterDiskType

def print_virtual_disk_info(mount_point='/dev/md0'):
    result = subprocess.run(['lsblk', mount_point, '-o', 'NAME,TYPE,SIZE'], capture_output=True, text=True)
    print("Listing virtual disks:")
    if result.returncode != 0:
        print("Error listing virtual disks:", result.stderr.strip())
        return
    if not result.stdout.strip():
        print("No virtual disks found.")
        return
    print(result.stdout.strip())

def create_filesystem(device='/dev/md0', fstype='ext4', mount_dir='/mnt/raid0'):
    print(f"Create file system at {device} with type {fstype} and mount it at {mount_dir}.")
    if not device or not fstype:
        raise ValueError("Device and filesystem type must be specified.")
    if not mount_dir:
        raise ValueError("Mount directory must be specified.")
    cmd = ['sudo', f'mkfs.{fstype}', device]
    try:
        subprocess.run(cmd, check=True)
        print(f"{fstype} filesystem created successfully on {device}.")
        # Create mount directory if it doesn't exist
        subprocess.run(['sudo', 'mkdir', '-p', mount_dir], check=True)
        # Mount the device
        subprocess.run(['sudo', 'mount', device, mount_dir], check=True)
        print(f"{device} mounted at {mount_dir}.")
    except subprocess.CalledProcessError as e:
        print(f"Error creating filesystem or mounting: {e}")
        raise

def create_raid0(devices, mount_point='/dev/md0'):
    """
    Create a RAID 0 array using mdadm.
    :param devices: List of device paths, e.g. ['/dev/sdb', '/dev/sdc']
    :param mount_point: Name of the RAID device to create
    """
    print("creating RAID 0 array with devices:", devices)

    if not devices:
        raise ValueError("No devices provided for RAID 0 array.")

    if len(devices) < 2:
        raise ValueError("At least two devices are required to create a RAID 0 array.")

    cmd = [
        'sudo', 'mdadm', '--create', mount_point,
        '--level=0',
        '--raid-devices={}'.format(len(devices))
    ] + devices
    print("Running:", ' '.join(cmd))
    try:
        subprocess.run(cmd, check=True)
        print(f"RAID 0 array created successfully at {mount_point} with devices: {', '.join(devices)}")
        print_virtual_disk_info(mount_point)
    except subprocess.CalledProcessError as e:
        print(f"Error creating RAID 0 array: {e}")
        raise

def is_device_in_raid(device):
    result = subprocess.run(['sudo', 'mdadm', '--examine', device], capture_output=True, text=True)
    if result.returncode != 0:
        return False
    if "No md superblock detected" in result.stdout:
        return False
    if "Raid Level" in result.stdout or "Array UUID" in result.stdout:
        return True
    return False

def read_disk_paths(devices):
    return [device.device_node for device in devices]

def get_device_paths(min_size_gb=256, diskcount=0, FilterDiskType=FilterDiskType.OnlyDifferentModelOfOSDisk):
    ds = list_disks(min_size_gb, diskcount, FilterDiskType)
    device_paths = read_disk_paths(ds)
    if not device_paths:
        raise ValueError("No suitable disks found for RAID 0.")
    rems = []
    for device in device_paths:
        if is_device_in_raid(device):
            print(f"Warning: {device} is already part of a RAID array. It will not be used for RAID 0.")
            rems.append(device)
    for device in rems:
            device_paths.remove(device)
    print("Disks to be used for RAID 0:", device_paths)
    return device_paths

def get_raid0_list_disks(mount_point='/dev/md0'):
    print(f"Getting list of disks in RAID 0 array at {mount_point}...")

    result = subprocess.run(['sudo', 'mdadm', '--detail', mount_point], capture_output=True, text=True)
    if result.returncode != 0:
        raise ValueError(f"Failed to get details of RAID array {mount_point}: {result.stderr.strip()}")
    disks = []
    for line in result.stdout.splitlines():
        line = line.strip()
        if mount_point in line:
            continue
        if line.startswith('/dev/'):
            disks.append(line.split()[0])
        elif '/dev/' in line:
            # mdadm output: ... Active ... /dev/sdb
            parts = line.split()
            for part in parts:
                if part.startswith('/dev/'):
                    disks.append(part)
    return disks

def clear_superblock(disks):
    print("Clearing superblocks from disks:", disks)
    try:
        subprocess.run(['sudo', 'mdadm', '--zero-superblock'] + disks, check=True)
        print("Superblocks cleared successfully.")
    except subprocess.CalledProcessError as e:
        print(f"Failed to clear superblocks: {e}")
        raise

def clear_raid0(mount_point='/dev/md0'):
    print(f"Clearing existing RAID 0 array at {mount_point}...")
    try:
        raid_disk_paths = get_raid0_list_disks(mount_point)
    except ValueError as e:
        print(e)
        print('clear superblock from all disks')
        ds=list_disks(0)
        raid_disk_paths = read_disk_paths(ds)
        clear_superblock(raid_disk_paths)
        return

    try:
        raid_disk_paths = get_raid0_list_disks(mount_point)
        subprocess.run(['sudo', 'mdadm', '--stop', mount_point], check=True)
        subprocess.run(['sudo', 'mdadm', '--remove', mount_point], check=True)
        print(f"RAID 0 array at {mount_point} cleared successfully.")

    except subprocess.CalledProcessError as e:
        print(f"Failed to clear RAID 0 array at {mount_point}: {e}. Ensure it is not in use.")

    clear_superblock(raid_disk_paths)

if __name__ == "__main__":
    #By default, only remote disks are searched.
    #If you want to search only local disks, use --onlylocaldisk option.
    #If you want to remove an existing RAID 0 array, use --remove option.
    #If you want to remove an existing RAID 0 array and quit without creating RAID 0, use --remove-quit option.
    parser = argparse.ArgumentParser()
    parser.add_argument('--devicePath', type=str, help='RAID device path for the striped volume', default='/dev/md0')
    parser.add_argument('--sizeGreaterThan', type=int, help='Minimum disk size in GB for striping', default=256)
    parser.add_argument('--diskCount', type=int, help='Number of disks to use for striping. Zero value means all disks.', required=False, default=0)
    parser.add_argument('--mountDirectory', type=str, help='Directory to mount the filesystem', default='/mnt/raid0')
    parser.add_argument('--fstype', type=str, help='Filesystem type', default='ext4')
    parser.add_argument('--onlylocaldisk', action='store_true', help='(Optional) Search only for local disk.', required=False, default=False)
    parser.add_argument('--remove', action='store_true', help='(Optional) Remove an existing RAID 0 array', required=False, default=False)
    parser.add_argument('--remove-quit', action='store_true', help='(Optional) Just remove an existing RAID 0 array and quit without creating RAID 0', required=False, default=False)

    args = parser.parse_args()
    
    if args.remove or args.remove_quit:
        clear_raid0(args.devicePath)
        if args.remove_quit:
            print("Exiting after removing RAID 0 array.")
            exit(0)

    print("Using RAID device:", args.devicePath)
    print("Minimum disk size (GB):", args.sizeGreaterThan)
    print("Disk count:", args.diskCount)
    print("Using directory:", args.mountDirectory)
    print("Filesystem type:", args.fstype)

    filter_disk_type = FilterDiskType.OnlyDifferentModelOfOSDisk
    if args.onlylocaldisk:
        print("Searching only for local disks.")
        filter_disk_type = FilterDiskType.OnlyLocalDisks
    else:
        print("Searching only for remote disks.")
    
    device_paths = get_device_paths(args.sizeGreaterThan, args.diskCount, filter_disk_type)

    if args.devicePath in device_paths:
        print(f"Warning: {args.devicePath} is in the list of devices. It will not be used for RAID 0. Try again using --remove.")
        exit(0)
        
    if len(device_paths) == 0:
        raise ValueError("No eligible disks found for striping.")

    if len(device_paths) == 1:
        print(f"Only one disk found ({device_paths[0]}). Skipping RAID 0 creation and mounting directly.")
        create_filesystem(device_paths[0], args.fstype, args.mountDirectory)
    else:
        create_raid0(device_paths, args.devicePath)
        create_filesystem(args.devicePath, args.fstype, args.mountDirectory)