# Sysbench OLTP Client/Server Lifecycle

All SQL workloads tend to have a lot of moving parts and complexities. Below details a comprehensive look into the lifecycle of this workload, in order to offer a clearer look as to what is happening under the hood when VC runs the Sysbench workload on a MySQL server.

## MySQL Database Configuration
Using the [LinuxPackageInstallation](../../dependencies/0060-install-mysql.md) and [MySQLServerConfiguration](../../dependencies/0060-install-mysql-database.md) dependencies, VC can install and create a fresh, new database for the purpose of running Sysbench. Additionally, a user can provide their own database and skip database creation. However, if opting for this, take note that Sysbench will only successfully run against databases with its expected table naming conventions (ie. sbtest1, sbtest2, ...) and schema. That is, Sysbench will not run against any random database. Its workload scripts are specifically designed to recognize databases of a certain setup.

Regardless of if a user chooses to create a database through VC or bring their own, three MySQL commands are required for seamless Sysbench execution: RaisedStatementCount, ConfigureUser, and ConfigureNetwork (though the last two are not required for single-system runs). RaisedStatementCount increases the MySQL variable max_prepared_stmt_count to its maximum value, which is essential in Sysbench execution; the workload generator prepares MySQL statements in advance for execution, and if this variable is not high enough, Sysbench will fail even on reasonably high thread counts. The ConfigureUser setting grants all permissions to the client on the database; ConfigureNetwork edits the bind-address in /etc/mysql/mysql.conf.d/mysqld.cnf to allow for connections from other servers on the network.

To run these commands manually, execute the following:
``` bash
# ConfigureNetwork commands
sed -i \"s/.*bind-address.*/bind-address = {serverIPAddress}/\" /etc/mysql/mysql.conf.d/mysqld.cnf
systemctl restart mysql.service

# CreateUser MySQL commands
DROP USER IF EXISTS '{databaseName}'@'{clientIp}'
CREATE USER '{databaseName}'@'{clientIp}'
GRANT ALL ON *.* TO '{databaseName}'@'{clientIp}'

# RaisedStatementCount MySQL command
SET GLOBAL MAX_PREPARED_STMT_COUNT=100000;
```

### Common MySQL Issues
Should VC throw an error that the MySQL server has failed to start, usually this means there an issue with the MySQL configuration file. The config file is a very delicate thing, and too many changes may altogether destroy the integrity of the service. From experience, it can be very difficult to debug and find a root cause for the service failing to start. In this case, it may be easiest to uninstall the MySQL server altogether an allow VC to re-install it. Furthermore, ensure that the state json (state/sysbencholtpstate.json) is removed on both the server and client; the SysbenchOLTPServerExecutor takes care of some required database preparation on its side, and should the state not be reset, the server will believe it ran MySQL configuration and bypass a necessary step. More on this later.

To purge the mysql-server, run the following command:
``` bash
sudo apt-get purge --auto-remove mysql-server
```

## Setting Up Disks
A user will likely want to run Sysbench on one or multiple disks. In that case, VC is able to [FormatDisks](../../dependencies/0070-format-disks.md) and [MountDisks](../../dependencies/0071-mount-disks.md) for the user. VC will then use the mount points it created to store the testing database. VC will run Sysbench on up to five disks.

## About Sysbench
Sysbench prepares queries to test a MySQL server. Sysbench has various benchmark types (executed by lua scripts) it can run against the server, listed [here](./sysbench-oltp.md).
Sysbench exposes parameters that allow a user to execute the workload in single-server and client-server scenarios. However, any disk configuration must be done by the user themselves. That is, Sysbench does not expose any parameter to distribute the tables to one or multiple disks (ie. a data directory). If MySQL is not setup beforehand to prepare the server to put the database on one or multiple disks, Sysbench will go ahead and place the tables in the default location, the OS disk.
Sysbench also configures a few parameters, covered in depth below.

### Parameters

* **Threads**: The Sysbench workload generator prepares a list of statements to run on MySQL. MySQL can only accept so many prepared statements. We already max out this variable in a scenario in the MySQLConfiguation dependency, but even when setting the max_prepared_stmt_count to a large number, Sysbench can throw a 'too many connections' error. This has shown to be the case even on VMs with high core counts and equipped with the ability for high thread counts. User caution is advised for thread counts > 176; the VC team has only successfully seen Sysbench run with thread counts less than that number.
* **RecordCount and NumTables**: Sysbench does not configure its database by size, only by record and table counts. The number of records can be configured in the Default scenario, and can be programmatically determined in the Balanced Scenario. More on what each scenario offers can be found in the [profiles section](./sysbench-oltp-profiles.md). Note: the select_random_* workloads only run on 1 table.
* **Duration**: Recommended execution time is 20 minutes; it is advised to run the workload for at least 10 to 15 minutes for stable, consistent results.

If interested in running sysbench manually for familiarity reasons, here are examples of the three main commands VC executes with sysbench per execution (cleanup, prepare, and run) in a client-server scenario.

``` bash
# drops all existing tables in the given database
sysbench oltp_common --tables=10 --table-size=100 --mysql-db=sbtest --mysql-host=1.2.3.4 cleanup

# creates n fresh tables
sysbench oltp_common --tables=10 --table-size=100 --mysql-db=sbtest --mysql-host=1.2.3.4 prepare

# running the oltp_write_only workload
sysbench oltp_write_only --threads=10 --tables=10 --table-size=100 --mysql-db=sbtest --mysql-host=1.2.3.4 --time=600 run
```

## Server-Side Execution
The server side completes the following preparation steps before it listens on port 4500:

* Initialize any variables, including DiskFilter.
* Check the current state if the DatabaseScenario has been initialized. If not, it will do so.
* In-Memory initialization involves running a script to increase buffer size in the MySQL config file. The new buffer size is programmatically determined as totalMemoryinKilobytes / 1024.
* Balanced initialization finds any data disks attached and filters them on any given DiskFilter, along with the default OSDisk:false filter. The server then executes a script to enable apparmor permissions for the mount points to those disks, for mysql to chown those directories, and for them to be set as recognized innodb_directories. These configurations are essential to distributing different tables on different disks with no issue.
* The list of mount paths to the disks VC intends to use is then stored in the Sysbench state (to pass to the client for later use). The DatabaseScenario is set to initialized.
 
## Client-Side Execution
The client side completes the following preparation steps before it runs the sysbench 'run' command:

* Initialize any variables, including Duration, DatabaseName, Workload, NumTables, RecordCount, and Threads.
* Since Sysbench is executed from the client-side, the SysbenchOLTPClientExecutor is also in charge of creating the tables (ie. 'prepare' command) too. The client programmatically determines if the table and record count are different than what exist on the given database. If the requested table or record counts are higher than what exist, the client will run the 'cleanup' and 'prepare' steps to populate the database on the server. If not, Sysbench will leave it alone.
* If running on the Balanced scenario, the client will ping the server state for the target disks' mount points, which will serve as the arguments to an executed script. This balanced-client.sh script runs a CREATE TABLE n times to create temporary tables, targeting a given mount point as the chosen DATA DIRECTORY, copying all the records in the sysbench-generated table over. This must be done as MySQL cannot create a table with a blank schema. Then, the old sbtestx tables are DROPped and the new ones on the disk are renamed to be the same title as the old tables. The tables are split evenly across the disks; ie. for 2 disks and 10 tables, there will be 5 tables on one and 5 on the other.