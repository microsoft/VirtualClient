# PostgreSQL
PostgreSQL is a powerful, open source object-relational database system that uses and extends the SQL language combined with many features that safely store and scale the most complicated data workloads.

* [Official PostgreSQL Documentation](https://www.postgresql.org/about/)

The following is the widely used tools for benchmarking performance of a PostgreSQL server include:
 [HammerDB Tool](https://www.hammerdb.com/docs/index.html)

## What is Being Measured?
HammerDB is used to generate various traffic patterns against Redis instances. These toolsets performs creation of Database and perform transactions against
the PostgreSQL server and provides NOPM (number of orders per minute), and TPM (transactions per minute).

## Workload Metrics
The following metrics are examples of those captured by the Virtual Client when running the HammerDB tool against a
PostgreSQL server.

| Metric Name  | Value  |
|--------------|----------------|
| Number Of Operations Per Minute|	12855 |
|Transactions Per Minute	| 29441|
 