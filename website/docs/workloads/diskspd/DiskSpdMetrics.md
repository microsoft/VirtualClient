# DiskSpd Workload Metrics
The following document illustrates the type of results that are emitted by the DiskSpd workload and captured by the
Virtual Client for net impact analysis.

### Workload-Specific Metrics
The following metrics are emitted by the DiskSpd workload itself.

| Execution Profile   | Test Name | Metric Name | Example Value (min) | Example Value (max) | Example Value (avg) | Unit |
|---------------------|-----------|-------------|---------------------|---------------------|---------------------|------|
| PERF-IO-DISKSPD.json | diskspd_randread_250MB_64k_d16_th32_direct | avg. latency | 164.402 | 175.368 | 164.6749014084505 | milliseconds |
| PERF-IO-DISKSPD.json | diskspd_randread_250MB_64k_d16_th32_direct | iops stdev | 0.4 | 358.43 | 48.438098591549287 | milliseconds |
| PERF-IO-DISKSPD.json | diskspd_randread_250MB_64k_d16_th32_direct | latency stdev | 13.791 | 35.869 | 19.055985915492955 | milliseconds |
| PERF-IO-DISKSPD.json | diskspd_randread_250MB_64k_d16_th32_direct | read IO operations | 1401358.0 | 1494891.0 | 1492452.133802817 |  |
| PERF-IO-DISKSPD.json | diskspd_randread_250MB_64k_d16_th32_direct | read avg. latency | 164.402 | 175.368 | 164.6749014084505 | milliseconds |
| PERF-IO-DISKSPD.json | diskspd_randread_250MB_64k_d16_th32_direct | read iops | 2919.48 | 3114.32 | 3109.2208450704245 |  |
| PERF-IO-DISKSPD.json | diskspd_randread_250MB_64k_d16_th32_direct | read iops stdev | 0.4 | 358.43 | 48.438098591549287 | milliseconds |
| PERF-IO-DISKSPD.json | diskspd_randread_250MB_64k_d16_th32_direct | read latency stdev | 13.791 | 35.869 | 19.055985915492955 | milliseconds |
| PERF-IO-DISKSPD.json | diskspd_randread_250MB_64k_d16_th32_direct | read throughput | 182.47 | 194.65 | 194.32626760563375 | MiB/sec |
| PERF-IO-DISKSPD.json | diskspd_randread_250MB_64k_d16_th32_direct | read total bytes | 91839397888.0 | 97969176576.0 | 97809343040.90142 |  |
| PERF-IO-DISKSPD.json | diskspd_randread_250MB_64k_d16_th32_direct | total IO operations | 1401358.0 | 1494891.0 | 1492452.133802817 |  |
| PERF-IO-DISKSPD.json | diskspd_randread_250MB_64k_d16_th32_direct | total bytes | 91839397888.0 | 97969176576.0 | 97809343040.90142 |  |
| PERF-IO-DISKSPD.json | diskspd_randread_250MB_64k_d16_th32_direct | total iops | 2919.48 | 3114.32 | 3109.2208450704245 |  |
| PERF-IO-DISKSPD.json | diskspd_randread_250MB_64k_d16_th32_direct | total throughput | 182.47 | 194.65 | 194.32626760563375 | MiB/sec |
| PERF-IO-DISKSPD.json | diskspd_randread_4GB_12k_d1_th1_direct | avg. latency | 1.495 | 3.118 | 1.8799295774647882 | milliseconds |
| PERF-IO-DISKSPD.json | diskspd_randread_4GB_12k_d1_th1_direct | iops stdev | 4.05 | 83.73 | 19.523521126760558 | milliseconds |
| PERF-IO-DISKSPD.json | diskspd_randread_4GB_12k_d1_th1_direct | latency stdev | 0.207 | 4.037 | 0.6115492957746479 | milliseconds |
| PERF-IO-DISKSPD.json | diskspd_randread_4GB_12k_d1_th1_direct | read IO operations | 153891.0 | 320880.0 | 259548.92253521126 |  |
| PERF-IO-DISKSPD.json | diskspd_randread_4GB_12k_d1_th1_direct | read avg. latency | 1.495 | 3.118 | 1.8799295774647882 | milliseconds |
| PERF-IO-DISKSPD.json | diskspd_randread_4GB_12k_d1_th1_direct | read iops | 320.61 | 668.48 | 540.7183098591552 |  |
| PERF-IO-DISKSPD.json | diskspd_randread_4GB_12k_d1_th1_direct | read iops stdev | 4.05 | 83.73 | 19.523521126760558 | milliseconds |
| PERF-IO-DISKSPD.json | diskspd_randread_4GB_12k_d1_th1_direct | read latency stdev | 0.207 | 4.037 | 0.6115492957746478 | milliseconds |
| PERF-IO-DISKSPD.json | diskspd_randread_4GB_12k_d1_th1_direct | read throughput | 3.76 | 7.83 | 6.33654929577465 | MiB/sec |
| PERF-IO-DISKSPD.json | diskspd_randread_4GB_12k_d1_th1_direct | read total bytes | 1891012608.0 | 3942973440.0 | 3189337160.112676 |  |
| PERF-IO-DISKSPD.json | diskspd_randread_4GB_12k_d1_th1_direct | total IO operations | 153891.0 | 320880.0 | 259548.92253521126 |  |
| PERF-IO-DISKSPD.json | diskspd_randread_4GB_12k_d1_th1_direct | total bytes | 1891012608.0 | 3942973440.0 | 3189337160.112676 |  |
| PERF-IO-DISKSPD.json | diskspd_randread_4GB_12k_d1_th1_direct | total iops | 320.61 | 668.48 | 540.7183098591552 |  |
| PERF-IO-DISKSPD.json | diskspd_randread_4GB_12k_d1_th1_direct | total throughput | 3.76 | 7.83 | 6.33654929577465 | MiB/sec |
| PERF-IO-DISKSPD.json | diskspd_randread_4GB_16k_d1_th1_direct | avg. latency | 1.511 | 3.047 | 1.8567517730496455 | milliseconds |
| PERF-IO-DISKSPD.json | diskspd_randread_4GB_16k_d1_th1_direct | iops stdev | 5.01 | 72.58 | 17.65836879432625 | milliseconds |
| PERF-IO-DISKSPD.json | diskspd_randread_4GB_16k_d1_th1_direct | latency stdev | 0.198 | 7.971 | 0.52777304964539 | milliseconds |
| PERF-IO-DISKSPD.json | diskspd_randread_4GB_16k_d1_th1_direct | read IO operations | 157452.0 | 317445.0 | 262927.7304964539 |  |
| PERF-IO-DISKSPD.json | diskspd_randread_4GB_16k_d1_th1_direct | read avg. latency | 1.511 | 3.047 | 1.8567517730496455 | milliseconds |
| PERF-IO-DISKSPD.json | diskspd_randread_4GB_16k_d1_th1_direct | read iops | 328.02 | 661.34 | 547.7575177304965 |  |
| PERF-IO-DISKSPD.json | diskspd_randread_4GB_16k_d1_th1_direct | read iops stdev | 5.01 | 72.58 | 17.658368794326245 | milliseconds |
| PERF-IO-DISKSPD.json | diskspd_randread_4GB_16k_d1_th1_direct | read latency stdev | 0.198 | 7.971 | 0.5277730496453899 | milliseconds |
| PERF-IO-DISKSPD.json | diskspd_randread_4GB_16k_d1_th1_direct | read throughput | 5.13 | 10.33 | 8.55879432624113 | MiB/sec |
| PERF-IO-DISKSPD.json | diskspd_randread_4GB_16k_d1_th1_direct | read total bytes | 2579693568.0 | 5201018880.0 | 4307807936.4539 |  |
| PERF-IO-DISKSPD.json | diskspd_randread_4GB_16k_d1_th1_direct | total IO operations | 157452.0 | 317445.0 | 262927.7304964539 |  |
| PERF-IO-DISKSPD.json | diskspd_randread_4GB_16k_d1_th1_direct | total bytes | 2579693568.0 | 5201018880.0 | 4307807936.4539 |  |
| PERF-IO-DISKSPD.json | diskspd_randread_4GB_16k_d1_th1_direct | total iops | 328.02 | 661.34 | 547.7575177304965 |  |
| PERF-IO-DISKSPD.json | diskspd_randread_4GB_16k_d1_th1_direct | total throughput | 5.13 | 10.33 | 8.55879432624113 | MiB/sec |
| PERF-IO-DISKSPD.json | diskspd_randread_4GB_32k_d1_th1_direct | avg. latency | 1.524 | 3.141 | 1.8830704225352107 | milliseconds |
| PERF-IO-DISKSPD.json | diskspd_randread_4GB_32k_d1_th1_direct | iops stdev | 4.87 | 79.98 | 17.089788732394366 | milliseconds |
| PERF-IO-DISKSPD.json | diskspd_randread_4GB_32k_d1_th1_direct | latency stdev | 0.179 | 6.169 | 0.46878873239436588 | milliseconds |
| PERF-IO-DISKSPD.json | diskspd_randread_4GB_32k_d1_th1_direct | read IO operations | 152786.0 | 314776.0 | 259519.4295774648 |  |
| PERF-IO-DISKSPD.json | diskspd_randread_4GB_32k_d1_th1_direct | read avg. latency | 1.524 | 3.141 | 1.8830704225352107 | milliseconds |
| PERF-IO-DISKSPD.json | diskspd_randread_4GB_32k_d1_th1_direct | read iops | 318.3 | 655.78 | 540.6559154929578 |  |
| PERF-IO-DISKSPD.json | diskspd_randread_4GB_32k_d1_th1_direct | read iops stdev | 4.87 | 79.98 | 17.089788732394369 | milliseconds |
| PERF-IO-DISKSPD.json | diskspd_randread_4GB_32k_d1_th1_direct | read latency stdev | 0.179 | 6.169 | 0.4687887323943658 | milliseconds |
| PERF-IO-DISKSPD.json | diskspd_randread_4GB_32k_d1_th1_direct | read throughput | 9.95 | 20.49 | 16.895422535211258 | MiB/sec |
| PERF-IO-DISKSPD.json | diskspd_randread_4GB_32k_d1_th1_direct | read total bytes | 5006491648.0 | 10314579968.0 | 8503932668.394366 |  |
| PERF-IO-DISKSPD.json | diskspd_randread_4GB_32k_d1_th1_direct | total IO operations | 152786.0 | 314776.0 | 259519.4295774648 |  |
| PERF-IO-DISKSPD.json | diskspd_randread_4GB_32k_d1_th1_direct | total bytes | 5006491648.0 | 10314579968.0 | 8503932668.394366 |  |
| PERF-IO-DISKSPD.json | diskspd_randread_4GB_32k_d1_th1_direct | total iops | 318.3 | 655.78 | 540.6559154929578 |  |
| PERF-IO-DISKSPD.json | diskspd_randread_4GB_32k_d1_th1_direct | total throughput | 9.95 | 20.49 | 16.895422535211258 | MiB/sec |
| PERF-IO-DISKSPD.json | diskspd_randread_4GB_4k_d1_th1_direct | avg. latency | 1.468 | 3.08 | 1.8431418439716308 | milliseconds |
| PERF-IO-DISKSPD.json | diskspd_randread_4GB_4k_d1_th1_direct | iops stdev | 4.76 | 64.55 | 19.404113475177306 | milliseconds |
| PERF-IO-DISKSPD.json | diskspd_randread_4GB_4k_d1_th1_direct | latency stdev | 0.206 | 3.826 | 0.4912198581560281 | milliseconds |
| PERF-IO-DISKSPD.json | diskspd_randread_4GB_4k_d1_th1_direct | read IO operations | 155804.0 | 326823.0 | 264961.9645390071 |  |
| PERF-IO-DISKSPD.json | diskspd_randread_4GB_4k_d1_th1_direct | read avg. latency | 1.468 | 3.08 | 1.8431418439716308 | milliseconds |
| PERF-IO-DISKSPD.json | diskspd_randread_4GB_4k_d1_th1_direct | read iops | 324.59 | 680.87 | 551.9943262411346 |  |
| PERF-IO-DISKSPD.json | diskspd_randread_4GB_4k_d1_th1_direct | read iops stdev | 4.76 | 64.55 | 19.404113475177306 | milliseconds |
| PERF-IO-DISKSPD.json | diskspd_randread_4GB_4k_d1_th1_direct | read latency stdev | 0.206 | 3.826 | 0.4912198581560281 | milliseconds |
| PERF-IO-DISKSPD.json | diskspd_randread_4GB_4k_d1_th1_direct | read throughput | 1.27 | 2.66 | 2.156382978723405 | MiB/sec |
| PERF-IO-DISKSPD.json | diskspd_randread_4GB_4k_d1_th1_direct | read total bytes | 638173184.0 | 1338667008.0 | 1085284206.751773 |  |
| PERF-IO-DISKSPD.json | diskspd_randread_4GB_4k_d1_th1_direct | total IO operations | 155804.0 | 326823.0 | 264961.9645390071 |  |
| PERF-IO-DISKSPD.json | diskspd_randread_4GB_4k_d1_th1_direct | total bytes | 638173184.0 | 1338667008.0 | 1085284206.751773 |  |
| PERF-IO-DISKSPD.json | diskspd_randread_4GB_4k_d1_th1_direct | total iops | 324.59 | 680.87 | 551.9943262411346 |  |
| PERF-IO-DISKSPD.json | diskspd_randread_4GB_4k_d1_th1_direct | total throughput | 1.27 | 2.66 | 2.156382978723405 | MiB/sec |
| PERF-IO-DISKSPD.json | diskspd_randread_4GB_8k_d1_th1_direct | avg. latency | 1.479 | 3.067 | 1.8674366197183095 | milliseconds |
| PERF-IO-DISKSPD.json | diskspd_randread_4GB_8k_d1_th1_direct | iops stdev | 3.91 | 106.07 | 21.24999999999999 | milliseconds |
| PERF-IO-DISKSPD.json | diskspd_randread_4GB_8k_d1_th1_direct | latency stdev | 0.196 | 5.416 | 0.5352887323943661 | milliseconds |
| PERF-IO-DISKSPD.json | diskspd_randread_4GB_8k_d1_th1_direct | read IO operations | 156464.0 | 324257.0 | 261229.8028169014 |  |
| PERF-IO-DISKSPD.json | diskspd_randread_4GB_8k_d1_th1_direct | read avg. latency | 1.479 | 3.067 | 1.8674366197183095 | milliseconds |
| PERF-IO-DISKSPD.json | diskspd_randread_4GB_8k_d1_th1_direct | read iops | 325.96 | 675.53 | 544.2195070422533 |  |
| PERF-IO-DISKSPD.json | diskspd_randread_4GB_8k_d1_th1_direct | read iops stdev | 3.91 | 106.07 | 21.24999999999999 | milliseconds |
| PERF-IO-DISKSPD.json | diskspd_randread_4GB_8k_d1_th1_direct | read latency stdev | 0.196 | 5.416 | 0.5352887323943661 | milliseconds |
| PERF-IO-DISKSPD.json | diskspd_randread_4GB_8k_d1_th1_direct | read throughput | 2.55 | 5.28 | 4.251690140845068 | MiB/sec |
| PERF-IO-DISKSPD.json | diskspd_randread_4GB_8k_d1_th1_direct | read total bytes | 1281753088.0 | 2656313344.0 | 2139994544.6760565 |  |
| PERF-IO-DISKSPD.json | diskspd_randread_4GB_8k_d1_th1_direct | total IO operations | 156464.0 | 324257.0 | 261229.8028169014 |  |
| PERF-IO-DISKSPD.json | diskspd_randread_4GB_8k_d1_th1_direct | total bytes | 1281753088.0 | 2656313344.0 | 2139994544.6760565 |  |
| PERF-IO-DISKSPD.json | diskspd_randread_4GB_8k_d1_th1_direct | total iops | 325.96 | 675.53 | 544.2195070422533 |  |
| PERF-IO-DISKSPD.json | diskspd_randread_4GB_8k_d1_th1_direct | total throughput | 2.55 | 5.28 | 4.251690140845068 | MiB/sec |
| PERF-IO-DISKSPD.json | diskspd_randrw_250MB_64k_d32_th16_direct | avg. latency | 164.476 | 166.854 | 164.55774999999975 | milliseconds |
| PERF-IO-DISKSPD.json | diskspd_randrw_250MB_64k_d32_th16_direct | iops stdev | 0.4 | 335.31 | 45.112905405405417 | milliseconds |
| PERF-IO-DISKSPD.json | diskspd_randrw_250MB_64k_d32_th16_direct | latency stdev | 15.541 | 45.258 | 20.04705405405406 | milliseconds |
| PERF-IO-DISKSPD.json | diskspd_randrw_250MB_64k_d32_th16_direct | read IO operations | 1030443.0 | 1045504.0 | 1044883.1824324324 |  |
| PERF-IO-DISKSPD.json | diskspd_randrw_250MB_64k_d32_th16_direct | read avg. latency | 162.639 | 167.098 | 165.22820945945944 | milliseconds |
| PERF-IO-DISKSPD.json | diskspd_randrw_250MB_64k_d32_th16_direct | read iops | 2146.72 | 2178.09 | 2176.8063513513518 |  |
| PERF-IO-DISKSPD.json | diskspd_randrw_250MB_64k_d32_th16_direct | read iops stdev | 23.48 | 235.25 | 47.05094594594596 | milliseconds |
| PERF-IO-DISKSPD.json | diskspd_randrw_250MB_64k_d32_th16_direct | read latency stdev | 14.404 | 43.986 | 19.780695945945955 | milliseconds |
| PERF-IO-DISKSPD.json | diskspd_randrw_250MB_64k_d32_th16_direct | read latency/operation(P50) | 153.898 | 168.132 | 158.87808108108113 | milliseconds |
| PERF-IO-DISKSPD.json | diskspd_randrw_250MB_64k_d32_th16_direct | read latency/operation(P75) | 177.392 | 195.166 | 184.07579729729725 | milliseconds |
| PERF-IO-DISKSPD.json | diskspd_randrw_250MB_64k_d32_th16_direct | read latency/operation(P90) | 187.295 | 200.349 | 194.27925675675679 | milliseconds |
| PERF-IO-DISKSPD.json | diskspd_randrw_250MB_64k_d32_th16_direct | read latency/operation(P95) | 190.784 | 203.254 | 197.55067567567577 | milliseconds |
| PERF-IO-DISKSPD.json | diskspd_randrw_250MB_64k_d32_th16_direct | read latency/operation(P99) | 195.193 | 217.498 | 205.51137162162156 | milliseconds |
| PERF-IO-DISKSPD.json | diskspd_randrw_250MB_64k_d32_th16_direct | read throughput | 134.17 | 136.13 | 136.04986486486505 | MiB/sec |
| PERF-IO-DISKSPD.json | diskspd_randrw_250MB_64k_d32_th16_direct | read total bytes | 67531112448.0 | 68518150144.0 | 68477464243.89189 |  |
| PERF-IO-DISKSPD.json | diskspd_randrw_250MB_64k_d32_th16_direct | total IO operations | 1472894.0 | 1494296.0 | 1493457.4527027028 |  |
| PERF-IO-DISKSPD.json | diskspd_randrw_250MB_64k_d32_th16_direct | total bytes | 96527581184.0 | 97930182656.0 | 97875227620.32433 |  |
| PERF-IO-DISKSPD.json | diskspd_randrw_250MB_64k_d32_th16_direct | total iops | 3068.47 | 3113.08 | 3111.321891891891 |  |
| PERF-IO-DISKSPD.json | diskspd_randrw_250MB_64k_d32_th16_direct | total latency/operation(P50) | 154.267 | 166.814 | 158.10785135135127 | milliseconds |
| PERF-IO-DISKSPD.json | diskspd_randrw_250MB_64k_d32_th16_direct | total latency/operation(P75) | 176.278 | 194.704 | 183.82747972972977 | milliseconds |
| PERF-IO-DISKSPD.json | diskspd_randrw_250MB_64k_d32_th16_direct | total latency/operation(P90) | 185.538 | 199.188 | 194.16953378378376 | milliseconds |
| PERF-IO-DISKSPD.json | diskspd_randrw_250MB_64k_d32_th16_direct | total latency/operation(P95) | 190.208 | 202.182 | 197.65907432432429 | milliseconds |
| PERF-IO-DISKSPD.json | diskspd_randrw_250MB_64k_d32_th16_direct | total latency/operation(P99) | 195.977 | 214.632 | 205.01164189189178 | milliseconds |
| PERF-IO-DISKSPD.json | diskspd_randrw_250MB_64k_d32_th16_direct | total throughput | 191.78 | 194.57 | 194.4583108108106 | MiB/sec |
| PERF-IO-DISKSPD.json | diskspd_randrw_250MB_64k_d32_th16_direct | write IO operations | 442451.0 | 448877.0 | 448574.2702702703 |  |
| PERF-IO-DISKSPD.json | diskspd_randrw_250MB_64k_d32_th16_direct | write avg. latency | 158.373 | 169.167 | 162.99595945945939 | milliseconds |
| PERF-IO-DISKSPD.json | diskspd_randrw_250MB_64k_d32_th16_direct | write iops | 921.76 | 935.13 | 934.5156756756758 |  |
| PERF-IO-DISKSPD.json | diskspd_randrw_250MB_64k_d32_th16_direct | write iops stdev | 23.31 | 105.28 | 31.83027027027026 | milliseconds |
| PERF-IO-DISKSPD.json | diskspd_randrw_250MB_64k_d32_th16_direct | write latency stdev | 14.343 | 47.957 | 20.377797297297297 | milliseconds |
| PERF-IO-DISKSPD.json | diskspd_randrw_250MB_64k_d32_th16_direct | write latency/operation(P50) | 150.668 | 167.912 | 155.86480405405406 | milliseconds |
| PERF-IO-DISKSPD.json | diskspd_randrw_250MB_64k_d32_th16_direct | write latency/operation(P75) | 165.138 | 194.167 | 183.37019594594603 | milliseconds |
| PERF-IO-DISKSPD.json | diskspd_randrw_250MB_64k_d32_th16_direct | write latency/operation(P90) | 181.615 | 200.692 | 194.13100000000009 | milliseconds |
| PERF-IO-DISKSPD.json | diskspd_randrw_250MB_64k_d32_th16_direct | write latency/operation(P95) | 184.417 | 202.241 | 196.23643243243238 | milliseconds |
| PERF-IO-DISKSPD.json | diskspd_randrw_250MB_64k_d32_th16_direct | write latency/operation(P99) | 191.943 | 210.948 | 202.17590540540543 | milliseconds |
| PERF-IO-DISKSPD.json | diskspd_randrw_250MB_64k_d32_th16_direct | write throughput | 57.61 | 58.45 | 58.40655405405404 | MiB/sec |
| PERF-IO-DISKSPD.json | diskspd_randrw_250MB_64k_d32_th16_direct | write total bytes | 28996468736.0 | 29417603072.0 | 29397763376.432435 |  |
| PERF-IO-DISKSPD.json | diskspd_randrw_4GB_12k_d1_th1_direct | avg. latency | 1.535 | 4.928 | 1.9329800000000004 | milliseconds |
| PERF-IO-DISKSPD.json | diskspd_randrw_4GB_12k_d1_th1_direct | iops stdev | 7.81 | 78.01 | 29.02146666666667 | milliseconds |
| PERF-IO-DISKSPD.json | diskspd_randrw_4GB_12k_d1_th1_direct | latency stdev | 0.387 | 5.914 | 0.9186400000000004 | milliseconds |
| PERF-IO-DISKSPD.json | diskspd_randrw_4GB_12k_d1_th1_direct | read IO operations | 68228.0 | 218554.0 | 178340.87333333333 |  |
| PERF-IO-DISKSPD.json | diskspd_randrw_4GB_12k_d1_th1_direct | read avg. latency | 1.67 | 5.907 | 2.08474 | milliseconds |
| PERF-IO-DISKSPD.json | diskspd_randrw_4GB_12k_d1_th1_direct | read iops | 142.14 | 455.32 | 371.5371333333336 |  |
| PERF-IO-DISKSPD.json | diskspd_randrw_4GB_12k_d1_th1_direct | read iops stdev | 9.6 | 55.06 | 22.260199999999999 | milliseconds |
| PERF-IO-DISKSPD.json | diskspd_randrw_4GB_12k_d1_th1_direct | read latency stdev | 0.357 | 6.829 | 0.88642 | milliseconds |
| PERF-IO-DISKSPD.json | diskspd_randrw_4GB_12k_d1_th1_direct | read latency/operation(P50) | 1.526 | 3.277 | 1.8945600000000005 | milliseconds |
| PERF-IO-DISKSPD.json | diskspd_randrw_4GB_12k_d1_th1_direct | read latency/operation(P75) | 1.637 | 4.358 | 2.059646666666667 | milliseconds |
| PERF-IO-DISKSPD.json | diskspd_randrw_4GB_12k_d1_th1_direct | read latency/operation(P90) | 2.136 | 20.081 | 2.7709333333333348 | milliseconds |
| PERF-IO-DISKSPD.json | diskspd_randrw_4GB_12k_d1_th1_direct | read latency/operation(P95) | 2.371 | 26.428 | 3.2660666666666677 | milliseconds |
| PERF-IO-DISKSPD.json | diskspd_randrw_4GB_12k_d1_th1_direct | read latency/operation(P99) | 2.721 | 27.414 | 4.919613333333331 | milliseconds |
| PERF-IO-DISKSPD.json | diskspd_randrw_4GB_12k_d1_th1_direct | read throughput | 1.67 | 5.34 | 4.354200000000001 | MiB/sec |
| PERF-IO-DISKSPD.json | diskspd_randrw_4GB_12k_d1_th1_direct | read total bytes | 838385664.0 | 2685591552.0 | 2191452651.52 |  |
| PERF-IO-DISKSPD.json | diskspd_randrw_4GB_12k_d1_th1_direct | total IO operations | 97361.0 | 312431.0 | 254795.84666666669 |  |
| PERF-IO-DISKSPD.json | diskspd_randrw_4GB_12k_d1_th1_direct | total bytes | 1196371968.0 | 3839152128.0 | 3130931363.84 |  |
| PERF-IO-DISKSPD.json | diskspd_randrw_4GB_12k_d1_th1_direct | total iops | 202.83 | 650.89 | 530.8155999999997 |  |
| PERF-IO-DISKSPD.json | diskspd_randrw_4GB_12k_d1_th1_direct | total latency/operation(P50) | 1.47 | 3.15 | 1.8289533333333324 | milliseconds |
| PERF-IO-DISKSPD.json | diskspd_randrw_4GB_12k_d1_th1_direct | total latency/operation(P75) | 1.596 | 3.636 | 1.996926666666667 | milliseconds |
| PERF-IO-DISKSPD.json | diskspd_randrw_4GB_12k_d1_th1_direct | total latency/operation(P90) | 1.846 | 4.708 | 2.418966666666665 | milliseconds |
| PERF-IO-DISKSPD.json | diskspd_randrw_4GB_12k_d1_th1_direct | total latency/operation(P95) | 2.341 | 25.293 | 3.231779999999999 | milliseconds |
| PERF-IO-DISKSPD.json | diskspd_randrw_4GB_12k_d1_th1_direct | total latency/operation(P99) | 2.719 | 27.274 | 4.710073333333332 | milliseconds |
| PERF-IO-DISKSPD.json | diskspd_randrw_4GB_12k_d1_th1_direct | total throughput | 2.38 | 7.63 | 6.220133333333332 | MiB/sec |
| PERF-IO-DISKSPD.json | diskspd_randrw_4GB_12k_d1_th1_direct | write IO operations | 29133.0 | 93877.0 | 76454.97333333333 |  |
| PERF-IO-DISKSPD.json | diskspd_randrw_4GB_12k_d1_th1_direct | write avg. latency | 1.189 | 2.667 | 1.5790800000000003 | milliseconds |
| PERF-IO-DISKSPD.json | diskspd_randrw_4GB_12k_d1_th1_direct | write iops | 60.69 | 195.58 | 159.27866666666666 |  |
| PERF-IO-DISKSPD.json | diskspd_randrw_4GB_12k_d1_th1_direct | write iops stdev | 8.98 | 25.52 | 14.188733333333339 | milliseconds |
| PERF-IO-DISKSPD.json | diskspd_randrw_4GB_12k_d1_th1_direct | write latency stdev | 0.152 | 7.891 | 0.6923533333333336 | milliseconds |
| PERF-IO-DISKSPD.json | diskspd_randrw_4GB_12k_d1_th1_direct | write latency/operation(P50) | 1.125 | 2.542 | 1.4188400000000008 | milliseconds |
| PERF-IO-DISKSPD.json | diskspd_randrw_4GB_12k_d1_th1_direct | write latency/operation(P75) | 1.227 | 3.414 | 1.5724466666666666 | milliseconds |
| PERF-IO-DISKSPD.json | diskspd_randrw_4GB_12k_d1_th1_direct | write latency/operation(P90) | 1.318 | 3.83 | 1.9640133333333332 | milliseconds |
| PERF-IO-DISKSPD.json | diskspd_randrw_4GB_12k_d1_th1_direct | write latency/operation(P95) | 1.383 | 4.993 | 2.4968933333333349 | milliseconds |
| PERF-IO-DISKSPD.json | diskspd_randrw_4GB_12k_d1_th1_direct | write latency/operation(P99) | 1.525 | 12.442 | 4.216160000000002 | milliseconds |
| PERF-IO-DISKSPD.json | diskspd_randrw_4GB_12k_d1_th1_direct | write throughput | 0.71 | 2.29 | 1.8669333333333336 | MiB/sec |
| PERF-IO-DISKSPD.json | diskspd_randrw_4GB_12k_d1_th1_direct | write total bytes | 357986304.0 | 1153560576.0 | 939478712.32 |  |
| PERF-IO-DISKSPD.json | diskspd_randrw_4GB_16k_d1_th1_direct | avg. latency | 1.596 | 3.498 | 1.9671812080536922 | milliseconds |
| PERF-IO-DISKSPD.json | diskspd_randrw_4GB_16k_d1_th1_direct | iops stdev | 9.21 | 63.57 | 28.603489932885919 | milliseconds |
| PERF-IO-DISKSPD.json | diskspd_randrw_4GB_16k_d1_th1_direct | latency stdev | 0.436 | 4.698 | 0.7790872483221478 | milliseconds |
| PERF-IO-DISKSPD.json | diskspd_randrw_4GB_16k_d1_th1_direct | read IO operations | 96067.0 | 210227.0 | 174211.23489932886 |  |
| PERF-IO-DISKSPD.json | diskspd_randrw_4GB_16k_d1_th1_direct | read avg. latency | 1.729 | 3.359 | 2.0841879194630876 | milliseconds |
| PERF-IO-DISKSPD.json | diskspd_randrw_4GB_16k_d1_th1_direct | read iops | 200.13 | 437.96 | 362.9334899328859 |  |
| PERF-IO-DISKSPD.json | diskspd_randrw_4GB_16k_d1_th1_direct | read iops stdev | 9.91 | 45.46 | 21.504362416107388 | milliseconds |
| PERF-IO-DISKSPD.json | diskspd_randrw_4GB_16k_d1_th1_direct | read latency stdev | 0.384 | 5.599 | 0.7167785234899332 | milliseconds |
| PERF-IO-DISKSPD.json | diskspd_randrw_4GB_16k_d1_th1_direct | read latency/operation(P50) | 1.567 | 3.161 | 1.9133020134228185 | milliseconds |
| PERF-IO-DISKSPD.json | diskspd_randrw_4GB_16k_d1_th1_direct | read latency/operation(P75) | 1.698 | 4.009 | 2.0984362416107387 | milliseconds |
| PERF-IO-DISKSPD.json | diskspd_randrw_4GB_16k_d1_th1_direct | read latency/operation(P90) | 2.283 | 5.366 | 2.739657718120805 | milliseconds |
| PERF-IO-DISKSPD.json | diskspd_randrw_4GB_16k_d1_th1_direct | read latency/operation(P95) | 2.447 | 5.891 | 3.0448926174496657 | milliseconds |
| PERF-IO-DISKSPD.json | diskspd_randrw_4GB_16k_d1_th1_direct | read latency/operation(P99) | 2.813 | 12.602 | 4.2696912751677849 | milliseconds |
| PERF-IO-DISKSPD.json | diskspd_randrw_4GB_16k_d1_th1_direct | read throughput | 3.13 | 6.84 | 5.670939597315435 | MiB/sec |
| PERF-IO-DISKSPD.json | diskspd_randrw_4GB_16k_d1_th1_direct | read total bytes | 1573961728.0 | 3444359168.0 | 2854276872.590604 |  |
| PERF-IO-DISKSPD.json | diskspd_randrw_4GB_16k_d1_th1_direct | total IO operations | 137180.0 | 300566.0 | 248888.20805369129 |  |
| PERF-IO-DISKSPD.json | diskspd_randrw_4GB_16k_d1_th1_direct | total bytes | 2247557120.0 | 4924473344.0 | 4077784400.751678 |  |
| PERF-IO-DISKSPD.json | diskspd_randrw_4GB_16k_d1_th1_direct | total iops | 285.78 | 626.16 | 518.5081879194628 |  |
| PERF-IO-DISKSPD.json | diskspd_randrw_4GB_16k_d1_th1_direct | total latency/operation(P50) | 1.52 | 3.269 | 1.8570268456375852 | milliseconds |
| PERF-IO-DISKSPD.json | diskspd_randrw_4GB_16k_d1_th1_direct | total latency/operation(P75) | 1.673 | 3.859 | 2.0566912751677854 | milliseconds |
| PERF-IO-DISKSPD.json | diskspd_randrw_4GB_16k_d1_th1_direct | total latency/operation(P90) | 2.159 | 5.267 | 2.6358657718120797 | milliseconds |
| PERF-IO-DISKSPD.json | diskspd_randrw_4GB_16k_d1_th1_direct | total latency/operation(P95) | 2.429 | 5.846 | 3.054966442953022 | milliseconds |
| PERF-IO-DISKSPD.json | diskspd_randrw_4GB_16k_d1_th1_direct | total latency/operation(P99) | 2.851 | 11.566 | 4.416523489932886 | milliseconds |
| PERF-IO-DISKSPD.json | diskspd_randrw_4GB_16k_d1_th1_direct | total throughput | 4.47 | 9.78 | 8.101812080536913 | MiB/sec |
| PERF-IO-DISKSPD.json | diskspd_randrw_4GB_16k_d1_th1_direct | write IO operations | 41113.0 | 90339.0 | 74676.97315436242 |  |
| PERF-IO-DISKSPD.json | diskspd_randrw_4GB_16k_d1_th1_direct | write avg. latency | 1.169 | 3.822 | 1.6943087248322156 | milliseconds |
| PERF-IO-DISKSPD.json | diskspd_randrw_4GB_16k_d1_th1_direct | write iops | 85.65 | 188.2 | 155.5738926174497 |  |
| PERF-IO-DISKSPD.json | diskspd_randrw_4GB_16k_d1_th1_direct | write iops stdev | 7.01 | 21.18 | 14.219127516778525 | milliseconds |
| PERF-IO-DISKSPD.json | diskspd_randrw_4GB_16k_d1_th1_direct | write latency stdev | 0.217 | 2.616 | 0.7568859060402684 | milliseconds |
| PERF-IO-DISKSPD.json | diskspd_randrw_4GB_16k_d1_th1_direct | write latency/operation(P50) | 1.1 | 3.795 | 1.4965100671140937 | milliseconds |
| PERF-IO-DISKSPD.json | diskspd_randrw_4GB_16k_d1_th1_direct | write latency/operation(P75) | 1.181 | 3.931 | 1.7577785234899337 | milliseconds |
| PERF-IO-DISKSPD.json | diskspd_randrw_4GB_16k_d1_th1_direct | write latency/operation(P90) | 1.311 | 5.118 | 2.236053691275168 | milliseconds |
| PERF-IO-DISKSPD.json | diskspd_randrw_4GB_16k_d1_th1_direct | write latency/operation(P95) | 1.492 | 6.559 | 2.7635436241610757 | milliseconds |
| PERF-IO-DISKSPD.json | diskspd_randrw_4GB_16k_d1_th1_direct | write latency/operation(P99) | 2.302 | 11.583 | 4.596382550335569 | milliseconds |
| PERF-IO-DISKSPD.json | diskspd_randrw_4GB_16k_d1_th1_direct | write throughput | 1.34 | 2.94 | 2.4307382550335565 | MiB/sec |
| PERF-IO-DISKSPD.json | diskspd_randrw_4GB_16k_d1_th1_direct | write total bytes | 673595392.0 | 1480114176.0 | 1223507528.161074 |  |
| PERF-IO-DISKSPD.json | diskspd_randrw_4GB_32k_d1_th1_direct | avg. latency | 1.704 | 10.257 | 2.251106666666667 | milliseconds |
| PERF-IO-DISKSPD.json | diskspd_randrw_4GB_32k_d1_th1_direct | iops stdev | 9.8 | 99.25 | 36.42760000000001 | milliseconds |
| PERF-IO-DISKSPD.json | diskspd_randrw_4GB_32k_d1_th1_direct | latency stdev | 0.457 | 9.514 | 1.1497933333333337 | milliseconds |
| PERF-IO-DISKSPD.json | diskspd_randrw_4GB_32k_d1_th1_direct | read IO operations | 32811.0 | 196964.0 | 160031.16666666667 |  |
| PERF-IO-DISKSPD.json | diskspd_randrw_4GB_32k_d1_th1_direct | read avg. latency | 1.844 | 12.751 | 2.4216333333333335 | milliseconds |
| PERF-IO-DISKSPD.json | diskspd_randrw_4GB_32k_d1_th1_direct | read iops | 68.35 | 410.34 | 333.3934666666667 |  |
| PERF-IO-DISKSPD.json | diskspd_randrw_4GB_32k_d1_th1_direct | read iops stdev | 6.95 | 69.57 | 26.727133333333325 | milliseconds |
| PERF-IO-DISKSPD.json | diskspd_randrw_4GB_32k_d1_th1_direct | read latency stdev | 0.46 | 10.297 | 1.1689800000000002 | milliseconds |
| PERF-IO-DISKSPD.json | diskspd_randrw_4GB_32k_d1_th1_direct | read latency/operation(P50) | 1.645 | 5.98 | 2.0517200000000006 | milliseconds |
| PERF-IO-DISKSPD.json | diskspd_randrw_4GB_32k_d1_th1_direct | read latency/operation(P75) | 1.997 | 22.864 | 2.6241466666666657 | milliseconds |
| PERF-IO-DISKSPD.json | diskspd_randrw_4GB_32k_d1_th1_direct | read latency/operation(P90) | 2.445 | 30.542 | 3.3847600000000006 | milliseconds |
| PERF-IO-DISKSPD.json | diskspd_randrw_4GB_32k_d1_th1_direct | read latency/operation(P95) | 2.557 | 31.011 | 4.0228 | milliseconds |
| PERF-IO-DISKSPD.json | diskspd_randrw_4GB_32k_d1_th1_direct | read latency/operation(P99) | 2.865 | 31.722 | 6.404666666666664 | milliseconds |
| PERF-IO-DISKSPD.json | diskspd_randrw_4GB_32k_d1_th1_direct | read throughput | 2.14 | 12.82 | 10.41833333333334 | MiB/sec |
| PERF-IO-DISKSPD.json | diskspd_randrw_4GB_32k_d1_th1_direct | read total bytes | 1075150848.0 | 6454116352.0 | 5243901269.333333 |  |
| PERF-IO-DISKSPD.json | diskspd_randrw_4GB_32k_d1_th1_direct | total IO operations | 46786.0 | 281440.0 | 228600.4 |  |
| PERF-IO-DISKSPD.json | diskspd_randrw_4GB_32k_d1_th1_direct | total bytes | 1533083648.0 | 9222225920.0 | 7490777907.2 |  |
| PERF-IO-DISKSPD.json | diskspd_randrw_4GB_32k_d1_th1_direct | total iops | 97.47 | 586.33 | 476.2435333333335 |  |
| PERF-IO-DISKSPD.json | diskspd_randrw_4GB_32k_d1_th1_direct | total latency/operation(P50) | 1.583 | 5.678 | 1.9786533333333325 | milliseconds |
| PERF-IO-DISKSPD.json | diskspd_randrw_4GB_32k_d1_th1_direct | total latency/operation(P75) | 1.792 | 8.558 | 2.3089466666666658 | milliseconds |
| PERF-IO-DISKSPD.json | diskspd_randrw_4GB_32k_d1_th1_direct | total latency/operation(P90) | 2.377 | 30.119 | 3.293633333333331 | milliseconds |
| PERF-IO-DISKSPD.json | diskspd_randrw_4GB_32k_d1_th1_direct | total latency/operation(P95) | 2.515 | 30.802 | 3.8684 | milliseconds |
| PERF-IO-DISKSPD.json | diskspd_randrw_4GB_32k_d1_th1_direct | total latency/operation(P99) | 2.849 | 31.579 | 6.415693333333333 | milliseconds |
| PERF-IO-DISKSPD.json | diskspd_randrw_4GB_32k_d1_th1_direct | total throughput | 3.05 | 18.32 | 14.882600000000002 | MiB/sec |
| PERF-IO-DISKSPD.json | diskspd_randrw_4GB_32k_d1_th1_direct | write IO operations | 13975.0 | 84476.0 | 68569.23333333334 |  |
| PERF-IO-DISKSPD.json | diskspd_randrw_4GB_32k_d1_th1_direct | write avg. latency | 1.286 | 5.027 | 1.8526066666666679 | milliseconds |
| PERF-IO-DISKSPD.json | diskspd_randrw_4GB_32k_d1_th1_direct | write iops | 29.11 | 175.99 | 142.85060000000002 |  |
| PERF-IO-DISKSPD.json | diskspd_randrw_4GB_32k_d1_th1_direct | write iops stdev | 5.78 | 32.15 | 15.310533333333339 | milliseconds |
| PERF-IO-DISKSPD.json | diskspd_randrw_4GB_32k_d1_th1_direct | write latency stdev | 0.286 | 3.574 | 0.7721133333333332 | milliseconds |
| PERF-IO-DISKSPD.json | diskspd_randrw_4GB_32k_d1_th1_direct | write latency/operation(P50) | 1.168 | 5.133 | 1.6744266666666672 | milliseconds |
| PERF-IO-DISKSPD.json | diskspd_randrw_4GB_32k_d1_th1_direct | write latency/operation(P75) | 1.276 | 5.306 | 1.91748 | milliseconds |
| PERF-IO-DISKSPD.json | diskspd_randrw_4GB_32k_d1_th1_direct | write latency/operation(P90) | 1.389 | 5.599 | 2.3793599999999999 | milliseconds |
| PERF-IO-DISKSPD.json | diskspd_randrw_4GB_32k_d1_th1_direct | write latency/operation(P95) | 1.464 | 7.88 | 2.961186666666667 | milliseconds |
| PERF-IO-DISKSPD.json | diskspd_randrw_4GB_32k_d1_th1_direct | write latency/operation(P99) | 2.722 | 13.418 | 4.80694 | milliseconds |
| PERF-IO-DISKSPD.json | diskspd_randrw_4GB_32k_d1_th1_direct | write throughput | 0.91 | 5.5 | 4.464266666666666 | MiB/sec |
| PERF-IO-DISKSPD.json | diskspd_randrw_4GB_32k_d1_th1_direct | write total bytes | 457932800.0 | 2768109568.0 | 2246876637.866667 |  |
| PERF-IO-DISKSPD.json | diskspd_randrw_4GB_4k_d1_th1_direct | avg. latency | 1.547 | 4.035 | 1.9043800000000014 | milliseconds |
| PERF-IO-DISKSPD.json | diskspd_randrw_4GB_4k_d1_th1_direct | iops stdev | 7.89 | 66.69 | 23.775933333333339 | milliseconds |
| PERF-IO-DISKSPD.json | diskspd_randrw_4GB_4k_d1_th1_direct | latency stdev | 0.356 | 4.267 | 0.8074533333333333 | milliseconds |
| PERF-IO-DISKSPD.json | diskspd_randrw_4GB_4k_d1_th1_direct | read IO operations | 83249.0 | 216903.0 | 180137.64666666668 |  |
| PERF-IO-DISKSPD.json | diskspd_randrw_4GB_4k_d1_th1_direct | read avg. latency | 1.621 | 4.606 | 2.019206666666666 | milliseconds |
| PERF-IO-DISKSPD.json | diskspd_randrw_4GB_4k_d1_th1_direct | read iops | 173.43 | 451.87 | 375.28033333333328 |  |
| PERF-IO-DISKSPD.json | diskspd_randrw_4GB_4k_d1_th1_direct | read iops stdev | 9.08 | 48.66 | 19.301133333333337 | milliseconds |
| PERF-IO-DISKSPD.json | diskspd_randrw_4GB_4k_d1_th1_direct | read latency stdev | 0.279 | 4.98 | 0.7395399999999995 | milliseconds |
| PERF-IO-DISKSPD.json | diskspd_randrw_4GB_4k_d1_th1_direct | read latency/operation(P50) | 1.568 | 3.149 | 1.9044933333333348 | milliseconds |
| PERF-IO-DISKSPD.json | diskspd_randrw_4GB_4k_d1_th1_direct | read latency/operation(P75) | 1.658 | 3.29 | 2.020153333333334 | milliseconds |
| PERF-IO-DISKSPD.json | diskspd_randrw_4GB_4k_d1_th1_direct | read latency/operation(P90) | 1.768 | 4.08 | 2.2237933333333346 | milliseconds |
| PERF-IO-DISKSPD.json | diskspd_randrw_4GB_4k_d1_th1_direct | read latency/operation(P95) | 1.913 | 20.293 | 2.746853333333333 | milliseconds |
| PERF-IO-DISKSPD.json | diskspd_randrw_4GB_4k_d1_th1_direct | read latency/operation(P99) | 2.6 | 24.562 | 4.354686666666667 | milliseconds |
| PERF-IO-DISKSPD.json | diskspd_randrw_4GB_4k_d1_th1_direct | read throughput | 0.68 | 1.77 | 1.4659333333333338 | MiB/sec |
| PERF-IO-DISKSPD.json | diskspd_randrw_4GB_4k_d1_th1_direct | read total bytes | 340987904.0 | 888434688.0 | 737843800.7466667 |  |
| PERF-IO-DISKSPD.json | diskspd_randrw_4GB_4k_d1_th1_direct | total IO operations | 118917.0 | 310073.0 | 257360.26 |  |
| PERF-IO-DISKSPD.json | diskspd_randrw_4GB_4k_d1_th1_direct | total bytes | 487084032.0 | 1270059008.0 | 1054147624.96 |  |
| PERF-IO-DISKSPD.json | diskspd_randrw_4GB_4k_d1_th1_direct | total iops | 247.74 | 645.97 | 536.1578666666666 |  |
| PERF-IO-DISKSPD.json | diskspd_randrw_4GB_4k_d1_th1_direct | total latency/operation(P50) | 1.539 | 3.079 | 1.8623666666666672 | milliseconds |
| PERF-IO-DISKSPD.json | diskspd_randrw_4GB_4k_d1_th1_direct | total latency/operation(P75) | 1.652 | 3.239 | 2.003546666666666 | milliseconds |
| PERF-IO-DISKSPD.json | diskspd_randrw_4GB_4k_d1_th1_direct | total latency/operation(P90) | 1.783 | 3.59 | 2.2346800000000006 | milliseconds |
| PERF-IO-DISKSPD.json | diskspd_randrw_4GB_4k_d1_th1_direct | total latency/operation(P95) | 1.964 | 16.093 | 2.728553333333333 | milliseconds |
| PERF-IO-DISKSPD.json | diskspd_randrw_4GB_4k_d1_th1_direct | total latency/operation(P99) | 2.687 | 24.376 | 4.287686666666664 | milliseconds |
| PERF-IO-DISKSPD.json | diskspd_randrw_4GB_4k_d1_th1_direct | total throughput | 0.97 | 2.52 | 2.0946000000000004 | MiB/sec |
| PERF-IO-DISKSPD.json | diskspd_randrw_4GB_4k_d1_th1_direct | write IO operations | 35668.0 | 93170.0 | 77222.61333333333 |  |
| PERF-IO-DISKSPD.json | diskspd_randrw_4GB_4k_d1_th1_direct | write avg. latency | 1.196 | 2.703 | 1.6366199999999998 | milliseconds |
| PERF-IO-DISKSPD.json | diskspd_randrw_4GB_4k_d1_th1_direct | write iops | 74.31 | 194.1 | 160.87766666666679 |  |
| PERF-IO-DISKSPD.json | diskspd_randrw_4GB_4k_d1_th1_direct | write iops stdev | 8.24 | 21.81 | 13.18513333333333 | milliseconds |
| PERF-IO-DISKSPD.json | diskspd_randrw_4GB_4k_d1_th1_direct | write latency stdev | 0.217 | 6.638 | 0.6922933333333331 | milliseconds |
| PERF-IO-DISKSPD.json | diskspd_randrw_4GB_4k_d1_th1_direct | write latency/operation(P50) | 1.121 | 2.556 | 1.497513333333334 | milliseconds |
| PERF-IO-DISKSPD.json | diskspd_randrw_4GB_4k_d1_th1_direct | write latency/operation(P75) | 1.214 | 3.28 | 1.6368533333333329 | milliseconds |
| PERF-IO-DISKSPD.json | diskspd_randrw_4GB_4k_d1_th1_direct | write latency/operation(P90) | 1.308 | 3.402 | 1.9870466666666669 | milliseconds |
| PERF-IO-DISKSPD.json | diskspd_randrw_4GB_4k_d1_th1_direct | write latency/operation(P95) | 1.452 | 4.915 | 2.495333333333332 | milliseconds |
| PERF-IO-DISKSPD.json | diskspd_randrw_4GB_4k_d1_th1_direct | write latency/operation(P99) | 2.259 | 11.912 | 4.132973333333334 | milliseconds |
| PERF-IO-DISKSPD.json | diskspd_randrw_4GB_4k_d1_th1_direct | write throughput | 0.29 | 0.76 | 0.6279999999999998 | MiB/sec |
| PERF-IO-DISKSPD.json | diskspd_randrw_4GB_4k_d1_th1_direct | write total bytes | 146096128.0 | 381624320.0 | 316303824.2133333 |  |
| PERF-IO-DISKSPD.json | diskspd_randrw_4GB_8k_d1_th1_direct | avg. latency | 1.505 | 2.94 | 1.8595799999999985 | milliseconds |
| PERF-IO-DISKSPD.json | diskspd_randrw_4GB_8k_d1_th1_direct | iops stdev | 7.09 | 77.34 | 25.8422 | milliseconds |
| PERF-IO-DISKSPD.json | diskspd_randrw_4GB_8k_d1_th1_direct | latency stdev | 0.389 | 3.815 | 0.7394400000000002 | milliseconds |
| PERF-IO-DISKSPD.json | diskspd_randrw_4GB_8k_d1_th1_direct | read IO operations | 114244.0 | 222881.0 | 183127.69333333334 |  |
| PERF-IO-DISKSPD.json | diskspd_randrw_4GB_8k_d1_th1_direct | read avg. latency | 1.635 | 3.077 | 1.9875933333333343 | milliseconds |
| PERF-IO-DISKSPD.json | diskspd_randrw_4GB_8k_d1_th1_direct | read iops | 238.01 | 464.33 | 381.5117333333333 |  |
| PERF-IO-DISKSPD.json | diskspd_randrw_4GB_8k_d1_th1_direct | read iops stdev | 9.14 | 54.23 | 20.26466666666667 | milliseconds |
| PERF-IO-DISKSPD.json | diskspd_randrw_4GB_8k_d1_th1_direct | read latency stdev | 0.315 | 4.491 | 0.6558333333333333 | milliseconds |
| PERF-IO-DISKSPD.json | diskspd_randrw_4GB_8k_d1_th1_direct | read latency/operation(P50) | 1.524 | 3.01 | 1.86794 | milliseconds |
| PERF-IO-DISKSPD.json | diskspd_randrw_4GB_8k_d1_th1_direct | read latency/operation(P75) | 1.617 | 3.118 | 1.9975133333333324 | milliseconds |
| PERF-IO-DISKSPD.json | diskspd_randrw_4GB_8k_d1_th1_direct | read latency/operation(P90) | 1.817 | 3.406 | 2.3052799999999995 | milliseconds |
| PERF-IO-DISKSPD.json | diskspd_randrw_4GB_8k_d1_th1_direct | read latency/operation(P95) | 2.357 | 4.703 | 2.8260400000000005 | milliseconds |
| PERF-IO-DISKSPD.json | diskspd_randrw_4GB_8k_d1_th1_direct | read latency/operation(P99) | 2.721 | 10.005 | 3.8712066666666664 | milliseconds |
| PERF-IO-DISKSPD.json | diskspd_randrw_4GB_8k_d1_th1_direct | read throughput | 1.86 | 3.63 | 2.9804666666666677 | MiB/sec |
| PERF-IO-DISKSPD.json | diskspd_randrw_4GB_8k_d1_th1_direct | read total bytes | 935886848.0 | 1825841152.0 | 1500182063.7866667 |  |
| PERF-IO-DISKSPD.json | diskspd_randrw_4GB_8k_d1_th1_direct | total IO operations | 163211.0 | 318656.0 | 261642.06666666669 |  |
| PERF-IO-DISKSPD.json | diskspd_randrw_4GB_8k_d1_th1_direct | total bytes | 1337024512.0 | 2610429952.0 | 2143371810.1333335 |  |
| PERF-IO-DISKSPD.json | diskspd_randrw_4GB_8k_d1_th1_direct | total iops | 340.02 | 663.86 | 545.0813333333333 |  |
| PERF-IO-DISKSPD.json | diskspd_randrw_4GB_8k_d1_th1_direct | total latency/operation(P50) | 1.464 | 2.953 | 1.8088666666666673 | milliseconds |
| PERF-IO-DISKSPD.json | diskspd_randrw_4GB_8k_d1_th1_direct | total latency/operation(P75) | 1.574 | 3.082 | 1.9521466666666672 | milliseconds |
| PERF-IO-DISKSPD.json | diskspd_randrw_4GB_8k_d1_th1_direct | total latency/operation(P90) | 1.707 | 3.313 | 2.251106666666667 | milliseconds |
| PERF-IO-DISKSPD.json | diskspd_randrw_4GB_8k_d1_th1_direct | total latency/operation(P95) | 2.323 | 4.206 | 2.7953133333333337 | milliseconds |
| PERF-IO-DISKSPD.json | diskspd_randrw_4GB_8k_d1_th1_direct | total latency/operation(P99) | 2.851 | 8.684 | 4.03702 | milliseconds |
| PERF-IO-DISKSPD.json | diskspd_randrw_4GB_8k_d1_th1_direct | total throughput | 2.66 | 5.19 | 4.257999999999999 | MiB/sec |
| PERF-IO-DISKSPD.json | diskspd_randrw_4GB_8k_d1_th1_direct | write IO operations | 48967.0 | 95775.0 | 78514.37333333334 |  |
| PERF-IO-DISKSPD.json | diskspd_randrw_4GB_8k_d1_th1_direct | write avg. latency | 1.204 | 2.618 | 1.5609000000000002 | milliseconds |
| PERF-IO-DISKSPD.json | diskspd_randrw_4GB_8k_d1_th1_direct | write iops | 102.01 | 199.53 | 163.56980000000005 |  |
| PERF-IO-DISKSPD.json | diskspd_randrw_4GB_8k_d1_th1_direct | write iops stdev | 8.73 | 26.22 | 13.914399999999996 | milliseconds |
| PERF-IO-DISKSPD.json | diskspd_randrw_4GB_8k_d1_th1_direct | write latency stdev | 0.209 | 5.815 | 0.7208133333333334 | milliseconds |
| PERF-IO-DISKSPD.json | diskspd_randrw_4GB_8k_d1_th1_direct | write latency/operation(P50) | 1.133 | 2.486 | 1.4083000000000006 | milliseconds |
| PERF-IO-DISKSPD.json | diskspd_randrw_4GB_8k_d1_th1_direct | write latency/operation(P75) | 1.225 | 3.332 | 1.543693333333333 | milliseconds |
| PERF-IO-DISKSPD.json | diskspd_randrw_4GB_8k_d1_th1_direct | write latency/operation(P90) | 1.304 | 3.789 | 1.9229133333333338 | milliseconds |
| PERF-IO-DISKSPD.json | diskspd_randrw_4GB_8k_d1_th1_direct | write latency/operation(P95) | 1.395 | 4.747 | 2.453333333333332 | milliseconds |
| PERF-IO-DISKSPD.json | diskspd_randrw_4GB_8k_d1_th1_direct | write latency/operation(P99) | 1.55 | 10.292 | 4.118166666666667 | milliseconds |
| PERF-IO-DISKSPD.json | diskspd_randrw_4GB_8k_d1_th1_direct | write throughput | 0.8 | 1.56 | 1.2781333333333338 | MiB/sec |
| PERF-IO-DISKSPD.json | diskspd_randrw_4GB_8k_d1_th1_direct | write total bytes | 401137664.0 | 784588800.0 | 643189746.3466667 |  |
| PERF-IO-DISKSPD.json | diskspd_randwrite_16GB_1024k_d1_th1_direct | avg. latency | 5.135 | 36.225 | 9.277780000000002 | milliseconds |
| PERF-IO-DISKSPD.json | diskspd_randwrite_16GB_1024k_d1_th1_direct | iops stdev | 0.68 | 69.22 | 9.374733333333336 | milliseconds |
| PERF-IO-DISKSPD.json | diskspd_randwrite_16GB_1024k_d1_th1_direct | latency stdev | 0.419 | 11.013 | 2.339473333333334 | milliseconds |
| PERF-IO-DISKSPD.json | diskspd_randwrite_16GB_1024k_d1_th1_direct | total IO operations | 13249.0 | 93456.0 | 62839.12 |  |
| PERF-IO-DISKSPD.json | diskspd_randwrite_16GB_1024k_d1_th1_direct | total bytes | 13892583424.0 | 97995718656.0 | 65891593093.12 |  |
| PERF-IO-DISKSPD.json | diskspd_randwrite_16GB_1024k_d1_th1_direct | total iops | 27.6 | 194.69 | 130.9129333333333 |  |
| PERF-IO-DISKSPD.json | diskspd_randwrite_16GB_1024k_d1_th1_direct | total throughput | 27.6 | 194.69 | 130.9129333333333 | MiB/sec |
| PERF-IO-DISKSPD.json | diskspd_randwrite_16GB_1024k_d1_th1_direct | write IO operations | 13249.0 | 93456.0 | 62839.12 |  |
| PERF-IO-DISKSPD.json | diskspd_randwrite_16GB_1024k_d1_th1_direct | write avg. latency | 5.135 | 36.225 | 9.277780000000002 | milliseconds |
| PERF-IO-DISKSPD.json | diskspd_randwrite_16GB_1024k_d1_th1_direct | write iops | 27.6 | 194.69 | 130.9129333333333 |  |
| PERF-IO-DISKSPD.json | diskspd_randwrite_16GB_1024k_d1_th1_direct | write iops stdev | 0.68 | 69.22 | 9.374733333333336 | milliseconds |
| PERF-IO-DISKSPD.json | diskspd_randwrite_16GB_1024k_d1_th1_direct | write latency stdev | 0.419 | 11.013 | 2.339473333333334 | milliseconds |
| PERF-IO-DISKSPD.json | diskspd_randwrite_16GB_1024k_d1_th1_direct | write throughput | 27.6 | 194.69 | 130.9129333333333 | MiB/sec |
| PERF-IO-DISKSPD.json | diskspd_randwrite_16GB_1024k_d1_th1_direct | write total bytes | 13892583424.0 | 97995718656.0 | 65891593093.12 |  |
| PERF-IO-DISKSPD.json | diskspd_randwrite_250MB_64k_d16_th32_direct | avg. latency | 164.317 | 178.29 | 164.87118666666633 | milliseconds |
| PERF-IO-DISKSPD.json | diskspd_randwrite_250MB_64k_d16_th32_direct | iops stdev | 0.4 | 403.23 | 58.28606666666662 | milliseconds |
| PERF-IO-DISKSPD.json | diskspd_randwrite_250MB_64k_d16_th32_direct | latency stdev | 7.022 | 77.349 | 19.86773333333334 | milliseconds |
| PERF-IO-DISKSPD.json | diskspd_randwrite_250MB_64k_d16_th32_direct | total IO operations | 1378425.0 | 1495753.0 | 1490741.6733333334 |  |
| PERF-IO-DISKSPD.json | diskspd_randwrite_250MB_64k_d16_th32_direct | total bytes | 90336460800.0 | 98025668608.0 | 97697246303.57334 |  |
| PERF-IO-DISKSPD.json | diskspd_randwrite_250MB_64k_d16_th32_direct | total iops | 2871.67 | 3116.1 | 3105.6589333333347 |  |
| PERF-IO-DISKSPD.json | diskspd_randwrite_250MB_64k_d16_th32_direct | total throughput | 179.48 | 194.76 | 194.10399999999994 | MiB/sec |
| PERF-IO-DISKSPD.json | diskspd_randwrite_250MB_64k_d16_th32_direct | write IO operations | 1378425.0 | 1495753.0 | 1490741.6733333334 |  |
| PERF-IO-DISKSPD.json | diskspd_randwrite_250MB_64k_d16_th32_direct | write avg. latency | 164.317 | 178.29 | 164.87118666666633 | milliseconds |
| PERF-IO-DISKSPD.json | diskspd_randwrite_250MB_64k_d16_th32_direct | write iops | 2871.67 | 3116.1 | 3105.6589333333347 |  |
| PERF-IO-DISKSPD.json | diskspd_randwrite_250MB_64k_d16_th32_direct | write iops stdev | 0.4 | 403.23 | 58.28606666666662 | milliseconds |
| PERF-IO-DISKSPD.json | diskspd_randwrite_250MB_64k_d16_th32_direct | write latency stdev | 7.022 | 77.349 | 19.86773333333334 | milliseconds |
| PERF-IO-DISKSPD.json | diskspd_randwrite_250MB_64k_d16_th32_direct | write throughput | 179.48 | 194.76 | 194.10399999999994 | MiB/sec |
| PERF-IO-DISKSPD.json | diskspd_randwrite_250MB_64k_d16_th32_direct | write total bytes | 90336460800.0 | 98025668608.0 | 97697246303.57334 |  |
| PERF-IO-DISKSPD.json | diskspd_randwrite_4GB_12k_d1_th1_direct | avg. latency | 1.135 | 2.672 | 1.533119863013699 | milliseconds |
| PERF-IO-DISKSPD.json | diskspd_randwrite_4GB_12k_d1_th1_direct | iops stdev | 4.67 | 161.17 | 46.7683219178082 | milliseconds |
| PERF-IO-DISKSPD.json | diskspd_randwrite_4GB_12k_d1_th1_direct | latency stdev | 0.175 | 8.58 | 0.6317123287671232 | milliseconds |
| PERF-IO-DISKSPD.json | diskspd_randwrite_4GB_12k_d1_th1_direct | total IO operations | 179594.0 | 422479.0 | 321251.5308219178 |  |
| PERF-IO-DISKSPD.json | diskspd_randwrite_4GB_12k_d1_th1_direct | total bytes | 2206851072.0 | 5191421952.0 | 3947538810.739726 |  |
| PERF-IO-DISKSPD.json | diskspd_randwrite_4GB_12k_d1_th1_direct | total iops | 374.15 | 880.14 | 669.2633904109592 |  |
| PERF-IO-DISKSPD.json | diskspd_randwrite_4GB_12k_d1_th1_direct | total throughput | 4.38 | 10.31 | 7.842910958904115 | MiB/sec |
| PERF-IO-DISKSPD.json | diskspd_randwrite_4GB_12k_d1_th1_direct | write IO operations | 179594.0 | 422479.0 | 321251.5308219178 |  |
| PERF-IO-DISKSPD.json | diskspd_randwrite_4GB_12k_d1_th1_direct | write avg. latency | 1.135 | 2.672 | 1.5331198630136989 | milliseconds |
| PERF-IO-DISKSPD.json | diskspd_randwrite_4GB_12k_d1_th1_direct | write iops | 374.15 | 880.14 | 669.2633904109594 |  |
| PERF-IO-DISKSPD.json | diskspd_randwrite_4GB_12k_d1_th1_direct | write iops stdev | 4.67 | 161.17 | 46.7683219178082 | milliseconds |
| PERF-IO-DISKSPD.json | diskspd_randwrite_4GB_12k_d1_th1_direct | write latency stdev | 0.175 | 8.58 | 0.6317123287671234 | milliseconds |
| PERF-IO-DISKSPD.json | diskspd_randwrite_4GB_12k_d1_th1_direct | write throughput | 4.38 | 10.31 | 7.842910958904115 | MiB/sec |
| PERF-IO-DISKSPD.json | diskspd_randwrite_4GB_12k_d1_th1_direct | write total bytes | 2206851072.0 | 5191421952.0 | 3947538810.739726 |  |
| PERF-IO-DISKSPD.json | diskspd_randwrite_4GB_16k_d1_th1_direct | avg. latency | 1.11 | 2.69 | 1.5059794520547949 | milliseconds |
| PERF-IO-DISKSPD.json | diskspd_randwrite_4GB_16k_d1_th1_direct | iops stdev | 5.61 | 154.98 | 47.12595890410961 | milliseconds |
| PERF-IO-DISKSPD.json | diskspd_randwrite_4GB_16k_d1_th1_direct | latency stdev | 0.168 | 5.896 | 0.6435000000000002 | milliseconds |
| PERF-IO-DISKSPD.json | diskspd_randwrite_4GB_16k_d1_th1_direct | total IO operations | 178369.0 | 432018.0 | 327170.4691780822 |  |
| PERF-IO-DISKSPD.json | diskspd_randwrite_4GB_16k_d1_th1_direct | total bytes | 2922397696.0 | 7078182912.0 | 5360360967.013699 |  |
| PERF-IO-DISKSPD.json | diskspd_randwrite_4GB_16k_d1_th1_direct | total iops | 371.6 | 900.02 | 681.5940410958901 |  |
| PERF-IO-DISKSPD.json | diskspd_randwrite_4GB_16k_d1_th1_direct | total throughput | 5.81 | 14.06 | 10.649828767123293 | MiB/sec |
| PERF-IO-DISKSPD.json | diskspd_randwrite_4GB_16k_d1_th1_direct | write IO operations | 178369.0 | 432018.0 | 327170.4691780822 |  |
| PERF-IO-DISKSPD.json | diskspd_randwrite_4GB_16k_d1_th1_direct | write avg. latency | 1.11 | 2.69 | 1.5059794520547954 | milliseconds |
| PERF-IO-DISKSPD.json | diskspd_randwrite_4GB_16k_d1_th1_direct | write iops | 371.6 | 900.02 | 681.5940410958903 |  |
| PERF-IO-DISKSPD.json | diskspd_randwrite_4GB_16k_d1_th1_direct | write iops stdev | 5.61 | 154.98 | 47.12595890410961 | milliseconds |
| PERF-IO-DISKSPD.json | diskspd_randwrite_4GB_16k_d1_th1_direct | write latency stdev | 0.168 | 5.896 | 0.6435000000000002 | milliseconds |
| PERF-IO-DISKSPD.json | diskspd_randwrite_4GB_16k_d1_th1_direct | write throughput | 5.81 | 14.06 | 10.649828767123293 | MiB/sec |
| PERF-IO-DISKSPD.json | diskspd_randwrite_4GB_16k_d1_th1_direct | write total bytes | 2922397696.0 | 7078182912.0 | 5360360967.013699 |  |
| PERF-IO-DISKSPD.json | diskspd_randwrite_4GB_32k_d1_th1_direct | avg. latency | 1.195 | 3.495 | 1.5737200000000005 | milliseconds |
| PERF-IO-DISKSPD.json | diskspd_randwrite_4GB_32k_d1_th1_direct | iops stdev | 6.77 | 135.12 | 48.35406666666666 | milliseconds |
| PERF-IO-DISKSPD.json | diskspd_randwrite_4GB_32k_d1_th1_direct | latency stdev | 0.187 | 5.726 | 0.6924799999999999 | milliseconds |
| PERF-IO-DISKSPD.json | diskspd_randwrite_4GB_32k_d1_th1_direct | total IO operations | 137298.0 | 401515.0 | 313483.74666666667 |  |
| PERF-IO-DISKSPD.json | diskspd_randwrite_4GB_32k_d1_th1_direct | total bytes | 4498980864.0 | 13156843520.0 | 10272235410.773333 |  |
| PERF-IO-DISKSPD.json | diskspd_randwrite_4GB_32k_d1_th1_direct | total iops | 286.04 | 836.46 | 653.0811333333334 |  |
| PERF-IO-DISKSPD.json | diskspd_randwrite_4GB_32k_d1_th1_direct | total throughput | 8.94 | 26.14 | 20.408799999999994 | MiB/sec |
| PERF-IO-DISKSPD.json | diskspd_randwrite_4GB_32k_d1_th1_direct | write IO operations | 137298.0 | 401515.0 | 313483.74666666667 |  |
| PERF-IO-DISKSPD.json | diskspd_randwrite_4GB_32k_d1_th1_direct | write avg. latency | 1.195 | 3.495 | 1.5737200000000007 | milliseconds |
| PERF-IO-DISKSPD.json | diskspd_randwrite_4GB_32k_d1_th1_direct | write iops | 286.04 | 836.46 | 653.0811333333334 |  |
| PERF-IO-DISKSPD.json | diskspd_randwrite_4GB_32k_d1_th1_direct | write iops stdev | 6.77 | 135.12 | 48.35406666666666 | milliseconds |
| PERF-IO-DISKSPD.json | diskspd_randwrite_4GB_32k_d1_th1_direct | write latency stdev | 0.187 | 5.726 | 0.6924799999999999 | milliseconds |
| PERF-IO-DISKSPD.json | diskspd_randwrite_4GB_32k_d1_th1_direct | write throughput | 8.94 | 26.14 | 20.408799999999994 | MiB/sec |
| PERF-IO-DISKSPD.json | diskspd_randwrite_4GB_32k_d1_th1_direct | write total bytes | 4498980864.0 | 13156843520.0 | 10272235410.773333 |  |
| PERF-IO-DISKSPD.json | diskspd_randwrite_4GB_4k_d1_th1_direct | avg. latency | 1.128 | 2.805 | 1.6146883561643837 | milliseconds |
| PERF-IO-DISKSPD.json | diskspd_randwrite_4GB_4k_d1_th1_direct | iops stdev | 3.86 | 132.8 | 42.07886986301368 | milliseconds |
| PERF-IO-DISKSPD.json | diskspd_randwrite_4GB_4k_d1_th1_direct | latency stdev | 0.191 | 4.083 | 0.720414383561643 | milliseconds |
| PERF-IO-DISKSPD.json | diskspd_randwrite_4GB_4k_d1_th1_direct | total IO operations | 171051.0 | 425039.0 | 310297.09246575346 |  |
| PERF-IO-DISKSPD.json | diskspd_randwrite_4GB_4k_d1_th1_direct | total bytes | 700624896.0 | 1740959744.0 | 1270976890.739726 |  |
| PERF-IO-DISKSPD.json | diskspd_randwrite_4GB_4k_d1_th1_direct | total iops | 356.35 | 885.48 | 646.4416095890409 |  |
| PERF-IO-DISKSPD.json | diskspd_randwrite_4GB_4k_d1_th1_direct | total throughput | 1.39 | 3.46 | 2.5251027397260286 | MiB/sec |
| PERF-IO-DISKSPD.json | diskspd_randwrite_4GB_4k_d1_th1_direct | write IO operations | 171051.0 | 425039.0 | 310297.09246575346 |  |
| PERF-IO-DISKSPD.json | diskspd_randwrite_4GB_4k_d1_th1_direct | write avg. latency | 1.128 | 2.805 | 1.6146883561643839 | milliseconds |
| PERF-IO-DISKSPD.json | diskspd_randwrite_4GB_4k_d1_th1_direct | write iops | 356.35 | 885.48 | 646.4416095890409 |  |
| PERF-IO-DISKSPD.json | diskspd_randwrite_4GB_4k_d1_th1_direct | write iops stdev | 3.86 | 132.8 | 42.07886986301368 | milliseconds |
| PERF-IO-DISKSPD.json | diskspd_randwrite_4GB_4k_d1_th1_direct | write latency stdev | 0.191 | 4.083 | 0.720414383561643 | milliseconds |
| PERF-IO-DISKSPD.json | diskspd_randwrite_4GB_4k_d1_th1_direct | write throughput | 1.39 | 3.46 | 2.525102739726029 | MiB/sec |
| PERF-IO-DISKSPD.json | diskspd_randwrite_4GB_4k_d1_th1_direct | write total bytes | 700624896.0 | 1740959744.0 | 1270976890.739726 |  |
| PERF-IO-DISKSPD.json | diskspd_randwrite_4GB_8k_d1_th1_direct | avg. latency | 1.119 | 2.658 | 1.5268356164383573 | milliseconds |
| PERF-IO-DISKSPD.json | diskspd_randwrite_4GB_8k_d1_th1_direct | iops stdev | 4.16 | 176.74 | 44.08325342465754 | milliseconds |
| PERF-IO-DISKSPD.json | diskspd_randwrite_4GB_8k_d1_th1_direct | latency stdev | 0.166 | 3.955 | 0.6379726027397266 | milliseconds |
| PERF-IO-DISKSPD.json | diskspd_randwrite_4GB_8k_d1_th1_direct | total IO operations | 180506.0 | 428669.0 | 322588.76369863018 |  |
| PERF-IO-DISKSPD.json | diskspd_randwrite_4GB_8k_d1_th1_direct | total bytes | 1478705152.0 | 3511656448.0 | 2642647152.219178 |  |
| PERF-IO-DISKSPD.json | diskspd_randwrite_4GB_8k_d1_th1_direct | total iops | 376.04 | 893.04 | 672.0497945205484 |  |
| PERF-IO-DISKSPD.json | diskspd_randwrite_4GB_8k_d1_th1_direct | total throughput | 2.94 | 6.98 | 5.250034246575344 | MiB/sec |
| PERF-IO-DISKSPD.json | diskspd_randwrite_4GB_8k_d1_th1_direct | write IO operations | 180506.0 | 428669.0 | 322588.76369863018 |  |
| PERF-IO-DISKSPD.json | diskspd_randwrite_4GB_8k_d1_th1_direct | write avg. latency | 1.119 | 2.658 | 1.5268356164383573 | milliseconds |
| PERF-IO-DISKSPD.json | diskspd_randwrite_4GB_8k_d1_th1_direct | write iops | 376.04 | 893.04 | 672.0497945205483 |  |
| PERF-IO-DISKSPD.json | diskspd_randwrite_4GB_8k_d1_th1_direct | write iops stdev | 4.16 | 176.74 | 44.08325342465754 | milliseconds |
| PERF-IO-DISKSPD.json | diskspd_randwrite_4GB_8k_d1_th1_direct | write latency stdev | 0.166 | 3.955 | 0.6379726027397265 | milliseconds |
| PERF-IO-DISKSPD.json | diskspd_randwrite_4GB_8k_d1_th1_direct | write throughput | 2.94 | 6.98 | 5.250034246575344 | MiB/sec |
| PERF-IO-DISKSPD.json | diskspd_randwrite_4GB_8k_d1_th1_direct | write total bytes | 1478705152.0 | 3511656448.0 | 2642647152.219178 |  |
| PERF-IO-DISKSPD.json | diskspd_read_250MB_64k_d16_th32_direct | avg. latency | 164.477 | 188.518 | 164.97304225352085 | milliseconds |
| PERF-IO-DISKSPD.json | diskspd_read_250MB_64k_d16_th32_direct | iops stdev | 0.4 | 960.2 | 61.12929577464791 | milliseconds |
| PERF-IO-DISKSPD.json | diskspd_read_250MB_64k_d16_th32_direct | latency stdev | 12.191 | 170.588 | 20.51592957746479 | milliseconds |
| PERF-IO-DISKSPD.json | diskspd_read_250MB_64k_d16_th32_direct | read IO operations | 1303968.0 | 1494296.0 | 1490084.6830985917 |  |
| PERF-IO-DISKSPD.json | diskspd_read_250MB_64k_d16_th32_direct | read avg. latency | 164.477 | 188.518 | 164.97304225352085 | milliseconds |
| PERF-IO-DISKSPD.json | diskspd_read_250MB_64k_d16_th32_direct | read iops | 2716.59 | 3113.06 | 3104.294295774648 |  |
| PERF-IO-DISKSPD.json | diskspd_read_250MB_64k_d16_th32_direct | read iops stdev | 0.4 | 960.2 | 61.12929577464791 | milliseconds |
| PERF-IO-DISKSPD.json | diskspd_read_250MB_64k_d16_th32_direct | read latency stdev | 12.191 | 170.588 | 20.515929577464794 | milliseconds |
| PERF-IO-DISKSPD.json | diskspd_read_250MB_64k_d16_th32_direct | read throughput | 169.79 | 194.57 | 194.01908450704216 | MiB/sec |
| PERF-IO-DISKSPD.json | diskspd_read_250MB_64k_d16_th32_direct | read total bytes | 85456846848.0 | 97930182656.0 | 97654189791.5493 |  |
| PERF-IO-DISKSPD.json | diskspd_read_250MB_64k_d16_th32_direct | total IO operations | 1303968.0 | 1494296.0 | 1490084.6830985917 |  |
| PERF-IO-DISKSPD.json | diskspd_read_250MB_64k_d16_th32_direct | total bytes | 85456846848.0 | 97930182656.0 | 97654189791.5493 |  |
| PERF-IO-DISKSPD.json | diskspd_read_250MB_64k_d16_th32_direct | total iops | 2716.59 | 3113.06 | 3104.294295774648 |  |
| PERF-IO-DISKSPD.json | diskspd_read_250MB_64k_d16_th32_direct | total throughput | 169.79 | 194.57 | 194.01908450704216 | MiB/sec |
| PERF-IO-DISKSPD.json | diskspd_read_4GB_12k_d1_th1_direct | avg. latency | 1.453 | 3.022 | 1.8423239436619719 | milliseconds |
| PERF-IO-DISKSPD.json | diskspd_read_4GB_12k_d1_th1_direct | iops stdev | 4.56 | 71.39 | 19.50500000000001 | milliseconds |
| PERF-IO-DISKSPD.json | diskspd_read_4GB_12k_d1_th1_direct | latency stdev | 0.196 | 8.87 | 0.5722535211267604 | milliseconds |
| PERF-IO-DISKSPD.json | diskspd_read_4GB_12k_d1_th1_direct | read IO operations | 158759.0 | 330056.0 | 264865.0633802817 |  |
| PERF-IO-DISKSPD.json | diskspd_read_4GB_12k_d1_th1_direct | read avg. latency | 1.453 | 3.022 | 1.8423239436619719 | milliseconds |
| PERF-IO-DISKSPD.json | diskspd_read_4GB_12k_d1_th1_direct | read iops | 330.74 | 687.59 | 551.7930985915492 |  |
| PERF-IO-DISKSPD.json | diskspd_read_4GB_12k_d1_th1_direct | read iops stdev | 4.56 | 71.39 | 19.50500000000001 | milliseconds |
| PERF-IO-DISKSPD.json | diskspd_read_4GB_12k_d1_th1_direct | read latency stdev | 0.196 | 8.87 | 0.5722535211267605 | milliseconds |
| PERF-IO-DISKSPD.json | diskspd_read_4GB_12k_d1_th1_direct | read throughput | 3.88 | 8.06 | 6.466267605633799 | MiB/sec |
| PERF-IO-DISKSPD.json | diskspd_read_4GB_12k_d1_th1_direct | read total bytes | 1950830592.0 | 4055728128.0 | 3254661898.816901 |  |
| PERF-IO-DISKSPD.json | diskspd_read_4GB_12k_d1_th1_direct | total IO operations | 158759.0 | 330056.0 | 264865.0633802817 |  |
| PERF-IO-DISKSPD.json | diskspd_read_4GB_12k_d1_th1_direct | total bytes | 1950830592.0 | 4055728128.0 | 3254661898.816901 |  |
| PERF-IO-DISKSPD.json | diskspd_read_4GB_12k_d1_th1_direct | total iops | 330.74 | 687.59 | 551.7930985915492 |  |
| PERF-IO-DISKSPD.json | diskspd_read_4GB_12k_d1_th1_direct | total throughput | 3.88 | 8.06 | 6.466267605633799 | MiB/sec |
| PERF-IO-DISKSPD.json | diskspd_read_4GB_16k_d1_th1_direct | avg. latency | 1.474 | 3.004 | 1.850302816901408 | milliseconds |
| PERF-IO-DISKSPD.json | diskspd_read_4GB_16k_d1_th1_direct | iops stdev | 4.11 | 104.53 | 20.891267605633808 | milliseconds |
| PERF-IO-DISKSPD.json | diskspd_read_4GB_16k_d1_th1_direct | latency stdev | 0.169 | 6.016 | 0.5710704225352113 | milliseconds |
| PERF-IO-DISKSPD.json | diskspd_read_4GB_16k_d1_th1_direct | read IO operations | 159751.0 | 325431.0 | 263615.11971830987 |  |
| PERF-IO-DISKSPD.json | diskspd_read_4GB_16k_d1_th1_direct | read avg. latency | 1.474 | 3.004 | 1.850302816901408 | milliseconds |
| PERF-IO-DISKSPD.json | diskspd_read_4GB_16k_d1_th1_direct | read iops | 332.81 | 677.98 | 549.1895070422535 |  |
| PERF-IO-DISKSPD.json | diskspd_read_4GB_16k_d1_th1_direct | read iops stdev | 4.11 | 104.53 | 20.891267605633808 | milliseconds |
| PERF-IO-DISKSPD.json | diskspd_read_4GB_16k_d1_th1_direct | read latency stdev | 0.169 | 6.016 | 0.5710704225352115 | milliseconds |
| PERF-IO-DISKSPD.json | diskspd_read_4GB_16k_d1_th1_direct | read throughput | 5.2 | 10.59 | 8.581267605633803 | MiB/sec |
| PERF-IO-DISKSPD.json | diskspd_read_4GB_16k_d1_th1_direct | read total bytes | 2617360384.0 | 5331861504.0 | 4319070121.464788 |  |
| PERF-IO-DISKSPD.json | diskspd_read_4GB_16k_d1_th1_direct | total IO operations | 159751.0 | 325431.0 | 263615.11971830987 |  |
| PERF-IO-DISKSPD.json | diskspd_read_4GB_16k_d1_th1_direct | total bytes | 2617360384.0 | 5331861504.0 | 4319070121.464788 |  |
| PERF-IO-DISKSPD.json | diskspd_read_4GB_16k_d1_th1_direct | total iops | 332.81 | 677.98 | 549.1895070422535 |  |
| PERF-IO-DISKSPD.json | diskspd_read_4GB_16k_d1_th1_direct | total throughput | 5.2 | 10.59 | 8.581267605633803 | MiB/sec |
| PERF-IO-DISKSPD.json | diskspd_read_4GB_32k_d1_th1_direct | avg. latency | 1.537 | 3.092 | 1.8835774647887318 | milliseconds |
| PERF-IO-DISKSPD.json | diskspd_read_4GB_32k_d1_th1_direct | iops stdev | 6.55 | 128.53 | 19.983309859154944 | milliseconds |
| PERF-IO-DISKSPD.json | diskspd_read_4GB_32k_d1_th1_direct | latency stdev | 0.184 | 9.209 | 0.6021619718309856 | milliseconds |
| PERF-IO-DISKSPD.json | diskspd_read_4GB_32k_d1_th1_direct | read IO operations | 155162.0 | 312097.0 | 259415.20422535213 |  |
| PERF-IO-DISKSPD.json | diskspd_read_4GB_32k_d1_th1_direct | read avg. latency | 1.537 | 3.092 | 1.8835774647887318 | milliseconds |
| PERF-IO-DISKSPD.json | diskspd_read_4GB_32k_d1_th1_direct | read iops | 323.25 | 650.19 | 540.4395774647887 |  |
| PERF-IO-DISKSPD.json | diskspd_read_4GB_32k_d1_th1_direct | read iops stdev | 6.55 | 128.53 | 19.983309859154944 | milliseconds |
| PERF-IO-DISKSPD.json | diskspd_read_4GB_32k_d1_th1_direct | read latency stdev | 0.184 | 9.209 | 0.6021619718309857 | milliseconds |
| PERF-IO-DISKSPD.json | diskspd_read_4GB_32k_d1_th1_direct | read throughput | 10.1 | 20.32 | 16.888521126760574 | MiB/sec |
| PERF-IO-DISKSPD.json | diskspd_read_4GB_32k_d1_th1_direct | read total bytes | 5084348416.0 | 10226794496.0 | 8500517412.056338 |  |
| PERF-IO-DISKSPD.json | diskspd_read_4GB_32k_d1_th1_direct | total IO operations | 155162.0 | 312097.0 | 259415.20422535213 |  |
| PERF-IO-DISKSPD.json | diskspd_read_4GB_32k_d1_th1_direct | total bytes | 5084348416.0 | 10226794496.0 | 8500517412.056338 |  |
| PERF-IO-DISKSPD.json | diskspd_read_4GB_32k_d1_th1_direct | total iops | 323.25 | 650.19 | 540.4395774647887 |  |
| PERF-IO-DISKSPD.json | diskspd_read_4GB_32k_d1_th1_direct | total throughput | 10.1 | 20.32 | 16.888521126760574 | MiB/sec |
| PERF-IO-DISKSPD.json | diskspd_read_4GB_4k_d1_th1_direct | avg. latency | 1.466 | 2.948 | 1.9627986111111118 | milliseconds |
| PERF-IO-DISKSPD.json | diskspd_read_4GB_4k_d1_th1_direct | iops stdev | 4.14 | 66.24 | 19.570347222222226 | milliseconds |
| PERF-IO-DISKSPD.json | diskspd_read_4GB_4k_d1_th1_direct | latency stdev | 0.201 | 7.517 | 0.5595555555555556 | milliseconds |
| PERF-IO-DISKSPD.json | diskspd_read_4GB_4k_d1_th1_direct | read IO operations | 162781.0 | 327110.0 | 248805.34027777779 |  |
| PERF-IO-DISKSPD.json | diskspd_read_4GB_4k_d1_th1_direct | read avg. latency | 1.466 | 2.948 | 1.9627986111111114 | milliseconds |
| PERF-IO-DISKSPD.json | diskspd_read_4GB_4k_d1_th1_direct | read iops | 339.13 | 681.47 | 518.3359027777777 |  |
| PERF-IO-DISKSPD.json | diskspd_read_4GB_4k_d1_th1_direct | read iops stdev | 4.14 | 66.24 | 19.570347222222226 | milliseconds |
| PERF-IO-DISKSPD.json | diskspd_read_4GB_4k_d1_th1_direct | read latency stdev | 0.201 | 7.517 | 0.5595555555555556 | milliseconds |
| PERF-IO-DISKSPD.json | diskspd_read_4GB_4k_d1_th1_direct | read throughput | 1.32 | 2.66 | 2.0246527777777785 | MiB/sec |
| PERF-IO-DISKSPD.json | diskspd_read_4GB_4k_d1_th1_direct | read total bytes | 666750976.0 | 1339842560.0 | 1019106673.7777778 |  |
| PERF-IO-DISKSPD.json | diskspd_read_4GB_4k_d1_th1_direct | total IO operations | 162781.0 | 327110.0 | 248805.34027777779 |  |
| PERF-IO-DISKSPD.json | diskspd_read_4GB_4k_d1_th1_direct | total bytes | 666750976.0 | 1339842560.0 | 1019106673.7777778 |  |
| PERF-IO-DISKSPD.json | diskspd_read_4GB_4k_d1_th1_direct | total iops | 339.13 | 681.47 | 518.3359027777777 |  |
| PERF-IO-DISKSPD.json | diskspd_read_4GB_4k_d1_th1_direct | total throughput | 1.32 | 2.66 | 2.024652777777778 | MiB/sec |
| PERF-IO-DISKSPD.json | diskspd_read_4GB_8k_d1_th1_direct | avg. latency | 1.449 | 2.986 | 1.8415070422535219 | milliseconds |
| PERF-IO-DISKSPD.json | diskspd_read_4GB_8k_d1_th1_direct | iops stdev | 4.92 | 65.35 | 19.650774647887336 | milliseconds |
| PERF-IO-DISKSPD.json | diskspd_read_4GB_8k_d1_th1_direct | latency stdev | 0.194 | 6.509 | 0.5412183098591549 | milliseconds |
| PERF-IO-DISKSPD.json | diskspd_read_4GB_8k_d1_th1_direct | read IO operations | 160700.0 | 331101.0 | 264828.1478873239 |  |
| PERF-IO-DISKSPD.json | diskspd_read_4GB_8k_d1_th1_direct | read avg. latency | 1.449 | 2.986 | 1.8415070422535214 | milliseconds |
| PERF-IO-DISKSPD.json | diskspd_read_4GB_8k_d1_th1_direct | read iops | 334.79 | 689.78 | 551.7169014084502 |  |
| PERF-IO-DISKSPD.json | diskspd_read_4GB_8k_d1_th1_direct | read iops stdev | 4.92 | 65.35 | 19.650774647887336 | milliseconds |
| PERF-IO-DISKSPD.json | diskspd_read_4GB_8k_d1_th1_direct | read latency stdev | 0.194 | 6.509 | 0.5412183098591549 | milliseconds |
| PERF-IO-DISKSPD.json | diskspd_read_4GB_8k_d1_th1_direct | read throughput | 2.62 | 5.39 | 4.310070422535213 | MiB/sec |
| PERF-IO-DISKSPD.json | diskspd_read_4GB_8k_d1_th1_direct | read total bytes | 1316454400.0 | 2712379392.0 | 2169472187.4929578 |  |
| PERF-IO-DISKSPD.json | diskspd_read_4GB_8k_d1_th1_direct | total IO operations | 160700.0 | 331101.0 | 264828.1478873239 |  |
| PERF-IO-DISKSPD.json | diskspd_read_4GB_8k_d1_th1_direct | total bytes | 1316454400.0 | 2712379392.0 | 2169472187.4929578 |  |
| PERF-IO-DISKSPD.json | diskspd_read_4GB_8k_d1_th1_direct | total iops | 334.79 | 689.78 | 551.7169014084504 |  |
| PERF-IO-DISKSPD.json | diskspd_read_4GB_8k_d1_th1_direct | total throughput | 2.62 | 5.39 | 4.310070422535211 | MiB/sec |
| PERF-IO-DISKSPD.json | diskspd_readwrite_250MB_64k_d16_th32_direct | avg. latency | 164.467 | 167.674 | 164.58167123287647 | milliseconds |
| PERF-IO-DISKSPD.json | diskspd_readwrite_250MB_64k_d16_th32_direct | iops stdev | 0.41 | 344.86 | 46.38095890410958 | milliseconds |
| PERF-IO-DISKSPD.json | diskspd_readwrite_250MB_64k_d16_th32_direct | latency stdev | 15.556 | 51.793 | 20.42584246575342 | milliseconds |
| PERF-IO-DISKSPD.json | diskspd_readwrite_250MB_64k_d16_th32_direct | read IO operations | 1026096.0 | 1046285.0 | 1045457.5684931506 |  |
| PERF-IO-DISKSPD.json | diskspd_readwrite_250MB_64k_d16_th32_direct | read avg. latency | 162.606 | 168.257 | 165.23766438356155 | milliseconds |
| PERF-IO-DISKSPD.json | diskspd_readwrite_250MB_64k_d16_th32_direct | read iops | 2137.7 | 2179.71 | 2178.0002054794515 |  |
| PERF-IO-DISKSPD.json | diskspd_readwrite_250MB_64k_d16_th32_direct | read iops stdev | 24.19 | 239.91 | 47.63219178082192 | milliseconds |
| PERF-IO-DISKSPD.json | diskspd_readwrite_250MB_64k_d16_th32_direct | read latency stdev | 14.518 | 49.914 | 20.126383561643846 | milliseconds |
| PERF-IO-DISKSPD.json | diskspd_readwrite_250MB_64k_d16_th32_direct | read latency/operation(P50) | 153.724 | 167.837 | 158.8345821917809 | milliseconds |
| PERF-IO-DISKSPD.json | diskspd_readwrite_250MB_64k_d16_th32_direct | read latency/operation(P75) | 177.525 | 194.846 | 184.1007602739726 | milliseconds |
| PERF-IO-DISKSPD.json | diskspd_readwrite_250MB_64k_d16_th32_direct | read latency/operation(P90) | 187.196 | 200.492 | 194.24106849315053 | milliseconds |
| PERF-IO-DISKSPD.json | diskspd_readwrite_250MB_64k_d16_th32_direct | read latency/operation(P95) | 190.071 | 202.622 | 197.44393835616445 | milliseconds |
| PERF-IO-DISKSPD.json | diskspd_readwrite_250MB_64k_d16_th32_direct | read latency/operation(P99) | 193.37 | 218.169 | 205.32247260273977 | milliseconds |
| PERF-IO-DISKSPD.json | diskspd_readwrite_250MB_64k_d16_th32_direct | read throughput | 133.61 | 136.23 | 136.12486301369845 | MiB/sec |
| PERF-IO-DISKSPD.json | diskspd_readwrite_250MB_64k_d16_th32_direct | read total bytes | 67246227456.0 | 68569333760.0 | 68515107208.76712 |  |
| PERF-IO-DISKSPD.json | diskspd_readwrite_250MB_64k_d16_th32_direct | total IO operations | 1465681.0 | 1494296.0 | 1493237.308219178 |  |
| PERF-IO-DISKSPD.json | diskspd_readwrite_250MB_64k_d16_th32_direct | total bytes | 96054870016.0 | 97930182656.0 | 97860800231.45206 |  |
| PERF-IO-DISKSPD.json | diskspd_readwrite_250MB_64k_d16_th32_direct | total iops | 3053.5 | 3113.07 | 3110.8586301369884 |  |
| PERF-IO-DISKSPD.json | diskspd_readwrite_250MB_64k_d16_th32_direct | total latency/operation(P50) | 154.262 | 166.66 | 158.10439041095894 | milliseconds |
| PERF-IO-DISKSPD.json | diskspd_readwrite_250MB_64k_d16_th32_direct | total latency/operation(P75) | 176.445 | 194.489 | 183.8431917808219 | milliseconds |
| PERF-IO-DISKSPD.json | diskspd_readwrite_250MB_64k_d16_th32_direct | total latency/operation(P90) | 185.577 | 199.443 | 194.13324657534245 | milliseconds |
| PERF-IO-DISKSPD.json | diskspd_readwrite_250MB_64k_d16_th32_direct | total latency/operation(P95) | 190.161 | 201.969 | 197.5252602739726 | milliseconds |
| PERF-IO-DISKSPD.json | diskspd_readwrite_250MB_64k_d16_th32_direct | total latency/operation(P99) | 196.246 | 215.286 | 204.86450684931504 | milliseconds |
| PERF-IO-DISKSPD.json | diskspd_readwrite_250MB_64k_d16_th32_direct | total throughput | 190.84 | 194.57 | 194.4284246575343 | MiB/sec |
| PERF-IO-DISKSPD.json | diskspd_readwrite_250MB_64k_d16_th32_direct | write IO operations | 439585.0 | 448165.0 | 447779.7397260274 |  |
| PERF-IO-DISKSPD.json | diskspd_readwrite_250MB_64k_d16_th32_direct | write avg. latency | 159.327 | 168.813 | 163.0501506849315 | milliseconds |
| PERF-IO-DISKSPD.json | diskspd_readwrite_250MB_64k_d16_th32_direct | write iops | 915.8 | 933.66 | 932.8586301369858 |  |
| PERF-IO-DISKSPD.json | diskspd_readwrite_250MB_64k_d16_th32_direct | write iops stdev | 23.83 | 109.97 | 32.71410958904107 | milliseconds |
| PERF-IO-DISKSPD.json | diskspd_readwrite_250MB_64k_d16_th32_direct | write latency stdev | 14.245 | 69.434 | 20.80611643835617 | milliseconds |
| PERF-IO-DISKSPD.json | diskspd_readwrite_250MB_64k_d16_th32_direct | write latency/operation(P50) | 150.628 | 167.384 | 155.99070547945207 | milliseconds |
| PERF-IO-DISKSPD.json | diskspd_readwrite_250MB_64k_d16_th32_direct | write latency/operation(P75) | 167.08 | 197.656 | 183.4390547945206 | milliseconds |
| PERF-IO-DISKSPD.json | diskspd_readwrite_250MB_64k_d16_th32_direct | write latency/operation(P90) | 181.96 | 200.877 | 194.03419178082198 | milliseconds |
| PERF-IO-DISKSPD.json | diskspd_readwrite_250MB_64k_d16_th32_direct | write latency/operation(P95) | 184.735 | 202.269 | 196.19018493150686 | milliseconds |
| PERF-IO-DISKSPD.json | diskspd_readwrite_250MB_64k_d16_th32_direct | write latency/operation(P99) | 193.038 | 210.797 | 202.1542260273972 | milliseconds |
| PERF-IO-DISKSPD.json | diskspd_readwrite_250MB_64k_d16_th32_direct | write throughput | 57.24 | 58.35 | 58.30363013698638 | MiB/sec |
| PERF-IO-DISKSPD.json | diskspd_readwrite_250MB_64k_d16_th32_direct | write total bytes | 28808642560.0 | 29370941440.0 | 29345693022.684934 |  |
| PERF-IO-DISKSPD.json | diskspd_readwrite_4GB_12k_d1_th1_direct | avg. latency | 1.448 | 4.935 | 1.8656849315068485 | milliseconds |
| PERF-IO-DISKSPD.json | diskspd_readwrite_4GB_12k_d1_th1_direct | iops stdev | 5.96 | 88.62 | 26.226438356164377 | milliseconds |
| PERF-IO-DISKSPD.json | diskspd_readwrite_4GB_12k_d1_th1_direct | latency stdev | 0.286 | 5.954 | 0.8137808219178079 | milliseconds |
| PERF-IO-DISKSPD.json | diskspd_readwrite_4GB_12k_d1_th1_direct | read IO operations | 68024.0 | 232149.0 | 185547.48630136986 |  |
| PERF-IO-DISKSPD.json | diskspd_readwrite_4GB_12k_d1_th1_direct | read avg. latency | 1.536 | 5.891 | 1.9879246575342476 | milliseconds |
| PERF-IO-DISKSPD.json | diskspd_readwrite_4GB_12k_d1_th1_direct | read iops | 141.71 | 483.64 | 386.5513013698628 |  |
| PERF-IO-DISKSPD.json | diskspd_readwrite_4GB_12k_d1_th1_direct | read iops stdev | 8.38 | 63.5 | 20.90623287671233 | milliseconds |
| PERF-IO-DISKSPD.json | diskspd_readwrite_4GB_12k_d1_th1_direct | read latency stdev | 0.207 | 6.894 | 0.7531369863013702 | milliseconds |
| PERF-IO-DISKSPD.json | diskspd_readwrite_4GB_12k_d1_th1_direct | read latency/operation(P50) | 1.517 | 3.221 | 1.885630136986302 | milliseconds |
| PERF-IO-DISKSPD.json | diskspd_readwrite_4GB_12k_d1_th1_direct | read latency/operation(P75) | 1.598 | 4.338 | 2.0067602739726029 | milliseconds |
| PERF-IO-DISKSPD.json | diskspd_readwrite_4GB_12k_d1_th1_direct | read latency/operation(P90) | 1.678 | 20.187 | 2.353958904109589 | milliseconds |
| PERF-IO-DISKSPD.json | diskspd_readwrite_4GB_12k_d1_th1_direct | read latency/operation(P95) | 1.732 | 26.439 | 2.633609589041096 | milliseconds |
| PERF-IO-DISKSPD.json | diskspd_readwrite_4GB_12k_d1_th1_direct | read latency/operation(P99) | 1.89 | 27.389 | 3.866404109589041 | milliseconds |
| PERF-IO-DISKSPD.json | diskspd_readwrite_4GB_12k_d1_th1_direct | read throughput | 1.66 | 5.67 | 4.530205479452053 | MiB/sec |
| PERF-IO-DISKSPD.json | diskspd_readwrite_4GB_12k_d1_th1_direct | read total bytes | 835878912.0 | 2852646912.0 | 2280007511.6712329 |  |
| PERF-IO-DISKSPD.json | diskspd_readwrite_4GB_12k_d1_th1_direct | total IO operations | 97228.0 | 331265.0 | 264843.8561643836 |  |
| PERF-IO-DISKSPD.json | diskspd_readwrite_4GB_12k_d1_th1_direct | total bytes | 1194737664.0 | 4070584320.0 | 3254401304.547945 |  |
| PERF-IO-DISKSPD.json | diskspd_readwrite_4GB_12k_d1_th1_direct | total iops | 202.55 | 690.13 | 551.7493835616442 |  |
| PERF-IO-DISKSPD.json | diskspd_readwrite_4GB_12k_d1_th1_direct | total latency/operation(P50) | 1.461 | 3.108 | 1.827835616438356 | milliseconds |
| PERF-IO-DISKSPD.json | diskspd_readwrite_4GB_12k_d1_th1_direct | total latency/operation(P75) | 1.564 | 3.542 | 1.9616095890410966 | milliseconds |
| PERF-IO-DISKSPD.json | diskspd_readwrite_4GB_12k_d1_th1_direct | total latency/operation(P90) | 1.654 | 4.647 | 2.150191780821918 | milliseconds |
| PERF-IO-DISKSPD.json | diskspd_readwrite_4GB_12k_d1_th1_direct | total latency/operation(P95) | 1.712 | 25.457 | 2.66095890410959 | milliseconds |
| PERF-IO-DISKSPD.json | diskspd_readwrite_4GB_12k_d1_th1_direct | total latency/operation(P99) | 1.899 | 27.236 | 4.05330821917808 | milliseconds |
| PERF-IO-DISKSPD.json | diskspd_readwrite_4GB_12k_d1_th1_direct | total throughput | 2.37 | 8.09 | 6.465547945205481 | MiB/sec |
| PERF-IO-DISKSPD.json | diskspd_readwrite_4GB_12k_d1_th1_direct | write IO operations | 29204.0 | 99116.0 | 79296.3698630137 |  |
| PERF-IO-DISKSPD.json | diskspd_readwrite_4GB_12k_d1_th1_direct | write avg. latency | 1.228 | 2.931 | 1.579705479452055 | milliseconds |
| PERF-IO-DISKSPD.json | diskspd_readwrite_4GB_12k_d1_th1_direct | write iops | 60.84 | 206.49 | 165.19828767123284 |  |
| PERF-IO-DISKSPD.json | diskspd_readwrite_4GB_12k_d1_th1_direct | write iops stdev | 8.29 | 27.64 | 13.960342465753426 | milliseconds |
| PERF-IO-DISKSPD.json | diskspd_readwrite_4GB_12k_d1_th1_direct | write latency stdev | 0.169 | 4.324 | 0.644061643835616 | milliseconds |
| PERF-IO-DISKSPD.json | diskspd_readwrite_4GB_12k_d1_th1_direct | write latency/operation(P50) | 1.135 | 2.571 | 1.427260273972603 | milliseconds |
| PERF-IO-DISKSPD.json | diskspd_readwrite_4GB_12k_d1_th1_direct | write latency/operation(P75) | 1.227 | 3.083 | 1.5666506849315074 | milliseconds |
| PERF-IO-DISKSPD.json | diskspd_readwrite_4GB_12k_d1_th1_direct | write latency/operation(P90) | 1.348 | 6.457 | 1.9568150684931512 | milliseconds |
| PERF-IO-DISKSPD.json | diskspd_readwrite_4GB_12k_d1_th1_direct | write latency/operation(P95) | 1.416 | 8.33 | 2.467767123287673 | milliseconds |
| PERF-IO-DISKSPD.json | diskspd_readwrite_4GB_12k_d1_th1_direct | write latency/operation(P99) | 1.535 | 14.961 | 4.1606575342465759 | milliseconds |
| PERF-IO-DISKSPD.json | diskspd_readwrite_4GB_12k_d1_th1_direct | write throughput | 0.71 | 2.42 | 1.9360958904109594 | MiB/sec |
| PERF-IO-DISKSPD.json | diskspd_readwrite_4GB_12k_d1_th1_direct | write total bytes | 358858752.0 | 1217937408.0 | 974393792.8767123 |  |
| PERF-IO-DISKSPD.json | diskspd_readwrite_4GB_16k_d1_th1_direct | avg. latency | 1.499 | 3.423 | 1.879282758620689 | milliseconds |
| PERF-IO-DISKSPD.json | diskspd_readwrite_4GB_16k_d1_th1_direct | iops stdev | 6.76 | 73.64 | 26.72179310344829 | milliseconds |
| PERF-IO-DISKSPD.json | diskspd_readwrite_4GB_16k_d1_th1_direct | latency stdev | 0.279 | 3.821 | 0.6156758620689659 | milliseconds |
| PERF-IO-DISKSPD.json | diskspd_readwrite_4GB_16k_d1_th1_direct | read IO operations | 98147.0 | 224192.0 | 182695.0551724138 |  |
| PERF-IO-DISKSPD.json | diskspd_readwrite_4GB_16k_d1_th1_direct | read avg. latency | 1.572 | 3.23 | 1.9595862068965513 | milliseconds |
| PERF-IO-DISKSPD.json | diskspd_readwrite_4GB_16k_d1_th1_direct | read iops | 204.47 | 467.05 | 380.6089655172415 |  |
| PERF-IO-DISKSPD.json | diskspd_readwrite_4GB_16k_d1_th1_direct | read iops stdev | 7.73 | 52.1 | 20.483034482758627 | milliseconds |
| PERF-IO-DISKSPD.json | diskspd_readwrite_4GB_16k_d1_th1_direct | read latency stdev | 0.226 | 4.537 | 0.49887586206896558 | milliseconds |
| PERF-IO-DISKSPD.json | diskspd_readwrite_4GB_16k_d1_th1_direct | read latency/operation(P50) | 1.535 | 3.084 | 1.8878275862068963 | milliseconds |
| PERF-IO-DISKSPD.json | diskspd_readwrite_4GB_16k_d1_th1_direct | read latency/operation(P75) | 1.628 | 3.223 | 2.002744827586207 | milliseconds |
| PERF-IO-DISKSPD.json | diskspd_readwrite_4GB_16k_d1_th1_direct | read latency/operation(P90) | 1.747 | 4.154 | 2.198206896551725 | milliseconds |
| PERF-IO-DISKSPD.json | diskspd_readwrite_4GB_16k_d1_th1_direct | read latency/operation(P95) | 1.842 | 5.697 | 2.440558620689657 | milliseconds |
| PERF-IO-DISKSPD.json | diskspd_readwrite_4GB_16k_d1_th1_direct | read latency/operation(P99) | 2.147 | 9.34 | 3.341855172413793 | milliseconds |
| PERF-IO-DISKSPD.json | diskspd_readwrite_4GB_16k_d1_th1_direct | read throughput | 3.19 | 7.3 | 5.947172413793105 | MiB/sec |
| PERF-IO-DISKSPD.json | diskspd_readwrite_4GB_16k_d1_th1_direct | read total bytes | 1608040448.0 | 3673161728.0 | 2993275783.9448277 |  |
| PERF-IO-DISKSPD.json | diskspd_readwrite_4GB_16k_d1_th1_direct | total IO operations | 140197.0 | 320053.0 | 260770.88965517243 |  |
| PERF-IO-DISKSPD.json | diskspd_readwrite_4GB_16k_d1_th1_direct | total bytes | 2296987648.0 | 5243748352.0 | 4272470256.110345 |  |
| PERF-IO-DISKSPD.json | diskspd_readwrite_4GB_16k_d1_th1_direct | total iops | 292.07 | 666.76 | 543.2653103448278 |  |
| PERF-IO-DISKSPD.json | diskspd_readwrite_4GB_16k_d1_th1_direct | total latency/operation(P50) | 1.483 | 3.186 | 1.841572413793103 | milliseconds |
| PERF-IO-DISKSPD.json | diskspd_readwrite_4GB_16k_d1_th1_direct | total latency/operation(P75) | 1.599 | 3.756 | 1.984620689655173 | milliseconds |
| PERF-IO-DISKSPD.json | diskspd_readwrite_4GB_16k_d1_th1_direct | total latency/operation(P90) | 1.747 | 4.218 | 2.2126137931034486 | milliseconds |
| PERF-IO-DISKSPD.json | diskspd_readwrite_4GB_16k_d1_th1_direct | total latency/operation(P95) | 1.923 | 4.972 | 2.5555034482758627 | milliseconds |
| PERF-IO-DISKSPD.json | diskspd_readwrite_4GB_16k_d1_th1_direct | total latency/operation(P99) | 2.355 | 8.689 | 3.863944827586209 | milliseconds |
| PERF-IO-DISKSPD.json | diskspd_readwrite_4GB_16k_d1_th1_direct | total throughput | 4.56 | 10.42 | 8.488689655172417 | MiB/sec |
| PERF-IO-DISKSPD.json | diskspd_readwrite_4GB_16k_d1_th1_direct | write IO operations | 42050.0 | 95861.0 | 78075.83448275863 |  |
| PERF-IO-DISKSPD.json | diskspd_readwrite_4GB_16k_d1_th1_direct | write avg. latency | 1.227 | 3.871 | 1.691358620689656 | milliseconds |
| PERF-IO-DISKSPD.json | diskspd_readwrite_4GB_16k_d1_th1_direct | write iops | 87.6 | 199.7 | 162.65579310344826 |  |
| PERF-IO-DISKSPD.json | diskspd_readwrite_4GB_16k_d1_th1_direct | write iops stdev | 6.82 | 25.54 | 14.27827586206896 | milliseconds |
| PERF-IO-DISKSPD.json | diskspd_readwrite_4GB_16k_d1_th1_direct | write latency stdev | 0.227 | 2.479 | 0.7033931034482763 | milliseconds |
| PERF-IO-DISKSPD.json | diskspd_readwrite_4GB_16k_d1_th1_direct | write latency/operation(P50) | 1.122 | 3.779 | 1.5174482758620698 | milliseconds |
| PERF-IO-DISKSPD.json | diskspd_readwrite_4GB_16k_d1_th1_direct | write latency/operation(P75) | 1.213 | 3.916 | 1.7360482758620686 | milliseconds |
| PERF-IO-DISKSPD.json | diskspd_readwrite_4GB_16k_d1_th1_direct | write latency/operation(P90) | 1.357 | 4.334 | 2.208689655172414 | milliseconds |
| PERF-IO-DISKSPD.json | diskspd_readwrite_4GB_16k_d1_th1_direct | write latency/operation(P95) | 1.447 | 5.264 | 2.7404758620689657 | milliseconds |
| PERF-IO-DISKSPD.json | diskspd_readwrite_4GB_16k_d1_th1_direct | write latency/operation(P99) | 2.262 | 10.503 | 4.429420689655172 | milliseconds |
| PERF-IO-DISKSPD.json | diskspd_readwrite_4GB_16k_d1_th1_direct | write throughput | 1.37 | 3.12 | 2.5417241379310337 | MiB/sec |
| PERF-IO-DISKSPD.json | diskspd_readwrite_4GB_16k_d1_th1_direct | write total bytes | 688947200.0 | 1570586624.0 | 1279194472.1655174 |  |
| PERF-IO-DISKSPD.json | diskspd_readwrite_4GB_32k_d1_th1_direct | avg. latency | 1.578 | 10.44 | 2.142390410958904 | milliseconds |
| PERF-IO-DISKSPD.json | diskspd_readwrite_4GB_32k_d1_th1_direct | iops stdev | 9.13 | 87.94 | 30.724109589041104 | milliseconds |
| PERF-IO-DISKSPD.json | diskspd_readwrite_4GB_32k_d1_th1_direct | latency stdev | 0.345 | 9.466 | 1.013698630136986 | milliseconds |
| PERF-IO-DISKSPD.json | diskspd_readwrite_4GB_32k_d1_th1_direct | read IO operations | 32177.0 | 213016.0 | 169559.66438356165 |  |
| PERF-IO-DISKSPD.json | diskspd_readwrite_4GB_32k_d1_th1_direct | read avg. latency | 1.663 | 12.639 | 2.2571438356164377 | milliseconds |
| PERF-IO-DISKSPD.json | diskspd_readwrite_4GB_32k_d1_th1_direct | read iops | 67.03 | 443.77 | 353.24445205479449 |  |
| PERF-IO-DISKSPD.json | diskspd_readwrite_4GB_32k_d1_th1_direct | read iops stdev | 6.53 | 61.9 | 23.164794520547944 | milliseconds |
| PERF-IO-DISKSPD.json | diskspd_readwrite_4GB_32k_d1_th1_direct | read latency stdev | 0.27 | 10.28 | 0.9738767123287677 | milliseconds |
| PERF-IO-DISKSPD.json | diskspd_readwrite_4GB_32k_d1_th1_direct | read latency/operation(P50) | 1.599 | 6.029 | 2.0213287671232869 | milliseconds |
| PERF-IO-DISKSPD.json | diskspd_readwrite_4GB_32k_d1_th1_direct | read latency/operation(P75) | 1.738 | 22.397 | 2.424753424657534 | milliseconds |
| PERF-IO-DISKSPD.json | diskspd_readwrite_4GB_32k_d1_th1_direct | read latency/operation(P90) | 1.887 | 30.559 | 2.833965753424657 | milliseconds |
| PERF-IO-DISKSPD.json | diskspd_readwrite_4GB_32k_d1_th1_direct | read latency/operation(P95) | 1.979 | 31.087 | 3.2529452054794509 | milliseconds |
| PERF-IO-DISKSPD.json | diskspd_readwrite_4GB_32k_d1_th1_direct | read latency/operation(P99) | 2.44 | 31.969 | 5.359438356164383 | milliseconds |
| PERF-IO-DISKSPD.json | diskspd_readwrite_4GB_32k_d1_th1_direct | read throughput | 2.09 | 13.87 | 11.03917808219178 | MiB/sec |
| PERF-IO-DISKSPD.json | diskspd_readwrite_4GB_32k_d1_th1_direct | read total bytes | 1054375936.0 | 6980108288.0 | 5556131082.520548 |  |
| PERF-IO-DISKSPD.json | diskspd_readwrite_4GB_32k_d1_th1_direct | total IO operations | 45962.0 | 304064.0 | 242044.5684931507 |  |
| PERF-IO-DISKSPD.json | diskspd_readwrite_4GB_32k_d1_th1_direct | total bytes | 1506082816.0 | 9963569152.0 | 7931316420.383562 |  |
| PERF-IO-DISKSPD.json | diskspd_readwrite_4GB_32k_d1_th1_direct | total iops | 95.75 | 633.45 | 504.25226027397266 |  |
| PERF-IO-DISKSPD.json | diskspd_readwrite_4GB_32k_d1_th1_direct | total latency/operation(P50) | 1.543 | 5.769 | 1.9653767123287667 | milliseconds |
| PERF-IO-DISKSPD.json | diskspd_readwrite_4GB_32k_d1_th1_direct | total latency/operation(P75) | 1.677 | 8.625 | 2.201753424657533 | milliseconds |
| PERF-IO-DISKSPD.json | diskspd_readwrite_4GB_32k_d1_th1_direct | total latency/operation(P90) | 1.864 | 30.029 | 2.834109589041095 | milliseconds |
| PERF-IO-DISKSPD.json | diskspd_readwrite_4GB_32k_d1_th1_direct | total latency/operation(P95) | 1.978 | 30.85 | 3.3318698630137004 | milliseconds |
| PERF-IO-DISKSPD.json | diskspd_readwrite_4GB_32k_d1_th1_direct | total latency/operation(P99) | 2.629 | 31.611 | 5.636568493150686 | milliseconds |
| PERF-IO-DISKSPD.json | diskspd_readwrite_4GB_32k_d1_th1_direct | total throughput | 2.99 | 19.8 | 15.757739726027398 | MiB/sec |
| PERF-IO-DISKSPD.json | diskspd_readwrite_4GB_32k_d1_th1_direct | write IO operations | 13785.0 | 91048.0 | 72484.90410958904 |  |
| PERF-IO-DISKSPD.json | diskspd_readwrite_4GB_32k_d1_th1_direct | write avg. latency | 1.17 | 5.363 | 1.87413698630137 | milliseconds |
| PERF-IO-DISKSPD.json | diskspd_readwrite_4GB_32k_d1_th1_direct | write iops | 28.72 | 189.68 | 151.00842465753423 |  |
| PERF-IO-DISKSPD.json | diskspd_readwrite_4GB_32k_d1_th1_direct | write iops stdev | 5.74 | 28.37 | 14.350068493150685 | milliseconds |
| PERF-IO-DISKSPD.json | diskspd_readwrite_4GB_32k_d1_th1_direct | write latency stdev | 0.241 | 2.557 | 0.7713493150684934 | milliseconds |
| PERF-IO-DISKSPD.json | diskspd_readwrite_4GB_32k_d1_th1_direct | write latency/operation(P50) | 1.11 | 5.265 | 1.6863013698630143 | milliseconds |
| PERF-IO-DISKSPD.json | diskspd_readwrite_4GB_32k_d1_th1_direct | write latency/operation(P75) | 1.181 | 5.432 | 1.934027397260273 | milliseconds |
| PERF-IO-DISKSPD.json | diskspd_readwrite_4GB_32k_d1_th1_direct | write latency/operation(P90) | 1.286 | 5.716 | 2.443047945205479 | milliseconds |
| PERF-IO-DISKSPD.json | diskspd_readwrite_4GB_32k_d1_th1_direct | write latency/operation(P95) | 1.437 | 7.79 | 2.9998835616438357 | milliseconds |
| PERF-IO-DISKSPD.json | diskspd_readwrite_4GB_32k_d1_th1_direct | write latency/operation(P99) | 2.451 | 10.978 | 4.794623287671234 | milliseconds |
| PERF-IO-DISKSPD.json | diskspd_readwrite_4GB_32k_d1_th1_direct | write throughput | 0.9 | 5.93 | 4.718698630136984 | MiB/sec |
| PERF-IO-DISKSPD.json | diskspd_readwrite_4GB_32k_d1_th1_direct | write total bytes | 451706880.0 | 2983460864.0 | 2375185337.8630139 |  |
| PERF-IO-DISKSPD.json | diskspd_readwrite_4GB_4k_d1_th1_direct | avg. latency | 1.51 | 3.964 | 1.8748767123287662 | milliseconds |
| PERF-IO-DISKSPD.json | diskspd_readwrite_4GB_4k_d1_th1_direct | iops stdev | 5.54 | 185.36 | 28.56952054794517 | milliseconds |
| PERF-IO-DISKSPD.json | diskspd_readwrite_4GB_4k_d1_th1_direct | latency stdev | 0.303 | 6.439 | 0.8682054794520548 | milliseconds |
| PERF-IO-DISKSPD.json | diskspd_readwrite_4GB_4k_d1_th1_direct | read IO operations | 84708.0 | 222494.0 | 183792.18493150685 |  |
| PERF-IO-DISKSPD.json | diskspd_readwrite_4GB_4k_d1_th1_direct | read avg. latency | 1.48 | 4.543 | 1.9668972602739726 | milliseconds |
| PERF-IO-DISKSPD.json | diskspd_readwrite_4GB_4k_d1_th1_direct | read iops | 176.47 | 463.52 | 382.89465753424659 |  |
| PERF-IO-DISKSPD.json | diskspd_readwrite_4GB_4k_d1_th1_direct | read iops stdev | 8.11 | 130.18 | 22.8954794520548 | milliseconds |
| PERF-IO-DISKSPD.json | diskspd_readwrite_4GB_4k_d1_th1_direct | read latency stdev | 0.199 | 5.082 | 0.758308219178082 | milliseconds |
| PERF-IO-DISKSPD.json | diskspd_readwrite_4GB_4k_d1_th1_direct | read latency/operation(P50) | 1.465 | 3.158 | 1.874650684931507 | milliseconds |
| PERF-IO-DISKSPD.json | diskspd_readwrite_4GB_4k_d1_th1_direct | read latency/operation(P75) | 1.543 | 3.289 | 1.98095890410959 | milliseconds |
| PERF-IO-DISKSPD.json | diskspd_readwrite_4GB_4k_d1_th1_direct | read latency/operation(P90) | 1.62 | 3.564 | 2.1395273972602748 | milliseconds |
| PERF-IO-DISKSPD.json | diskspd_readwrite_4GB_4k_d1_th1_direct | read latency/operation(P95) | 1.673 | 20.232 | 2.567383561643836 | milliseconds |
| PERF-IO-DISKSPD.json | diskspd_readwrite_4GB_4k_d1_th1_direct | read latency/operation(P99) | 1.809 | 24.554 | 3.921260273972602 | milliseconds |
| PERF-IO-DISKSPD.json | diskspd_readwrite_4GB_4k_d1_th1_direct | read throughput | 0.69 | 1.81 | 1.4959589041095889 | MiB/sec |
| PERF-IO-DISKSPD.json | diskspd_readwrite_4GB_4k_d1_th1_direct | read total bytes | 346963968.0 | 911335424.0 | 752812789.479452 |  |
| PERF-IO-DISKSPD.json | diskspd_readwrite_4GB_4k_d1_th1_direct | total IO operations | 121044.0 | 317641.0 | 262340.0410958904 |  |
| PERF-IO-DISKSPD.json | diskspd_readwrite_4GB_4k_d1_th1_direct | total bytes | 495796224.0 | 1301057536.0 | 1074544808.328767 |  |
| PERF-IO-DISKSPD.json | diskspd_readwrite_4GB_4k_d1_th1_direct | total iops | 252.17 | 661.74 | 546.5336986301367 |  |
| PERF-IO-DISKSPD.json | diskspd_readwrite_4GB_4k_d1_th1_direct | total latency/operation(P50) | 1.52 | 3.079 | 1.8342054794520555 | milliseconds |
| PERF-IO-DISKSPD.json | diskspd_readwrite_4GB_4k_d1_th1_direct | total latency/operation(P75) | 1.623 | 3.238 | 1.9713767123287669 | milliseconds |
| PERF-IO-DISKSPD.json | diskspd_readwrite_4GB_4k_d1_th1_direct | total latency/operation(P90) | 1.731 | 3.444 | 2.1667602739726026 | milliseconds |
| PERF-IO-DISKSPD.json | diskspd_readwrite_4GB_4k_d1_th1_direct | total latency/operation(P95) | 1.824 | 14.796 | 2.586150684931506 | milliseconds |
| PERF-IO-DISKSPD.json | diskspd_readwrite_4GB_4k_d1_th1_direct | total latency/operation(P99) | 2.3 | 24.359 | 4.202582191780822 | milliseconds |
| PERF-IO-DISKSPD.json | diskspd_readwrite_4GB_4k_d1_th1_direct | total throughput | 0.99 | 2.58 | 2.13527397260274 | MiB/sec |
| PERF-IO-DISKSPD.json | diskspd_readwrite_4GB_4k_d1_th1_direct | write IO operations | 36336.0 | 95147.0 | 78547.85616438356 |  |
| PERF-IO-DISKSPD.json | diskspd_readwrite_4GB_4k_d1_th1_direct | write avg. latency | 1.145 | 2.668 | 1.6596095890410967 | milliseconds |
| PERF-IO-DISKSPD.json | diskspd_readwrite_4GB_4k_d1_th1_direct | write iops | 75.7 | 198.22 | 163.6389041095891 |  |
| PERF-IO-DISKSPD.json | diskspd_readwrite_4GB_4k_d1_th1_direct | write iops stdev | 8.77 | 56.24 | 14.636575342465758 | milliseconds |
| PERF-IO-DISKSPD.json | diskspd_readwrite_4GB_4k_d1_th1_direct | write latency stdev | 0.235 | 8.832 | 0.8268767123287669 | milliseconds |
| PERF-IO-DISKSPD.json | diskspd_readwrite_4GB_4k_d1_th1_direct | write latency/operation(P50) | 1.094 | 2.53 | 1.4996301369863019 | milliseconds |
| PERF-IO-DISKSPD.json | diskspd_readwrite_4GB_4k_d1_th1_direct | write latency/operation(P75) | 1.161 | 2.634 | 1.6327945205479453 | milliseconds |
| PERF-IO-DISKSPD.json | diskspd_readwrite_4GB_4k_d1_th1_direct | write latency/operation(P90) | 1.25 | 3.938 | 2.024191780821918 | milliseconds |
| PERF-IO-DISKSPD.json | diskspd_readwrite_4GB_4k_d1_th1_direct | write latency/operation(P95) | 1.38 | 6.83 | 2.617273972602739 | milliseconds |
| PERF-IO-DISKSPD.json | diskspd_readwrite_4GB_4k_d1_th1_direct | write latency/operation(P99) | 2.122 | 27.795 | 4.640006849315068 | milliseconds |
| PERF-IO-DISKSPD.json | diskspd_readwrite_4GB_4k_d1_th1_direct | write throughput | 0.3 | 0.77 | 0.6389041095890408 | MiB/sec |
| PERF-IO-DISKSPD.json | diskspd_readwrite_4GB_4k_d1_th1_direct | write total bytes | 148832256.0 | 389722112.0 | 321732018.84931507 |  |
| PERF-IO-DISKSPD.json | diskspd_readwrite_4GB_8k_d1_th1_direct | avg. latency | 1.46 | 3.019 | 1.8137465753424663 | milliseconds |
| PERF-IO-DISKSPD.json | diskspd_readwrite_4GB_8k_d1_th1_direct | iops stdev | 4.65 | 76.24 | 21.33349315068494 | milliseconds |
| PERF-IO-DISKSPD.json | diskspd_readwrite_4GB_8k_d1_th1_direct | latency stdev | 0.312 | 2.664 | 0.6250342465753427 | milliseconds |
| PERF-IO-DISKSPD.json | diskspd_readwrite_4GB_8k_d1_th1_direct | read IO operations | 111377.0 | 230305.0 | 188481.1301369863 |  |
| PERF-IO-DISKSPD.json | diskspd_readwrite_4GB_8k_d1_th1_direct | read avg. latency | 1.571 | 3.182 | 1.9327328767123282 | milliseconds |
| PERF-IO-DISKSPD.json | diskspd_readwrite_4GB_8k_d1_th1_direct | read iops | 232.03 | 479.79 | 392.66342465753419 |  |
| PERF-IO-DISKSPD.json | diskspd_readwrite_4GB_8k_d1_th1_direct | read iops stdev | 7.51 | 53.93 | 17.645890410958903 | milliseconds |
| PERF-IO-DISKSPD.json | diskspd_readwrite_4GB_8k_d1_th1_direct | read latency stdev | 0.179 | 3.16 | 0.5221095890410956 | milliseconds |
| PERF-IO-DISKSPD.json | diskspd_readwrite_4GB_8k_d1_th1_direct | read latency/operation(P50) | 1.536 | 3.131 | 1.879657534246575 | milliseconds |
| PERF-IO-DISKSPD.json | diskspd_readwrite_4GB_8k_d1_th1_direct | read latency/operation(P75) | 1.614 | 3.239 | 1.9843972602739722 | milliseconds |
| PERF-IO-DISKSPD.json | diskspd_readwrite_4GB_8k_d1_th1_direct | read latency/operation(P90) | 1.713 | 3.362 | 2.1241095890410959 | milliseconds |
| PERF-IO-DISKSPD.json | diskspd_readwrite_4GB_8k_d1_th1_direct | read latency/operation(P95) | 1.776 | 3.643 | 2.2791780821917815 | milliseconds |
| PERF-IO-DISKSPD.json | diskspd_readwrite_4GB_8k_d1_th1_direct | read latency/operation(P99) | 1.901 | 6.788 | 3.032883561643836 | milliseconds |
| PERF-IO-DISKSPD.json | diskspd_readwrite_4GB_8k_d1_th1_direct | read throughput | 1.81 | 3.75 | 3.067808219178081 | MiB/sec |
| PERF-IO-DISKSPD.json | diskspd_readwrite_4GB_8k_d1_th1_direct | read total bytes | 912400384.0 | 1886658560.0 | 1544037418.0821918 |  |
| PERF-IO-DISKSPD.json | diskspd_readwrite_4GB_8k_d1_th1_direct | total IO operations | 158954.0 | 328643.0 | 269026.3219178082 |  |
| PERF-IO-DISKSPD.json | diskspd_readwrite_4GB_8k_d1_th1_direct | total bytes | 1302151168.0 | 2692243456.0 | 2203863629.150685 |  |
| PERF-IO-DISKSPD.json | diskspd_readwrite_4GB_8k_d1_th1_direct | total iops | 331.15 | 684.65 | 560.4636301369865 |  |
| PERF-IO-DISKSPD.json | diskspd_readwrite_4GB_8k_d1_th1_direct | total latency/operation(P50) | 1.49 | 3.065 | 1.8215068493150673 | milliseconds |
| PERF-IO-DISKSPD.json | diskspd_readwrite_4GB_8k_d1_th1_direct | total latency/operation(P75) | 1.587 | 3.199 | 1.9482191780821919 | milliseconds |
| PERF-IO-DISKSPD.json | diskspd_readwrite_4GB_8k_d1_th1_direct | total latency/operation(P90) | 1.692 | 3.337 | 2.1054041095890407 | milliseconds |
| PERF-IO-DISKSPD.json | diskspd_readwrite_4GB_8k_d1_th1_direct | total latency/operation(P95) | 1.754 | 3.899 | 2.29671917808219 | milliseconds |
| PERF-IO-DISKSPD.json | diskspd_readwrite_4GB_8k_d1_th1_direct | total latency/operation(P99) | 1.877 | 6.477 | 3.422698630136986 | milliseconds |
| PERF-IO-DISKSPD.json | diskspd_readwrite_4GB_8k_d1_th1_direct | total throughput | 2.59 | 5.35 | 4.378493150684932 | MiB/sec |
| PERF-IO-DISKSPD.json | diskspd_readwrite_4GB_8k_d1_th1_direct | write IO operations | 47577.0 | 98338.0 | 80545.19178082192 |  |
| PERF-IO-DISKSPD.json | diskspd_readwrite_4GB_8k_d1_th1_direct | write avg. latency | 1.189 | 2.635 | 1.5352534246575338 | milliseconds |
| PERF-IO-DISKSPD.json | diskspd_readwrite_4GB_8k_d1_th1_direct | write iops | 99.12 | 204.87 | 167.79986301369866 |  |
| PERF-IO-DISKSPD.json | diskspd_readwrite_4GB_8k_d1_th1_direct | write iops stdev | 8.41 | 25.24 | 13.254109589041093 | milliseconds |
| PERF-IO-DISKSPD.json | diskspd_readwrite_4GB_8k_d1_th1_direct | write latency stdev | 0.155 | 3.414 | 0.6262808219178081 | milliseconds |
| PERF-IO-DISKSPD.json | diskspd_readwrite_4GB_8k_d1_th1_direct | write latency/operation(P50) | 1.12 | 2.509 | 1.404773972602739 | milliseconds |
| PERF-IO-DISKSPD.json | diskspd_readwrite_4GB_8k_d1_th1_direct | write latency/operation(P75) | 1.191 | 2.612 | 1.5231164383561645 | milliseconds |
| PERF-IO-DISKSPD.json | diskspd_readwrite_4GB_8k_d1_th1_direct | write latency/operation(P90) | 1.29 | 3.817 | 1.830801369863014 | milliseconds |
| PERF-IO-DISKSPD.json | diskspd_readwrite_4GB_8k_d1_th1_direct | write latency/operation(P95) | 1.396 | 4.765 | 2.325753424657534 | milliseconds |
| PERF-IO-DISKSPD.json | diskspd_readwrite_4GB_8k_d1_th1_direct | write latency/operation(P99) | 1.536 | 8.958 | 3.957013698630138 | milliseconds |
| PERF-IO-DISKSPD.json | diskspd_readwrite_4GB_8k_d1_th1_direct | write throughput | 0.77 | 1.6 | 1.310821917808219 | MiB/sec |
| PERF-IO-DISKSPD.json | diskspd_readwrite_4GB_8k_d1_th1_direct | write total bytes | 389750784.0 | 805584896.0 | 659826211.0684931 |  |
| PERF-IO-DISKSPD.json | diskspd_write_16GB_1024k_d1_th1_direct | avg. latency | 5.135 | 49.074 | 10.535652777777772 | milliseconds |
| PERF-IO-DISKSPD.json | diskspd_write_16GB_1024k_d1_th1_direct | iops stdev | 0.54 | 50.28 | 7.891249999999998 | milliseconds |
| PERF-IO-DISKSPD.json | diskspd_write_16GB_1024k_d1_th1_direct | latency stdev | 0.462 | 22.974 | 2.5845347222222219 | milliseconds |
| PERF-IO-DISKSPD.json | diskspd_write_16GB_1024k_d1_th1_direct | total IO operations | 9781.0 | 93451.0 | 60300.4375 |  |
| PERF-IO-DISKSPD.json | diskspd_write_16GB_1024k_d1_th1_direct | total bytes | 10256121856.0 | 97990475776.0 | 63229591552.0 |  |
| PERF-IO-DISKSPD.json | diskspd_write_16GB_1024k_d1_th1_direct | total iops | 20.38 | 194.69 | 125.62416666666678 |  |
| PERF-IO-DISKSPD.json | diskspd_write_16GB_1024k_d1_th1_direct | total throughput | 20.38 | 194.69 | 125.62416666666678 | MiB/sec |
| PERF-IO-DISKSPD.json | diskspd_write_16GB_1024k_d1_th1_direct | write IO operations | 9781.0 | 93451.0 | 60300.4375 |  |
| PERF-IO-DISKSPD.json | diskspd_write_16GB_1024k_d1_th1_direct | write avg. latency | 5.135 | 49.074 | 10.535652777777774 | milliseconds |
| PERF-IO-DISKSPD.json | diskspd_write_16GB_1024k_d1_th1_direct | write iops | 20.38 | 194.69 | 125.62416666666678 |  |
| PERF-IO-DISKSPD.json | diskspd_write_16GB_1024k_d1_th1_direct | write iops stdev | 0.54 | 50.28 | 7.891249999999999 | milliseconds |
| PERF-IO-DISKSPD.json | diskspd_write_16GB_1024k_d1_th1_direct | write latency stdev | 0.462 | 22.974 | 2.5845347222222219 | milliseconds |
| PERF-IO-DISKSPD.json | diskspd_write_16GB_1024k_d1_th1_direct | write throughput | 20.38 | 194.69 | 125.62416666666678 | MiB/sec |
| PERF-IO-DISKSPD.json | diskspd_write_16GB_1024k_d1_th1_direct | write total bytes | 10256121856.0 | 97990475776.0 | 63229591552.0 |  |
| PERF-IO-DISKSPD.json | diskspd_write_250MB_64k_d16_th32_direct | avg. latency | 164.455 | 328.516 | 166.81582638888865 | milliseconds |
| PERF-IO-DISKSPD.json | diskspd_write_250MB_64k_d16_th32_direct | iops stdev | 0.4 | 1311.14 | 72.38854166666664 | milliseconds |
| PERF-IO-DISKSPD.json | diskspd_write_250MB_64k_d16_th32_direct | latency stdev | 2.791 | 366.491 | 23.69397222222222 | milliseconds |
| PERF-IO-DISKSPD.json | diskspd_write_250MB_64k_d16_th32_direct | total IO operations | 748088.0 | 1494360.0 | 1480070.7291666668 |  |
| PERF-IO-DISKSPD.json | diskspd_write_250MB_64k_d16_th32_direct | total bytes | 49026695168.0 | 97934376960.0 | 96997915306.66667 |  |
| PERF-IO-DISKSPD.json | diskspd_write_250MB_64k_d16_th32_direct | total iops | 1558.48 | 3113.22 | 3083.42673611111 |  |
| PERF-IO-DISKSPD.json | diskspd_write_250MB_64k_d16_th32_direct | total throughput | 97.41 | 194.58 | 192.71486111111114 | MiB/sec |
| PERF-IO-DISKSPD.json | diskspd_write_250MB_64k_d16_th32_direct | write IO operations | 748088.0 | 1494360.0 | 1480070.7291666668 |  |
| PERF-IO-DISKSPD.json | diskspd_write_250MB_64k_d16_th32_direct | write avg. latency | 164.455 | 328.516 | 166.81582638888868 | milliseconds |
| PERF-IO-DISKSPD.json | diskspd_write_250MB_64k_d16_th32_direct | write iops | 1558.48 | 3113.22 | 3083.42673611111 |  |
| PERF-IO-DISKSPD.json | diskspd_write_250MB_64k_d16_th32_direct | write iops stdev | 0.4 | 1311.14 | 72.38854166666664 | milliseconds |
| PERF-IO-DISKSPD.json | diskspd_write_250MB_64k_d16_th32_direct | write latency stdev | 2.791 | 366.491 | 23.69397222222222 | milliseconds |
| PERF-IO-DISKSPD.json | diskspd_write_250MB_64k_d16_th32_direct | write throughput | 97.41 | 194.58 | 192.71486111111114 | MiB/sec |
| PERF-IO-DISKSPD.json | diskspd_write_250MB_64k_d16_th32_direct | write total bytes | 49026695168.0 | 97934376960.0 | 96997915306.66667 |  |
| PERF-IO-DISKSPD.json | diskspd_write_4GB_12k_d1_th1_direct | avg. latency | 1.122 | 3.232 | 1.4856319444444446 | milliseconds |
| PERF-IO-DISKSPD.json | diskspd_write_4GB_12k_d1_th1_direct | iops stdev | 7.57 | 143.94 | 42.522291666666678 | milliseconds |
| PERF-IO-DISKSPD.json | diskspd_write_4GB_12k_d1_th1_direct | latency stdev | 0.178 | 5.744 | 0.5623055555555556 | milliseconds |
| PERF-IO-DISKSPD.json | diskspd_write_4GB_12k_d1_th1_direct | total IO operations | 148467.0 | 427482.0 | 332352.81944444446 |  |
| PERF-IO-DISKSPD.json | diskspd_write_4GB_12k_d1_th1_direct | total bytes | 1824362496.0 | 5252898816.0 | 4083951445.3333337 |  |
| PERF-IO-DISKSPD.json | diskspd_write_4GB_12k_d1_th1_direct | total iops | 309.31 | 890.57 | 692.389791666667 |  |
| PERF-IO-DISKSPD.json | diskspd_write_4GB_12k_d1_th1_direct | total throughput | 3.62 | 10.44 | 8.113888888888889 | MiB/sec |
| PERF-IO-DISKSPD.json | diskspd_write_4GB_12k_d1_th1_direct | write IO operations | 148467.0 | 427482.0 | 332352.81944444446 |  |
| PERF-IO-DISKSPD.json | diskspd_write_4GB_12k_d1_th1_direct | write avg. latency | 1.122 | 3.232 | 1.4856319444444444 | milliseconds |
| PERF-IO-DISKSPD.json | diskspd_write_4GB_12k_d1_th1_direct | write iops | 309.31 | 890.57 | 692.389791666667 |  |
| PERF-IO-DISKSPD.json | diskspd_write_4GB_12k_d1_th1_direct | write iops stdev | 7.57 | 143.94 | 42.522291666666678 | milliseconds |
| PERF-IO-DISKSPD.json | diskspd_write_4GB_12k_d1_th1_direct | write latency stdev | 0.178 | 5.744 | 0.5623055555555556 | milliseconds |
| PERF-IO-DISKSPD.json | diskspd_write_4GB_12k_d1_th1_direct | write throughput | 3.62 | 10.44 | 8.113888888888889 | MiB/sec |
| PERF-IO-DISKSPD.json | diskspd_write_4GB_12k_d1_th1_direct | write total bytes | 1824362496.0 | 5252898816.0 | 4083951445.3333337 |  |
| PERF-IO-DISKSPD.json | diskspd_write_4GB_16k_d1_th1_direct | avg. latency | 1.15 | 2.537 | 1.5075416666666668 | milliseconds |
| PERF-IO-DISKSPD.json | diskspd_write_4GB_16k_d1_th1_direct | iops stdev | 5.41 | 125.66 | 46.99923611111112 | milliseconds |
| PERF-IO-DISKSPD.json | diskspd_write_4GB_16k_d1_th1_direct | latency stdev | 0.158 | 2.965 | 0.6175763888888889 | milliseconds |
| PERF-IO-DISKSPD.json | diskspd_write_4GB_16k_d1_th1_direct | total IO operations | 189107.0 | 417028.0 | 327028.84027777778 |  |
| PERF-IO-DISKSPD.json | diskspd_write_4GB_16k_d1_th1_direct | total bytes | 3098329088.0 | 6832586752.0 | 5358040519.111111 |  |
| PERF-IO-DISKSPD.json | diskspd_write_4GB_16k_d1_th1_direct | total iops | 393.97 | 868.8 | 681.3021527777779 |  |
| PERF-IO-DISKSPD.json | diskspd_write_4GB_16k_d1_th1_direct | total throughput | 6.16 | 13.57 | 10.645555555555554 | MiB/sec |
| PERF-IO-DISKSPD.json | diskspd_write_4GB_16k_d1_th1_direct | write IO operations | 189107.0 | 417028.0 | 327028.84027777778 |  |
| PERF-IO-DISKSPD.json | diskspd_write_4GB_16k_d1_th1_direct | write avg. latency | 1.15 | 2.537 | 1.5075416666666668 | milliseconds |
| PERF-IO-DISKSPD.json | diskspd_write_4GB_16k_d1_th1_direct | write iops | 393.97 | 868.8 | 681.3021527777779 |  |
| PERF-IO-DISKSPD.json | diskspd_write_4GB_16k_d1_th1_direct | write iops stdev | 5.41 | 125.66 | 46.99923611111112 | milliseconds |
| PERF-IO-DISKSPD.json | diskspd_write_4GB_16k_d1_th1_direct | write latency stdev | 0.158 | 2.965 | 0.6175763888888889 | milliseconds |
| PERF-IO-DISKSPD.json | diskspd_write_4GB_16k_d1_th1_direct | write throughput | 6.16 | 13.57 | 10.645555555555554 | MiB/sec |
| PERF-IO-DISKSPD.json | diskspd_write_4GB_16k_d1_th1_direct | write total bytes | 3098329088.0 | 6832586752.0 | 5358040519.111111 |  |
| PERF-IO-DISKSPD.json | diskspd_write_4GB_32k_d1_th1_direct | avg. latency | 1.157 | 2.57 | 1.5400069444444445 | milliseconds |
| PERF-IO-DISKSPD.json | diskspd_write_4GB_32k_d1_th1_direct | iops stdev | 5.67 | 220.33 | 46.3536111111111 | milliseconds |
| PERF-IO-DISKSPD.json | diskspd_write_4GB_32k_d1_th1_direct | latency stdev | 0.187 | 6.75 | 0.6412291666666665 | milliseconds |
| PERF-IO-DISKSPD.json | diskspd_write_4GB_32k_d1_th1_direct | total IO operations | 186669.0 | 414419.0 | 319122.9513888889 |  |
| PERF-IO-DISKSPD.json | diskspd_write_4GB_32k_d1_th1_direct | total bytes | 6116769792.0 | 13579681792.0 | 10457020871.11111 |  |
| PERF-IO-DISKSPD.json | diskspd_write_4GB_32k_d1_th1_direct | total iops | 388.89 | 863.36 | 664.8282638888884 |  |
| PERF-IO-DISKSPD.json | diskspd_write_4GB_32k_d1_th1_direct | total throughput | 12.15 | 26.98 | 20.775763888888883 | MiB/sec |
| PERF-IO-DISKSPD.json | diskspd_write_4GB_32k_d1_th1_direct | write IO operations | 186669.0 | 414419.0 | 319122.9513888889 |  |
| PERF-IO-DISKSPD.json | diskspd_write_4GB_32k_d1_th1_direct | write avg. latency | 1.157 | 2.57 | 1.5400069444444445 | milliseconds |
| PERF-IO-DISKSPD.json | diskspd_write_4GB_32k_d1_th1_direct | write iops | 388.89 | 863.36 | 664.8282638888884 |  |
| PERF-IO-DISKSPD.json | diskspd_write_4GB_32k_d1_th1_direct | write iops stdev | 5.67 | 220.33 | 46.3536111111111 | milliseconds |
| PERF-IO-DISKSPD.json | diskspd_write_4GB_32k_d1_th1_direct | write latency stdev | 0.187 | 6.75 | 0.6412291666666665 | milliseconds |
| PERF-IO-DISKSPD.json | diskspd_write_4GB_32k_d1_th1_direct | write throughput | 12.15 | 26.98 | 20.775763888888883 | MiB/sec |
| PERF-IO-DISKSPD.json | diskspd_write_4GB_32k_d1_th1_direct | write total bytes | 6116769792.0 | 13579681792.0 | 10457020871.11111 |  |
| PERF-IO-DISKSPD.json | diskspd_write_4GB_4k_d1_th1_direct | avg. latency | 1.142 | 2.703 | 1.5762397260273978 | milliseconds |
| PERF-IO-DISKSPD.json | diskspd_write_4GB_4k_d1_th1_direct | iops stdev | 4.35 | 174.78 | 42.38287671232878 | milliseconds |
| PERF-IO-DISKSPD.json | diskspd_write_4GB_4k_d1_th1_direct | latency stdev | 0.214 | 4.753 | 0.5975410958904107 | milliseconds |
| PERF-IO-DISKSPD.json | diskspd_write_4GB_4k_d1_th1_direct | total IO operations | 177503.0 | 419972.0 | 317956.12328767127 |  |
| PERF-IO-DISKSPD.json | diskspd_write_4GB_4k_d1_th1_direct | total bytes | 727052288.0 | 1720205312.0 | 1302348280.9863015 |  |
| PERF-IO-DISKSPD.json | diskspd_write_4GB_4k_d1_th1_direct | total iops | 369.79 | 874.93 | 662.3978767123286 |  |
| PERF-IO-DISKSPD.json | diskspd_write_4GB_4k_d1_th1_direct | total throughput | 1.44 | 3.42 | 2.587328767123288 | MiB/sec |
| PERF-IO-DISKSPD.json | diskspd_write_4GB_4k_d1_th1_direct | write IO operations | 177503.0 | 419972.0 | 317956.12328767127 |  |
| PERF-IO-DISKSPD.json | diskspd_write_4GB_4k_d1_th1_direct | write avg. latency | 1.142 | 2.703 | 1.5762397260273978 | milliseconds |
| PERF-IO-DISKSPD.json | diskspd_write_4GB_4k_d1_th1_direct | write iops | 369.79 | 874.93 | 662.3978767123286 |  |
| PERF-IO-DISKSPD.json | diskspd_write_4GB_4k_d1_th1_direct | write iops stdev | 4.35 | 174.78 | 42.38287671232877 | milliseconds |
| PERF-IO-DISKSPD.json | diskspd_write_4GB_4k_d1_th1_direct | write latency stdev | 0.214 | 4.753 | 0.5975410958904107 | milliseconds |
| PERF-IO-DISKSPD.json | diskspd_write_4GB_4k_d1_th1_direct | write throughput | 1.44 | 3.42 | 2.587328767123288 | MiB/sec |
| PERF-IO-DISKSPD.json | diskspd_write_4GB_4k_d1_th1_direct | write total bytes | 727052288.0 | 1720205312.0 | 1302348280.9863015 |  |
| PERF-IO-DISKSPD.json | diskspd_write_4GB_8k_d1_th1_direct | avg. latency | 1.113 | 2.53 | 1.481986301369863 | milliseconds |
| PERF-IO-DISKSPD.json | diskspd_write_4GB_8k_d1_th1_direct | iops stdev | 7.56 | 240.94 | 48.4877397260274 | milliseconds |
| PERF-IO-DISKSPD.json | diskspd_write_4GB_8k_d1_th1_direct | latency stdev | 0.178 | 6.31 | 0.6880890410958906 | milliseconds |
| PERF-IO-DISKSPD.json | diskspd_write_4GB_8k_d1_th1_direct | total IO operations | 189635.0 | 430937.0 | 332019.1780821918 |  |
| PERF-IO-DISKSPD.json | diskspd_write_4GB_8k_d1_th1_direct | total bytes | 1553489920.0 | 3530235904.0 | 2719901106.849315 |  |
| PERF-IO-DISKSPD.json | diskspd_write_4GB_8k_d1_th1_direct | total iops | 395.07 | 897.78 | 691.6960273972602 |  |
| PERF-IO-DISKSPD.json | diskspd_write_4GB_8k_d1_th1_direct | total throughput | 3.09 | 7.01 | 5.403904109589042 | MiB/sec |
| PERF-IO-DISKSPD.json | diskspd_write_4GB_8k_d1_th1_direct | write IO operations | 189635.0 | 430937.0 | 332019.1780821918 |  |
| PERF-IO-DISKSPD.json | diskspd_write_4GB_8k_d1_th1_direct | write avg. latency | 1.113 | 2.53 | 1.481986301369863 | milliseconds |
| PERF-IO-DISKSPD.json | diskspd_write_4GB_8k_d1_th1_direct | write iops | 395.07 | 897.78 | 691.6960273972602 |  |
| PERF-IO-DISKSPD.json | diskspd_write_4GB_8k_d1_th1_direct | write iops stdev | 7.56 | 240.94 | 48.4877397260274 | milliseconds |
| PERF-IO-DISKSPD.json | diskspd_write_4GB_8k_d1_th1_direct | write latency stdev | 0.178 | 6.31 | 0.6880890410958906 | milliseconds |
| PERF-IO-DISKSPD.json | diskspd_write_4GB_8k_d1_th1_direct | write throughput | 3.09 | 7.01 | 5.403904109589042 | MiB/sec |
| PERF-IO-DISKSPD.json | diskspd_write_4GB_8k_d1_th1_direct | write total bytes | 1553489920.0 | 3530235904.0 | 2719901106.849315 |  |