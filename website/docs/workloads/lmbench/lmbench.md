# LMbench
LMbench (version 3) is a suite of simple, portable benchmarks ANSI/C microbenchmarks for UNIX/POSIX. In general, it measures two key 
features: component bandwidth and latency. LMbench is intended to provide system developers insights into basic performance and costs 
of key system operations.

* [LMbench Documentation](http://www.bitmover.com/lmbench/whatis_lmbench.html)
* [LMbench Manual](http://www.bitmover.com/lmbench/man_lmbench.html)

### What is Being Tested?
The following performance analysis tests are ran as part of the LMbench workload. Note that although LMbench runs benchmarks covering
various aspects of the system, the memory performance benchmarks are the ones that are most interesting for net impact analysis.

http://www.bitmover.com/lmbench/man_lmbench.html

| Bandwidth Benchmark   | Description                                               |
|-----------------------|-----------------------------------------------------------|
| Cached file read      | Measures times for reading and summing a file             |
| Memory copy (bcopy)   | Measures memory copy operation speeds                     |
| Memory read           | Measures memory read operation speeds                     |
| Memory write          | Measures memory write operation speeds                    |
| Pipe                  | Measures data movement times through named pipes          |
| TCP                   | Measures data movement times through TCP/IP sockets       |

| Latency Benchmark     | Description                                                    |
|-----------------------|----------------------------------------------------------------|
| Context switching     | Measures context switching time for processes on the system    |
| Networking: connection establishment, pipe, TCP, UDP, and RPC hot potato   | Measures inter-process connection latency via communications sockets |
| File system creates and deletes       | Measures file system create/delete performance              |
| Process creation                      | Measures the time the system takes to create new processes  |
| System call overhead                  | Measures the time it takes to make simple operating system calls |
| Memory read latency                   | Measures memory read latency       |

### System Requirement
The following section provides special considerations required for the system on which the LMbench workload will be run.

* Physical Memory = 16 GB minimum  
* Disk Space = At least 20 MB of free space on the OS disk

### Supported Platforms
* Linux x64
* Linux arm64

### Package Dependencies
The following package dependencies are required to be installed on the Unix/Linux system in order to support the requirements
of the LMbench workload. Note that the Virtual Client will handle the installation of any required dependencies.

* gcc
* make
