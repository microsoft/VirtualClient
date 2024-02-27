# Graph500
Graph500 3.0.0 is an open-source data-intensive workload. The intent of benchmark problems (“Search” and “Shortest-Path”) is to 
develop a compact application that has multiple analysis techniques (multiple kernels) accessing a single data structure representing
a weighted, undirected graph. In addition to a kernel to construct the graph from the input tuple list, there are two additional computational 
kernels to operate on the graph.

This toolset was compiled from the official website and modified so that it is easier to integrate into Virtual Client.

* [Graph500 GitHub](https://github.com/Graph500/graph500)
* [Graph500 Documentation](https://graph500.org/?page_id=12)
* [Graph500 Reference code](https://graph500.org/?page_id=47)

## What is Being Measured?
Graph500 3.0.0 benchmark includes a scalable data generator which produces edge tuples containing the start vertex and end vertex for each edge.
The first kernel constructs an undirected graph in a format usable by all subsequent kernels. No subsequent modifications are permitted to 
benefit specific kernels. The second kernel performs a breadth-first search of the graph. The third kernel performs multiple single-source 
shortest path computations on the graph. Performance information is collected for each of timed kernels.

## Workload Metrics
The following metrics are examples of those captured by the Virtual Client when running the Graph500 workload.

| Metric Name | Example Value (min) | Example Value (max) | Example Value (avg) | Unit |
|-------------|---------------------|---------------------|---------------------|------|
| NBFS | 64.0 | 64.0 | 64.0 |  |
| SCALE | 10.0 | 10.0 | 10.0 |  |
| bfs  firstquartile_TEPS | 15761000.0 | 67038700.0 | 56845098.813881169 | TEPS |
| bfs  firstquartile_time | 0.000227928 | 0.000816584 | 0.0002837979744698972 | seconds |
| bfs  firstquartile_validate | 0.000623822 | 0.00194085 | 0.0007578336428823721 |  |
| bfs  harmonic_mean_TEPS | 14672800.0 | 67092300.0 | 57506177.04407339 | TEPS |
| bfs  harmonic_stddev_TEPS | 22300.4 | 16672800.0 | 434906.75255222557 | TEPS |
| bfs  max_TEPS | 28098700.0 | 82860500.0 | 63389412.83665595 | TEPS |
| bfs  max_time | 0.000244141 | 0.035387 | 0.00043510345943020097 | seconds |
| bfs  max_validate | 0.000693321 | 0.047008 | 0.001041856006748564 |  |
| bfs  mean_time | 0.00024204 | 0.00110674 | 0.0002955602169665692 | seconds |
| bfs  mean_validate | 0.000684407 | 0.00187422 | 0.0007817111418189024 |  |
| bfs  median_TEPS | 19440900.0 | 67137800.0 | 57980723.75420894 | TEPS |
| bfs  median_time | 0.000241876 | 0.000835299 | 0.0002921084375797816 | seconds |
| bfs  median_validate | 0.000674605 | 0.00196362 | 0.0007719406334723332 |  |
| bfs  min_TEPS | 458897.0 | 66514900.0 | 48857022.214175458 | TEPS |
| bfs  min_time | 0.00019598 | 0.000577927 | 0.0002590177863707418 | seconds |
| bfs  min_validate | 0.000576735 | 0.0015285 | 0.0007192900956972653 |  |
| bfs  stddev_time | 6.44066e-7 | 0.00475421 | 0.000027982194087248686 | seconds |
| bfs  stddev_validate | 0.00000336358 | 0.00576562 | 0.000052281602518571059 |  |
| bfs  thirdquartile_TEPS | 19886500.0 | 71246100.0 | 59167580.126158859 | TEPS |
| bfs  thirdquartile_time | 0.000242233 | 0.00103033 | 0.0003012372028236916 | seconds |
| bfs  thirdquartile_validate | 0.000685215 | 0.00210428 | 0.0007918067777274781 |  |
| construction_time | 0.000588655 | 0.0252874 | 0.0007551961154509821 | seconds |
| edgefactor | 16.0 | 16.0 | 16.0 |  |
| firstquartile_nedge | 16239.0 | 16239.0 | 16239.0 |  |
| graph_generation | 0.00362253 | 0.0378437 | 0.00531108784011771 |  |
| max_nedge | 16239.0 | 16239.0 | 16239.0 |  |
| mean_nedge | 16239.0 | 16239.0 | 16239.0 |  |
| median_nedge | 16239.0 | 16239.0 | 16239.0 |  |
| min_nedge | 16239.0 | 16239.0 | 16239.0 |  |
| num_mpi_processes | 1.0 | 1.0 | 1.0 |  |
| sssp firstquartile_TEPS | 7103440.0 | 28303100.0 | 24138339.69064667 | TEPS |
| sssp firstquartile_time | 0.000540376 | 0.00134814 | 0.0006448348231073277 | seconds |
| sssp firstquartile_validate | 0.000415564 | 0.00120902 | 0.0005190775895766969 |  |
| sssp harmonic_mean_TEPS | 6574410.0 | 29088900.0 | 24924518.398338397 | TEPS |
| sssp harmonic_stddev_TEPS | 50786.4 | 6888650.0 | 219380.63832570378 | TEPS |
| sssp max_TEPS | 12752500.0 | 33079800.0 | 27849111.3531797 | TEPS |
| sssp max_time | 0.000631809 | 0.032387 | 0.000936351942484911 | seconds |
| sssp max_validate | 0.000469446 | 0.0316949 | 0.0007093147225431522 |  |
| sssp mean_time | 0.000558253 | 0.00247003 | 0.0006791608308789002 | seconds |
| sssp mean_validate | 0.000464756 | 0.00173109 | 0.0005354811297832183 |  |
| sssp median_TEPS | 11287900.0 | 29421700.0 | 25267604.791714319 | TEPS |
| sssp median_time | 0.000551939 | 0.00143862 | 0.0006679553658494311 | seconds |
| sssp median_validate | 0.000457287 | 0.00122118 | 0.0005292734985867806 |  |
| sssp min_TEPS | 501405.0 | 25702400.0 | 20665139.949527839 | TEPS |
| sssp min_time | 0.000490904 | 0.00127339 | 0.0005974515020459081 | seconds |
| sssp min_validate | 0.000386953 | 0.00117636 | 0.0004918033350196396 |  |
| sssp stddev_time | 0.0000224343 | 0.00482533 | 0.00006000765749555588 | seconds |
| sssp stddev_validate | 0.00000136926 | 0.0052025 | 0.00003556998883639069 |  |
| sssp thirdquartile_TEPS | 12045500.0 | 30051300.0 | 26064637.438403917 | TEPS |
| sssp thirdquartile_time | 0.000573754 | 0.00228608 | 0.0007020952992005033 | seconds |
| sssp thirdquartile_validate | 0.000464916 | 0.00133038 | 0.0005423333166048377 |  |
| stddev_nedge | 0.0 | 0.0 | 0.0 |  |
| thirdquartile_nedge | 16239.0 | 16239.0 | 16239.0 |  |
