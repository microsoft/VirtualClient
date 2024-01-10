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

## Running on Disk
A user will likely want to run Sysbench on one or multiple disks. In that case, VC is able to [FormatDisks](../../dependencies/0070-format-disks.md) and [MountDisks](../../dependencies/0071-mount-disks.md) for the user. VC will then use the mount points it created to store the testing database.
Sysbench can run on up to five disks.

## about sysbench
prepared statements
no disk path arg, builds on os disk
cannot configure size, just tables records
common commands, run it manually

## parameters

* **Threads**: The Sysbench workload generator prepares a list of statements to run on MySQL. MySQL can only accept so many prepared statements. We already max out this variable in a scenario in the MySQLConfiguation dependency, but even when setting the max_prepared_stmt_count to a large number, Sysbench can throw a 'too many connections' error. This has shown to be the case even on VMs with high core counts and equipped with the ability for high thread counts. User caution is advised for thread counts > 176; the VC team has only successfully seen Sysbench run with thread counts less than that number.

server-side

client-side

workloads

states

mysql server wont start