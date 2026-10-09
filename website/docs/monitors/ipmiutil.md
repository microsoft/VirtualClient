---
slug: /monitors/ipmiutil
sidebar_position: 7
---

# IPMIUtil Monitor

IPMIUtil is a collection of utilities for systems that implement the Intelligent Platform Management Interface (IPMI). Virtual Client uses IPMIUtil as a background monitor to capture sensor readings and Baseboard Management Controller (BMC) System Event Log (SEL) records while a workload runs.

* [IPMIUtil project](https://ipmiutil.sourceforge.net/)
* [IPMIUtil user guide](https://ipmiutil.sourceforge.net/docs/UserGuide)
* [IPMIUtil source](https://sourceforge.net/projects/ipmiutil/)

## Supported Platforms

* linux-x64
* linux-arm64
* win-x64
* win-arm64

The system must expose an IPMI/BMC interface that IPMIUtil can access. The monitor does not emulate BMC hardware and cannot produce data on systems without supported management hardware and drivers.

## Installation

The `MONITORS-IPMIUTIL.json` profile installs IPMIUtil from the operating-system package repository on Linux. On Windows, it downloads `host.monitors.1.0.1.zip` from the Virtual Client `packages` container, so a package store must be supplied when the package is not already cached.

``` bash
# Linux
sudo ./VirtualClient --profile=MONITORS-IPMIUTIL.json --timeout=30 --logger=csv --log-to-file

# Windows
VirtualClient.exe --profile=MONITORS-IPMIUTIL.json --timeout=30 --packages="<package store connection>" --logger=csv --log-to-file
```

The monitors search for `ipmiutil` or `ipmiutil.exe` on `PATH`. The SEL monitor also checks the legacy
`C:\BladeFX_latest\BladeFX\Tools\IpmiUtil\ipmiutil.exe` location as a final fallback.

## Sensor Monitoring

`IpmiUtilSensorMonitor` executes:

``` text
ipmiutil sensor -s
```

Each numeric sensor reading is emitted as a metric. The metric name is the IPMI sensor name, and the sensor status is included as a tag. The parser maps common units as follows:

| IPMI unit | Virtual Client unit | Relativity |
|-----------|---------------------|------------|
| `A` | amps | Undefined |
| `C` | Celsius | Lower is better |
| `V` | volts | Undefined |
| `W` | watts | Lower is better |
| `%` | percentage | Undefined |

Non-numeric readings such as `N/A`, `ERR`, or `---` are emitted as monitor events rather than discarded.

## SEL Monitoring

`IpmiUtilSelMonitor` uses the following commands:

| Command | Purpose |
|---------|---------|
| `ipmiutil sel -uc` | Read decoded SEL records. |
| `ipmiutil sel -r` | Read raw SEL records and combine them with decoded records. |
| `ipmiutil sel -d` | Clear the SEL when `ClearSel` is enabled. |

Matching SEL records are emitted as count metrics. Metric metadata preserves the record ID, severity, sensor, timestamp, source, event type, decoded details, and raw details. Tags include `IPMI`, the event source, and severity.

Common metric names include:

| SEL event | Example metric |
|-----------|----------------|
| Processor internal error | `Processor_IERR` |
| Processor FRB2 timeout | `Processor_FRB2` |
| Uncorrectable memory ECC | `Memory_Uncorrectable_ECC` |
| Correctable memory ECC | `Memory_Correctable_ECC` |
| Fatal non-maskable interrupt | `Critical_Interrupt_Fatal_NMI` |
| Power-supply predictive failure | `Power_Supply_Predictive_Failure` |

## Parameters

The following parameters can be overridden on the command line.

| Parameter | Purpose | Default |
|-----------|---------|---------|
| `MonitorFrequency` | Interval between captures. | `00:01:00` |
| `MonitorWarmupPeriod` | Delay before the first capture. | `00:00:00` |
| `MonitorIterations` | Maximum captures; `-1` continues until cancellation. | `-1` |
| `ClearSel` | Clears the BMC SEL after it is captured. Use with care because this modifies persistent BMC state. | `false` |
| `EventsFilter` | Regular expression applied to decoded SEL records. | `MAJ\|CRT` |
| `CaptureSuspectedEvents` | Writes suspected hardware events to separate output files. | `false` |
| `IncludeWarningsInSuspectedEvents` | Includes warning-level records when suspected-event capture is enabled. | `true` |
| `EnableSelDecoding` | Runs an optional Sherlock or Generic decoder against raw SEL data. | `false` |
| `DecoderType` | Decoder engine: `Sherlock` or `Generic`. Required when decoding is enabled. | |
| `DecoderArguments` | Additional arguments passed to the selected decoder. | |
| `DecoderPackageName` | Installed package containing the decoder executable. | |
| `DecoderExecutableName` | Overrides the decoder executable name. | |

Optional SEL decoding requires a separately supplied decoder package. The IPMIUtil package does not include either decoder.

## Usage Examples

``` bash
# Capture sensors and major/critical SEL events for 30 minutes.
sudo ./VirtualClient --profile=MONITORS-IPMIUTIL.json --timeout=30

# Capture all decoded SEL severities without clearing the SEL.
sudo ./VirtualClient --profile=MONITORS-IPMIUTIL.json --timeout=30 --parameters="EventsFilter=.*;;;ClearSel=false"

# Capture only one iteration after a 10-second warmup.
VirtualClient.exe --profile=MONITORS-IPMIUTIL.json --timeout=5 --parameters="MonitorIterations=1;;;MonitorWarmupPeriod=00:00:10" --packages="<package store connection>"
```

## Troubleshooting

* Run Virtual Client with elevated privileges when the operating system restricts access to the IPMI device.
* Confirm `ipmiutil --help` on Linux or `ipmiutil.exe /?` on Windows returns output before diagnosing the monitor.
* An empty result can mean the system has no accessible BMC sensors or SEL records; it does not necessarily indicate a parser failure.
* If the Windows dependency fails, confirm the public package store contains `packages/host.monitors.1.0.1.zip`.
* Avoid enabling `ClearSel` during diagnosis until the SEL data has been preserved.
