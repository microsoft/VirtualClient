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
| Transactions Per Minute	| 29441|

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
 