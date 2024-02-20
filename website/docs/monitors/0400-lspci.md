# Lspci
The PCI Utilities package contains a library for portable access to PCI bus
configuration registers and several utilities based on this library.

- lspci: displays detailed information about all PCI buses and devices.

* [lspci repo](https://github.com/pciutils/pciutils)
* [Windows Build Download](https://eternallybored.org/misc/pciutils/)

## Dependency
Most Linux distro comes with lspci pre-installed. On Windows, VC is packaging a win-x64 build inside VC package itself.

## Supported Platforms
* linux-x64
* linux-arm64
* win-x64


## lspci Output Description
The following section describes the various counters/metrics that are available with the lspci toolset.

The command we are using is `lspci -vvv`. The data structure of the lspci output is not a straightforward dicctionary. 
The data structure is parsed according to the contract at [PciDevice.cs](../../../src/VirtualClient/VirtualClient.Monitors/Lspci/PciDevice.cs).

For each PCI device, we are parsing the name, address, various properties and capabilities.

### Example
This is an example of the minimum profile to run LspciMonitor. The PCI devices is not expected to change often, so the monitor frequency could be set very low.

```json
{
    "Description": "Default Monitors",
    "Parameters": {
      "MonitorFrequency": "12:00:00",
      "MonitorWarmupPeriod": "00:01:00"
    },
    "Monitors": [
      {
        "Type": "LspciMonitor",
        "Parameters": {
          "Scenario": "CapturePCIDevicesDetails",
          "MonitorFrequency": "$.Parameters.MonitorFrequency",
          "MonitorWarmupPeriod": "$.Parameters.MonitorWarmupPeriod"
        }
      }
    ]
  }
```