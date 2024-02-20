# Stress-ng
Stress-ng is designed to exercise various physical subsystems of a computer as well as the various operating system kernel interfaces. The workload was originally intended to make 
a machine work hard and trip hardware issues such as thermal overruns as well as operating system bugs that only occur when a system is being "thrashed" hard.

* [StressNg Github](https://github.com/ColinIanKing/stress-ng)
* [StressNg Guide On Ubuntu Wiki](https://wiki.ubuntu.com/Kernel/Reference/stress-ng)

## What is Being Measured?
The Stress-ng workload is designed to measure test throughput rates. This can be useful to observe performance changes across different operating system 
releases or types of hardware.

## Workload Metrics
The following metrics are examples of those captured by the Virtual Client when running the Stress-ng workload.

| Metric Name | Example Value (min) | Example Value (max) | Example Value (avg) | Unit |
|-------------|---------------------|---------------------|---------------------|------|
| cpu-bogo-ops | 127547.0 | 154149.0 | 143854.0520109837 | BogoOps |
| cpu-bogo-ops-per-second-real-time | 2125.201814 | 2568.771695 | 2396.355623708609 | BogoOps/s |
| cpu-bogo-ops-per-second-usr-sys-time | 138.15239 | 162.927536 | 151.7284266787272 | BogoOps/s |
| cpu-system-time | 0.01 | 1.22 | 0.26765912762520197 | second |
| cpu-user-time | 894.69 | 958.66 | 947.796421647819 | second |
| cpu-wall-clock-time | 60.006434 | 60.068574 | 60.03039786962844 | second |
