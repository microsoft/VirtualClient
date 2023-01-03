# SPECcpu (coming soon...)
SPEC CPU is a workload created and licensed by the Standard Performance Evalution Corporation. The SPEC CPU® 2017 benchmark package contains SPEC's 
next-generation, industry-standardized, CPU intensive suites for measuring and comparing compute intensive performance, stressing a system's processor, 
memory subsystem and compiler.

* [SPEC CPU Documentation](https://www.spec.org/cpu2017/)  
* [SPEC CPU 2017 QuickStart](https://spec.org/cpu2017/Docs/quick-start.html)  
* [SPEC CPU Download](https://pro.spec.org/private/osg/cpu/cpu2017/src/)  
* [SPEC CPU Benchmarks](https://www.spec.org/cpu2017/Docs/overview.html#Q13)

:::caution Not Supported Yet...
*This workload is supported but not yet made available in the Virtual Client package store. The Virtual Client team is currently working to define and document the process for integration 
of commercial workloads into the Virtual Client that require purchase and/or license. Please bear with us while we are figuring this process out.*
:::

## System Requirements
The following section provides special considerations required for the system on which the SPEC CPU workload will be run.

https://spec.org/cpu2017/Docs/system-requirements.html

* Physical Memory = 16 GB minimum  
* Disk Space = 250 GB minimum on the OS disk

## What is Being Measured?
SPEC teams designed these suites to provide a comparative measure of compute-intensive performance across the widest practical range of hardware 
using workloads developed from real user applications. The benchmarks are provided as source code and require the use of compiler commands 
as well as other commands via a shell or command prompt window. SPEC CPU 2017 also includes an optional metric for measuring energy consumption.

The SPEC CPU 2017 benchmark package contains 43 benchmarks, organized into four distinct workload suites:
* SPECspeed® 2017 Integer
  * Used for measure the time required for the computer to complete single integer calculations.
* SPECspeed® 2017 Floating Point 
  * Used to measure the time required for the computer to complete single floating-point calculations.
* SPECrate® 2017 Integer  
  * Measure the throughput or work per unit of time on the computer for integer calculations.
* SPECrate® 2017 Floating Point
  * Measure the throughput or work per unit of time on the computer for floating-point calculations.

## Workload Metrics
The following metrics are examples of those captured by the Virtual Client when running the SPECcpu workload.

| Scenario | Metric Name | Example Value (min) | Example Value (max) | Example Value (avg) | Unit |
|-----------|-------------|---------------------|---------------------|---------------------|------|
| INTspeed | [SPECcpu-base-600.perlbench_s](https://www.spec.org/cpu2017/Docs/benchmarks/600.perlbench_s.html)| 5.05 | 5.44 | 5.291236749116601 | Score |
| INTspeed | [SPECcpu-base-602.gcc_s](https://www.spec.org/cpu2017/Docs/benchmarks/602.gcc_s.html) | 7.07 | 7.68 | 7.355406360424022 | Score |
| INTspeed | [SPECcpu-base-605.mcf_s](https://www.spec.org/cpu2017/Docs/benchmarks/605.mcf_s.html) | 5.07 | 5.98 | 5.436749116607786 | Score |
| INTspeed | [SPECcpu-base-620.omnetpp_s](https://www.spec.org/cpu2017/Docs/benchmarks/620.omnetpp_s.html) | 3.81 | 5.51 | 5.185088339222612 | Score |
| INTspeed | [SPECcpu-base-623.xalancbmk_s](https://www.spec.org/cpu2017/Docs/benchmarks/623.xalancbmk_s.html) | 3.04 | 3.53 | 3.3250176678445224 | Score |
| INTspeed | [SPECcpu-base-625.x264_s](https://www.spec.org/cpu2017/Docs/benchmarks/625.x264_s.html) | 3.62 | 3.78 | 3.7309893992932956 | Score |
| INTspeed | [SPECcpu-base-631.deepsjeng_s](https://www.spec.org/cpu2017/Docs/benchmarks/631.deepsjeng_s.html) | 3.49 | 3.65 | 3.5914487632508944 | Score |
| INTspeed | [SPECcpu-base-641.leela_s](https://www.spec.org/cpu2017/Docs/benchmarks/641.leela_s.html) | 3.32 | 3.45 | 3.419363957597164 | Score |
| INTspeed | [SPECcpu-base-648.exchange2_s](https://www.spec.org/cpu2017/Docs/benchmarks/648.exchange2_s.html) | 6.65 | 6.95 | 6.884946996466445 | Score |
| INTspeed | [SPECcpu-base-657.xz_s](https://www.spec.org/cpu2017/Docs/benchmarks/657.xz_s.html) | 9.24 | 10.3 | 9.804134275618383 | Score |
| INTspeed | SPECspeed(R)2017_int_base | 4.82 | 5.2 | 5.07127208480564 | Score |
| INTrate | [SPECcpu-base-500.perlbench_r](https://www.spec.org/cpu2017/Docs/benchmarks/500.perlbench_r.html) | 16.0 | 16.8 | 16.39600000000001 | Score |
| INTrate | [SPECcpu-base-502.gcc_r](https://www.spec.org/cpu2017/Docs/benchmarks/502.gcc_r.html) | 18.4 | 19.8 | 18.812666666666673 | Score |
| INTrate | [SPECcpu-base-505.mcf_r](https://www.spec.org/cpu2017/Docs/benchmarks/505.mcf_r.html) | 16.5 | 18.1 | 16.872666666666679 | Score |
| INTrate | [SPECcpu-base-520.omnetpp_r](https://www.spec.org/cpu2017/Docs/benchmarks/520.omnetpp_r.html) | 10.1 | 11.5 | 10.736666666666674 | Score |
| INTrate | [SPECcpu-base-523.xalancbmk_r](https://www.spec.org/cpu2017/Docs/benchmarks/523.xalancbmk_r.html) | 9.55 | 10.3 | 10.016400000000007 | Score |
| INTrate | [SPECcpu-base-525.x264_r](https://www.spec.org/cpu2017/Docs/benchmarks/525.x264_r.html) | 14.4 | 14.8 | 14.65066666666666 | Score |
| INTrate | [SPECcpu-base-531.deepsjeng_r](https://www.spec.org/cpu2017/Docs/benchmarks/531.deepsjeng_r.html) | 14.0 | 15.5 | 15.25466666666666 | Score |
| INTrate | [SPECcpu-base-541.leela_r](https://www.spec.org/cpu2017/Docs/benchmarks/541.leela_r.html) | 14.7 | 15.6 | 15.44600000000001 | Score |
| INTrate | [SPECcpu-base-548.exchange2_r](https://www.spec.org/cpu2017/Docs/benchmarks/548.exchange2_r.html) | 22.0 | 24.0 | 23.70733333333333 | Score |
| INTrate | [SPECcpu-base-557.xz_r](https://www.spec.org/cpu2017/Docs/benchmarks/557.xz_r.html) | 13.7 | 14.7 | 14.183333333333318 | Score |
| INTrate | SPECrate(R)2017_int_base | 14.9 | 15.7 | 15.179999999999988 | Score |
| FPspeed | [SPECcpu-base-603.bwaves_s](https://www.spec.org/cpu2017/Docs/benchmarks/603.bwaves_s.html) | 22.9 | 27.0 | 26.577027027027027 | Score |
| FPspeed | [SPECcpu-base-607.cactuBSSN_s](https://www.spec.org/cpu2017/Docs/benchmarks/607.cactuBSSN_s.html) | 22.0 | 23.1 | 22.409459459459474 | Score |
| FPspeed | [SPECcpu-base-619.lbm_s](https://www.spec.org/cpu2017/Docs/benchmarks/619.lbm_s.html) | 4.45 | 8.34 | 8.101216216216216 | Score |
| FPspeed | [SPECcpu-base-621.wrf_s](https://www.spec.org/cpu2017/Docs/benchmarks/621.wrf_s.html) | 13.3 | 15.4 | 15.07432432432433 | Score |
| FPspeed | [SPECcpu-base-627.cam4_s](https://www.spec.org/cpu2017/Docs/benchmarks/627.cam4_s.html) | 11.6 | 12.0 | 11.767567567567568 | Score |
| FPspeed | [SPECcpu-base-628.pop2_s](https://www.spec.org/cpu2017/Docs/benchmarks/628.pop2_s.html) | 12.7 | 14.2 | 13.904054054054047 | Score |
| FPspeed | [SPECcpu-base-638.imagick_s](https://www.spec.org/cpu2017/Docs/benchmarks/638.imagick_s.html) | 1.9 | 1.97 | 1.9393243243243255 | Score |
| FPspeed | [SPECcpu-base-644.nab_s](https://www.spec.org/cpu2017/Docs/benchmarks/644.nab_s.html) | 20.5 | 21.3 | 20.995945945945946 | Score |
| FPspeed | [SPECcpu-base-649.fotonik3d_s](https://www.spec.org/cpu2017/Docs/benchmarks/649.fotonik3d_s.html) | 11.4 | 21.1 | 20.560810810810805 | Score |
| FPspeed | [SPECcpu-base-654.roms_s](https://www.spec.org/cpu2017/Docs/benchmarks/654.roms_s.html) | 14.1 | 16.5 | 15.964864864864867 | Score |
| FPspeed | SPECspeed(R)2017_fp_base | 11.3 | 13.4 | 13.18108108108109 | Score |
| FPrate | [SPECcpu-base-503.bwaves_r](https://www.spec.org/cpu2017/Docs/benchmarks/503.bwaves_r.html) | 54.7 | 57.6 | 56.76533333333332 | Score |
| FPrate | [SPECcpu-base-507.cactuBSSN_r](https://www.spec.org/cpu2017/Docs/benchmarks/507.cactuBSSN_r.html) | 14.6 | 15.7 | 15.186666666666673 | Score |
| FPrate | [SPECcpu-base-508.namd_r](https://www.spec.org/cpu2017/Docs/benchmarks/508.namd_r.html) | 12.1 | 12.7 | 12.536000000000005 | Score |
| FPrate | [SPECcpu-base-510.parest_r](https://www.spec.org/cpu2017/Docs/benchmarks/510.parest_r.html) | 19.6 | 20.4 | 20.148000000000005 | Score |
| FPrate | [SPECcpu-base-511.povray_r](https://www.spec.org/cpu2017/Docs/benchmarks/511.povray_r.html) | 15.9 | 16.9 | 16.657333333333339 | Score |
| FPrate | [SPECcpu-base-519.lbm_r](https://www.spec.org/cpu2017/Docs/benchmarks/519.lbm_r.html) | 10.3 | 11.2 | 10.857333333333328 | Score |
| FPrate | [SPECcpu-base-521.wrf_r](https://www.spec.org/cpu2017/Docs/benchmarks/521.wrf_r.html) | 17.8 | 18.5 | 18.244 | Score |
| FPrate | [SPECcpu-base-526.blender_r](https://www.spec.org/cpu2017/Docs/benchmarks/526.blender_r.html) | 18.8 | 19.4 | 19.101333333333323 | Score |
| FPrate | [SPECcpu-base-527.cam4_r](https://www.spec.org/cpu2017/Docs/benchmarks/527.cam4_r.html) | 15.6 | 16.1 | 15.981333333333329 | Score |
| FPrate | [SPECcpu-base-538.imagick_r](https://www.spec.org/cpu2017/Docs/benchmarks/538.imagick_r.html) | 21.1 | 21.7 | 21.482666666666668 | Score |
| FPrate | [SPECcpu-base-544.nab_r](https://www.spec.org/cpu2017/Docs/benchmarks/544.nab_r.html) | 21.2 | 21.8 | 21.581333333333335 | Score |
| FPrate | [SPECcpu-base-549.fotonik3d_r](https://www.spec.org/cpu2017/Docs/benchmarks/549.fotonik3d_r.html) | 28.5 | 30.4 | 29.62 | Score |
| FPrate | [SPECcpu-base-554.roms_r](https://www.spec.org/cpu2017/Docs/benchmarks/554.roms_r.html) | 13.4 | 14.4 | 13.927999999999992 | Score |
| FPrate | SPECrate(R)2017_fp_base | 18.5 | 19.3 | 18.99866666666667 | Score |