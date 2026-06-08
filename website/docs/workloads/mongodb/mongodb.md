# MongoDB
MongoDB is a document-oriented NoSQL database used for high volume data storage. Instead of using tables and rows as in traditional relational databases, MongoDB makes use of collections and documents. Documents consist of key-value pairs which are the basic unit of data in MongoDB.

* [Official MongoDB Documentation](https://www.mongodb.com/docs/)
* [MongoDB GitHub Repo](https://github.com/mongodb/mongo)

The widely used tool for benchmarking performance of a MongoDB server is Yahoo Cloud Serving Benchmark (YCSB):
* [YCSB Documentation](https://github.com/brianfrankcooper/YCSB/wiki)
* [YCSB MongoDB Binding](https://github.com/brianfrankcooper/YCSB/blob/master/mongodb/README.md)

## What is Being Measured?
The YCSB (Yahoo Cloud Serving Benchmark) toolset is used to generate various workload patterns against MongoDB instances. YCSB performs operations such as INSERT, READ, UPDATE, and SCAN against the MongoDB server and provides throughput and latency percentile distributions.

YCSB includes six core workload types (workloada through workloadf), each representing different use case scenarios:
- **Workload A (Update Heavy)**: 50% reads, 50% updates - Simulates a session store with recent data updates
- **Workload B (Read Mostly)**: 95% reads, 5% updates - Typical photo tagging application
- **Workload C (Read Only)**: 100% reads - User profile cache where profiles are constructed elsewhere
- **Workload D (Read Latest)**: 95% reads, 5% inserts - User status updates with latest data being more popular
- **Workload E (Short Ranges)**: 95% scans, 5% inserts - Threaded conversations where each scan picks up recent posts
- **Workload F (Read-Modify-Write)**: 50% reads, 50% read-modify-write - User database with transactions

## Workload Metrics
The following metrics are examples of those captured by the Virtual Client when running the YCSB workload against a
MongoDB server.

### YCSB Workload Metrics
The following table shows the list of metrics that are captured from the execution of the YCSB workload against a MongoDB server.

| Metric Name  | Example Value  | Unit | Description |
|--------------|----------------|------|-------------|
| Throughput | 45235.67 | operations/sec | Overall operations processed per second |
| Operations | 5000000 | count | Total number of operations performed |
| RunTime | 110532.0 | milliseconds | Total execution time of the workload |
| INSERT-Operations | 250000 | count | Number of insert operations performed |
| INSERT-AverageLatency | 2.34 | milliseconds | Average latency for insert operations |
| INSERT-MinLatency | 0.89 | milliseconds | Minimum latency for insert operations |
| INSERT-MaxLatency | 156.78 | milliseconds | Maximum latency for insert operations |
| INSERT-95thPercentileLatency | 4.52 | milliseconds | 95th percentile latency for insert operations |
| INSERT-99thPercentileLatency | 8.91 | milliseconds | 99th percentile latency for insert operations |
| READ-Operations | 2375000 | count | Number of read operations performed |
| READ-AverageLatency | 1.87 | milliseconds | Average latency for read operations |
| READ-MinLatency | 0.45 | milliseconds | Minimum latency for read operations |
| READ-MaxLatency | 98.34 | milliseconds | Maximum latency for read operations |
| READ-95thPercentileLatency | 3.21 | milliseconds | 95th percentile latency for read operations |
| READ-99thPercentileLatency | 6.78 | milliseconds | 99th percentile latency for read operations |
| UPDATE-Operations | 2375000 | count | Number of update operations performed |
| UPDATE-AverageLatency | 2.12 | milliseconds | Average latency for update operations |
| UPDATE-MinLatency | 0.67 | milliseconds | Minimum latency for update operations |
| UPDATE-MaxLatency | 134.56 | milliseconds | Maximum latency for update operations |
| UPDATE-95thPercentileLatency | 4.23 | milliseconds | 95th percentile latency for update operations |
| UPDATE-99thPercentileLatency | 7.89 | milliseconds | 99th percentile latency for update operations |
| SCAN-Operations | 125000 | count | Number of scan operations performed |
| SCAN-AverageLatency | 15.67 | milliseconds | Average latency for scan operations |
| SCAN-MinLatency | 5.23 | milliseconds | Minimum latency for scan operations |
| SCAN-MaxLatency | 456.78 | milliseconds | Maximum latency for scan operations |
| SCAN-95thPercentileLatency | 34.56 | milliseconds | 95th percentile latency for scan operations |
| SCAN-99thPercentileLatency | 78.90 | milliseconds | 99th percentile latency for scan operations |

## Useful MongoDB Server Commands
The following section contains commands that are useful for MongoDB server management, investigations, and debugging.

``` bash
# Key files and directories associated with MongoDB
# - /etc/mongod.conf
#   The main configuration file for the MongoDB server.
#
# - /var/lib/mongodb
#   Default data directory where MongoDB stores database files.
#
# - /var/log/mongodb/mongod.log
#   Default log file location.

# Show MongoDB server status (systemd-based systems)
sudo systemctl status mongod

# Show MongoDB server status (init.d-based systems)
sudo service mongod status

# Start MongoDB server
sudo systemctl start mongod
# or
sudo service mongod start

# Stop MongoDB server
sudo systemctl stop mongod
# or
sudo service mongod stop

# Restart MongoDB server
sudo systemctl restart mongod
# or
sudo service mongod restart

# Enable MongoDB to start on boot
sudo systemctl enable mongod

# Fix common ownership issues (when server won't start after VM restart)
sudo chown -R mongodb:mongodb /var/lib/mongodb 
sudo chown mongodb:mongodb /tmp/mongodb-27017.sock

# Enter MongoDB shell (legacy mongo client)
mongosh

# Connect to MongoDB server on specific host and port
mongosh --host localhost --port 27017

# Show all databases
mongosh --eval "show dbs"

# Drop YCSB database
mongosh --host localhost --eval "use ycsb" --eval "db.dropDatabase()"

# Show database collections
mongosh --eval "use ycsb" --eval "show collections"

# Check database size
mongosh --eval "use ycsb" --eval "db.stats(1024*1024)"

# Show current operations
mongosh --eval "db.currentOp()"

# Check connected clients (useful for debugging client-server scenarios)
mongosh --eval "db.currentOp(true).inprog.reduce((accumulator, connection) => { ipaddress = connection.client ? connection.client.split(':')[0] : 'Internal'; accumulator[ipaddress] = (accumulator[ipaddress] || 0) + 1; accumulator['TOTAL_CONNECTION_COUNT']++; return accumulator; }, { TOTAL_CONNECTION_COUNT: 0 })"

# Show server status with detailed metrics
mongosh --eval "db.serverStatus()"

# Check replication status (if using replica sets)
mongosh --eval "rs.status()"

# Show MongoDB server logs
sudo tail -f /var/log/mongodb/mongod.log

# Check MongoDB version
mongod --version
```

## MongoDB Configuration for Remote Access
When running MongoDB in client-server scenarios, you need to configure the server to accept remote connections:

``` bash
# Edit MongoDB configuration file
sudo nano /etc/mongod.conf

# Update the network interfaces section to bind to all interfaces:
# net:
#   port: 27017
#   bindIp: 0.0.0.0

# Restart MongoDB after configuration changes
sudo systemctl restart mongod

# Verify MongoDB is listening on the correct port
sudo netstat -plntu | grep mongod
# or
sudo ss -tlnp | grep mongod
```

## YCSB Command Examples
The following are common YCSB command patterns used with MongoDB:

``` bash
# Load data into MongoDB (basic)
./bin/ycsb load mongodb -s -P workloads/workloada -p recordcount=1000000

# Load data with custom properties file
./bin/ycsb load mongodb -s -P workloads/workloada -P large.dat

# Run workload against MongoDB
./bin/ycsb run mongodb -s -P workloads/workloada -threads 16 -target 10000

# Run workload against remote MongoDB server
./bin/ycsb run mongodb -s -P workloads/workloada -threads 16 \
  -p mongodb.url="mongodb://10.0.0.5:27017/ycsb?w=0"

# Run with custom operation count and time series measurements
./bin/ycsb run mongodb -s -P workloads/workloada \
  -threads 16 \
  -p operationcount=5000000 \
  -p measurementtype=timeseries \
  -p timeseries.granularity=2000

# Load data from multiple clients (parallel loading)
# Client 1:
./bin/ycsb load mongodb -s -P workloads/workloada \
  -p insertstart=0 -p insertcount=5000000

# Client 2:
./bin/ycsb load mongodb -s -P workloads/workloada \
  -p insertstart=5000000 -p insertcount=5000000
```

## Important Notes and Warnings

### Database Growth
⚠️ **Warning**: Workload E (short ranges) and Workload D (read latest) insert new records into the database. Running these workloads repeatedly will cause the dataset to grow in size over time. This can lead to server failure if MongoDB runs out of disk space. Monitor disk usage and periodically clean the database when running these workloads.

### Disk Space Requirements
Different database sizes require different amounts of disk space:
- **Small (500K records)**: ~8-10 GB
- **Medium (2.5M records)**: ~40-50 GB
- **Large (20M records)**: ~320-400 GB
- **XLarge (55M records)**: ~880-1100 GB

### Performance Considerations
- **Thread Count**: Generally set to half the logical core count for balanced performance
- **Target Operations**: Use `-target` parameter to control throughput for latency testing
- **Write Concern**: The `w=0` parameter in the MongoDB URL provides better performance but less durability
- **Batch Size**: For insert-heavy workloads, increase `mongodb.batchsize` for better throughput

## Additional Resources
For more detailed information on MongoDB workload profiles and testing scenarios, see:
* [MongoDB Workload Profiles](./mongodb-profiles.md)
* [YCSB Details and Usage](./YCSB.md)
* [MongoDB Testing Scripts](./testing_scripts.md)
* [YCSB Core Workloads Documentation](https://github.com/brianfrankcooper/YCSB/wiki/Core-Workloads)
* [YCSB Core Properties](https://github.com/brianfrankcooper/YCSB/wiki/Core-Properties)