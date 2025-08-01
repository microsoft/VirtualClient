# Prime95
Prime95 has been a popular choice for stress/torture testing a CPU since its introduction, especially with overclockers 
and system builders. The software feeds the the processor a barrage of integer and floating-point calculations that can be
consistently verified with the goal of testing the stability of the CPU and L1/L2/L3 processor caches. Additionally, it utilizes all of the cores
on the system to ensure a consistently high stress test environment.

Prime95 is designed to run indefinately on a system till any error is encountered. The workload is onboarded to the Virtual Client with the goal
of supporting a dual-purpose: to test the performance/timing of the CPU in computing the calculations while also placing it under stress.

* [Prime95 Documentation](https://www.mersenne.org/download/)

## Reasons Why Prime95 Workload Might Find Problems?
If system is not being overclocked, the most likely cause is memory. It is not uncommon
for memory to not run correctly at its rated speed (incorrectly "binned").  This is
most easily tested by swapping it with memory from another compatible computer and
retesting. Overheating is another possible source of problems. CPU temperature can be
monitored to make sure it is under the limits.

Occasionally, the power supply is incapable of supplying sufficient power to the
system under heavy load, this can be diagnosed by monitoring the 12v, 5v and
3.3v voltages - there might be a substantial drop in these voltages when
putting the system under load and generally means the PSU itself needs to be replaced
with a more capable unit.

If system is being overclocked, the most likely problems are either the CPU core
voltage being set too low or drooping too far under heavy low. Another frequently seen
issue is the motherboard failing to set a suitable voltage for the memory controller
when an XMP profile is enabled.

The above are just some possible causes, and actual problem might require thorough diagnosis.

It might be noted that the faster prime95 finds a hardware error the more likely it is
that other programs running on the system will experience problems.

## What is Being Measured?
The Prime95 "torture test" continuously stresses the CPU on the system with calculations for primality across varying FFT size. It checks whether 
the calculations are within certain parameters while they are in progress and compares the computer's final results to results that are known to be 
correct. The time-to-compute the calculations is captured as well. Any mismatch is treated as an error indicating Hardware issues.

The following list describes the measurements captured by the workload running across different FFT sizes:

* Number of Tests passed.
* Number of Tests failed.

## Workload Metrics
The following metrics are examples of those captured by the Virtual Client when running the Prime95 workload.

:::info
*Note that if the failed test count is greater than 0, it denotes an overall Prime95 test failure and some harware error. The test time is the time 
for which the system was stressed with torture test. A higher the test time without error typically indicates more confidence in Prime95 results.*
:::

| Metric Name  | Example Value (min) | Example Value (max) | Example Value (avg) | Unit |
|--------------|---------------------|---------------------|---------------------|------|
| failTestCount | 0.0 | 0.0 | 0.0 |  |
| passTestCount | 32.0 | 192.0 | 115.45833333333333 |  |