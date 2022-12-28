# LAPACK
LAPACK 3.10.0 is an open-source set of libraries written in Fortran 90 and provides routines for solving systems of simultaneous linear equations,
least-squares solutions of linear systems of equations, eigenvalue problems, and singular value problems. 
It has been designed to be efficient on a wide range of modern high-performance computers.

This toolset was compiled from the official website and modified so that it is easier to integrate into VirtualClient.

* [LAPACK Offical Website](http://www.netlib.org/lapack/)
* [LAPACK Github](https://github.com/Reference-LAPACK/lapack)
* [LAPACK Installation Guide](http://www.netlib.org/lapack/lawnspdf/lawn41.pdf)

-----------------------------------------------------------------------

### What is Being Tested?
LAPACK is designed to be a very simple benchmarking tool. It produces the amount of time it takes to test a set of LAPACK driver routines
under different precisions such as single precision, double, complex, complex double. 

| Metric Name                          | Description                                                           |
|--------------------------------------|----------------------------------------------------------------------|
| compute_time_LIN_Single_Precision    | Time for testing Linear equation test routines under single precision|
| compute_time_LIN_Double_Precision    | Time for testing Linear equation test routines under double precision    |
| compute_time_LIN_Complex             | Time for testing Linear equation test routines under complex precision     |
| compute_time_LIN_Complex_Double      | Time for testing Linear equation test routines under complex double precision    |
| compute_time_EIG_Single_Precision    | Time for testing Eigensystem test routines under single precision     |
| compute_time_EIG_Double_Precision    | Time for testing Eigensystem test routines under double precision   |
| compute_time_EIG_Complex             | Time for testing Eigensystem test routines under complex precision      |
| compute_time_EIG_Complex_Double      | Time for testing Eigensystem test routines under complex double precision    |

-----------------------------------------------------------------------

### Supported Platforms
* Linux x64
* Linux arm64
* Windows x64
* Windows arm64
