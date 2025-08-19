# OpenSSL-TLS
This version workload sets up OpenSSL client and server processes and measures the file throughput at the client side. 
OpenSSL offers a pair of benchmarking tools to measure the network performance of TLS connections: `openssl s_server_` and `openssl s_time`.

# Setting up the server
To set up the server, you can use the `openssl s_server` command. This command starts a simple TLS server that listens for incoming connections. You will need to provide a certificate and a private key for the server to use.
```bash
openssl s_server -accept 4433 -cert server.crt -key server.key -WWW 
```
WWW option sets up a mock webserver to respond to HTTP requests, which is useful for testing purposes.

# Setting up the client
To set up the client, you can use the `openssl s_time` command. This command connects to the TLS server and can be used to send requests and receive responses.
In a TLS scenario, client side certificates can also be validated if server uses verify option. Since this is a simple benchmark, we will not use client certificates.
```bash
openssl s_time -connect localhost:4433 -new -time 10
```
this simple command will connect to the server and send requests for 10 seconds, measuring number of new connections (in 10 seconds) and number of reuse connections (in 10 seconds). This however does not report throughput.
To measure throughput, we can request a specific html file from the server and that reports number of bytes for every transaction and total throughput. 
```bash
s_time -connect :{ServerPort} -www /test_1k.html -time {Duration.TotalSeconds} -ciphersuites TLS_AES_128_GCM_SHA256 -tls1_3
```

This command requests a html file of size 1K and uses AES_128_GCM_SHA256 algorithm for its secure transaction. 

# Chosen algorithms

s_server/s_time can be used with SSL/TLS different versions, we decided to restrict to Tls1_3 as this is the most widely used cyphersuite currently.

Difference between Tls1_2 and Tls1_3 is documented [here](Tls12vsTls13.md).

The following Tls1_3 cryptographic cypher suites are measured.

* TLS_AES_128_GCM_SHA256
* TLS_AES_256_GCM_SHA384
* TLS_CHACHA20_POLY1305_SHA256

## What is Being Measured?
OpenSSL client requests specific file size varying 1KiB - 512 MiB and reports the throughput. 



### Workload Metrics
The following metrics are examples of those captured by the Virtual Client when running the OpenSSL Speed workload

There are 6 metrics.

#### TotalBytesRead - for a given duration total bytes transferred from server to client
#### NumberOfConnections	 - number of connections made for a given duration
#### Duration	       - seconds for which client transacts with the server through s_time tool _ 
#### BytesReadPerConnection - TotalBytesRead/NumberOfConnections
#### NewConnectionThroughput - TotalBytesRead/Duration
#### NewConnectionsPerSec	- NumberOfConnections/Duration

These metrics are reported for every file size that is requested in the profile and for three of the Tls1_3 cybersuites. TLS_AES_128_GCM_SHA256, TLS_AES_256_GCM_SHA384, TLS_CHACHA20_POLY1305_SHA256

Two sets of these metrics are reported, new connection and reuse connection. 

What is the difference between new and reuse connection. 

* New connection
	Every ServerHello - keyExchange happens.

* Reuse connection
	First ServerHello, keyExchange happens and this is saved on the client end and then on ClientHello sends these learned keys and server acknowledges them. 
	As a result the number of connections and throughput can be noticed slightly higher in reuse scenario.


| ScenarioName	                         | MetricName	               | MetricUnit	      | max_MetricValue |
|----------------------------------------|-----------------------------|------------------|-----------------|
| tls_client_chacha20-poly1305-sha256-1k | TotalBytesRead	           | bytes	          | 14193720		|
| tls_client_chacha20-poly1305-sha256-1k | NumberOfConnections	       | count	          | 13290			|
| tls_client_chacha20-poly1305-sha256-1k | Duration	                   | seconds          | 31				|
| tls_client_chacha20-poly1305-sha256-1k | BytesReadPerConnection	   | bytes/connection | 1068			|
| tls_client_chacha20-poly1305-sha256-1k | NewConnectionThroughput	   | bytes/sec	      | 457861.935483871|
| tls_client_chacha20-poly1305-sha256-1k | NewConnectionsPerSec	       | count	          | 428.709677419355|
| tls_client_chacha20-poly1305-sha256-1k | ReuseTotalBytesRead	       | bytes	          | 24037476		|
| tls_client_chacha20-poly1305-sha256-1k | ReuseNumberOfConnections	   | count	          | 22507			|
| tls_client_chacha20-poly1305-sha256-1k | ReuseDuration	           | seconds	      | 31				|
| tls_client_chacha20-poly1305-sha256-1k | ReuseBytesReadPerConnection | bytes/connection |	1068			|
| tls_client_chacha20-poly1305-sha256-1k | ReuseConnectionThroughput   | bytes/sec        | 775402.451612903|
| tls_client_chacha20-poly1305-sha256-1k | ReuseConnectionsPerSec	   | count	          | 726.032258064516|

# Additional utilities

## 1. server side cert/key generation 
keys and certs are generated through this command.
```bash
openssl req -x509 -newkey rsa:2048 -keyout server.key -out server.crt -days 3650 -nodes
```
[please note this cert is set to expire after 10 years i.e., 3650 days]

## 2.  html file generation
```bash
#!/bin/bash

# Usage: ./generate_html.sh filename size_in_kb
# Example: ./generate_html.sh test.html 1024

FILENAME=$1
SIZE_KB=$2
SIZE_BYTES=$((SIZE_KB * 1024))

HEADER="<html>
<head><title>Test File</title></head>
<body>
"
FOOTER="</body>
</html>"

# Calculate current size of header + footer
HEADER_SIZE=$(echo -n "$HEADER$FOOTER" | wc -c)
PADDING_SIZE=$((SIZE_BYTES - HEADER_SIZE))

if [ $PADDING_SIZE -le 0 ]; then
  echo "Requested size too small. Minimum size is $HEADER_SIZE bytes."
  exit 1
fi

# Generate padding
PADDING="<!-- $(head -c $((PADDING_SIZE - 12)) < /dev/zero | tr '\0' 'A') -->"

# Write to file
{
echo "$HEADER"
echo "$PADDING"
echo "$FOOTER"
} > "$FILENAME"

echo "Generated $FILENAME with size approximately ${SIZE_KB}KB."
```

# Reference

* [OpenSSL GitHub](https://github.com/openssl/openssl)
* [OpenSSL Documentation](https://www.openssl.org/)
* [OpenSSL s_server](https://docs.openssl.org/3.3/man1/openssl-s_server/)
* [OpenSSL s_time](https://docs.openssl.org/3.3/man1/openssl-s_time/)

# Technical Debt:

Evaluate what is the ideal buffer size to measure, currently the profile has sizes from 1KiB - 512MiB
Evalueate ideal duration to use, currently 30 seconds is used. 
