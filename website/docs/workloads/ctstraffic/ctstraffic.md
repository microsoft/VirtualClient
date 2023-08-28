# CtsTraffic
ctsTraffic is a highly scalable client/server networking tool giving detailed performance and reliability analytics

* [Official CtsTraffic Documentation](https://github.com/microsoft/ctsTraffic/tree/master/documents)
* [Official Release](https://github.com/microsoft/ctsTraffic/tree/master/Releases/2.0.3.0)

## What is Being Measured?
ctsTraffic is designed as a classic client/server model, where the server will wait listening for client requests, while the client initiates connections to servers. ctsTraffic servers will accept any number of connections from any number of clients; ctsTraffic clients can target and make connections to one or more servers.
ctsTraffic is used to measure data transferred between client and server along with errors occured if any.

## Workload Metrics
The following metrics are examples of those captured by the Virtual Client when running ctstraffic benchmark.

| Metric Name  | Value  | Unit | Desciption |
|--------------|----------------|----------|
| SendBps|	12855 | B/s | bytes/sec that were sent within the TimeSlice* period |
|RecvBps	| 29441| B/s | bytes/sec that were received within the TimeSlice period |
| InFlight|	12855 | | count of established connections transmitting IO pattern data |
|Completed	| 29441| | cumulative count of successfully completed IO patterns | 
| NetworkError|	12855 | | cumulative count of failed IO patterns due to Winsock errors |
|DataError	| 29441| | cumulative count of failed IO patterns due to data errors |

*TimeSlice - cumulative runtime in seconds
 