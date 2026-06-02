# Testing Installation Scripts
These scripts are used for testing. Made for quickly installing mongo or YCSB onto a VM.

## Installing MongoDB on x64 Ubuntu System

```bash
#!/bin/bash

# Script to Install MongoDB locally onto linux machine, assuming fresh machine
# Run from root

echo "Grabbing GPG key"
# Get MongoDB public GPG key
curl -fsSL https://www.mongodb.org/static/pgp/server-7.0.asc | sudo gpg -o /usr/share/keyrings/mongodb-server-7.0.gpg --dearmor
echo "Done grabbing GPG key"

# Run the command "cat /etc/lsb-release" and capture the output
output=$(cat /etc/lsb-release)

# Extract the value of DISTRIB_CODENAME
codename=$(echo "$output" | grep -oP 'DISTRIB_CODENAME=\K[^ ]+')

echo $codename

case $codename in
  "jammy"|"focal"|"bionic"|"noble")
    echo "deb [ arch=amd64,arm64 signed-by=/usr/share/keyrings/mongodb-server-7.0.gpg ] https://repo.mongodb.org/apt/ubuntu ${codename}/mongodb-org/7.0 multiverse" | sudo tee /etc/apt/sources.list.d/mongodb-org-7.0.list
    ;;
  *)
    echo "DISTRIB_CODENAME is unsupported [Supported: jammy, focal, bionic, noble]"
    ;;
esac

echo "sudo apt-get update"
#Reload local package database
sudo apt-get update

echo "Starting Mongo install"
#Install latest Mongo
sudo apt-get install -y mongodb-org=7.0.12 mongodb-org-database=7.0.12 mongodb-org-server=7.0.12 mongodb-mongosh=1.10.6 mongodb-org-mongos=7.0.12 mongodb-org-tools=7.0.12

echo "Done installing Mongo!"

# Locking Mongo packages at current version
echo "mongodb-org-database hold" | sudo dpkg --set-selections
echo "mongodb-org-server hold" | sudo dpkg --set-selections
echo "mongodb-mongosh hold" | sudo dpkg --set-selections
echo "mongodb-org-mongos hold" | sudo dpkg --set-selections
echo "mongodb-org-tools hold" | sudo dpkg --set-selections

# Fix for server being down
sudo chown -R mongodb:mongodb /var/lib/mongodb 
sudo chown mongodb:mongodb /tmp/mongodb-27017.sock

# Server Restart for good measure
sudo systemctl restart mongod || sudo service mongod restart

echo "end."
```

## Installing YCSB (Version 0.17.0) on Linux system

```
# !/bin/bash

# Script to install and run YCSB locally on a Linux machine.
#
MAVEN_VERSION="3.9.8"
YCSB_VERSION="0.17.0"

echo "Installing java Runtime Environment"
sudo apt install -y default-jre
echo "java Runtime Environment: Done"
echo "Installing java Development Kit"
sudo apt install -y default-jdk
echo "java Development Kit: Done"

echo "Installing YCSB ..."
curl -O --location https://github.com/brianfrankcooper/YCSB/releases/download/${YCSB_VERSION}/ycsb-${YCSB_VERSION}.tar.gz
tar xfvz ycsb-${YCSB_VERSION}.tar.gz
rm ycsb-${YCSB_VERSION}.tar.gz
cd ycsb-${YCSB_VERSION}

echo "YCSB install complete! :)"
echo "Done."
```

## Configure MongoDB Server
```
# !/bin/bash

# Add VM's private ip to bindIp list in /etc/mongod.conf

# Grab hostname
ip=$(hostname -I)

# Navigate to etc
cd
cd /etc

# Add hostname to mongod.conf
sudo sed -i "/bindIp:/ s/$/, $ip/" mongod.conf

# Gets rid of OS limit connection
ulimit -Sn
```

