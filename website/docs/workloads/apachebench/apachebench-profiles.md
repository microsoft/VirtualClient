---
slug: /workloads/apachebench-profiles
---

# ApacheBench Profiles
The following profile runs benchmarking scenarios using the ApacheBench workload.

* [Workload Details](/docs/workloads/apachebench)

## PERF-APACHEBENCH.json
Runs ApacheBench against an Apache HTTP Server on the local system. The profile installs and starts the server, then executes ApacheBench with
HTTP keep-alive enabled using the configured request count and concurrency level.

* [Workload Profile](https://github.com/microsoft/VirtualClient/blob/main/src/VirtualClient/VirtualClient.Main/profiles/PERF-APACHEBENCH.json)

* **Supported Platform-Architectures**
  * linux-x64
  * linux-arm64
  * win-x64

* **Dependencies**  
  The dependencies defined in the 'Dependencies' section of the profile itself are required in order to run the workload operations effectively.
  * Internet connection.
  * On Linux, the `apache2` and `unzip` packages.
  * On Windows, the `apache-httpd-2.4.57-v2.zip` dependency package.

  Additional information on components that exist within the 'Dependencies' section of the profile can be found in the following location:
  * [Installing Dependencies](/docs/category/dependencies/)

* **Profile Parameters**

  | Parameter              | Purpose                                                | Default Value |
  |------------------------|--------------------------------------------------------|---------------|
  | RequestCount           | Total number of requests sent during each iteration.   | 50000         |
  | ConcurrentRequestCount | Number of requests issued concurrently.                | 50            |

* **Profile Runtimes**  
  See the 'Metadata' section of the profile for estimated runtimes. These timings represent the length of time required to run a single round of
  profile actions.

* **Usage Examples**

  ```bash
  # Execute the workload profile
  ./VirtualClient --profile=PERF-APACHEBENCH.json --system=Demo --timeout=60

  # Execute with a custom request count and concurrency level
  ./VirtualClient --profile=PERF-APACHEBENCH.json --system=Demo --timeout=60 --parameters="RequestCount=100000,,,ConcurrentRequestCount=100"
  ```
