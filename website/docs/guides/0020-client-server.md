# Client/Server Support
The Virtual Client supports workloads that run across more than 1 systems. These are often called "client/server" workloads. For example the networking
workload suite requires 2 systems in order to operate the workload. One system performs the role of the "Client" and one the role of the "Server". The Client
issues calls to the Server and both sides measure various aspects of network performance. The systems required to conduct client/server workloads cannot be easily 
discovered by the Virtual Client when the application starts. In order to describe the environment/systems involved, the Virtual Client must be passed an 
"environment layout" on the command line.

Note that the documentation for each profile will designate whether the workload or monitor requires or supports multi-system, client/server operations.

## Environment Layouts
 An environment layout is a file containing a description of all systems required by the workload operations. The description is JSON-formatted and provides additional
information to the Virtual Client in support of the needs of the workload. The following table describes the parts of the JSON document and provides an example.

| Property         | Description | Required |
|------------------|-------------|----------|
| name             | Defines the name/ID of the system. This is how the Virtual Client will "lookup itself" in the list of instances. If an agent ID is passed in on the command line, that is the name that should be used. Otherwise, the machine name (as defined by the operating system) should be used. |
| ipAddress        | Defines the IP address of the system. Virtual Client instances use this IP address to |

``` json
# The layout file path is passed in on the command line.
VirtualClient.exe --profile=PERF-NETWORK.json --system=Demo --timeout=180 --layout="C:\users\any\vc\layout.json" --packages="{BlobStoreConnectionString|SAS URI}"

# Contents of the 'layout.json' file
{
    "clients": [
        {
            "name": "{agent_id_or_machine_name_1}",
            "ipAddress": "{ip_address_1}",
            "role": "{role_1}"
        },
        {
            "name": "{agent_id_or_machine_name_2}",
            "ipAddress": "{ip_address_2}",
            "role": "{role_2}"
        },
        # ...N-additional clients in the environment
    ]
}
```

## Environment Layout Example
In client/server workloads, each client instance in the environment layout will have a specific "role" assigned. These roles are specific to each different
workload profile/workload and can be determined by looking at the documentation for that particular profile.

``` json
# The following examples are JSON representation of an environment layout. Environment layouts are supplied to the 
# Virtual Client in a JSON file that is referenced on the command line.

# Example Environment Layout 1
# Command line reference to the environment layout:
VirtualClient.exe --profile=PERF-NETWORK.json --system=Demo --timeout=1440 --layoutPath="C:\any\path\to\layout.json" --packages="{BlobStoreConnectionString|SAS URI}"

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
VirtualClient.exe --profile=PERF-WEB-NGINX.json --system=Demo --timeout=1440 --layoutPath="C:\any\path\to\layout.json" --packages="{BlobStoreConnectionString|SAS URI}"

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

## Virtual Client REST API
The Virtual Client supports hosting a REST API within its process. The REST API enables instances of the Virtual Client running on different systems to communicate
with each other via HTTP protocol. For example, instances of the Virtual Client running in client/server scenarios often need the ability to determine if the other
instances are online and running, to send instructions and to save state in between important operations. The REST API provides endpoints for the following aspects 
required to facility reliable operations across different systems:

* **Heartbeats**  
  The heartbeat API is used by instances of the Virtual Client to determine when other instances of the application are online and running. This is often the very
  first thing that happens when multiple instances of the Virtual Client are trying to synchronize with each other.

* **Instructions/Eventing**  
  The instructions/eventing API is used by instances of the Virtual Client to send/receive instructions from other instances of the Virtual Client. For example, it
  is common for instances in a client role to send instructions to instances in the server role to indicate which server-side workloads to run.

* **State**  
  The state API is used to enable instances of the Virtual Client to save information that can be used at later points to make decisions on the order of operations.
  This is often used for example by instances in the server role to persist information that the client instances can poll for when making decisions on the execution
  of client-side workloads.

The Virtual Client REST API runs on port 4500 by default and is listening for basic HTTP traffic on that port. In order to communicate on this port, the firewall on
the system must be modified to allow for it. This means that any client/server workload that will be utilizing the REST API **MUST be run in elevated/privileged mode**.
On Windows systems, the workload should be ran under an account with access to modify the firewall settings or with 'Administrative' privileges. Similarly, on Linux systems, 
the workload should be ran under an account with access to modify the firewall settings or with `sudo`.

The port in which the REST API uses can be changed if needed. For example, it is possible that some other application running on the system is already using the default
port. The following examples illustrate how to use a different port:

* [Command Line Options](./0010-command-line.md)

``` bash
# Use a different port by specifying the --api-port
VirtualClient.exe --profile=PERF-NETWORK.json --system=Demo --timeout=1440 --api-port=4501 --layoutPath="C:\any\path\to\layout.json" --packages="{BlobStoreConnectionString|SAS URI}"

# Use a different port for each of the workload roles
VirtualClient.exe --profile=PERF-NETWORK.json --system=Demo --timeout=1440 --api-port=4501/Client,4502/Server --layoutPath="C:\any\path\to\layout.json" --packages="{BlobStoreConnectionString|SAS URI}"
```
