import pyudev
import psutil
from enum import Enum

class FilterDiskType(Enum):
    #Only Filter Disks with the Same ID Model name as the Operational System Disk Drive
    OnlySameModelAsOSDisk = 1
    #Only Filter Disks with Different ID Model name of the Operational System Disk Drive
    OnlyDifferentModelOfOSDisk = 2
    #Do not consider the Operational System Disk Drive
    NoOSDisk = 3
    #Consider all disks
    AllDisks = 4

def get_os_disk_device_node():
    partitions = psutil.disk_partitions()
    for partition in partitions:
        if partition.mountpoint == '/':  # Root filesystem
            return partition.device
    return None

def get_os_disk():
    os_disk_device_node = get_os_disk_device_node()
    if not os_disk_device_node:
        return None

    context = pyudev.Context()
    for device in context.list_devices(subsystem='block', DEVTYPE='disk'):
        if os_disk_device_node.startswith(device.device_node):
            return device
    return None

def get_disk_size(device):
    return device.attributes.asint('size') * 512 / (1024**3)

def list_disks(min_size_gb=256, diskcount=0, FilterDiskType=FilterDiskType.AllDisks):
    disks = list()
    context = pyudev.Context()
    os_disk = None
    if FilterDiskType == FilterDiskType.AllDisks:
        print("Listing all disks...")
        os_disk_model = None
    else:
        print(f"Filtering disks by model: {FilterDiskType.name}")
        os_disk = get_os_disk()
        if os_disk is None:
            print("No OS disk found.")
            return disks
        os_disk_model = os_disk.properties.get('ID_MODEL')

        if os_disk_model:
            print(f"OS Disk Model: {os_disk_model}")
        else:
            print("OS Disk Model: Not found")
            return disks

    for device in context.list_devices(subsystem='block', DEVTYPE='disk'):        
        sz = get_disk_size(device)
        
        if sz < 1 or sz < min_size_gb:
            continue

        if os_disk != None and device.device_node == os_disk.device_node:
            continue

        same_model = device.properties.get('ID_MODEL') == os_disk_model
        
        if FilterDiskType == FilterDiskType.OnlySameModelAsOSDisk and not same_model:
            continue
        if FilterDiskType == FilterDiskType.OnlyDifferentModelOfOSDisk and same_model:
            continue

        disks.append(device)            

    if (diskcount > 0 and diskcount < len(disks)):
        disks.sort(reverse=True, key=lambda d: get_disk_size(d))
        disks = disks[:diskcount]
    return disks

def print_disks(min_size_gb=256, diskcount=0, FilterDiskType=FilterDiskType.AllDisks):
    disks = list_disks(min_size_gb, diskcount, FilterDiskType)
    for disk in disks:
        print(f"Device: {disk.device_node}, Size: {get_disk_size(disk):.2f} GB, Model: {disk.properties.get('ID_MODEL')}, Children: {len(list(disk.children))}")

def OnlyDifferentModelOfOSDisk():
    print_disks(0, 0, FilterDiskType.OnlyDifferentModelOfOSDisk)
    return

#OnlyDifferentModelOfOSDisk()