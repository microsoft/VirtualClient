# OpenFOAM
OpenFOAM is a free, open source computational fluid dynamics (CFD) software package released free and open-source under the GNU General Public License 
through www.openfoam.com. It has a large user base across most areas of engineering and science, from both commercial and 
academic organisations. OpenFOAM has an extensive range of features to solve anything from complex fluid flows involving 
chemical reactions, turbulence and heat transfer, to solid dynamics and electromagnetics.

* [OpenFOAM](https://www.openfoam.com/)
* [OpenFOAM Wiki](https://openfoamwiki.net/)
* [OpenFOAM Getting Started](https://openfoamwiki.net/index.php/Tutorials/Before_Getting_Started)
* [Computational Fluid Dynamics](https://en.wikipedia.org/wiki/Computational_fluid_dynamics)
* [Tutorials](https://www.youtube.com/c/OpenFOAMTutorials/videos)
* [Arm64 Binaries Details](https://packages.ubuntu.com/search?keywords=openfoam)

## What is Being Measured?
OpenFOAM is used to measure performance for completing a number of CFD simulations in a minute of time (i.e. iterations per minute). The following set
of simulations are onboarded:

| Simulation Name   |   Solver              | Notes |
|-------------------|-----------------------|-------|
| motorBike         |  simpleFoam           | On linux-x64 platforms only. |
| pitzDaily         |  simpleFoam           |  |
| airFoil2D         |  simpleFoam           |  |
| elbow             |  icoFoam              |  |
| lockExchange      |  twoLiquidMixingFoam  |  |

## Workload Metrics
The following metrics are examples of those captured by the Virtual Client when running the OpenFOAM workload.

| Scenario  | Metric Name | Example Value (min) | Example Value (max) | Example Value (avg) | Unit |
|-----------|-------------|---------------------|---------------------|---------------------|------|
| pitzDaily | Iterations/min | 1575.48 | 1600.37 | 1690.7 | itrs/min |
| airFoil2D | Iterations/min | 2413.6 | 2435.79 | 2420.9 | itrs/min |
| elbow | Iterations/min | 17518.9 | 17605.5 | 16556.7 | itrs/min |
| motorbike | Iterations/min | 17.70 | 17.71 | 17.72 | itrs/min |
| lockExchange | Iterations/min | 32.25 | 32.27 | 32.30 | itrs/min |
