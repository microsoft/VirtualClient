# Compression/Decompression Workloads Metrics
The following document illustrates the type of results that are emitted by the compression/decompression workloads and captured by the
Virtual Client for net impact analysis.



### Workload-Specific Metrics
The following metrics are emitted by the compression/decompression workloads itself.

| Execution Profile   | Metric Name | Example Value (min) | Example Value (max) | Example Value (avg) | Unit |
|---------------------|-----------|-------------|---------------------|---------------------|---------------------|------|
| PERF-COMPRESSION.json (win-x64) |Compressed size and Original size ratio | 22.7 | 29.6 | 25.3 | |
| PERF-COMPRESSION.json (win-x64) |CompressionTime | 27.6 | 899.3 | 432.3 | seconds |
| PERF-COMPRESSION.json (win-arm64) |  Compressed size and Original size ratio | 21.4 | 26.5 | 24.9| |
| PERF-COMPRESSION.json (win-arm64) |CompressionTime | 25.4 | 950.4 | 500.7 | seconds |
| PERF-COMPRESSION.json (linux-x64) | ReductionRatio | 28.8 | 90.5 | 65.4 |  |
| PERF-COMPRESSION.json (linux-arm64) | ReductionRatio | 29.0 | 88.2 | 69.2 |  |
| PERF-COMPRESSION.json (linux-x64) | Decompression Speed(tornado 0.6a -9) | 170 | 200 | 190 | MB/s |
| PERF-COMPRESSION.json (linux-x64)  | Compression Speed(tornado 0.6a -9) | 5.1 | 5.95 | 5.4 | MB/s |
| PERF-COMPRESSION.json (linux-x64) |Compressed size and original size ratio(tornado 0.6a -9) | 25 | 29 | 27 | |
| PERF-COMPRESSION.json (linux-arm64) | Decompression Speed(tornado 0.6a -1) | 500 | 550 | 520 | MB/s |
| PERF-COMPRESSION.json (linux-arm64) |Compression Speed(tornado 0.6a -1) | 400 | 480 | 440 | MB/s |
| PERF-COMPRESSION.json (linux-arm64) |  Compressed size and original size ratio(tornado 0.6a -1) | 45.5 | 52 | 50.52| |
| PERF-COMPRESSION.json (linux-x64)  | CompressionTime | 1.5 | 3.9 | 2.5 | seconds |
| PERF-COMPRESSION.json (linux-x64) |Compressed size and original size ratio | 25.1 | 25.9 | 25.2| |
| PERF-COMPRESSION.json (linux-x64) |Decompressed size and original size ratio | 378.6 | 378.6 | 378.6| |
| PERF-COMPRESSION.json (linux-arm64) |CompressionTime | 1.7 | 3.9 | 3.2 | seconds |
| PERF-COMPRESSION.json (linux-arm64) |  Compressed size and original size ratio | 25.1 | 25.9 | 25.2| |
| PERF-COMPRESSION.json (linux-x64) |Decompressed size and original size ratio | 378.6 | 378.6 | 378.6| |

