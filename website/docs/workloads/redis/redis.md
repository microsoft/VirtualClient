# Redis

Redis is an open source (BSD licensed), in-memory data structure store used as a database, 
cache, message broker, and streaming engine. Redis works with an in-memory dataset. It is 
a client-server model workload in which Redis acts as server. There are different tools that acts are clients.
Two of the widely used tools are onboarded into Virtual Client. They are
1. [Memtier Benchmarking Tool](https://redis.com/blog/memtier_benchmark-a-high-throughput-benchmarking-tool-for-redis-memcached/)
2. [Redis Benchmarking Tool](https://redis.io/docs/reference/optimization/benchmarks/)

* [Official Redis Documentation](https://redis.io/docs/about/)
* [Redis Github Repo](https://github.com/redis/redis)

### What is Being Tested?

#### 1. Memtier Benchmarking Tool :
This tool can be used to generate various traffic patterns against Redis instances.
It provides a robust set of customization and reporting capabilities all wrapped into a convenient 
and easy-to-use command-line interface. It performs GET and SET operations on to the Redis Server Instances
and gives percentile latency distribution and Throughput.

#### 2.Redis Benchmarking Tool:
Redis includes the redis-benchmark utility that simulates running commands done by N clients 
while at the same time sending M total queries. The utility provides a default set of tests,
or you can supply a custom set of tests.Each of this tests generate load against the server and 
gives percentile latency distribution and Throughput.

### Supported Platforms

* Linux x64
* Linux arm64

### Supported Distros

* Ubuntu
* Debian
* CentOS8
* RHEL8
