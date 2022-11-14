# Graph500 Workload Metrics
The following document illustrates the type of results that are emitted by the Graph500 workload and captured by the
Virtual Client for net impact analysis.



### Workload-Specific Metrics
The following metrics are emitted by the Graph500 workload itself.

| Execution Profile           | Test Name       | Metric Name                       | Example Value (avg) | Unit  |
|-----------------------------|-----------------|-----------------------------------|---------------------|-------|
|PERF-GRAPH500 (linux-arm64)  |Graph500         |SCALE	                            |10	                   
|PERF-GRAPH500 (linux-arm64)  |Graph500         |edgefactor                     	|6	
|PERF-GRAPH500 (linux-arm64)  |Graph500	        |NBFS	                            |64	
|PERF-GRAPH500 (linux-arm64)  |Graph500	        |graph_generation	                |0.00201917	
|PERF-GRAPH500 (linux-arm64)  |Graph500	        |num_mpi_processes	                |1	
|PERF-GRAPH500 (linux-arm64)  |Graph500	        |construction_time	                |0.000249147	      |seconds
|PERF-GRAPH500 (linux-arm64)  |Graph500	        |bfs  min_time	                    |0.000108719	|seconds
|PERF-GRAPH500 (linux-arm64)  |Graph500	        |bfs  firstquartile_time	        |0.000109196	|seconds
|PERF-GRAPH500 (linux-arm64)  |Graph500	        |bfs  median_time	                |0.000109792	|seconds
|PERF-GRAPH500 (linux-arm64)  |Graph500	        |bfs  thirdquartile_time	        |0.000111103	|seconds
|PERF-GRAPH500 (linux-arm64)  |Graph500	        |bfs  max_time	                    |0.000171185	|seconds
|PERF-GRAPH500 (linux-arm64)  |Graph500	        |bfs  mean_time	                    |0.00011196	    |seconds
|PERF-GRAPH500 (linux-arm64)  |Graph500	        |bfs  stddev_time	                |0.00000815005	|seconds
|PERF-GRAPH500 (linux-arm64)  |Graph500	        |sssp min_time	                    |0.000368357	|seconds
|PERF-GRAPH500 (linux-arm64)  |Graph500	        |sssp firstquartile_time	        |0.000386834	|seconds
|PERF-GRAPH500 (linux-arm64)  |Graph500	        |sssp median_time	                |0.000398993	|seconds
|PERF-GRAPH500 (linux-arm64)  |Graph500	        |sssp thirdquartile_time	        |0.000412464	|seconds
|PERF-GRAPH500 (linux-arm64)  |Graph500	        |sssp max_time	                    |0.0004673	    |seconds
|PERF-GRAPH500 (linux-arm64)  |Graph500	        |sssp mean_time	                    |0.000401806	|seconds
|PERF-GRAPH500 (linux-arm64)  |Graph500	        |sssp stddev_time	                |0.0000209045	|seconds
|PERF-GRAPH500 (linux-arm64)  |Graph500	        |min_nedge	                        |6087	
|PERF-GRAPH500 (linux-arm64)  |Graph500	        |firstquartile_nedge	            |6087	
|PERF-GRAPH500 (linux-arm64)  |Graph500	        |median_nedge	                    |6087	
|PERF-GRAPH500 (linux-arm64)  |Graph500	        |thirdquartile_nedge	            |6087	
|PERF-GRAPH500 (linux-arm64)  |Graph500	        |max_nedge	                        |6087	
|PERF-GRAPH500 (linux-arm64)  |Graph500	        |mean_nedge	                        |6087	
|PERF-GRAPH500 (linux-arm64)  |Graph500         |stddev_nedge	                    |0	
|PERF-GRAPH500 (linux-arm64)  |Graph500	        |bfs  min_TEPS	                    |35558100	|TEPS
|PERF-GRAPH500 (linux-arm64)  |Graph500	        |bfs  firstquartile_TEPS	        |54787000	|TEPS
|PERF-GRAPH500 (linux-arm64)  |Graph500	        |bfs  median_TEPS	                |55441300	|TEPS
|PERF-GRAPH500 (linux-arm64)  |Graph500	        |bfs  thirdquartile_TEPS	        |55743900	|TEPS
|PERF-GRAPH500 (linux-arm64)  |Graph500	        |bfs  max_TEPS	                    |55988400	|TEPS
|PERF-GRAPH500 (linux-arm64)  |Graph500	        |bfs  harmonic_mean_TEPS	        |54367700	|TEPS
|PERF-GRAPH500 (linux-arm64)  |Graph500	        |bfs  harmonic_stddev_TEPS	        |498619	    |TEPS
|PERF-GRAPH500 (linux-arm64)  |Graph500	        |sssp min_TEPS	                    |13025900   |TEPS
|PERF-GRAPH500 (linux-arm64)  |Graph500	        |sssp firstquartile_TEPS	        |14757600	|TEPS
|PERF-GRAPH500 (linux-arm64)  |Graph500	        |sssp median_TEPS	                |15255900	|TEPS
|PERF-GRAPH500 (linux-arm64)  |Graph500	        |sssp thirdquartile_TEPS	        |15735400	|TEPS
|PERF-GRAPH500 (linux-arm64)  |Graph500	        |sssp max_TEPS	                    |16524700	|TEPS
|PERF-GRAPH500 (linux-arm64)  |Graph500	        |sssp harmonic_mean_TEPS	        |15149100	|TEPS
|PERF-GRAPH500 (linux-arm64)  |Graph500	        |sssp harmonic_stddev_TEPS	        |99297.7	|TEPS
|PERF-GRAPH500 (linux-arm64)  |Graph500	        |bfs  min_validate	                |0.000295639	
|PERF-GRAPH500 (linux-arm64)  |Graph500	        |bfs  firstquartile_validate	    |0.0002985	
|PERF-GRAPH500 (linux-arm64)  |Graph500	        |bfs  median_validate	            |0.000300169	
|PERF-GRAPH500 (linux-arm64)  |Graph500	        |bfs  thirdquartile_validate	    |0.000301957	
|PERF-GRAPH500 (linux-arm64)  |Graph500	        |bfs  max_validate	                |0.000398874	
|PERF-GRAPH500 (linux-arm64)  |Graph500	        |bfs  mean_validate	                |0.000303879	
|PERF-GRAPH500 (linux-arm64)  |Graph500	        |bfs  stddev_validate	            |0.0000149202	
|PERF-GRAPH500 (linux-arm64)  |Graph500	        |sssp min_validate	                |0.00019145	
|PERF-GRAPH500 (linux-arm64)  |Graph500	        |sssp firstquartile_validate	    |0.000191927	
|PERF-GRAPH500 (linux-arm64)  |Graph500	        |sssp median_validate	            |0.000192165	
|PERF-GRAPH500 (linux-arm64)  |Graph500	        |sssp thirdquartile_validate	    |0.000193	
|PERF-GRAPH500 (linux-arm64)  |Graph500	        |sssp max_validate	                |0.000205278	
|PERF-GRAPH500 (linux-arm64)  |Graph500	        |sssp mean_validate	                |0.000193551	
|PERF-GRAPH500 (linux-arm64)  |Graph500	        |sssp stddev_validate	            |0.00000318597	
|PERF-GRAPH500 (linux-x64)  |Graph500           |SCALE	                            |5	                   
|PERF-GRAPH500 (linux-x64)  |Graph500           |edgefactor                     	|4	
|PERF-GRAPH500 (linux-x64)  |Graph500	        |NBFS	                            |64	
|PERF-GRAPH500 (linux-x64)  |Graph500	        |graph_generation	                |0.0000905991
|PERF-GRAPH500 (linux-x64)  |Graph500	        |num_mpi_processes	                |1	
|PERF-GRAPH500 (linux-x64)  |Graph500	        |construction_time	                |0.0000600815	      |seconds
|PERF-GRAPH500 (linux-x64)  |Graph500	        |bfs  min_time	                    |0.0000178814	|seconds
|PERF-GRAPH500 (linux-x64)  |Graph500	        |bfs  firstquartile_time	        |0.0000337362	|seconds
|PERF-GRAPH500 (linux-x64)  |Graph500	        |bfs  median_time	                |0.0000362396	|seconds
|PERF-GRAPH500 (linux-x64)  |Graph500	        |bfs  thirdquartile_time	        |0.0000406504	|seconds
|PERF-GRAPH500 (linux-x64)  |Graph500	        |bfs  max_time	                    |0.0000998974	|seconds
|PERF-GRAPH500 (linux-x64)  |Graph500	        |bfs  mean_time	                    |0.0000393242	    |seconds
|PERF-GRAPH500 (linux-x64)  |Graph500	        |bfs  stddev_time	                |0.0000135865	|seconds
|PERF-GRAPH500 (linux-x64)  |Graph500	        |sssp min_time	                    |0.0000305176	|seconds
|PERF-GRAPH500 (linux-x64)  |Graph500	        |sssp firstquartile_time	        |0.00033915	|seconds
|PERF-GRAPH500 (linux-x64)  |Graph500	        |sssp median_time	                |0.000430703	|seconds
|PERF-GRAPH500 (linux-x64)  |Graph500	        |sssp thirdquartile_time	        |0.000537276	|seconds
|PERF-GRAPH500 (linux-x64)  |Graph500	        |sssp max_time	                    |0.00317717	    |seconds
|PERF-GRAPH500 (linux-x64)  |Graph500	        |sssp mean_time	                    |0.000514425	|seconds
|PERF-GRAPH500 (linux-x64)  |Graph500	        |sssp stddev_time	                |0.000489358	|seconds
|PERF-GRAPH500 (linux-x64)  |Graph500	        |min_nedge	                        |0	
|PERF-GRAPH500 (linux-x64)  |Graph500	        |firstquartile_nedge	            |119	
|PERF-GRAPH500 (linux-x64)  |Graph500	        |median_nedge	                    |119	
|PERF-GRAPH500 (linux-x64)  |Graph500	        |thirdquartile_nedge	            |119	
|PERF-GRAPH500 (linux-x64)  |Graph500	        |max_nedge	                        |119	
|PERF-GRAPH500 (linux-x64)  |Graph500	        |mean_nedge	                        |104.125	
|PERF-GRAPH500 (linux-x64)  |Graph500           |stddev_nedge	                    |39.666666667	
|PERF-GRAPH500 (linux-x64)  |Graph500	        |bfs  min_TEPS	                    |0      	|TEPS
|PERF-GRAPH500 (linux-x64)  |Graph500	        |bfs  firstquartile_TEPS	        |54787000	|TEPS
|PERF-GRAPH500 (linux-x64)  |Graph500	        |bfs  median_TEPS	                |55441300	|TEPS
|PERF-GRAPH500 (linux-x64)  |Graph500	        |bfs  thirdquartile_TEPS	        |55743900	|TEPS
|PERF-GRAPH500 (linux-x64)  |Graph500	        |bfs  max_TEPS	                    |55988400	|TEPS
|PERF-GRAPH500 (linux-x64)  |Graph500	        |bfs  harmonic_mean_TEPS	        |54367700	|TEPS
|PERF-GRAPH500 (linux-x64)  |Graph500	        |bfs  harmonic_stddev_TEPS	        |498619	    |TEPS
|PERF-GRAPH500 (linux-x64)  |Graph500	        |sssp min_TEPS	                    |13025900   |TEPS
|PERF-GRAPH500 (linux-x64)  |Graph500	        |sssp firstquartile_TEPS	        |14757600	|TEPS
|PERF-GRAPH500 (linux-x64)  |Graph500	        |sssp median_TEPS	                |15255900	|TEPS
|PERF-GRAPH500 (linux-x64)  |Graph500	        |sssp thirdquartile_TEPS	        |15735400	|TEPS
|PERF-GRAPH500 (linux-x64)  |Graph500	        |sssp max_TEPS	                    |16524700	|TEPS
|PERF-GRAPH500 (linux-x64)  |Graph500	        |sssp harmonic_mean_TEPS	        |15149100	|TEPS
|PERF-GRAPH500 (linux-x64)  |Graph500	        |sssp harmonic_stddev_TEPS	        |99297.7	|TEPS
|PERF-GRAPH500 (linux-x64)  |Graph500	        |bfs  min_validate	                |0.000295639	
|PERF-GRAPH500 (linux-x64)  |Graph500	        |bfs  firstquartile_validate	    |0.0002985	
|PERF-GRAPH500 (linux-x64)  |Graph500	        |bfs  median_validate	            |0.000300169	
|PERF-GRAPH500 (linux-x64)  |Graph500	        |bfs  thirdquartile_validate	    |0.000301957	
|PERF-GRAPH500 (linux-x64)  |Graph500	        |bfs  max_validate	                |0.000398874	
|PERF-GRAPH500 (linux-x64)  |Graph500	        |bfs  mean_validate	                |0.000303879	
|PERF-GRAPH500 (linux-x64)  |Graph500	        |bfs  stddev_validate	            |0.0000149202	
|PERF-GRAPH500 (linux-x64)  |Graph500	        |sssp min_validate	                |0.00019145	
|PERF-GRAPH500 (linux-x64)  |Graph500	        |sssp firstquartile_validate	    |0.000191927	
|PERF-GRAPH500 (linux-x64)  |Graph500	        |sssp median_validate	            |0.000192165	
|PERF-GRAPH500 (linux-x64)  |Graph500	        |sssp thirdquartile_validate	    |0.000193	
|PERF-GRAPH500 (linux-x64)  |Graph500	        |sssp max_validate	                |0.000205278	
|PERF-GRAPH500 (linux-x64)  |Graph500	        |sssp mean_validate	                |0.000193551	
|PERF-GRAPH500 (linux-x64)  |Graph500	        |sssp stddev_validate	            |0.00000318597	

