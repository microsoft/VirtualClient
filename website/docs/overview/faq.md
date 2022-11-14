---
id: faq
sidebar_position: 10
---

# FAQ

## Why we made VirtualClient?
We made VirtualClient because we care deeply about performance at Azure. We have the mission to make sure Azure's performance
brings the best experience to users. The only way to run a wide range of benchmarks across Azure's offerings constantly, 
is with automation. That's how VC is born.


## Why Microsoft open-sourced VirtualClient?
1. Learning a benchmark is hard. Learning hundreds of benchmarks is an impossible task. We want to empower every performance enthusitic person to be able to 
2. We need your contribution. If you can make improvements into how VirtualClient runs some benchmarks, we would really appreciate your contribution. This will enable everyone else around the globe

## Why you should (or not) use VirtualClient
1. VC is so much more than just scripts to automate a workload. The expertise that went into automation is more valuable than automation itself. We understand there is so much to learn about every workload and every piece of hardware. VC is positioned to host the collective wisdom of perf engineers from different fields. We studied how to run every benchmark, and the best configurations to use, so that you don't have to spend your time to learn them.
2. VC has the vision to become the united industrial standard of benchmarking and monitoring systems.
3. 

### When VirtualClient is probably not a good fit
1. You only need one-off testing with a workload you already understand. You might have some scripts that doesn't need much maintainces.
2. You are fine tuning for an absolutely best score from one particular workload, for benchmarking competitions. VirtualClient is designed to measure **general experience** 
from customers' perspective.


## How is VC used at Azure
Quality is top priority for Azure. Azure uses VC to protect the customer from updates that will regress the reliability or performance.

## Will VC support commercial workloads like SPECcpu, GeekBench?
Yes, VC plans to support commercial workloads in the future releases. VirtualClient needs to establish a model to enable the workload automation, without distributing the workloads themselves.

## Can VC be used in disconnected labs

## What if my workload is not supported by VC
1. Look at future release plan
2. Contribute
3. VC extension

## Does VC send any data to MSFT
1. No performance data at all
2. VC Storage will have download log, but you can use your own storage

