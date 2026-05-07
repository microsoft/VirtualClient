# PostgreSQL
PostgreSQL is a powerful, open source object-relational database system that uses and extends the SQL language combined with many features that safely store and scale the most complicated data workloads.

* [Official PostgreSQL Documentation](https://www.postgresql.org/about/)

The following is the widely used tools for benchmarking performance of a PostgreSQL server include:
 [HammerDB Tool](https://www.hammerdb.com/docs/index.html)

## What is Being Measured?
HammerDB is used to generate various traffic patterns against PostgreSQL instances, supporting both transactional (OLTP) and analytical (OLAP) workloads:

### TPCC Workload (Online Transaction Processing)
The TPCC benchmark simulates a complete computing environment where a population of terminal operators executes transactions against a database. It represents typical business application scenarios with:
- **New Order transactions**: Processing customer orders
- **Payment transactions**: Customer payment processing
- **Order Status queries**: Checking order status
- **Delivery transactions**: Processing delivered orders
- **Stock Level queries**: Monitoring inventory levels

TPCC provides metrics focused on transaction throughput:
- **NOPM (New Orders Per Minute)**: The primary metric measuring transaction processing capability
- **TPM (Transactions Per Minute)**: Total transaction throughput across all transaction types

### TPCH Workload (Online Analytical Processing)
The TPCH benchmark consists of a suite of business-oriented ad-hoc queries and concurrent data modifications. It represents decision support scenarios with:
- **Complex analytical queries**: Multi-table joins and aggregations
- **Business intelligence operations**: Data warehousing and reporting
- **Ad-hoc query processing**: Varied query patterns and complexity
- **Large dataset analysis**: Scalable data processing scenarios

TPCH provides metrics focused on query performance and analytical processing:
- **Query execution times**: Individual query performance measurements
- **QphH (Queries per Hour)**: Composite throughput metric
- **Geometric mean of query times**: Overall system analytical performance

## Workload Metrics
The following metrics are examples of those captured by the Virtual Client when running the HammerDB tool against a
PostgreSQL server.

### TPCC Workload Metrics
| Metric Name  | Example Value  | Description |
|--------------|----------------|-------------|
| New Orders Per Minute (NOPM) | 12,855 | Primary TPCC metric measuring transaction processing capability |
| Transactions Per Minute (TPM) | 29,441 | Total transaction throughput across all transaction types |
| Average Response Time | 0.85 ms | Average transaction response time |

### TPCH Workload Metrics
| Metric Name  | Example Value  | Description |
|--------------|----------------|-------------|
| Query 1 Execution Time | 2.34 seconds | Individual query performance (22 queries total) |
| Query 6 Execution Time | 0.89 seconds | Fastest executing query example |
| Query 9 Execution Time | 15.67 seconds | Complex multi-table join query |
| Geometric Mean Query Time | 4.52 seconds | Overall analytical performance indicator |
| QphH (Queries per Hour) | 1,847 | Composite throughput metric for decision support |
| Power Test Score | 2,156 | Single-user query execution performance |
| Throughput Test Score | 1,923 | Multi-user concurrent query performance |

## Useful PostgreSQL Server Commands
The following section contains commands that were useful when onboarding this workload that help in the process of investigations and debugging.

``` bash
# Key files and directories associated with PostgreSQL version 14
# - /usr/lib/postgresql/14/bin/
#   The directory where the  'psql' toolset noted/used below is installed.
#
# - /etc/postgresql/14/main/pg_hba.conf
#   The configuration file that defines the IP addresses on which the PostgreSQL server listens.
#
# - /etc/postgresql/14/main/postgresql.conf
#   The configuration file for the PostgreSQL server itself. Most of the server-wide settings are defined in this file.

# Show PostgreSQL server online status. Use the password for the current logged in user.
/etc/init.d/postgresql status

# Show PostgreSQL service/daemon status.
sudo systemctl status postgresql

# or
sudo systemctl --type=service | grep postgresql

# Show user/account under which the PostgreSQL service is running.
systemctl show -p UID -p User postgresql.service

# Set password for PostgreSQL super user (e.g. 'postgres') account in environment variable. This environment variable is used 
# by the 'psql' command when the password is not provided on the command line.
export PGPASSWORD={pwd}

# Enter PostgreSQL terminal. Note that any of the commands below using 'psql' directly can be executed from the
# terminal. Using the terminal saves you from having to type the password multiple times.
export PGPASSWORD={pwd}
psql -U postgres

postgres-# \l
postgres-# \du

# Show all databases
psql -U postgres -c "\l"

# Show all users for a PostgreSQL Server.
psql -U postgres -c "\du"

# List PostgreSQL database tables
psql -U postgres -d hammerdbtest -c "\dt"

# List PostgreSQL database tablespaces
psql -U postgres -c "\db+"

# Show PostgreSQL configuration file contents
~/virtualclient/content/linux-x64$ sudo cat /etc/postgresql/14/main/pg_hba.conf
~/virtualclient/content/linux-x64$ sudo cat /etc/postgresql/14/main/postgresql.conf

```
 