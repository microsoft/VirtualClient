# LAPACK
LAPACK 3.10.0 is an open-source set of libraries written in Fortran 90 and provides routines for solving systems of simultaneous linear equations,
least-squares solutions of linear systems of equations, eigenvalue problems, and singular value problems. 
It has been designed to be efficient on a wide range of modern high-performance computers.

This toolset was compiled from the official website and modified so that it is easier to integrate into VirtualClient.

* [LAPACK Official Website](http://www.netlib.org/lapack/)
* [LAPACK Github](https://github.com/Reference-LAPACK/lapack)
* [LAPACK Installation Guide](http://www.netlib.org/lapack/lawnspdf/lawn41.pdf)

## What is Being Measured?
LAPACK is designed to be a very simple benchmarking tool. It produces the amount of time it takes to test a set of LAPACK driver routines
under different precisions such as single precision, double, complex, complex double. 

* Compute times for linear algorithms (single-precision)
* Compute times for linear algorithms (double-precision)
* Compute times for linear algorithms (complex data type, single precision)
* Compute times for linear algorithms (complex data type, double complex precision)
* Compute times for Eigenvalue problems (single-precision)
* Compute times for Eigenvalue problems (double-precision)
* Compute times for Eigenvalue problems (complex data type, single precision)
* Compute times for Eigenvalue problems (complex data type, double complex precision)

## Workload Metrics
The following metrics are examples of those captured by the Virtual Client when running the LAPACK workload.

| Metric Name | Example Value (min) | Example Value (max) | Example Value (avg) | Unit | Description |
|-------------|---------------------|---------------------|---------------------|------|-------------|
| compute_time_EIG_Complex | 5.229999999999999 | 20.729999999999998 | 7.753214694064213 | seconds |
| compute_time_EIG_Complex_Double | 7.029999999999999 | 32.39000000000001 | 10.280553078192105 | seconds |
| compute_time_EIG_Double_Precision | 4.709999999999998 | 12.759999999999998 | 6.071805752927895 | seconds |
| compute_time_EIG_Single_Precision | 3.3399999999999996 | 10.229999999999999 | 4.584710148478463 | seconds |
| compute_time_LIN_Complex | 4.23 | 26.78 | 7.305731430530846 | seconds |
| compute_time_LIN_Complex_Double | 5.22 | 42.89 | 9.374530150384569 | seconds |
| compute_time_LIN_Double_Precision | 1.95 | 5.84 | 2.894817693450652 | seconds |
| compute_time_LIN_Single_Precision | 1.92 | 6.67 | 2.8232810280358646 | seconds |
