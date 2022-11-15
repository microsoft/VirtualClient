---
id: environment-layout
sidebar_position: 2
---

# Environment Layouts Json
Some information required to conduct experiments cannot be easily discovered by the Virtual Client when the application starts. This type of
information must be provided to the Virtual Client via an "environment layout". An environment layout provides additional information
to the Virtual Client in support of the needs of the experiment that is not otherwise discoverable from the information on the VM/host system
itself.

``` json
# Example Environment Layout
# The following example is a JSON representation of an environment layout. Environment layouts are supplied to the 
# Virtual Client in a JSON file that is referenced on the command line.

# Command line reference to the environment layout:
VirtualClient.exe --profile=PERF-IO-FIO.json --system=Azure --timeout=1440 --layoutPath="C:\any\path\to\layout.json"

# Contents of the 'layout.json' file:
{
    "clients": [
        {
            "name": "cluster01,dfea5b55-abe6-4193-9a4d-461bd133a09d,e46ae74e-0",
            "privateIPAddress": "10.1.0.1"
        }
    ]
}
```

### Advanced Scenarios (Client/Server)
Some experiment workload scenarios are more advanced than others. In a basic experiment, the Virtual Client application runs on a
single VM or host system and all of the necessities for running the workload are self-contained on that system (e.g. running a 
benchmark CPU workload such as OpenSSL). However, the Virtual Client application can support more advanced workload scenarios as
well where more than 1 VM or host system is required in order to support the needs of the workload. In these scenarios, the Virtual
Client requires the user/automation to provide a little information up front so that the details of the environment are known to each 
instance running on separate VMs/hosts.

In advanced environment layouts each client instance will have a specific "role" assigned. These roles are specific to each different
workload profile/workload and can be determined by looking at the documentation for that particular profile.

``` json
# Example Environment Layout
# The following example is a JSON representation of an environment layout. Environment layouts are supplied to the 
# Virtual Client in a JSON file that is referenced on the command line.

# Command line reference to the environment layout:
VirtualClient.exe --profile=PERF-NETWORK.json --system=Azure --timeout=1440 --layoutPath="C:\any\path\to\layout.json"

# Contents of the 'layout.json' file:
# In the PERF-NETWORKING.json workload profile, there are 2 roles: Client and Server. They must be named exactly that in
# the environment layout.
{
    "clients": [
        {
            "name": "cluster01,dfea5b55-abe6-4193-9a4d-461bd133a09d,e46ae74e-0",
            "role": "Client",
            "privateIPAddress": "10.1.0.1"
        },
        {
            "name": "cluster01,3a326892-b4f6-41ac-b185-b57253812323,e46ae74e-1",
            "role": "Server",
            "privateIPAddress": "10.1.0.2"
        }
    ]
}
```
