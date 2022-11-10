
# OpenFOAM
OpenFOAM is a free, open source computational fluid dynamics (CFD) software package released free and open-source under the GNU General Public License 
through www.openfoam.com. It has a large user base across most areas of engineering and science, from both commercial and 
academic organisations. OpenFOAM has an extensive range of features to solve anything from complex fluid flows involving 
chemical reactions, turbulence and heat transfer, to solid dynamics and electromagnetics.

### Documentation
* [OpenFOAM](https://www.openfoam.com/)

### Package Details
* [Workload Package Details](../VirtualClient.Documentation/DependencyPackages.md)

### Simulations Onboarded For linux-x64

| Simulation Name                                  |   Solver     |
|--------------------------------------|-----------|
| motorBike         |  simpleFoam   |
| pitzDaily         |  simpleFoam   |
| airFoil2D         |  simpleFoam   |
| elbow             |  icoFoam      |
| lockExchange      |  twoLiquidMixingFoam  |

### Simulations Onboarded For linux-arm64

| Simulation Name                                  |   Solver     |
|--------------------------------------|-----------|
| pitzDaily         |  simpleFoam   |
| airFoil2D         |  simpleFoam   |
| elbow             |  icoFoam      |
| lockExchange      |  twoLiquidMixingFoam  |

### What is Being Tested?
OpenFOAM is used to measure performance in terms of number of iterations for which particular simulation is run in a minute (iterations/minute). Below are the metrics measured by OpenFOAM Workload.

| Name                                  |   Unit     |
|--------------------------------------|-----------|
| Iterations/min          |  itrs/min   |

# References
* [OpenFOAM Wiki](https://openfoamwiki.net/)
* [OpenFOAM Getting Started](https://openfoamwiki.net/index.php/Tutorials/Before_Getting_Started)
* [Computational Fluid Dynamics](https://en.wikipedia.org/wiki/Computational_fluid_dynamics)
* [Tutorials](https://www.youtube.com/c/OpenFOAMTutorials/videos)
* [Arm64 Binaries Details](https://packages.ubuntu.com/search?keywords=openfoam)