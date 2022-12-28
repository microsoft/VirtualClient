# NAS Parallel
The NAS Parallel Benchmarks (NPB) are a small set of programs designed to help evaluate the performance of parallel supercomputers.
It supports both single and multi machine scanerio.
In multi machine scanerio we can have 1 client to N server environment.

The Message Passing Interface(MPI) is used to communicate over the network for running on multiple machines.
For single machine Open Multi-Processing(OpenMP) is used for parallel programming.

* [NAS Parallel](https://www.nas.nasa.gov/software/npb.html)
* [About NAS](https://www.nas.nasa.gov/aboutnas/about.html)

### What is Being Tested?

The following workload tests the High Performance Computing(HPC)/Parallel Computing capabilities of system.
Using MPI(For Multiple machines configuration) & OpenMP (For single machine configuration using threads).

Depending on the configuration of the workload different pieces of hardware are tested. If the hardware that is being tested has [Infiniband](https://www.mellanox.com/pdf/whitepapers/IB_Intro_WP_190.pdf)
hardware and drivers installed the Infiniband infastruture is tested; if it is not present regular network channels are tested.  
The list of benchmarks that are currently ran as part of the NAS Parallel workload are:

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

### System Requirements
It is recommended to run this workload on machines with infiniband drivers and hardware. There are other more exhaustive workloads for testing network performance for non-HPC machines in the Virtual Client.

It is also required that all machines under test can communicate freely over port 22.

### Supported Platforms
* Linux x64 (Debian, Ubuntu)
* Linux arm64 (Debian, Ubuntu)

### Package Dependencies
The following package dependencies are required to be installed on the Unix/Linux system in order to support the requirements
of the NAS Parallel workload. Note that the Virtual Client will handle the installation of any required dependencies.

* libopenmpi-dev
* make
* openmpi-bin
* gfortran

### The Steps to Build Binaries

1. Create "suite.def" & "make.def" using the templates given for example.
   
   a. For Multiple machines go to folder "NPI-MPI/config" folder.
   
   b. For Single machine go to folder "NPI-OMP/config" folder.

2. Build benchmarks using `make suite` command.
   
   a. For Multiple machines build benchmarks in 'NPB-MPI' folder by going to it as working directory.
   
   b. For Single machine build benchmarks in 'NPB-OMP' folder by going to it as working directory.

3. Set Environment variable using the command :
`export OMP_NUM_THREADS=<available_number_of_physical_cores>`
For example : `export OMP_NUM_THREADS=4`

4. For Multiple machines setup passwordless SSH using the blog info on [link](https://linuxize.com/post/how-to-setup-passwordless-ssh-login/). From 1 Client to all Servers.

5. a. For Multiple machine to run a benchmark run command `sudo runuser -l <Username> -c 'mpiexec -np <Number_of_Machines> -host <Host_IP_Address> <benchmarkpath>'`
   
   b. For Single machine to run a benchmark run command `sudo <benchmarkpath>'`

### Common Debug Issues

1. Before downloading any dependency run command `sudo apt-get update` .

2. Before running `mpiexec` setup passwordless SSH. OpenMPI does not works without it.

3. `sudo mpiexec` does not works because it uses 'root' user which does not have passwordless login permission.

4. To run through sudo use `sudo runuser -l <Username> ... `