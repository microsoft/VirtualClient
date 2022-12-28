# HPCG
HPCG is a software package that performs a fixed number of multigrid preconditioned (using a symmetric Gauss-Seidel smoother) conjugate gradient (PCG) 
iterations using double precision (64 bit) floating point values.

The HPCG rating is is a weighted GFLOP/s (billion floating operations per second) value that is composed of the operations performed in the PCG iteration 
phase over the time taken. The overhead time of problem construction and any modifications to improve performance are divided by 500 iterations (the amortization weight) 
and added to the runtime.

* [HPCG Official Site](https://hpcg-benchmark.org/)
* [HPCG Github](https://github.com/hpcg-benchmark/hpcg/)  
* [Spack Github](https://github.com/spack/spack)


## Setup
VirtualClient uses spack to install and load the HPCG binaries. Currently VirtualClient runs HPCG with openmpi support.

Example of VC's HPCG run script
```
. /home/vcvmadmin/VirtualClient/packages/spack/share/spack/setup-env.sh
spack install --reuse -n -y hpcg %gcc +openmp target=zen2 ^openmpi@4.1.1
spack load hpcg %gcc
mpirun --np 4 --use-hwthread-cpus --allow-run-as-root
```

Example of VC's hpcg.dat file
```
HPCG benchmark input file
HPC Benchmarking team, Microsoft Azure
104 104 104
1800
```

### Supported Platforms

* Linux x64
* Linux arm64

### Package Dependencies
* Spack Package Management
