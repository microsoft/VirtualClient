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
1. Learning a benchmark is hard. Learning hundreds of benchmarks is an impossible task. At Azure, we understand the complexity to reproduce benchmarking results, or to debug performance degradations. We want to empower every performance enthusitic person to be able to benchmark services and systems a little easier.
2. We need your contribution. If you can make improvements into how VirtualClient runs some benchmarks, we would really appreciate your contribution. This will enable everyone else around the globe use the benchmark you are enthusiastic about more efficiently.

## Why you should (or not) use VirtualClient
1. VC is so much more than just scripts to automate a workload. The expertise that went into automation is more valuable than automation itself. We understand there is so much to learn about every workload and every piece of hardware. VC is positioned to host the collective wisdom of perf engineers from different fields. We studied how to run every benchmark, and the best configurations to use, so that you don't have to spend your time to learn them.
2. VC has the vision to become the united industrial standard of benchmarking and monitoring systems.


#### When VirtualClient is probably not a good fit
1. You only need one-off testing with a workload you already understand. You might have some scripts that doesn't need much maintainences.
2. You are fine tuning for an absolutely best score from one particular workload, for benchmarking competitions. VirtualClient is designed to measure **general experience** 
from customers' perspective.


## How is VC used at Azure
Quality is one of the top priorities for Azure. Azure uses VC to protect the customer from updates that will regress the reliability or performance.

Azure runs large scale AB-testing on firmware, software updates that goes to the fleet. We block updates that measures a noticeable difference between baseline and control groups. This methodology has been protecting Azure customers' experience on Azure's offerings. Now, you can use the tool Azure used, to protect your hardware or software performance. 

## Will VC support commercial workloads like SPECcpu, GeekBench?
Yes, VC plans to support commercial workloads in the future releases. VirtualClient needs to establish a model to enable the workload automation, without distributing the workloads themselves.

## What if my workload is not supported by VC
1. Please look at our [**Roadmap**](./vision.md#roadmap) and see if we are planning for this. Send us a [**Feature Request**](https://github.com/microsoft/VirtualClient/issues/new?assignees=&labels=&template=feature_request.md&title=) if it's not on our radar yet.
2. We welcome your contribution! Please refer to our [**Development Guide**](../developing/develop-guide.md) and help everyone in the world to run one more workload with automation.
3. [**VC supports extensions**](../developing/vc-extension.md). You can develop additional workloads, monitors or dependencies in your own public or private repository.

## Does VC send any data to MSFT
VirtualClient does not collect any of your benchmark data and upload to Microsoft. When benchmarking at scale, and leveraging VC's telemetry capabilities, users need to explicitly provide a connection string, that points to a user-owned Azure Data Explorer cluster. VirtualClient does host a Azure storage account to host the benchmark binaries or source. The only information VirtualClient team could infer from usage, is the download traces from Azure storage account.

