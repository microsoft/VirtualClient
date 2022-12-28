---
id: environment-layout
sidebar_position: 2
---

# Environment Layouts JSON
Some information required to conduct experiments cannot be easily discovered by the Virtual Client when the application starts. This type of
information must be provided to the Virtual Client via an "environment layout". An environment layout provides additional information
to the Virtual Client in support of the needs of the experiment that is not otherwise discoverable from the information on the VM/host system
itself.

``` json
# Schema
# Contents of the 'layout.json' file:
{
    "clients": [
        {
            "name": "{agent_id_or_machine_name_1}",
            "privateIPAddress": "{ip_address_1}"
        },
        {
            "name": "{agent_id_or_machine_name_2}",
            "privateIPAddress": "{ip_address_2}"
        },
        ... (N-additional clients in the environment)
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
# The following examples are JSON representation of an environment layout. Environment layouts are supplied to the 
# Virtual Client in a JSON file that is referenced on the command line.

# Example Environment Layout 1
# Command line reference to the environment layout:
VirtualClient.exe --profile=PERF-NETWORK.json --system=Azure --timeout=1440 --layoutPath="C:\any\path\to\layout.json"

# Contents of the 'layout.json' file:
# In the PERF-NETWORKING.json workload profile, there are 2 roles: Client and Server. They must be named exactly that in
# the environment layout.
{
    "clients": [
        {
            "name": "vm-e46ae74e-0",
            "role": "Client",
            "privateIPAddress": "10.1.0.1"
        },
        {
            "name": "vm-e46ae74e-1",
            "role": "Server",
            "privateIPAddress": "10.1.0.2"
        }
    ]
}

# Example Environment Layout 2
# Command line reference to the environment layout:
VirtualClient.exe --profile=PERF-WEB-NGINX.json --system=Azure --timeout=1440 --layoutPath="C:\any\path\to\layout.json"

# Contents of the 'layout.json' file:
# In the PERF-WEB-NGINX.json workload profile, there are 3 possible roles: Client, Server and ReverseProxy. They must be named exactly that in
# the environment layout.
{
    "clients": [
        {
            "name": "vm-e46ae74e-0",
            "role": "Client",
            "privateIPAddress": "10.1.0.1"
        },
        {
            "name": "vm-e46ae74e-1",
            "role": "Server",
            "privateIPAddress": "10.1.0.2"
        },
        {
            "name": "vm-e46ae74e-2",
            "role": "ReverseProxy",
            "privateIPAddress": "10.1.0.3"
        }
    ]
}
```
