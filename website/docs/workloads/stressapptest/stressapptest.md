# StressAppTest
Stressful Application Test (or stressapptest, its unix name) is a memory interface test.
It tries to maximize randomized traffic to memory from processor and I/O, with the intent of creating a realistic high load situation in order
to test the existing hardware devices in a computer. It has been used at Google for some time and now it is available under the apache 2.0 license.

* [StressAppTest Official Documentation](https://github.com/stressapptest/stressapptest/blob/master/README.md)

## Overview
stressapptest is a userspace test, primarily composed of threads doing memory copies and directIO disk read/write. It allocates a large block of
memory (typically 94% of the total memory on the machine), and each thread will choose randomized blocks of memory to copy, or to write to disk.
Typically there are two threads per processor, and two threads for each disk. Result checking is done as the test proceeds by CRCing the data as
it is copied.

Please note that the StressAppTest implementation in Microsoft Virtual Client uses binaries compiled on latest available source code in Nov 2022
on linux-x64 and linux-arm64

* [StressAppTest GitHub Source Code](https://github.com/stressapptest/stressapptest)

## Caveats

This test works by stressing system interfaces. It is good at catching memory signal integrity or setup and hold problems, memory controller and
bus interface issues, and disk controller issues. It is moderately good at catching bad memory cells and cache coherency issues. It is not good
at catching bad processors, bad physical media on disks, or problems that require periods of inactivity to manifest themselves. It is not a
thorough test of OS internals. The test may cause marginal systems to become bricks if disk or memory errors cause hard drive corruption, or
if the physical components overheat.

## Workload Metrics
The following metrics are examples of those captured by the Virtual Client when running the StressAppTest workload.

:::info
*Note that if the hardwareErrorCount is greater than 0, it denotes an overall StressAppTest failure and some harware error, possibly in the DIMM.
The DIMM Slot and other details, as captured by StressAppTest, is added as a "Tag" of the hardwareErrorCount metric, for ease of debugging.*
:::

| Metric Name  | Example Value |
|--------------|---------------|
| hardwareErrorCount | 15 |
