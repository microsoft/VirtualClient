# LAPACK Workload Metrics
The following document illustrates the type of results that are emitted by the LAPACK workload and captured by the
Virtual Client for net impact analysis.

### System Metrics
* [Performance Counters](./PerformanceCounterMetrics.md)
* [Power/Temperature Measurements](./PowerMetrics.md)

### Workload-Specific Metrics
The following metrics are emitted by the LAPACK workload itself.

| Execution Profile           | Test Name      | Metric Name                       | Example Value (avg) | Unit  |
|-----------------------------|----------------|-----------------------------------|---------------------|-------|
|PERF-CPU-LAPACK (win-x64)    | LAPACK         |compute_time_LIN_Single_Precision  | 4.31                |seconds| 
|PERF-CPU-LAPACK (win-x64)    | LAPACK	       |compute_time_LIN_Double_Precision  | 4.47	             |seconds|
|PERF-CPU-LAPACK (win-x64)    | LAPACK	       |compute_time_LIN_Complex           | 21.42	             |seconds|
|PERF-CPU-LAPACK (win-x64)    | LAPACK	       |compute_time_LIN_Complex_Double    | 34.64	             |seconds|
|PERF-CPU-LAPACK (win-x64)    | LAPACK	       |compute_time_EIG_Single_Precision  | 6.72	             |seconds|
|PERF-CPU-LAPACK (win-x64)    | LAPACK	       |compute_time_EIG_Double_Precision  | 8.6499999999999986  |seconds|
|PERF-CPU-LAPACK (win-x64)    | LAPACK	       |compute_time_EIG_Complex	       | 15.889999999999999  |seconds|
|PERF-CPU-LAPACK (win-x64)    | LAPACK	       |compute_time_EIG_Complex_Double    | 24.129999999999995  |seconds|
|PERF-CPU-LAPACK (linux-x64)  | LAPACK	       |compute_time_LIN_Single_Precision  |4.33                 |seconds|
|PERF-CPU-LAPACK (linux-x64)  | LAPACK	       |compute_time_LIN_Double_Precision  |4.4                  |seconds|
|PERF-CPU-LAPACK (linux-x64)  | LAPACK	       |compute_time_LIN_Complex           |11.26                |seconds|
|PERF-CPU-LAPACK (linux-x64)  | LAPACK	       |compute_time_LIN_Complex_Double    |12.13                |seconds|
|PERF-CPU-LAPACK (linux-x64)  | LAPACK	       |compute_time_EIG_Single_Precision  |6.75                 |seconds|
|PERF-CPU-LAPACK (linux-x64)  | LAPACK	       |compute_time_EIG_Double_Precision  |8.6699999999999964   |seconds|
|PERF-CPU-LAPACK (linux-x64)  | LAPACK	       |compute_time_EIG_Complex           |11.689999999999998   |seconds|
|PERF-CPU-LAPACK (linux-x64)  |LAPACK	       |compute_time_EIG_Complex_Double	   |14.149999999999997	 |seconds|
|PERF-CPU-LAPACK (win-arm64)  |LAPACK	       |compute_time_LIN_Single_Precision  |4.92	             |seconds|
|PERF-CPU-LAPACK (win-arm64)  |LAPACK	       |compute_time_LIN_Double_Precision  |4.95	             |seconds|
|PERF-CPU-LAPACK (win-arm64)  |LAPACK	       |compute_time_LIN_Complex	        |20.78	              |seconds|
|PERF-CPU-LAPACK (win-arm64)  |LAPACK	       |compute_time_LIN_Complex_Double 	|31.91	            |seconds|
|PERF-CPU-LAPACK (win-arm64)  |LAPACK	       |compute_time_EIG_Single_Precision	|7.8299999999999974	    |seconds|
|PERF-CPU-LAPACK (win-arm64)  |LAPACK	       |compute_time_EIG_Double_Precision	|9.8099999999999987	    |seconds|
|PERF-CPU-LAPACK (win-arm64)  |LAPACK	       |compute_time_EIG_Complex_Double	    |24.400000000000006	    |seconds|
|PERF-CPU-LAPACK (win-arm64)  |LAPACK	       |compute_time_EIG_Complex	        |16.800000000000004	    |seconds|







