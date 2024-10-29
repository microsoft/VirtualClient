# Rally

Rally is an open-source tool that benchmarks Elasticsearch. It can also:

* Setup and teardown a new Elasticsearch cluster
* Manage benchmark data across Elasticsearch versions
* Runs various benchmarks and collects metrics
* Compare performance results

This is all possible on single VM, server-client, and cluster-client scenarios.

* [Rally Source Code](https://github.com/elastic/rally)
* [Rally Documentation](https://esrally.readthedocs.io/en/2.9.0/index.html)

## Rally Setup

Rally is only supported on Linux. With the right dependencies installed, Rally can be installed with the following command:

``` bash
python3 -m pip install esrally
```

VC installs esrally in an isolated Python environment using venv to install and run Rally without elevated privileges.

For best results, it is recommended to alter the root directories of the rally.ini config file (under the /home/user/.rally directory) to point to the data directory of an attached disk. The default is to place this on the OS disk, but the benchmark files may take up more space than is available (nyc_taxis track, for example, takes up 79G). VC automatically takes care of this on execution.

Furthermore, on the server-side, it is best to set the vm.max_map_count to the maximum value. This expands the amount of memory map areas a process has. Without increasing it, Rally may fail if it does not have enough memory map areas to prepare and operate.

On a client-server or client-cluster scenario, the Rally daemon should be started as follows, allowing for seamless communication:

```bash
# on the client
esrallyd start --node-ip=IP_OF_COORDINATOR_NODE --coordinator-ip=IP_OF_COORDINATOR_NODE

# on any server node
esrallyd start --node-ip=IP_OF_THIS_NODE --coordinator-ip=IP_OF_COORDINATOR_NODE
```

Then, rally can be executed with the following command on the client to set up an Elasticsearch node on any servers, and run the benchmark from the coordinator-side.

```bash
esrally race --track={track_name} --distribution-version={distribution_version} --target-hosts={server_ip_1}:39200,{server_ip_2}:39200
```

If only the benchmark execution is needed, ie. an Elasticsearch server already exists (either on Windows or Linux), Rally can be run with the option --pipeline=benchmark-only.

## What is Being Measured?

Rally hosts a various amount of "tracks", each of which are essentially test suites with a collection of tasks. Some tasks are similar between tracks (eg. "index", "index-append", "default"), and some are unique to the track. Tracks can also be created independent of Rally and used by the workload. Examples of popular tracks are listed below.

| Track Name        | Description   | Size (in GB)  |
|-------------------|---------------|---------------|
| geonames      | POIs from Geonames    |  3.6      |
| geopoint      | Point coordinates from PlanetOSM      |   2.8     |
| http_logs     |   HTTP server log data                |   33      |
| nyc_taxis     | Taxi rides in New York in 2015        |   79      |
| noaa          |   Global daily weather measurements from NOAA     | 10    |
| pmc           | Full text benchmark with academic papers from PMC |       |

## Workload Metrics

The following metrics are examples of those captured by Virtual Client when running the Rally-Elasticsearch workload with the geonames track. Utilize the RallyClientExecutor Parameter "MetricsFilters": "Verbosity:2" for metrics of higher detail.

Each task is measured for throughput, service time, processing time, and latency. Throughput metrics are recorded for median, min, max, and mean. The others are recorded for 50th, 90th, 99th, and 100th percentiles, as well as the mean. With a metrics verbosity of 1 (the VC default), the median throughput as well as the 50th and 90th percentiles are taken to output in the final telemetry.

| Metric Name | Example Value | Unit |
|-------------|---------------|------|
| index-append_throughput_median | 191.7326520 | docs/s |
| index-append_service_time_50_0 | 3151.838972 | ms     |
| index-append_latency_90_0 | 16505.7971 | ms           |
| term_throughput_median    | 19.8840623 | ops/s        |
| term_latency_50_0         | 13.6623771 | ms           |
| term_processing_time_90_0 | 16.5516432 | ms           |
| scroll_throughput_median  | 12.5401983 | pages/s      |
| scroll_service_time_90_0  | 950.841824 | ms           |
| scroll_processing_time_50_0 | 925.6542731 | ms        |
