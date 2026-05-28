# STREAM Workload

STREAM is a benchmark for measuring memory bandwidth performance .

* [Official STREAM Benchmark Documentation](https://www.cs.virginia.edu/stream/)
* [Intels STREAM TRIAD Documentaion](https://www.intel.com/content/www/us/en/developer/articles/technical/optimizing-memory-bandwidth-on-stream-triad.html)

---

### What is Being Tested?

The following performance analysis tests are ran as part of the STREAM workload. It measures the Memory Bandwidth in MB/s (1 MB=10^6 B, *not* 2^20 B)


| Bandwidth Benchmark   | Description                                               |
|-----------------------|-----------------------------------------------------------|
| Copy                  | Measures memory copy operation speeds                     |
| Scale                 | Measures memory scale operation speeds                    |
| Add                   | Measures memory add operation speeds                      |
| Triad                 | Measures memory triad operation speeds                    |

---

### Supported Platform/Architectures

* linux-x64
* linux-arm64

### MSFT STREAM Parameters:
  --rnd-threads RND_THREADS: Number of total threads to randomize the work.
  --threads THREADS: Number of threads to split the work.
  --lat-threads LAT_THREADS: Number of threads running latency test
  --l2tol2-threads L2toL@_THREADS: Number of threads running latency test from L2 to L2
  --bw-array BW_ARRAY_SIZE: Array size in 1KB blocks per thread to measure bandwidth.
  --lat-array LAT_ARRAY_SIZE: Array size in 1KB blocks to measure latency.
  --lat-accesses LAT_ACCESSES: Number of accesses to measure latency.
  --lat-seq : Measure latency using sequential address on a 64B basic.
  --l2tol2-array L2toL2_ARRAY_SIZE: Array size in 1KB blocks to measure L2 to L2 latency.
  --iter ITERATIONS: Number of iterations to measure performance.
  --internal-iter INTERNAL_ITER: Internal iteration for bandwidth loop to guarantee stable results.
  --internal-iter-lat INTERNAL_ITER_LAT: Internal iteration for latency loop to guarantee stable results.
  --pattern PATTERN: Type of pattern, 0 is fixed, 1 is maximum switch and 2 is fully random.
  --enable-lat ENABLE_LAT: Measure latency accessing to a vector randomly
  --enable-l2tol2 ENABLE_L2toL2: Measure latency accessing remote l2 vector randomly. This test disables all other
  --verbose VERBOSE: 0 to no verborse level, 1 is basic verbose level to check synch points and 2 is for debug.
  --limit-time SECONDS: Number of seconds to run per iteration
  --kernel KERNEL: 0 is READ
                   1 is COPY
                   2 is SCALE
                   3 is ADD
                   4 is TRIAD
                   5 is WRITE
                   6 is ALL (0-5)
                   7 is NONE (ENABLE_LAT is required on this mode)
                   8 is 1R1W
                   9 is 2R1W
  --total-size-kb TOTAL_SIZE: Total Array size in KB to measure bandwidth, the tool will split it across all threads.
  --total-size-mb TOTAL_SIZE: Total Array size in MB to measure bandwidth, the tool will split it across all threads.
  --total-size-gb TOTAL_SIZE: Total Array size in GB to measure bandwidth, the tool will split it across all threads.
  --use-scalar: This parameters enables standard scalar kernels
  --use-sve: This parameters enables SVE kernels
  --use-neon: This parameters enables NEON 8.1 kernels (default)
  --use-numa: This parameters forces each thread to alloc the memory to be NUMA aware
  --numa-sequential: This parameters forces NUMA0 until it's fully used
  --numa-inverted: This parameters forces memory allocation on the oposite NUMA node
  --numa-alloc node_id: This parameters forces memory allocation on a fix NUMA node
  --silent: This parameters disables all the outputs except the performance report
  --perCoreReport: This parameters the performance report per core
  --streams-seq: This parameters runs the streams kernels in the same order
  --help provides this info