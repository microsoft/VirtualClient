---
slug: /workloads/kafka
---

# Kafka

Apache Kafka is a distributed event-streaming platform. The Virtual Client workload runs Kafka's bundled
`kafka-producer-perf-test` and `kafka-consumer-perf-test` tools against a Kafka cluster using KRaft
(Kafka's built-in metadata quorum, without ZooKeeper).

* [Kafka documentation](https://kafka.apache.org/38/documentation/)
* [Kafka source](https://github.com/apache/kafka)
* [Workload profiles](./kafka-profiles.md)

## Supported platforms

* linux-x64
* linux-arm64
* win-x64
* win-arm64

The workload requires elevated privileges, dependency-package access, and sufficient memory and disk space
for the configured brokers, concurrent Java clients, and records. Use dedicated benchmark systems.
This is a plaintext benchmark configuration, not a production Kafka deployment.

## Workload flow

The profile supports a single system or separate Client and Server systems. A multi-system layout
must contain exactly one Server system; multiple brokers run on that system, not across Server VMs:

1. Download Kafka and the Microsoft Build of OpenJDK, then start the Virtual Client API.
2. Generate per-broker KRaft configuration files and a cluster UUID, format the log directories, and start
   the configured number of brokers. Brokers on each server use the same host and different ports.
3. Create `sync-test-rep-one`, `sync-test-rep-dynamic`, and `async-test-rep-dynamic` topics.
4. Produce records with batch sizes of 8196 and 64000, then consume the synchronous topic.
5. Produce and consume the asynchronous topic using the configured client-instance count.

In a multi-system layout, clients wait for the server API heartbeat and online signal before starting.
`ServerInstanceCount` controls broker processes on the Server system; it does not specify the number of VMs.

The dynamic replication factor is 1 on Windows or when the server-instance count is at most 3.
On Linux it is `ServerInstanceCount - 2` for counts of 4 or 5, and 4 for larger counts.

Topic creation and producer actions are marked `WarmUp` in the shipped profile. They run once per
component execution lifetime, rather than every profile iteration. Only consumer executions currently
emit benchmark metrics. Producer process output is logged, but the presence of a producer parser does
not mean the profile emits producer metrics.

## Packages and dependencies

| Package name | Blob in the `packages` container | Purpose |
|---|---|---|
| `kafka` | `kafka_2.13-3.8.0-3.zip` | Kafka 3.8.0, Scala 2.13, KRaft configuration, and bundled performance tools |
| `javadevelopmentkit` | `microsoft-jdk-21.0.1.zip` | Platform-specific Java runtime |

The JDK installer registers the platform-specific runtime. The executors set `JAVA_HOME` and explicitly
pass it through elevated Linux commands because `sudo` may remove environment variables.

Kafka is distributed under Apache License 2.0 and includes third-party notices. Preserve the `LICENSE`
and `NOTICE` files when using or redistributing the package. See the
[Microsoft Build of OpenJDK documentation](https://learn.microsoft.com/java/openjdk/) for JDK licensing
and distribution information.

### Windows package adjustments

The `-3` Kafka package contains Windows script changes:

* `kafka-run-class.bat` uses a wildcard library classpath to avoid "The input line is too long".
* `kafka-server-start.bat` does not depend on WMIC for detecting 32-bit Windows; supported Windows
  platforms are 64-bit.
* Kafka and ZooKeeper stop scripts use PowerShell/CIM instead of the deprecated `wmic.exe`.

These changes are already packaged; no manual script editing is required. Revision `-3` adds explicit
modification notices to the scripts changed in revision `-2`, without changing script logic, binaries,
or existing license/notice files. Do not substitute the unmodified upstream archive when verifying this workload.

## Metrics

Metrics are logged with tool name `Kafka-Benchmark` and the action's scenario name. Names, units, and
relativity below describe the implementation, including its existing naming conventions.

### Emitted consumer metrics

| Metric | Unit | Relativity | Meaning |
|---|---|---|---|
| `data_consumed_in_megabyte` | megabytes | Higher is better | Total data consumed |
| `megabyte_per_sec_throughput` | megabytes/sec | Higher is better | Overall data throughput |
| `data_consumed_in_nMsg` | operations | Higher is better | Total messages consumed |
| `nMsg_per_sec_throughput` | operations/sec | Higher is better | Overall message throughput |
| `fetch_time_in_millisec` | milliseconds | Lower is better | Fetch time reported by the consumer tool |
| `fetch_megabyte_per_sec` | megabytes/sec | Higher is better | Fetch data throughput |
| `fetch_nMsg_per_sec` | operations/sec | Higher is better | Fetch message throughput |

Concurrent consumer processes each emit their own metric set. Do not interpret a single process's
throughput as the aggregate of all clients.

### Producer parser metrics

`KafkaProducerMetricsParser` can parse the following metrics from producer output. The current executor
does **not** emit these metrics during producer actions.

| Metric | Unit | Relativity |
|---|---|---|
| `Total_Records_Sent` | operations | Higher is better |
| `Records_Per_Sec` | operations/sec | Higher is better |
| `Latency-Avg` | milliseconds | Lower is better |
| `Latency-Max` | milliseconds | Lower is better |
| `Latency-P50` | milliseconds | Lower is better |
| `Latency-P95` | milliseconds | Lower is better |
| `Latency-P99` | milliseconds | Lower is better |
| `Latency-P99.9` | milliseconds | Lower is better |

## Troubleshooting

| Symptom | Checks |
|---|---|
| Package missing or download fails | Verify the package-store URL, container, exact blob names, access, and package registration. A cached package is not proof that a fresh system can download it. |
| Java cannot start on Linux | Verify the selected JDK architecture, executable permissions, and `JAVA_HOME` in the elevated process. |
| Windows classpath or WMIC errors | Confirm the profile downloaded `kafka_2.13-3.8.0-3.zip`, not an older cached or upstream archive. |
| Client waits for the server | Verify layout names/roles/IP addresses, server logs, Virtual Client API reachability, and Kafka broker/controller ports. |
| Broker fails to start | Check Java memory, disk capacity, port conflicts, KRaft configuration, and log-directory permissions. |
| No producer metrics | Expected: the executor captures benchmark metrics only for consumer commands. Inspect producer process logs for generation results. |
| Results parsing fails | Inspect the final CSV output from the performance tool. The parsers expect the packaged Kafka version's output format. |

### State and cleanup

The workload rewrites per-broker configuration and resets `/tmp/kraft-combined-logs-<index>` directories.
On Windows, Kafka/Java path handling determines the corresponding log location. Server reset and disposal
can terminate Java processes on the benchmark host, including Java processes unrelated to Kafka.
Do not run on a system hosting other Java applications or valuable Kafka data.

For repeatable comparisons, use dedicated disposable systems and reset Kafka processes, logs/topics,
Virtual Client state, and dependency caches between runs. Compare identical OS images, machine sizes,
profiles, and parameters. Report performance variation separately from command and metric-schema parity.
