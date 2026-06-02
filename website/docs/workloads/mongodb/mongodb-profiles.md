# MongoDB Profile Components
---

## Dependencies
* **JDKPackageDependencyInstallation**  
    Jdk package installation which is necessary for the intallation of YCSB. (Client)

* **DependencyPackageInstallation**  
  Installation of the YCSB package zip file. (Client)

* **LinuxPackageInstallation**  
  Installation of necessary Linux packages. (Server/Client)

### TBA (To Be Added):
* **FormatDisks** and **MountDisks**  
  Format any unformatted disks on the server, then mount any unmounted disks. (Server)

* **MongoDBServerInstallation**  
  Installation of MongoDB server. (Server)

* **ApiServer**  
  Starts the API server for Client-Server workloads.

## Actions
* **MongoDBRunYCSB**  
  Runs a given workload on the MongoDB server.

## PERF-MONGODB-TYPE.json
Runs multiple workload variations using YCSB's built in workloads to test the bandwitch of CPU, Memory, and Disk I/O.
Loads a single type of dataset at a time, then runs various workloads against it.

* **Supperted Platform/Architectures**
  * linux-x64 (Ubuntu)


* **Supports Disconnected Scenarios**  
  * No. Internet connection required.

* **Dependencies**  
  The dependencies defined in the 'Dependencies' section of the profile itself are required in order to run the workload operations effectively.
  * Internet connection.

  Additional information on components that exist within the 'Dependencies' section of the profile can be found in the following locations:
  * [Installing Dependencies](https://microsoft.github.io/VirtualClient/docs/category/dependencies/)

* **Profile Parameters**  
The following parameters can be optionally supplied on the command line. See the 'Usage Scenarios/Examples' above for examples on how to supply parameters to Virtual Client profiles.

| Parameter                 | Purpose                                                                                                                                       |Default                        |
|---------------------------|-----------------------------------------------------------------------------------------------------------------------------------------------|-------------------------------|
| ThreadCount               | Optional. Number of threads to use during workload execution.                                                                                 | calculate(LogicalCoreCount/2) |
| WorkloadName              | Optional. **Name of workload to initialy load**; options listed [here](https://github.com/brianfrankcooper/YCSB/wiki/Core-Workloads#running-the-workloads). |         workloada             |
| Duration                  | Optional. Timespan duration of each action in the workload.                                                                                   |         00:05:00              |

---
***Warning***  
  workloade and workloadd insert records into the database. This will cause the dataset to grow in size over time.
  This can lead to a server failure if MongoDB runs out of disk space.

---

* **Profile Runtimes**  
  See the 'Metadata' section of the profile for estimated runtimes. These timings represent the length of time required to run a single round of profile actions. These timings can be used to determine minimum required runtimes for the Virtual Client in order to get results. These are often estimates based on the
  number of system cores.

* **Usage Examples**
  The following section provides a few basic examples of how to use the workload profile.

  ``` bash
  # When running on a single system (environment layout not required)
  ./VirtualClient --profile=PERF-MONGODB-TYPE.json --packageStore="{BlobConnectionString}
  ```

## PERF-MONGODB-LOAD.json
Runs a single workload variation against different record sizes to test the bandwitch of CPU, Memory, and Disk I/O.
Loads a single type of dataset at a time, then runs a single workload variation with increasing record sizes against it.

---
***Warning***  
  This workload can cause the dataset to grow in size over time if given record sizes are larger than those already in the database.
  This can lead to a server failure if MongoDBruns out of disk space.
 
---

* **Supperted Platform/Architectures**
  * linux-x64 (Ubuntu)


* **Supports Disconnected Scenarios**  
  * No. Internet connection required.

* **Dependencies**  
  The dependencies defined in the 'Dependencies' section of the profile itself are required in order to run the workload operations effectively.
  * Internet connection.

  Additional information on components that exist within the 'Dependencies' section of the profile can be found in the following locations:
  * [Installing Dependencies](https://microsoft.github.io/VirtualClient/docs/category/dependencies/)

* **Profile Parameters**  
The following parameters can be optionally supplied on the command line. See the 'Usage Scenarios/Examples' above for examples on how to supply parameters to Virtual Client profiles.

| Parameter                 | Purpose                                                                                                                                       |Default                        |
|---------------------------|-----------------------------------------------------------------------------------------------------------------------------------------------|-------------------------------|
| ThreadCount               | Optional. Number of threads to use during workload execution.                                                                                 | calculate(LogicalCoreCount/2) |
| WorkloadName              | Optional. **Name of workload to run**; options listed [here](https://github.com/brianfrankcooper/YCSB/wiki/Core-Workloads#running-the-workloads).|         workloada             |
| Duration                  | Optional. Timespan duration of each action in the workload.                                                                                   |         00:05:00              |

* **Profile Runtimes**  
  See the 'Metadata' section of the profile for estimated runtimes. These timings represent the length of time required to run a single round of profile actions. These timings can be used to determine minimum required runtimes for the Virtual Client in order to get results. These are often estimates based on the
  number of system cores.

* **Usage Examples**
  The following section provides a few basic examples of how to use the workload profile.

  ``` bash
  # When running on a single system (environment layout not required)
  ./VirtualClient --profile=PERF-MONGODB-LOAD.json --packageStore="{BlobConnectionString}
  ```