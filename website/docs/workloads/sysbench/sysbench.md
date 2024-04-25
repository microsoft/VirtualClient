# Sysbench OLTP
Sysbench is an open-source multi-threaded database benchmark tool for database online transacation processing (OLTP) operations against a
MySQL database.

* [Sysbench GitHub](https://github.com/akopytov/sysbench)  
* [Example Sysbench Uses](https://www.flamingbytes.com/posts/sysbench/)

## What is Being Measured?
The Sysbench test suite executes varied transactions on the database system including reads, writes, and other queries. The list of OLTP benchmarks 
supported by Sysbench are as follows:

| Benchmark Name        | Description                                                           |
|-----------------------|-----------------------------------------------------------------------|
| oltp_read_write       | Measures performance of read and write queries on MySQL database      |
| oltp_read_only        | Measures performance of only read queries on MySQL database           |
| oltp_write_only       | Measures performance of only write queries on MySQL database          |
| oltp_delete           | Measures performance of only delete queries on the MySQL database     |
| oltp_insert           | Measures performance of only insert queries on MySQL database         |
| oltp_update_index     | Measures performance of index updates on the MySQL database           |
| oltp_update_non_index | Measures performance of non-index updates on the MySQL database       |
| select_random_points  | Measures performance of random point select on the MySQL database     |
| select_random_ranges  | Measures performance of random range select on the MySQL database     |


## Workload Metrics
The following metrics are examples of those captured by the Virtual Client when running the Sysbench OLTP workload

| Execution Profile     | Test Name | Metric Name | Example Value |
|-----------------------|-----------|-------------|---------------|
| PERF-MYSQL-SYSBENCH-OLTP.json | Sysbench OLTP | # read queries | 5503894 |
| PERF-MYSQL-SYSBENCH-OLTP.json | Sysbench OLTP | # write queries | 259534 |
| PERF-MYSQL-SYSBENCH-OLTP.json | Sysbench OLTP | # other queries | 1284332 |
| PERF-MYSQL-SYSBENCH-OLTP.json | Sysbench OLTP | # transactions | 257421 |
| PERF-MYSQL-SYSBENCH-OLTP.json | Sysbench OLTP | transactions/sec | 153.01 |
| PERF-MYSQL-SYSBENCH-OLTP.json | Sysbench OLTP | # queries | 5948220 |
| PERF-MYSQL-SYSBENCH-OLTP.json | Sysbench OLTP | queries/sec | 2850.17 |
| PERF-MYSQL-SYSBENCH-OLTP.json | Sysbench OLTP | # ignored errors | 0 |
| PERF-MYSQL-SYSBENCH-OLTP.json | Sysbench OLTP | ignored errors/sec | 0.00 |
| PERF-MYSQL-SYSBENCH-OLTP.json | Sysbench OLTP | # reconnects | 0 |
| PERF-MYSQL-SYSBENCH-OLTP.json | Sysbench OLTP | reconnects/sec | 0.00 |
| PERF-MYSQL-SYSBENCH-OLTP.json | Sysbench OLTP | elapsed time | 1800.0012 |
| PERF-MYSQL-SYSBENCH-OLTP.json | Sysbench OLTP | latency min | 7.19 |
| PERF-MYSQL-SYSBENCH-OLTP.json | Sysbench OLTP | latency avg | 26.97 |
| PERF-MYSQL-SYSBENCH-OLTP.json | Sysbench OLTP | latency max | 682.33 |
| PERF-MYSQL-SYSBENCH-OLTP.json | Sysbench OLTP | latency 95p | 67.58 |
| PERF-MYSQL-SYSBENCH-OLTP.json | Sysbench OLTP | latency sum | 7458935.25 |

## Useful MySQL Server Commands
The following section contains commands that were useful when onboarding this workload that help in the process of investigations and debugging.

``` bash
# Show MySQL server online status. Use the password for the current logged in user.
/etc/init.d/mysql status

# or
sudo mysqladmin -p status

# Show MySQL service/daemon status.
sudo systemctl status mysql.service

# or
sudo systemctl --type=service | grep mysql

# Show user/account under which the MySQL service is running.
systemctl show -p UID -p User mysql.service

# Show all users for a MySQL Server. Sysbench requires the user match the name of the database (e.g. sbtest).
mysql> SELECT User, Host, Select_priv, Insert_priv, Update_priv, Delete_priv, Create_priv, Drop_priv, File_priv, Grant_priv, References_priv, Index_priv, Alter_priv, Show_db_priv, Super_priv FROM mysql.user;

# Show current user
mysql> SELECT user();

# List MySQL database table fields
mysql> desc mysql.user;

mysql> Use sbtest;
mysql> desc sbtest.sbtest1;

# Show the information schema tables for the database.
mysql> Use information_schema;
mysql> SHOW TABLES;

# Drop sysbench database
~/vc_tools/sysbench/src$ ./sysbench oltp_common --threads=1 --tables=10 --table-size=1 --mysql-db=sbtest --mysql-host=localhost cleanup

# Create sysbench database
~/vc_tools/sysbench/src$ ./sysbench oltp_common --threads=1 --tables=10 --table-size=1 --mysql-db=sbtest --mysql-host=localhost prepare

# Show MySQL configuration file contents
~/vc_tools/sysbench/src$ cat /etc/mysql/mysql.conf.d/mysqld.cnf

# Show MySQL database tables (i.e. sbtest1.ibd, sbtest2.ibd, sbtest3.ibd...)
~/vc_tools/sysbench/src$ sudo ls -a /var/lib/mysql/sbtest
```

## Creating and Distributing a Sysbench Database
The following process can be used to create and then move the sysbench database. Virtual Client programmatically does then whenever there are extra disks on the
system beyond the OS disk.

* [MySQL Option Files](https://dev.mysql.com/doc/refman/8.0/en/option-files.html)
* [Moving Data Files Offline](https://dev.mysql.com/doc/refman/8.0/en/innodb-moving-data-files-offline.html)

The following steps show how to create the Sysbench database and then move/distribute it across multiple disks.

* **Set 'innodb_directories' variable in mysqld.cnf (e.g. /etc/mysql/mysql.conf.d/mysqld.cnf)**  
  This variable must be set to instruct the innodb engine to look for database table files in a location other
  than the default data directory.

  ``` bash
  # Set 'innodb_directories' variable in mysqld.cnf (e.g. /etc/mysql/mysql.conf.d/mysqld.cnf)
  #
  # e.g.
  # Here is entries for some specific programs
  # The following values assume you have at least 32M ram

  [mysqld]
  #
  # * Basic Settings
  #
  user          = mysql
  pid-file      = /var/run/mysqld/mysqld.pid
  socket        = /var/run/mysqld/mysqld.sock
  port          = 3306
  datadir       = /var/lib/mysql
  innodb_directories=/home/junovmadmin/mnt_vc/sdc1;/home/junovmadmin/mnt_vc/sdd1;/home/junovmadmin/mnt_vc/sde1
  ```

* **Disable Apparmor for the MySQL server (i.e. for the mysqld service)**  

  The Apparmor service will prevent the MySQL server service from having permissions to create the Sysbench database table
  files on the data disk location. You will know when Apparmor is preventing MySQL from creating the table files because you
  will receive the following error:

  ERROR 1030 (HY000): Got error 168 - 'Unknown (generic) error from engine' from storage engine

  ``` bash
  # Disable Apparmor for MySQL
  sudo ln -s /etc/apparmor.d/usr.sbin.mysqld /etc/apparmor.d/disable/
  sudo apparmor_parser -R /etc/apparmor.d/usr.sbin.mysqld

  # Verify that MySQL is no longer in the list of protected apps. If MySQL is disabled, you should not see '/var/lib/mysql' in the
  # output of the following command.
  sudo aa-status
  ```

* **Update SQL 'Create' command in Sysbench oltp_common.lua file**  
  In the Sysbench repo directory (after compilation), change the /src/lua/oltp_common.lua file to use an SQL 'CREATE IF NOT EXISTS'
  statement vs. a 'CREATE' statement. The latter will cause Sysbench to fail when attempting to populate an existing database in steps
  below.

  ``` bash
  # The script file will look like this before the change.
  CREATE TABLE sbtest%d(
    id %s,
    k INTEGER DEFAULT '0' NOT NULL,
    c CHAR(120) DEFAULT '' NOT NULL,
    pad CHAR(60) DEFAULT '' NOT NULL,
    %s (id)
  )

  # It should look like this afterwards. Note we have added the 'IF NOT EXISTS' clause.
  CREATE TABLE IF NOT EXISTS sbtest%d(
    id %s,
    k INTEGER DEFAULT '0' NOT NULL,
    c CHAR(120) DEFAULT '' NOT NULL,
    pad CHAR(60) DEFAULT '' NOT NULL,
    %s (id)
  )
  ```

* **Create a very small/minimal Sysbench database**  
  We have to use sysbench to create the database. However, we only want to create a database with schema and a bare minimum
  number of records, so that we can efficiently move it afterwards. Unfortunately, Sysbench is going to create the database
  in the default data directory location. Out of box, that is the OS drive.

  Note that the settings used here for --tables should match the number of tables ultimately desired. In a later step below, we
  are populating the database tables. The number of tables defined here should match between both steps.

  ``` bash
  # Drop an existing Sysbench database
  ~/vc_tools/sysbench/src$ ./sysbench oltp_common --threads=1 --tables=10 --table-size=1 --mysql-db=sbtest --mysql-host=localhost cleanup

  # Create Sysbench database
  ~/vc_tools/sysbench/src$ ./sysbench oltp_common --threads=1 --tables=10 --table-size=1 --mysql-db=sbtest --mysql-host=localhost prepare
  ```

* **Create exact copies of the Sysbench table files on the target data disk(s)**  
  The process here involves copying the table files (e.g. sbtest1.ibd, sbtest2.ibd...) to the data disk locations using a different 
  name for the table, dropping the original and then renaming the tables copied to the original names (e.g. sbtest1_move -> sbtest1).

  ```
  # Open the MySQL console.
  sudo mysql

  # Copy the table files to the new disk locations using out-of-box SQL commands. In this example we are distributing
  # 10 tables across 3 disks/locations.
  mysql> CREATE TABLE sbtest1_move DATA DIRECTORY = '/home/junovmadmin/mnt_vc/sdc1' AS (SELECT * FROM sbtest1);
  mysql> CREATE TABLE sbtest2_move DATA DIRECTORY = '/home/junovmadmin/mnt_vc/sdc1' AS (SELECT * FROM sbtest2);
  mysql> CREATE TABLE sbtest3_move DATA DIRECTORY = '/home/junovmadmin/mnt_vc/sdc1' AS (SELECT * FROM sbtest3);
  mysql> CREATE TABLE sbtest4_move DATA DIRECTORY = '/home/junovmadmin/mnt_vc/sdc1' AS (SELECT * FROM sbtest4);

  mysql> CREATE TABLE sbtest5_move DATA DIRECTORY = '/home/junovmadmin/mnt_vc/sdd1' AS (SELECT * FROM sbtest5);
  mysql> CREATE TABLE sbtest6_move DATA DIRECTORY = '/home/junovmadmin/mnt_vc/sdd1' AS (SELECT * FROM sbtest6);
  mysql> CREATE TABLE sbtest7_move DATA DIRECTORY = '/home/junovmadmin/mnt_vc/sdd1' AS (SELECT * FROM sbtest7);

  mysql> CREATE TABLE sbtest8_move DATA DIRECTORY = '/home/junovmadmin/mnt_vc/sde1' AS (SELECT * FROM sbtest8);
  mysql> CREATE TABLE sbtest9_move DATA DIRECTORY = '/home/junovmadmin/mnt_vc/sde1' AS (SELECT * FROM sbtest9);
  mysql> CREATE TABLE sbtest10_move DATA DIRECTORY = '/home/junovmadmin/mnt_vc/sde1' AS (SELECT * FROM sbtest10);
  ```

* **Drop the original Sysbench tables**  
  We will drop the original tables first so they are deleted from the original location. In this example, we have 10
  total Sysbench tables to drop.

  ``` bash
  mysql> DROP TABLE sbtest1;
  mysql> DROP TABLE sbtest2;
  mysql> DROP TABLE sbtest3;
  mysql> DROP TABLE sbtest4;
  mysql> DROP TABLE sbtest5;
  mysql> DROP TABLE sbtest6;
  mysql> DROP TABLE sbtest7;
  mysql> DROP TABLE sbtest8;
  mysql> DROP TABLE sbtest9;
  mysql> DROP TABLE sbtest10;
  ```

* **Rename the tables copied to the original table names (i.e. matching the ones just dropped)**  
  Once the previous tables have been dropped/deleted, we can simply rename the copied tables to the original
  names and we will have effectively moved the database.

  ``` bash
  mysql> RENAME TABLE sbtest1_move TO sbtest1;
  mysql> RENAME TABLE sbtest2_move TO sbtest2;
  mysql> RENAME TABLE sbtest3_move TO sbtest3;
  mysql> RENAME TABLE sbtest4_move TO sbtest4;
  mysql> RENAME TABLE sbtest5_move TO sbtest5;
  mysql> RENAME TABLE sbtest6_move TO sbtest6;
  mysql> RENAME TABLE sbtest7_move TO sbtest7;
  mysql> RENAME TABLE sbtest8_move TO sbtest8;
  mysql> RENAME TABLE sbtest9_move TO sbtest9;
  mysql> RENAME TABLE sbtest10_move TO sbtest10;
  ```

* **Populate the Sysbench database with the desired amount of data**  
  We created a minimal sized Sysbench database in earlier steps so that we could efficiently move it and distribute the table files across
  the data disks. In this step we populate the database with the full set of records.

  Note here that the value used for --tables in the step above where we created the initial database before moving it should match here.

  ``` bash
  # Fully populate the Sysbench database with records.
  ~/vc_tools/sysbench/src$ ./sysbench oltp_common --threads=1 --tables=10 --table-size=100000 --mysql-db=sbtest --mysql-host=localhost prepare
  ```

  For reference, the approximate size of a database created by Sysbench for a 10-table database is 1MB + 10kB per record.
