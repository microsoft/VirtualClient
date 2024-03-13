# 3DMark
3DMark is a tool for measuring the performance of PCs and mobile devices. It includes a range of benchmarks, each one designed for a specific class of hardware from smartphones to laptops to high-performance gaming PCs.

VC internally calls 3dMark benchmarks from the command line. Thus users need a valid [Professional Edition license](https://support.benchmarks.ul.com/en/support/solutions/articles/44002132667) to [run 3DMark benchmarks from the command line](https://support.benchmarks.ul.com/en/support/solutions/folders/44001218219).

3DMark includes many benchmarks. Each test is designed for a specific class of hardware. 3DMark made it easy to find the right test for your system. Refer to this [table](https://support.benchmarks.ul.com/support/solutions/articles/44002132650-which-3dmark-benchmark-should-i-use-) to see which benchmark suits your need.

* [3DMark Full User Guide](https://support.benchmarks.ul.com/support/solutions/articles/44002146295-3dmark-user-guide)

## What is Being Measured 
Each benchmark measures a different aspect of the hardware. Take Speedway for example, it measures DirectX 12 Ultimate benchmark for ray tracing capable gaming PCs running Windows 10 and 11. Refer to [this article](https://support.benchmarks.ul.com/support/solutions/articles/44002378655-overview-of-3dmark-speed-way-benchmark) to see what is being measured for each benchmark.

## Workload Metrics
Virtual Client captures different metrics for each benchmark. Below is an exmple for timespy_extreme.

### Example Metrics - timespy_extreme
| Name                     | Example            | Unit               |
|--------------------------|--------------------|--------------------|
| cpuScore	               | 65	                | score              |
| 3dMarkScore	           | 646.81	            | score              |
| graphicsScore	           | 34.237828	        | score              |


## Packaging and Setup
Users are expected to provide their own binaries and lisences for this workload. This package is not hosted on the VC blob store. The package structure should look like the following:
  ``` treeview
    3dmark-xxx
    ├── win-x64
    │   ├── DLC
    │   │   └── 3DMark
    │   │       └── dlc
    │   │           ├── cpu-profile-test
    │   │           └── etc.
    │   └── 3DMark
    │       ├── 3DMarkCmd.exe
    │       ├── custom_portroyal.3dmdef
    │       └── etc.
    └── 3dmark.vcpkg
  ```

  ### custom definition files
  Users need to package their own [3dmark definition files](https://support.benchmarks.ul.com/support/solutions/articles/44002145445-3dmark-xml-definition-files) together with the 3dmark executables. For example, if a users wants to run portroyal, a defintion file called custom_portroyal.3dmdef needs to be present in the same directory as 3DMarkCmd.exe (see above file tree as an example.)
  
  Each benchmark comes with a default custom_{benchmark}.3dmdef file. Read more about these files from [this article](https://support.benchmarks.ul.com/support/solutions/articles/44002145445-3dmark-xml-definition-files). 


