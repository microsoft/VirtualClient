# Prime95 Workload

Prime95 has been a popular choice for stress / torture testing a CPU since its introduction,
especially with overclockers and system builders. Since the software makes heavy use of the processor's
integer and floating point instructions, it feeds the processor a consistent and verifiable workload to
test the stability of the CPU and the L1/L2/L3 processor cache. Additionally, it uses all of the cores
of a multi-CPU / multi-core system to ensure a high-load stress test environment.

Prime95 is designed to run indefinately on a system till any error is encountered. Prime95 in
VirtualClient supports a parameter for custom timeout to stress test for requisite amount of time.

### What metrics are being captured?

The Prime95 Torture Test continuously stresses the system with calculations for primality testing with
varying FFT size. It checks whether the calculations are within certain parameters while they are in progress
and compares the computer's final results to results that are known to be correct.

Any mismatch is treated as an error indicating Hardware issues.

Prime95 gives a Pass/Fail verdict while testing with variations in FFT size. The metrics being captured
in Virtual Client for Prime95 are the following:

* Number of Tests passed with different FFT Sizes

* Number of Tests failed with different FFT Sizes

* Prime95 Test Runtime (Unit: seconds)

Prime95 also supports some command line arguments for fine grained control stress testing. The details of parameters
are mentioned in [Workload Profile Documentation](./prime95-profiles.md)

The ScenarioName in results also captures the input parameters.

Example ScenarioName: Prime95Workload_60mins_4K-8192K_8threads
here, Prime95 Workload was supposed to run for 60 minutes on 8 threads, with FFT Size between 4K and 8192K.

### Supported Platforms
* linux-x64
* win-x64

### Package Dependencies
Prime95 binary along with all required dlls are included in the package. There is no extra dependency.
The binaries were downloaded from GIMPS Prime95 Documentation website, mentioned above.

### Documentation
[GIMPS Prime95 Documentation](https://www.mersenne.org/download/)

### What to do if Prime95 finds a problem in system?

The exact cause of a hardware problem will require a thorough diagnosis.

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