# STREAM
STREAM is a synthetic benchmark designed to measure sustainable memory bandwidth and the corresponding computation rate for simple vector kernels. 
It is intended to provide a measure of memory performance independent of any particular computing platform's cache hierarchy, and has become a 
de-facto industry standard for measuring memory bandwidth.

* [STREAM Official Site](https://www.cs.virginia.edu/stream/)
* [Intel's STREAM TRIAD Documentation](https://www.intel.com/content/www/us/en/developer/articles/technical/optimizing-memory-bandwidth-on-stream-triad.html)

## What is Being Measured?
The STREAM benchmark measures sustainable memory bandwidth (in MB/s where 1 MB = 10^6 bytes, not 2^20 bytes) using four simple vector kernels. 
These kernels are designed to be simple enough to avoid introducing computational bottlenecks while being complex enough to be representative of 
real application behavior. Each kernel represents a common pattern found in scientific and engineering applications.

The STREAM benchmark runs the following memory bandwidth tests:

| Bandwidth Benchmark   | Description                                               |
|-----------------------|-----------------------------------------------------------|
| Copy                  | Measures memory copy operation speeds (a(i) = b(i))       |
| Scale                 | Measures memory scale operation speeds (a(i) = q*b(i))    |
| Add                   | Measures memory add operation speeds (a(i) = b(i) + c(i)) |
| Triad                 | Measures memory triad operation speeds (a(i) = b(i) + q*c(i)) |

## Workload Metrics
The following metrics are examples of those captured by the Virtual Client when running the STREAM workload. Virtual Client supports both the 
standard STREAM benchmark and the Microsoft-optimized STREAM implementation which provides additional metrics including latency measurements 
and the Write operation.

### Standard STREAM Metrics

| Metric Name | Example Value (min) | Example Value (max) | Example Value (avg) | Unit |
|-------------|---------------------|---------------------|---------------------|------|
| Best Rate Add | 8635.5 | 327893.5 | 42849.75 | MB/s |
| Best Rate Copy | 6787.4 | 346279.0 | 30720.40 | MB/s |
| Best Rate Scale | 6747.1 | 320023.2 | 30578.70 | MB/s |
| Best Rate Triad | 10141.2 | 305781.6 | 42735.67 | MB/s |

### Microsoft STREAM Metrics
The Microsoft STREAM implementation provides additional detailed metrics including minimum, average, and best rates for all operations, 
as well as latency measurements. It also includes a Write operation in addition to the standard STREAM operations.

#### Bandwidth Metrics

| Metric Name | Example Value (min) | Example Value (max) | Example Value (avg) | Unit |
|-------------|---------------------|---------------------|---------------------|------|
| Best Rate Add | 53351.0 | 54544.0 | 54011.50 | MB/s |
| Best Rate Copy | 72073.0 | 74208.0 | 73171.83 | MB/s |
| Best Rate Read | 48497.0 | 51087.0 | 50461.00 | MB/s |
| Best Rate Scale | 72486.0 | 74990.0 | 73716.00 | MB/s |
| Best Rate Triad | 54780.0 | 56567.0 | 55725.50 | MB/s |
| Best Rate Write | 87466.0 | 133326.0 | 116689.67 | MB/s |
| Avg Rate Add | 52848.0 | 54074.0 | 53553.17 | MB/s |
| Avg Rate Copy | 71016.0 | 73323.0 | 72106.67 | MB/s |
| Avg Rate Read | 47981.0 | 50433.0 | 49814.33 | MB/s |
| Avg Rate Scale | 71056.0 | 73315.0 | 72425.50 | MB/s |
| Avg Rate Triad | 53849.0 | 55720.0 | 55032.50 | MB/s |
| Avg Rate Write | 86252.0 | 124882.0 | 105829.00 | MB/s |
| Min Rate Add | 52199.0 | 53658.0 | 52981.17 | MB/s |
| Min Rate Copy | 70192.0 | 72302.0 | 71153.50 | MB/s |
| Min Rate Read | 47600.0 | 50057.0 | 49066.33 | MB/s |
| Min Rate Scale | 70110.0 | 72308.0 | 71063.83 | MB/s |
| Min Rate Triad | 53180.0 | 55407.0 | 54307.67 | MB/s |
| Min Rate Write | 84744.0 | 118017.0 | 96797.83 | MB/s |

#### Latency Metrics

| Metric Name | Example Value (min) | Example Value (max) | Example Value (avg) | Unit |
|-------------|---------------------|---------------------|---------------------|------|
| Avg Latency Add | 144.0 | 161.0 | 151.17 | nanoseconds |
| Avg Latency Copy | 155.0 | 183.0 | 164.83 | nanoseconds |
| Avg Latency Read | 144.0 | 152.0 | 147.50 | nanoseconds |
| Avg Latency Scale | 155.0 | 179.0 | 161.83 | nanoseconds |
| Avg Latency Triad | 141.0 | 163.0 | 151.67 | nanoseconds |
| Avg Latency Write | 196.0 | 377.0 | 261.17 | nanoseconds |
| Max Latency Add | 158.0 | 189.0 | 172.67 | nanoseconds |
| Max Latency Copy | 173.0 | 203.0 | 183.83 | nanoseconds |
| Max Latency Read | 156.0 | 181.0 | 168.50 | nanoseconds |
| Max Latency Scale | 169.0 | 205.0 | 178.67 | nanoseconds |
| Max Latency Triad | 157.0 | 193.0 | 171.00 | nanoseconds |
| Max Latency Write | 220.0 | 446.0 | 299.17 | nanoseconds |
| Min Latency Add | 126.0 | 145.0 | 134.50 | nanoseconds |
| Min Latency Copy | 143.0 | 169.0 | 151.67 | nanoseconds |
| Min Latency Read | 121.0 | 135.0 | 128.00 | nanoseconds |
| Min Latency Scale | 137.0 | 159.0 | 145.50 | nanoseconds |
| Min Latency Triad | 128.0 | 151.0 | 135.17 | nanoseconds |
| Min Latency Write | 175.0 | 319.0 | 224.00 | nanoseconds |