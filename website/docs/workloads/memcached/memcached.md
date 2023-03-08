# Memcached
Memcached is an open source (BSD licensed), high-performance, distributed memory object caching system. Memcached is an in-memory key-value store for small 
arbitrary data (strings, objects) from results of database calls, API calls, or page rendering. Memcached works with an in-memory dataset. It is a client-server
model workload in which Memcached acts as server. There are different tools that acts are clients.

One of the widely used is the memtier_benchmark produced by Redis Labs.
* [Memcached Performance](https://github.com/memcached/memcached/wiki/Performance)  
* [Memtier Benchmarking Tool](https://redis.com/blog/memtier_benchmark-a-high-throughput-benchmarking-tool-for-redis-memcached/)
* [Official Memcached Documentation](https://memcached.org/about)
* [Memcached Github Repo](https://github.com/memcached/memcached)
* [Memtier Benchmark Toolset](https://github.com/RedisLabs/memtier_benchmark)

## What is Being Measured?
The Memtier toolset is used to generate various traffic patterns against Memcached instances. It provides a robust set of customization and reporting 
capabilities all wrapped into a convenient and easy-to-use command-line interface. It performs GET and SET operations against a Memcached server 
and gives percentile latency distributions and throughput.

## Workload Metrics
The following metrics are examples of those captured by the Virtual Client when running the Memtier workload against a
Memcached server.

| Scenario Name   | Metric Name | Example Value (min) | Example Value (max) | Example Value (avg) | Unit |
|-----------------|-------------|---------------------|---------------------|---------------------|------|
| Memtier_4t_1c | p50lat | 0.159 | 4.095 | 0.2965735294117649 | milliseconds |
| Memtier_4t_1c | p90lat | 2.799 | 7.807 | 3.6170735294117639 | milliseconds |
| Memtier_4t_1c | p95lat | 3.743 | 9.535 | 4.346150735294118 | milliseconds |
| Memtier_4t_1c | p99.9lat | 11.519 | 61.951 | 16.81158823529412 | milliseconds |
| Memtier_4t_1c | p99lat | 5.023 | 16.319 | 6.137676470588234 | milliseconds |
| Memtier_4t_1c | throughput | 58417.19 | 815710.22 | 235439.3486305147 | operations/sec |
| Memtier_4t_1c | throughput_1 | 29092.65 | 203673.3 | 111331.60009650735 | operations/sec |
| Memtier_4t_1c | throughput_2 | 29315.66 | 206249.97 | 113622.22872585096 | operations/sec |
| Memtier_4t_1c | throughput_3 | 176794.47 | 206504.79 | 193725.01733333333 | operations/sec |
| Memtier_4t_1c | throughput_4 | 179516.37 | 203843.86 | 190337.24200000004 | operations/sec |
| Memtier_4t_2c | p50lat | 0.335 | 4.415 | 1.52554930875576 | milliseconds |
| Memtier_4t_2c | p90lat | 3.951 | 18.943 | 4.581628571428574 | milliseconds |
| Memtier_4t_2c | p95lat | 4.223 | 21.759 | 5.332559447004607 | milliseconds |
| Memtier_4t_2c | p99.9lat | 12.671 | 72.703 | 19.31950875576037 | milliseconds |
| Memtier_4t_2c | p99lat | 5.439 | 29.439 | 7.0393244239631349 | milliseconds |
| Memtier_4t_2c | throughput | 65999.11 | 797961.14 | 245532.84887788018 | operations/sec |
| Memtier_4t_2c | throughput_1 | 32380.33 | 202134.5 | 119005.53673041475 | operations/sec |
| Memtier_4t_2c | throughput_2 | 33331.74 | 204258.16 | 117608.7032130384 | operations/sec |
| Memtier_4t_2c | throughput_3 | 169854.02 | 200916.3 | 185826.25166666663 | operations/sec |
| Memtier_4t_2c | throughput_4 | 167405.91 | 194783.81 | 180833.36850000005 | operations/sec |
| Memtier_4t_4c | p50lat | 1.183 | 15.359 | 2.7599138090824835 | milliseconds |
| Memtier_4t_4c | p90lat | 4.799 | 26.367 | 11.749524559777568 | milliseconds |
| Memtier_4t_4c | p95lat | 5.215 | 30.719 | 13.026113994439293 | milliseconds |
| Memtier_4t_4c | p99.9lat | 20.735 | 83.967 | 33.21849953660797 | milliseconds |
| Memtier_4t_4c | p99lat | 6.335 | 41.471 | 18.379381835032434 | milliseconds |
| Memtier_4t_4c | throughput | 68264.98 | 817778.44 | 255551.02398053756 | operations/sec |
| Memtier_4t_4c | throughput_1 | 32987.04 | 210675.5 | 122982.00023864689 | operations/sec |
| Memtier_4t_4c | throughput_2 | 34447.69 | 206027.13 | 121898.65999073001 | operations/sec |
| Memtier_4t_4c | throughput_3 | 176203.54 | 209177.93 | 193846.44791666664 | operations/sec |
| Memtier_4t_4c | throughput_4 | 172918.81 | 203408.28 | 190946.7905 | operations/sec |
| Memtier_8t_2c | p50lat | 0.631 | 15.487 | 1.052861552853134 | milliseconds |
| Memtier_8t_2c | p90lat | 5.503 | 50.943 | 13.044298409728715 | milliseconds |
| Memtier_8t_2c | p95lat | 13.055 | 59.647 | 14.339385406922359 | milliseconds |
| Memtier_8t_2c | p99.9lat | 28.671 | 180.223 | 36.93806339181287 | milliseconds |
| Memtier_8t_2c | p99lat | 17.535 | 91.135 | 22.5084705332086 | milliseconds |
| Memtier_8t_2c | throughput | 57703.76 | 827385.88 | 257429.3466534144 | operations/sec |
| Memtier_8t_2c | throughput_1 | 32527.37 | 209887.17 | 124380.40026426566 | operations/sec |
| Memtier_8t_2c | throughput_2 | 23625.1 | 212309.54 | 122170.78199344877 | operations/sec |
| Memtier_8t_2c | throughput_3 | 181544.04 | 212802.62 | 197401.76108333336 | operations/sec |
| Memtier_8t_2c | throughput_4 | 179479.72 | 207057.14 | 192259.67658333336 | operations/sec |
| Memtier_8t_4c | p50lat | 1.231 | 30.463 | 7.030075523202911 | milliseconds |
| Memtier_8t_4c | p90lat | 14.143 | 59.391 | 16.208703366697 | milliseconds |
| Memtier_8t_4c | p95lat | 17.791 | 101.375 | 19.43780133454656 | milliseconds |
| Memtier_8t_4c | p99.9lat | 32.767 | 251.903 | 43.959487716105559 | milliseconds |
| Memtier_8t_4c | p99lat | 25.727 | 160.767 | 28.69444919623901 | milliseconds |
| Memtier_8t_4c | throughput | 60685.85 | 842329.67 | 265907.298343949 | operations/sec |
| Memtier_8t_4c | throughput_1 | 28006.45 | 215109.34 | 127300.9799454049 | operations/sec |
| Memtier_8t_4c | throughput_2 | 32502.93 | 217147.87 | 124150.41247801029 | operations/sec |
| Memtier_8t_4c | throughput_3 | 184452.87 | 211285.71 | 197710.7305 | operations/sec |
| Memtier_8t_4c | throughput_4 | 186077.54 | 213718.07 | 199465.28466666668 | operations/sec |
| Memtier_8t_8c | p50lat | 2.383 | 57.343 | 15.538551766138858 | milliseconds |
| Memtier_8t_8c | p90lat | 19.455 | 96.255 | 24.308904993909864 | milliseconds |
| Memtier_8t_8c | p95lat | 23.039 | 132.095 | 29.86248355663824 | milliseconds |
| Memtier_8t_8c | p99.9lat | 38.399 | 258.047 | 60.335019488428759 | milliseconds |
| Memtier_8t_8c | p99lat | 31.743 | 186.367 | 43.09107308160779 | milliseconds |
| Memtier_8t_8c | throughput | 64212.8 | 888657.03 | 270229.023270402 | operations/sec |
| Memtier_8t_8c | throughput_1 | 31641.23 | 230826.34 | 127721.14527405605 | operations/sec |
| Memtier_8t_8c | throughput_2 | 32303.92 | 227859.12 | 127505.81609584858 | operations/sec |
| Memtier_8t_8c | throughput_3 | 194345.26 | 221087.21 | 207721.54641666666 | operations/sec |
| Memtier_8t_8c | throughput_4 | 195513.22 | 224525.73 | 211335.26866666668 | operations/sec |
| Memtier_16t_4c | p50lat | 2.479 | 63.999 | 15.867709396390792 | milliseconds |
| Memtier_16t_4c | p90lat | 19.583 | 109.055 | 25.70262663347853 | milliseconds |
| Memtier_16t_4c | p95lat | 23.807 | 128.511 | 29.745658369632858 | milliseconds |
| Memtier_16t_4c | p99.9lat | 38.655 | 309.247 | 62.71748662103298 | milliseconds |
| Memtier_16t_4c | p99lat | 31.871 | 179.199 | 43.3985668948351 | milliseconds |
| Memtier_16t_4c | throughput | 58580.34 | 845621.72 | 253393.12167081517 | operations/sec |
| Memtier_16t_4c | throughput_1 | 28743.1 | 215014.62 | 121465.76731176104 | operations/sec |
| Memtier_16t_4c | throughput_2 | 29794.02 | 216446.78 | 124715.36404115996 | operations/sec |
| Memtier_16t_4c | throughput_3 | 183022.18 | 215183.61 | 199985.7881666667 | operations/sec |
| Memtier_16t_4c | throughput_4 | 188305.88 | 214475.28 | 200886.619 | operations/sec |
| Memtier_16t_8c | p50lat | 20.607 | 133.119 | 32.126001871490959 | milliseconds |
| Memtier_16t_8c | p90lat | 32.767 | 233.471 | 47.07609045539612 | milliseconds |
| Memtier_16t_8c | p95lat | 33.279 | 268.287 | 55.38091890205864 | milliseconds |
| Memtier_16t_8c | p99.9lat | 49.407 | 622.591 | 112.8486868371803 | milliseconds |
| Memtier_16t_8c | p99lat | 42.239 | 317.439 | 78.94473674360575 | milliseconds |
| Memtier_16t_8c | throughput | 55016.71 | 887996.49 | 250613.5241048035 | operations/sec |
| Memtier_16t_8c | throughput_1 | 27740.86 | 224915.68 | 122564.99112913289 | operations/sec |
| Memtier_16t_8c | throughput_2 | 27275.85 | 224914.49 | 120324.29177756951 | operations/sec |
| Memtier_16t_8c | throughput_3 | 199133.29 | 220958.03 | 209582.30199999998 | operations/sec |
| Memtier_16t_8c | throughput_4 | 198042.78 | 224867.07 | 213176.67700000006 | operations/sec |
| Memtier_16t_16c | p50lat | 33.535 | 344.063 | 64.34645722344094 | milliseconds |
| Memtier_16t_16c | p90lat | 52.991 | 518.143 | 91.89321247257914 | milliseconds |
| Memtier_16t_16c | p95lat | 57.343 | 557.055 | 111.11832309620807 | milliseconds |
| Memtier_16t_16c | p99.9lat | 69.631 | 978.943 | 204.75744312127865 | milliseconds |
| Memtier_16t_16c | p99lat | 62.975 | 724.991 | 151.58678063303038 | milliseconds |
| Memtier_16t_16c | throughput | 45402.08 | 903296.37 | 248871.23561892825 | operations/sec |
| Memtier_16t_16c | throughput_1 | 22297.41 | 230097.93 | 120755.43762770292 | operations/sec |
| Memtier_16t_16c | throughput_2 | 23104.67 | 231646.96 | 120205.79481632651 | operations/sec |
| Memtier_16t_16c | throughput_3 | 204582.28 | 230185.17 | 217630.71233333337 | operations/sec |
| Memtier_16t_16c | throughput_4 | 200293.43 | 232077.36 | 215070.2026666667 | operations/sec |