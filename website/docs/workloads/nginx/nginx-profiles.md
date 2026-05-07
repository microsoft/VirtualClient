# NGINX Workload Profiles
The following profiles run customer-representative or benchmarking scenarios using the NGINX web server workload with Wrk or Wrk2 HTTP workload generator toolsets.

## PERF-WEB-NGINX-WRK.json
Runs the NGINX web server workload with the wrk HTTP benchmarking tool to measure networking throughput and latency performance. This is a client/server
workload that requires two separate systems/VMs (a Client and a Server) connected via the same network.

* [Workload Profile](https://github.com/microsoft/VirtualClient/blob/main/src/VirtualClient/VirtualClient.Main/profiles/PERF-WEB-NGINX-WRK.json)

* **Supported Platform/Architectures**
  * linux-x64
  * linux-arm64

* **Supported Operating Systems**
  * Azure Linux
  * CentOS
  * Debian
  * RedHat
  * Suse
  * Ubuntu

* **Supports Disconnected Scenarios**  
  * No. Internet connection required.

* **Dependencies**  
  The dependencies defined in the 'Dependencies' section of the profile itself are required in order to run the workload operations effectively.
  * Internet connection.

* **Profile Parameters**  
  The following parameters can be optionally supplied on the command line to modify the behaviors of the workload.

  | Parameter             | Purpose | Default Value |
  |-----------------------|---------|---------------|
  | TestDuration          | Optional. The duration of each individual Wrk test scenario. | 00:02:30 |
  | Timeout               | Optional. The maximum time to wait for the workload to complete. | 00:30:00 |
  | FileSizeInKB          | Optional. The size of the file (in KB) served by the NGINX server for each request. | 1 |
  | EmitLatencySpectrum   | Optional. Whether to emit the full latency spectrum in the results. | false |
  | Workers               | Optional. The number of NGINX worker processes. If null, the default NGINX configuration is used. | null |
  | CommandArguments      | Optional. The command line arguments to pass to the Wrk toolset. |  |

* **Profile Components**  
  The profile executes the following scenarios across varying numbers of concurrent connections.

  | Role   | Component            | Description |
  |--------|----------------------|-------------|
  | Server | NginxServerExecutor  | Hosts the NGINX web server that serves HTTPS requests. |
  | Client | WrkExecutor          | Runs the wrk HTTP benchmarking tool against the NGINX server. |

* **Profile Runtimes**  
  See the 'Metadata' section of the profile for estimated runtimes. These timings represent the length of time required to run a single round of profile
  actions. These timings can be used to determine minimum required runtimes for the Virtual Client in order to get results. These are often estimates based on the
  number of system cores.

* **Usage Examples**  
  The following section provides a few basic examples of how to use the workload profile.

  ```bash
  # Execute the workload profile on the Server system
  VirtualClient.exe --profile=PERF-WEB-NGINX-WRK.json --system=Demo --timeout=1440

  # Execute the workload profile on the Client system
  VirtualClient.exe --profile=PERF-WEB-NGINX-WRK.json --system=Demo --timeout=1440
  ```

## PERF-WEB-NGINX-WRK2.json
Runs the NGINX web server workload with the wrk2 HTTP benchmarking tool to measure networking throughput and latency performance under a constant (rate-limited)
request rate. This is a client/server workload that requires two separate systems/VMs (a Client and a Server) connected via the same network. The wrk2 tool
differs from wrk in that it applies a constant throughput/rate to produce accurate latency measurements that are not subject to coordinated omission.

* [Workload Profile](https://github.com/microsoft/VirtualClient/blob/main/src/VirtualClient/VirtualClient.Main/profiles/PERF-WEB-NGINX-WRK2.json)

* **Supported Platform/Architectures**
  * linux-x64

* **Supported Operating Systems**
  * Ubuntu

* **Supports Disconnected Scenarios**  
  * No. Internet connection required.

* **Dependencies**  
  The dependencies defined in the 'Dependencies' section of the profile itself are required in order to run the workload operations effectively.
  * Internet connection.

* **Profile Parameters**  
  The following parameters can be optionally supplied on the command line to modify the behaviors of the workload.

  | Parameter             | Purpose | Default Value |
  |-----------------------|---------|---------------|
  | TestDuration          | Optional. The duration of each individual Wrk2 test scenario. | 00:02:30 |
  | Timeout               | Optional. The maximum time to wait for the workload to complete. | 00:20:00 |
  | FileSizeInKB          | Optional. The size of the file (in KB) served by the NGINX server for each request. | 1 |
  | EmitLatencySpectrum   | Optional. Whether to emit the full latency spectrum in the results. | false |
  | Workers               | Optional. The number of NGINX worker processes. If null, the default NGINX configuration is used. | null |
  | Rate                  | Optional. The constant request rate (requests/sec) applied by Wrk2. | 1000 |
  | CommandArguments      | Optional. The command line arguments to pass to the Wrk2 toolset. |  |

* **Profile Components**  
  The profile executes the following scenarios across varying numbers of concurrent connections.

  | Role   | Component            | Description |
  |--------|----------------------|-------------|
  | Server | NginxServerExecutor  | Hosts the NGINX web server that serves HTTPS requests. |
  | Client | Wrk2Executor         | Runs the wrk2 HTTP benchmarking tool against the NGINX server at a constant request rate. |

* **Profile Runtimes**  
  See the 'Metadata' section of the profile for estimated runtimes. These timings represent the length of time required to run a single round of profile
  actions. These timings can be used to determine minimum required runtimes for the Virtual Client in order to get results. These are often estimates based on the
  number of system cores.

* **Usage Examples**  
  The following section provides a few basic examples of how to use the workload profile.

  ```bash
  # Execute the workload profile on the Server system
  VirtualClient.exe --profile=PERF-WEB-NGINX-WRK2.json --system=Demo --timeout=1440

  # Execute the workload profile on the Client system
  VirtualClient.exe --profile=PERF-WEB-NGINX-WRK2.json --system=Demo --timeout=1440
  ```

## PERF-WEB-NGINX-WRK-RP.json
Runs the NGINX web server workload with the Wrk HTTP benchmarking tool in a reverse proxy configuration to measure networking throughput and latency performance.
This workload requires three separate systems/VMs (a Client, a Reverse Proxy, and a Server) connected via the same network. The Client sends requests to the
NGINX reverse proxy, which forwards the requests to the backend NGINX server.

:::warning
This profile requires three nodes for the roles Client, Reverse Proxy, and Server.
:::

* [Workload Profile](https://github.com/microsoft/VirtualClient/blob/main/src/VirtualClient/VirtualClient.Main/profiles/PERF-WEB-NGINX-WRK-RP.json)

* **Supported Platform/Architectures**
  * linux-x64
  * linux-arm64

* **Supported Operating Systems**
  * Azure Linux
  * CentOS
  * Debian
  * RedHat
  * Suse
  * Ubuntu

* **Supports Disconnected Scenarios**  
  * No. Internet connection required.

* **Dependencies**  
  The dependencies defined in the 'Dependencies' section of the profile itself are required in order to run the workload operations effectively.
  * Internet connection.

* **Profile Parameters**  
  The following parameters can be optionally supplied on the command line to modify the behaviors of the workload.

  | Parameter             | Purpose | Default Value |
  |-----------------------|---------|---------------|
  | TestDuration          | Optional. The duration of each individual Wrk test scenario. | 00:02:30 |
  | Timeout               | Optional. The maximum time to wait for the workload to complete. | 00:30:00 |
  | FileSizeInKB          | Optional. The size of the file (in KB) served by the NGINX server for each request. | 1 |
  | EmitLatencySpectrum   | Optional. Whether to emit the full latency spectrum in the results. | false |
  | Workers               | Optional. The number of NGINX worker processes. If null, the default NGINX configuration is used. | null |
  | CommandArguments      | Optional. The command line arguments to pass to the Wrk toolset. |  |

* **Profile Components**  
  The profile executes the following scenarios across a combination of connection counts.

  | Role         | Component            | Description |
  |--------------|----------------------|-------------|
  | Server       | NginxServerExecutor  | Hosts the backend NGINX web server that serves HTTPS requests. |
  | ReverseProxy | NginxServerExecutor  | Hosts the NGINX reverse proxy that forwards client requests to the backend server. |
  | Client       | WrkExecutor          | Runs the Wrk HTTP benchmarking tool against the NGINX reverse proxy. |

* **Profile Runtimes**  
  See the 'Metadata' section of the profile for estimated runtimes. These timings represent the length of time required to run a single round of profile
  actions. These timings can be used to determine minimum required runtimes for the Virtual Client in order to get results. These are often estimates based on the
  number of system cores.

* **Usage Examples**  
  The following section provides a few basic examples of how to use the workload profile.

  ```bash
  # Execute the workload profile on the Server system
  VirtualClient.exe --profile=PERF-WEB-NGINX-WRK-RP.json --system=Demo --timeout=1440

  # Execute the workload profile on the Reverse Proxy system
  VirtualClient.exe --profile=PERF-WEB-NGINX-WRK-RP.json --system=Demo --timeout=1440

  # Execute the workload profile on the Client system
  VirtualClient.exe --profile=PERF-WEB-NGINX-WRK-RP.json --system=Demo --timeout=1440
  ```

## PERF-WEB-NGINX-WRK2-RP.json
Runs the NGINX web server workload with the wrk2 HTTP benchmarking tool in a reverse proxy configuration to measure networking throughput and latency performance
under a constant (rate-limited) request rate. This workload requires three separate systems/VMs (a Client, a Reverse Proxy, and a Server) connected via the same
network. The Client sends requests at a constant rate to the NGINX reverse proxy, which forwards the requests to the backend NGINX server. The wrk2 tool differs
from wrk in that it applies a constant throughput/rate to produce accurate latency measurements that are not subject to coordinated omission.

:::warning
This profile requires three nodes for the roles Client, Reverse Proxy, and Server.
:::

* [Workload Profile](https://github.com/microsoft/VirtualClient/blob/main/src/VirtualClient/VirtualClient.Main/profiles/PERF-WEB-NGINX-WRK2-RP.json)

* **Supported Platform/Architectures**
  * linux-x64

* **Supported Operating Systems**
  * Ubuntu 22

* **Supports Disconnected Scenarios**  
  * No. Internet connection required.

* **Dependencies**  
  The dependencies defined in the 'Dependencies' section of the profile itself are required in order to run the workload operations effectively.
  * Internet connection.

* **Profile Parameters**  
  The following parameters can be optionally supplied on the command line to modify the behaviors of the workload.

  | Parameter             | Purpose | Default Value |
  |-----------------------|---------|---------------|
  | TestDuration          | Optional. The duration of each individual Wrk2 test scenario. | 00:02:30 |
  | Timeout               | Optional. The maximum time to wait for the workload to complete. | 00:20:00 |
  | FileSizeInKB          | Optional. The size of the file (in KB) served by the NGINX server for each request. | 1 |
  | EmitLatencySpectrum   | Optional. Whether to emit the full latency spectrum in the results. | false |
  | Workers               | Optional. The number of NGINX worker processes. If null, the default NGINX configuration is used. | null |
  | Rate                  | Optional. The constant request rate (requests/sec) applied by Wrk2. | 1000 |
  | CommandArguments      | Optional. The command line arguments to pass to the Wrk2 toolset. |  |

* **Profile Components**  
  The profile executes the following scenarios across combinations of connection counts (100, 1K, 5K, 10K) and thread counts (LogicalCoreCount, LogicalCoreCount/2, LogicalCoreCount/4, LogicalCoreCount/8):

  | Role         | Component            | Description |
  |--------------|----------------------|-------------|
  | Server       | NginxServerExecutor  | Hosts the backend NGINX web server that serves HTTPS requests. |
  | ReverseProxy | NginxServerExecutor  | Hosts the NGINX reverse proxy that forwards client requests to the backend server. |
  | Client       | Wrk2Executor         | Runs the Wrk2 HTTP benchmarking tool against the NGINX reverse proxy at a constant request rate. |

* **Profile Runtimes**  
  See the 'Metadata' section of the profile for estimated runtimes. These timings represent the length of time required to run a single round of profile
  actions. These timings can be used to determine minimum required runtimes for the Virtual Client in order to get results. These are often estimates based on the
  number of system cores.

* **Usage Examples**  
  The following section provides a few basic examples of how to use the workload profile.

  ```bash
  # Execute the workload profile on the Server system
  VirtualClient.exe --profile=PERF-WEB-NGINX-WRK2-RP.json --system=Demo --timeout=1440

  # Execute the workload profile on the Reverse Proxy system
  VirtualClient.exe --profile=PERF-WEB-NGINX-WRK2-RP.json --system=Demo --timeout=1440

  # Execute the workload profile on the Client system
  VirtualClient.exe --profile=PERF-WEB-NGINX-WRK2-RP.json --system=Demo --timeout=1440
  ```
