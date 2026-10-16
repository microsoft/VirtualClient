---
slug: /workloads/kafka-profiles
---

# Kafka Workload Profiles

* [Workload details](./kafka.md)
* [Client/server workloads](/docs/guides/client-server)
* [PERF-KAFKA.json](https://github.com/microsoft/VirtualClient/blob/main/src/VirtualClient/VirtualClient.Main/profiles/PERF-KAFKA.json)

## PERF-KAFKA.json

Runs Kafka's producer and consumer performance tools against local or remote KRaft brokers.
Supports linux-x64, linux-arm64, win-x64, and win-arm64. Internet/package-store access is required
to install dependencies on a fresh system. The profile recommends a minimum execution time of two hours;
the time to complete an iteration depends on the system, record counts, and concurrency.

### Profile parameters

Supply overrides with `--parameters="Name=Value,,,Name=Value"`.

| Parameter | Default | Purpose |
|---|---|---|
| `ServerInstanceCount` | 3 | Number of broker processes on the single Server system |
| `ClientInstanceCount` | 3 | Concurrent clients for the explicitly concurrent producer/consumer actions |
| `RecordSize` | 100 | Bytes per producer record |
| `Partitions` | 6 | Partitions per topic |
| `RecordCount` | 5000000 | Records requested per producer or consumer process |

The profile passes `ClientInstanceCount` as the executors' `MaxClientInstanceCount`.
The effective upper limit is `min(Partitions, 6)`: topic-creation actions validate against the profile's
`Partitions`, but producer and consumer actions do not receive that parameter and validate against the
executor default of 6. Increasing `Partitions` alone does not allow more than 6 client instances in
the shipped profile.

### Component parameters and fixed settings

| Parameter | Purpose |
|---|---|
| `Scenario` | Action/scenario identifier, also used to label emitted metrics |
| `PackageName` | Registered Kafka package, `kafka` |
| `Role` | `Server` or `Client` |
| `JdkPackageName` | Registered JDK package; defaults to `javadevelopmentkit` |
| `CommandType` | Client operation: `CreateTopic`, `ProducerTest`, or `ConsumerTest` |
| `CommandLine` | Arguments for the selected Kafka script; the executor formats the server address |
| `WarmUp` | Run the action once per component lifetime; defaults to false |
| `ClientInstanceCount` | Client process concurrency; executor default is 3 |
| `MaxClientInstanceCount` | Count validated against partitions; executor default is 3 |
| `ServerInstanceCount` | Broker count; executor default is 3 |
| `Partitions` | Partition count; executor default is 6 |

Executors retain compatibility aliases `ServerInstances`, `ClientInstances`, and `MaxClientInstances`
when used in custom component definitions. The shipped profile uses the canonical parameter names above.

Broker ports are fixed at 9092 plus the zero-based broker index: defaults are 9092, 9093, and 9094.
Controller ports are formed by prefixing `2` to the broker port: defaults are 29092, 29093, and 29094.
`Port` is not a configurable server property in this executor. Permit these ports and the configured
Virtual Client API port (default 4500) between the participating systems in host firewalls and network rules.

Producer commands use unlimited target throughput (`--throughput -1`), `acks=1`,
`buffer.memory=67108864`, and batch sizes of 8196 or 64000. These settings belong to the profile's
`CommandLine` definitions, not additional global parameter overrides.

### Single-system examples

An environment layout is not required. Both roles execute locally. Run with elevated privileges:

```bash
sudo ./VirtualClient --profile=PERF-KAFKA.json --system=Demo --timeout=1440

# Reduced record count for a smoke run, not a representative performance result.
sudo ./VirtualClient --profile=PERF-KAFKA.json --system=Demo --timeout=30 --parameters="RecordCount=100000"
```

```powershell
# Elevated PowerShell
.\VirtualClient.exe --profile=PERF-KAFKA.json --system=Demo --timeout=1440
```

The timeout is in minutes. Ensure it allows all required actions to finish.

### Client/server topology

Supply the same environment layout to both systems. Roles are `Client` and `Server`; names must match
the corresponding `--client-id` or system name, and IP addresses must be reachable.
Use exactly one Server entry. `ServerInstanceCount` controls the brokers on that system; distributing
those brokers across multiple Server entries is not supported by this executor.

```json
{
  "clients": [
    { "name": "Client01", "role": "Client", "ipAddress": "10.1.0.1" },
    { "name": "Server01", "role": "Server", "ipAddress": "10.1.0.2" }
  ]
}
```

```bash
# Server system
sudo ./VirtualClient --profile=PERF-KAFKA.json --system=Demo --timeout=1440 --client-id=Server01 --layout=/path/to/layout.json

# Client system
sudo ./VirtualClient --profile=PERF-KAFKA.json --system=Demo --timeout=1440 --client-id=Client01 --layout=/path/to/layout.json
```

On Windows, use `.\VirtualClient.exe` from an elevated shell and a Windows path for `--layout`.
Apply identical profile-parameter overrides on Client and Server systems.

### Interpreting results

The scenario names containing `sync` and `async` are profile labels; consult the command and client count
when interpreting concurrency. The actions without an explicit `ClientInstanceCount` also use the
executor's default of 3, so a `sync` label does not imply a single process.

Topic-creation and producer actions have `WarmUp=true`. Consumer actions generate the emitted metrics
described in [workload details](./kafka.md). Check both process logs and metric output when validating a run.
