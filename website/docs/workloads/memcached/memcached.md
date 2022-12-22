# Memcached

Memcached is an open source (BSD licensed), high-performance, distributed memory object caching system. Memcached is an in-memory key-value store for small arbitrary data (strings, objects) from results of database calls, API calls, or page rendering. Memcached works with an in-memory dataset. It is a client-server model workload in which Memcached acts as server. There are different tools that acts are clients.
One of the widely used tool is onboarded into Virtual Client. It is
1. [Memtier Benchmarking Tool](https://redis.com/blog/memtier_benchmark-a-high-throughput-benchmarking-tool-for-redis-memcached/)

* [Official Memcached Documentation](https://memcached.org/about)
* [Memcached Github Repo](https://github.com/memcached/memcached)

### What is Being Tested?

#### Memtier Benchmarking Tool :
This tool can be used to generate various traffic patterns against Memcached instances.
It provides a robust set of customization and reporting capabilities all wrapped into a convenient 
and easy-to-use command-line interface. It performs GET and SET operations on to the Memcached Server Instances and gives percentile latency distribution and throughput.

### Supported Platforms

* Linux x64
* Linux arm64

### Supported Distros

* Ubuntu
* Debian
* CentOS8
* RHEL8
* Mariner
