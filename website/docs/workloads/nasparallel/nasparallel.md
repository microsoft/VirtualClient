# NAS Parallel
The NAS Parallel Benchmarks (NPB) are a small set of programs designed to help evaluate the performance of parallel supercomputers.
It supports both single and multi machine scanerio.
In multi machine scanerio we can have 1 client to N server environment.

The Message Passing Interface(MPI) is used to communicate over the network for running on multiple machines.
For single machine Open Multi-Processing(OpenMP) is used for parallel programming.

* [NAS Parallel](https://www.nas.nasa.gov/software/npb.html)
* [About NAS](https://www.nas.nasa.gov/aboutnas/about.html)

## System Requirements
It is recommended to run this workload on machines with infiniband drivers and hardware. There are other more exhaustive workloads for testing network 
performance for non-HPC machines in the Virtual Client. It is also required that all machines under test can communicate freely over port 22.

## What is Being Measured?
The following workload tests the High Performance Computing(HPC)/Parallel Computing capabilities of system. Using MPI(For Multiple machines configuration) 
and OpenMP (for single-system configurations using threads).

Depending on the configuration of the workload different pieces of hardware are tested. If the hardware that is being tested has [Infiniband](https://www.mellanox.com/pdf/whitepapers/IB_Intro_WP_190.pdf),
then the hardware and drivers installed for the Infiniband infastruture are tested. If these are not present, regular network stacks/channels are tested.  

The list of benchmarks that are currently executed as part of the NAS Parallel workload include the following:

| Benchmark | Description |
|-----------|-------------|
| IS | Integer Sort, random memory access |
| EP | Embarrassingly Parallel |
| CG | Conjugate Gradient, irregular memory access and communication |
| MG | Multi-Grid on a sequence of meshes, long- and short-distance communication, memory intensive |
| FT | discrete 3D fast Fourier Transform, all-to-all communication |
| BT | Block Tri-diagonal solver  |
| SP | Scalar Penta-diagonal solver  |
| LU | Lower-Upper Gauss-Seidel solver |
| DC | Data Cube |
| DT | Data Traffic |
| UA | Unstructured Adaptive mesh, dynamic and irregular memory access |

## Workload Metrics
The following metrics are examples of those captured by the Virtual Client when running the NAS Parallel Benchmarks workloads.

| Scenario Name   | Metric Name | Example Value (min) | Example Value (max) | Example Value (avg) | Unit |
|-----------------|-------------|---------------------|---------------------|---------------------|------|
| MPI bt.D.x | ExecutionTime | 21671.77 | 30122.8 | 25266.78541666667 | Seconds |
| MPI bt.D.x | Mop/s total | 1936.6 | 2691.79 | 2325.0579166666668 | Mop/s |
| MPI bt.D.x | Mop/s/process | 968.3 | 1345.89 | 1162.5279166666667 | Mop/s |
| MPI cg.D.x | ExecutionTime | 3152.5 | 6436.32 | 3843.4735714285718 | Seconds |
| MPI cg.D.x | Mop/s total | 565.99 | 1155.56 | 977.1558928571428 | Mop/s |
| MPI cg.D.x | Mop/s/process | 283.0 | 577.78 | 488.5780952380952 | Mop/s |
| MPI ep.D.x | ExecutionTime | 658.63 | 1983.27 | 850.881388888889 | Seconds |
| MPI ep.D.x | Mop/s total | 69.3 | 208.68 | 186.3202777777778 | Mop/s |
| MPI ep.D.x | Mop/s/process | 34.65 | 104.34 | 93.15993055555555 | Mop/s |
| MPI ft.D.x | ExecutionTime | 3253.22 | 5572.11 | 4044.5483333333338 | Seconds |
| MPI ft.D.x | Mop/s total | 1608.68 | 2755.34 | 2267.0758333333335 | Mop/s |
| MPI ft.D.x | Mop/s/process | 804.34 | 1377.67 | 1133.5383333333332 | Mop/s |
| MPI is.C.x | ExecutionTime | 3.18 | 18.98 | 4.746808510638298 | Seconds |
| MPI is.C.x | Mop/s total | 70.71 | 422.28 | 340.1694326241135 | Mop/s |
| MPI is.C.x | Mop/s/process | 35.36 | 211.14 | 170.08503546099289 | Mop/s |
| MPI lu.D.x | ExecutionTime | 4289.31 | 6640.01 | 4633.097168141593 | Seconds |
| MPI lu.D.x | Mop/s total | 6008.71 | 9301.7 | 8731.985309734511 | Mop/s |
| MPI lu.D.x | Mop/s/process | 3004.36 | 4650.85 | 4365.992389380531 | Mop/s |
| OMP bt.D.x | ExecutionTime | 1100.69 | 5811.24 | 3684.0739181286546 | Seconds |
| OMP bt.D.x | Mop/s total | 10038.43 | 52999.25 | 17673.181461988304 | Mop/s |
| OMP bt.D.x | Mop/s/thread | 927.77 | 3541.78 | 2612.988596491228 | Mop/s |
| OMP cg.D.x | ExecutionTime | 765.05 | 3576.5 | 1400.8384210526318 | Seconds |
| OMP cg.D.x | Mop/s total | 1018.56 | 4761.68 | 3013.099298245614 | Mop/s |
| OMP cg.D.x | Mop/s/thread | 63.66 | 297.6 | 188.31877192982456 | Mop/s |
| OMP dc.B.x | ExecutionTime | 150.25 | 956.53 | 290.0096078431372 | Seconds |
| OMP dc.B.x | Mop/s total | 0.62 | 3.96 | 2.5537254901960786 | Mop/s |
| OMP dc.B.x | Mop/s/thread | 0.04 | 0.68 | 0.3661764705882352 | Mop/s |
| OMP ep.D.x | ExecutionTime | 83.92 | 338.17 | 312.5922929936306 | Seconds |
| OMP ep.D.x | Mop/s total | 406.41 | 1637.72 | 480.22484076433127 | Mop/s |
| OMP ep.D.x | Mop/s/thread | 25.79 | 103.16 | 80.0843949044586 | Mop/s |
| OMP is.C.x | ExecutionTime | 0.35 | 2.49 | 1.3286624203821658 | Seconds |
| OMP is.C.x | Mop/s total | 539.73 | 3841.83 | 1126.8812101910829 | Mop/s |
| OMP is.C.x | Mop/s/thread | 33.73 | 271.12 | 197.51891719745223 | Mop/s |
| OMP lu.D.x | ExecutionTime | 618.09 | 2992.73 | 1967.2742666666669 | Seconds |
| OMP lu.D.x | Mop/s total | 13331.59 | 64550.85 | 22582.2706 | Mop/s |
| OMP lu.D.x | Mop/s/thread | 961.44 | 4556.67 | 3596.0266666666668 | Mop/s |
| OMP mg.D.x | ExecutionTime | 77.26 | 450.16 | 194.33686274509805 | Seconds |
| OMP mg.D.x | Mop/s total | 6917.2 | 40304.46 | 21151.509411764706 | Mop/s |
| OMP mg.D.x | Mop/s/thread | 432.32 | 2519.03 | 1321.969411764706 | Mop/s |
| OMP sp.D.x | ExecutionTime | 915.28 | 5499.23 | 2607.270882352942 | Seconds |
| OMP sp.D.x | Mop/s total | 5370.93 | 32269.92 | 13349.254705882353 | Mop/s |
| OMP sp.D.x | Mop/s/thread | 474.1 | 2464.48 | 1727.7460784313724 | Mop/s |
| OMP ua.D.x | ExecutionTime | 804.09 | 3669.81 | 2349.879591836735 | Seconds |
| OMP ua.D.x | Mop/s total | 47.36 | 216.12 | 83.12265306122447 | Mop/s |
| OMP ua.D.x | Mop/s/thread | 3.42 | 16.42 | 11.545204081632655 | Mop/s |

## Packaging and Setup
1. Create "suite.def" & "make.def" using the templates given for example.
   a. For Multiple machines go to folder "NPI-MPI/config" folder.
   b. For Single machine go to folder "NPI-OMP/config" folder.

2. Build benchmarks using `make suite` command.
   a. For Multiple machines build benchmarks in 'NPB-MPI' folder by going to it as working directory.
   b. For Single machine build benchmarks in 'NPB-OMP' folder by going to it as working directory.

3. Set Environment variable using the command :
  `export OMP_NUM_THREADS=<available_number_of_physical_cores>` (e.g. `export OMP_NUM_THREADS=4`)

4. For multi-system scenarios setup passwordless SSH. See the blog post [here](https://linuxize.com/post/how-to-setup-passwordless-ssh-login/) for examples. Skip
   this step if you are running a single-system scenario.

5. Run the benchmark:
   a. For multi-system scenarios, run command `sudo runuser -l <Username> -c 'mpiexec -np <Number_of_Machines> -host <Host_IP_Address> <benchmarkpath>'`.
   b. For Single machine to run a benchmark run command `sudo <benchmarkpath>'`.

## Common Issues
* Before downloading any dependency run command `sudo apt-get update` .
* Before running `mpiexec` setup passwordless SSH. OpenMPI does not works without it.
* `sudo mpiexec` does not works because it uses 'root' user which does not have passwordless login permission.
* To run through sudo use `sudo runuser -l <Username> ... `