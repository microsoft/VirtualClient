# ApacheBench
Apache Bench (ab) is a tool for benchmarking Apache Hypertext Transfer Protocol (HTTP) server. This especially shows you how many requests per second your Apache installation is capable of serving.

* [Apache Bench Documentation](https://httpd.apache.org/docs/2.4/programs/ab.html)

## What is Being Measured?
ApacheBench is designed to be a benchmarking tool. It produces the metrics for request latencies. It performs N number of requests to server and C number of requests at a time. Virtual client set total of 50000 requests in a batch of 10 and 50 requests at a time respectively, per iteration.

## Workload Usage
ab [NoOfRequests] [NoOfConcurrentRequests]

n : Number of requests to be made to the server.
c : Number of concurrent requests are to be made to the server.

## Workload Metrics
The following metrics are examples of those captured by the Virtual Client when running the Apache Bench workload.

| Tool Name   | Metric Name                                | Example Value | Unit            |
|-------------|--------------------------------------------|---------------|-----------------|
| ApacheBench | Concurrency Level                          | 50            | number          |
| ApacheBench | Total requests                             | 50000         | number          |
| ApacheBench | Total time			                       | 34.095        | seconds         |
| ApacheBench | Total failed requests			           | 0             | number/sec      |
| ApacheBench | Requests					               | 1466.89       | number/sec      |
| ApacheBench | Time per request						   | 34.095        | milliseconds    |
| ApacheBench | Total data transferred			           | 16329340      | bytes           |
| ApacheBench | Data transfer rate						   | 467.72        | kilobytes/sec   |