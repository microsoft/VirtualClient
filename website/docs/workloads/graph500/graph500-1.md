# Graph500 Workload
Graph500 3.0.0 is an open-source data-intensive workload. The intent of benchmark problems (“Search” and “Shortest-Path”) is to 
develop a compact application that has multiple analysis techniques (multiple kernels) accessing a single data structure representing
a weighted, undirected graph. In addition to a kernel to construct the graph from the input tuple list, there are two additional computational 
kernels to operate on the graph.

This toolset was compiled from the official website and modified so that it is easier to integrate into VirtualClient.

* [Graph500 GitHub](https://github.com/Graph500/graph500)
* [Graph500 Documentation](https://graph500.org/?page_id=12)
* [Graph500 Reference code](https://graph500.org/?page_id=47)

-----------------------------------------------------------------------

### What is Being Tested?
Graph500 3.0.0 benchmark includes a scalable data generator which produces edge tuples containing the start vertex and end vertex for each edge.
The first kernel constructs an undirected graph in a format usable by all subsequent kernels. No subsequent modifications are permitted to 
benefit specific kernels. The second kernel performs a breadth-first search of the graph. The third kernel performs multiple single-source 
shortest path computations on the graph. Performance information is collected for each of timed kernels.

-----------------------------------------------------------------------

### Supported Platforms
* Linux x64
* Linux arm64
