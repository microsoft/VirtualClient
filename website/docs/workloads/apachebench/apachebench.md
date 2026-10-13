---
slug: /workloads/apachebench
---

# ApacheBench
ApacheBench (`ab`) is an HTTP load-generation tool distributed with the Apache HTTP Server. It sends concurrent requests to a web server and reports
request throughput, latency, failures, and data transfer rates. The Virtual Client workload installs and starts an Apache HTTP Server on the system,
runs ApacheBench against the local server, and converts the text output into structured metrics.

* [ApacheBench Documentation](https://httpd.apache.org/docs/2.4/programs/ab.html)
* [Apache HTTP Server Documentation](https://httpd.apache.org/docs/2.4/)

## What is Being Measured?
ApacheBench sends a configurable number of HTTP requests to `http://localhost:80/` using a configurable concurrency level. The workload enables
HTTP keep-alive and captures the following measurements:

* Total benchmark execution time.
* Complete and failed request counts.
* Requests completed per second.
* Mean request latency across all concurrent requests.
* Total data transferred and the average data transfer rate.

ApacheBench reports 2 `Time per request` values. The Virtual Client captures the value measured across all concurrent requests because it represents
the average effective request latency at the configured concurrency level.

## Workload Metrics
The following metrics are examples of those captured by the Virtual Client when running the ApacheBench workload.

| Name                   | Example Value | Unit            | Description                                              |
|------------------------|---------------|-----------------|----------------------------------------------------------|
| Concurrency Level      | 100           | number          | Number of requests issued concurrently                   |
| Total requests         | 100           | number          | Total number of completed requests                       |
| Total time             | 0.578         | seconds         | Total time required to complete the benchmark            |
| Total failed requests  | 0             | number          | Total number of requests that failed                     |
| Requests               | 172.97        | number/sec      | Number of requests completed per second                  |
| Total time per request | 5.781         | milliseconds    | Mean request latency across all concurrent requests      |
| Total data transferred | 65006         | bytes           | Total response data transferred                          |
| Data transfer rate     | 109.81        | kilobytes/sec   | Average response data transfer rate                      |

## Troubleshooting

### The Benchmark Cannot Connect to Localhost
Confirm that the Apache HTTP Server is running and listening on port 80. On Linux, check `systemctl status apache2`. On Windows, check the
Apache service and review the `Apache24/logs` directory in the extracted package.

### Windows Reports a Missing DLL
The Windows package includes `vc_redist.x64.exe`, which the executor installs before Apache HTTP Server. A failure with `0xC0000135`
generally indicates that the Visual C++ runtime was not installed successfully.

### Linux Setup Fails While Configuring the Firewall
The executor runs `ufw allow 80/tcp` with elevated permissions. Ensure `ufw` is available and that the Virtual Client process can run
elevated commands.

### Results Parsing Fails
Retain the ApacheBench standard output in the Virtual Client logs. The parser requires the standard summary fields beginning with
`Concurrency Level` and reports a workload-results parsing error when those fields are absent or incomplete.