## Test Workload
```
# !/bin/bash

# Navigate to YCSB package
cd YCSB-production/ycsb-mongodb

### r50w50small
# Load MongoDB database with data for r50w50small
./bin/ycsb.sh load mongodb -P workloads/workloada -p recordcount=1000000 -p maxexecutiontime=300 -p fieldcount=10 -p fieldlength=10 -p readallfields=true -threads 4 -s > small_test_load_data.txt

echo "r50w50small Loaded!"

# Run workload 
./bin/ycsb.sh run mongodb -P workloads/workloada -p recordcount=1000000 -p maxexecutiontime=300 -p fieldcount=10 -p fieldlength=10 -p readallfields=true -p operationcount=5000000 -threads 4 -s > small_workload_run_data.txt

echo "r50w50small Ran!"
###

mongosh --host localhost --eval "use ycsb" --eval "db.dropDatabase()"
echo "Small Database Dropped!"

### r50w50medium
# Load MongoDB database with data for r50w50medium
./bin/ycsb.sh load mongodb -P workloads/workloada -p recordcount=1000000 -p maxexecutiontime=300 -p fieldcount=50 -p fieldlength=50 -p readallfields=true -threads 4 -s > medium_test_load_data.txt

echo "r50w50medium Loaded!"

# Run workload
./bin/ycsb.sh run mongodb -P workloads/workloada -p recordcount=1000000 -p maxexecutiontime=300 -p fieldcount=50 -p fieldlength=50 -p readallfields=true -threads 4 -p operationcount=5000000 -s > medium_workload_run_data.txt

echo "r50w50medium Ran!"
###

mongosh --host localhost --eval "use ycsb" --eval "db.dropDatabase()"
echo "Medium Database Dropped!"

### r50w50large
# Load MongoDB database with data for r50w50medium
./bin/ycsb.sh load mongodb -P workloads/workloada -p recordcount=1000000 -p maxexecutiontime=300 -p fieldcount=100 -p fieldlength=100 -p readallfields=true -threads 4 -s > large_test_load_data.txt

echo "r50w50large Loaded!"

# Run workload
./bin/ycsb.sh run mongodb -P workloads/workloada -p recordcount=1000000 -p maxexecutiontime=300 -p fieldcount=100 -p fieldlength=100 -p readallfields=true -s -p operationcount=5000000 -threads 4 > large_workload_run_data.txt

echo "r50w50large Ran!"
###

mongosh --host localhost --eval "use ycsb" --eval "db.dropDatabase()"
echo "Large Database Dropped!"

### GAUNTLET (Running benchmark recommended by YCSB authors)

# Load MongoDB database A
./bin/ycsb.sh load mongodb -P workloads/workloada -p recordcount=1000000 -p maxexecutiontime=300 -p readallfields=true -threads 4 -s > a_test_load_data.txt

echo "Database A Loaded!"

# Run workload A
./bin/ycsb.sh run mongodb -P workloads/workloada -p recordcount=1000000 -p maxexecutiontime=300 -p readallfields=true -s -p operationcount=5000000 -threads 4 > a_workload_run_data.txt

echo "Workload A Ran!"

# Run workload B
./bin/ycsb.sh run mongodb -P workloads/workloadb -p recordcount=1000000 -p maxexecutiontime=300 -p readallfields=true -s -p operationcount=5000000 -threads 4 > b_workload_run_data.txt

echo "Workload B Ran!"

# Run workload C
./bin/ycsb.sh run mongodb -P workloads/workloadc -p recordcount=1000000 -p maxexecutiontime=300 -p readallfields=true -s -p operationcount=5000000 -threads 4 > c_workload_run_data.txt

echo "Workload C Ran!"

# Run workload D
./bin/ycsb.sh run mongodb -P workloads/workloadd -p recordcount=1000000 -p maxexecutiontime=300 -p readallfields=true -s -p operationcount=5000000 -threads 4 > d_workload_run_data.txt

echo "Workload D Ran!"

mongosh --host localhost --eval "use ycsb" --eval "db.dropDatabase()"
echo "Database A Dropped!"

# Load MongoDB database with data
./bin/ycsb.sh load mongodb -P workloads/workloade -p recordcount=1000000 -p maxexecutiontime=300 -s > test_loade_data.txt

echo "Database E Loaded!"

./bin/ycsb.sh run mongodb -P workloads/workloade -p recordcount=1000000 -p maxexecutiontime=300 readallfields=true -s -threads 4 -p operationcount=5000000 > workloade_run_data.txt

echo "Workload E Ran!"
###

echo "Workload Done!"
```