# YCSB
An extensible workload generator

Yahoo Cloud Serving Benchmark (YCSB) project is to develop a framework and common set of workloads for evaluating the performance of different "key-value" and "cloud" serving stores.

---
## Loading YCSB:

```
$ ./bin/ycsb load (database name) -P workloads/workloada -p recordcount=10000000
```

.dat files can be created to specify recordcount property.

`large.dat: recordcount = 100000000`

```
$ ./bin/ycsb load (database name) -P workloads/workloada -P large.dat
```

*When using this strategy to update recordcount during the load phase it must also be used during the run/transaction phase*
### To see output while loading data:
If dealing with a big data load it can be helpful to make sure everything is going well with status updates.

-s will require the Client to produce status report on stderr.

The character > will send load data to a file (aka. load.dat below)
```
$ ./bin/ycsb load (database name) -P workloads/workloada -P large.dat -s > load.dat
```

### Command Line Options (Load):
`-P` Load Property files
*Running Multiple Clients in Parallel*:
`-p insertstart` The index of the record to start at
`-p insertcount` The number of records to insert
## Executing YCSB:
Executing a workload/running transaction phase:

```
$./bin/ycsb run (database name) -P workloads/workloada -P large.dat -s -threads 10 -target 100 -p operationcount=50000000 -p measurementtype=timeseries -p timeseries.granularity=2000 > transactions.dat
```

### Command Line Options (Tx):
`-threads` number of client threads (default: 1)
`-target` throttle ops (default: un-throttled)
      (used to generate latency vs throughput curves)
`-p` Set parameters
`-s ... > transacations.day` output on stderr & transactions.dat
`-p operationcount=10000000` Amount of ops to run
`-p maxexecutiontime=300` Run length regardless of operation count in. (default=off) (Seconds)
`-p measurementtype=timeseries` sets latency reporting to timeseries (default: histogram)
`-p timeseries.granularity=2000` sets latency report rate (default: 1000)

## Running Multiple Clients in Parallel:

### Loading database from multiple clients:

Loading the database from multiple clients is done by partitioning the workload records.

Normally YCSB just loads all the records, but Command Line Options (Load) allow the ability to cut up the records in a workload. 

Example: Loading 10 million records/ 2 clients

First Client:
`-p insertstart=0`
`-p insertcount=5000000`

Second Client:
`-p insertstart=5000000`
`-p insertcount=5000000`

### Executing  from multiple clients:
Run transaction phase of the workload from multiple servers. Start up multiple client servers, each running the same workload targeted at the same database server.

``` 
DEPRICATED - Just connect using below format *Excecuting remotely*
1. Get connection string from host server
2. Boot client, install YCSB
3. Connect to server using connection string and Mongosh
```

***Executing remotely:***
Add:
 `-p mongodb.url="mongodb://{serverIP}:27017/{dbname}?w=0"`

Ex. Synchronous
```
$./bin/ycsb run mongodb -P workloads/workloada -P large.dat -s -threads 10 -p mongodb.url="mongodb://10.7.0.18:27017/ycsb?w=0" > transactions.dat
```

---
### Data Collection:

This tool spits out the following type of telemetry:

*Under tags**:

OVERALL:
* Total Execution time (ms)
* Average throughput across all threads (ops/s)
* Garbage Collection data  
UPDATE/CLEANUP/READ:
* Total operations (num operations)
* Average latency (ms)
* Max latency (ms)
* 95th percentile latency (ms)
* 99th percentile latency (ms)
* Return code counts (num codes)
* Histogram/Time series (optional) of operation times

---
### Mongo DB Specific Config Options:

`mongodb.batchsize` - Submits inserts in batches (improving throughput). Good for insert heavy workloads. Default is 1.

---

## Resources:

### YCSB Properties:
https://github.com/brianfrankcooper/YCSB/wiki/Core-Properties#core-workload-package-properties
https://github.com/brianfrankcooper/YCSB/wiki/Core-Workloads#running-the-workloads

### Sharding (For Implementation of Client/Server):
https://www.mongodb.com/resources/products/capabilities/database-sharding-explained
https://www.youtube.com/watch?v=aBaD0qHK1as&list=PLIRAZAlr4cfY1gugVw2enf6uVXyJaWwwv
https://github.com/neerajg5/mongodb-tutorial/blob/main/mongodb-sharding-ubuntu-git.txt
https://www.mongodb.com/docs/manual/sharding/#shard-keys
https://github.com/brianfrankcooper/YCSB/wiki/Running-a-Workload-in-Parallel
https://github.com/brianfrankcooper/YCSB/blob/master/mongodb/README.md
https://www.digitalocean.com/community/tutorials/how-to-configure-remote-access-for-mongodb-on-ubuntu-20-04

### Helpful MongoDB commands:
```bash

# For stop/start/restart/status MongoDB Server, command depends on if your system uses service or systemctl 
sudo service mongod {command}
sudo systemctl {command} mongod

# Often time when the server won't start after restarting the VM it is on this is a good fix
sudo chown -R mongodb:mongodb /var/lib/mongodb 
sudo chown mongodb:mongodb /tmp/mongodb-27017.sock

# Great Command for checking attached clients (while in thge mongoshell):
db.currentOp(true).inprog.reduce((accumulator, connection) => { ipaddress = connection.client ? connection.client.split(":")[0] : "Internal"; accumulator[ipaddress] = (accumulator[ipaddress] || 0) + 1; accumulator["TOTAL_CONNECTION_COUNT"]++; return accumulator; }, { TOTAL_CONNECTION_COUNT: 0 })

# Drop YCSB database
mongosh --host localhost --eval "use ycsb" --eval "db.dropDatabase()"
```