# GeekBench5
Geekbench5 is a third party tool that runs its own, pre-defined set of workloads to measure CPU performance. GeekBench5 is often used to compare cloud 
performance.

* [GeekBench Documentation](https://www.geekbench.com/)
* [GeekBench Workloads](https://www.geekbench.com/doc/geekbench5-compute-workloads.pdf)


## How to package GeekBench5
:::info
GeekBench5 is a commercial workload. VirtualClient cannot distribute the license and binary. You need to follow the following steps to package this workload and make it available locally or in a storage that you own.
:::
1. GeekBench can be downloaded here https://www.geekbench.com/download/.
2. You need to purchase a license for it to run properly https://www.primatelabs.com/store/.
3. `Geekbench 5.preferences` is the license file you get after purchasing. Package the binaries and license in this structure. You only need to pack the runtimes you are going to run. (You don't need "win-arm64, win-x64" if you don't plan on running on Windows).
  ```treeview
  geekbench-5.1.0
  ├───geekbench5.vcpkg
  │
  ├───linux-x64/
  │       Geekbench 5.preferences
  │       geekbench.plar
  │       geekbench5
  │       geekbench_x86_64
  │
  ├───win-arm64/
  │       Geekbench 5.preferences
  │       geekbench.plar
  │       geekbench5.exe
  │       geekbench_aarch64.exe
  │
  └───win-x64/
          amd_ags_x64.dll
          cpuidsdk64.dll
          Geekbench 5.preferences
          geekbench.plar
          geekbench5.exe
          geekbench_x86_64.exe
          pl_opencl_x86_64.dll
  ```
4. Zip the geekbench-5.1.0 directory into `geekbench-5.1.0.zip`, make sure the runtimes folders are the top level after extraction, and no extra `/geekbench-5.1.0/` top directory is created.
  ```bash
  7z a geekbench-5.1.0.zip ./geekbench-5.1.0/*
  ```
    or 
  ```bash
  cd geekbench-5.1.0; zip -r ../geekbench-5.1.0.zip *
  ```
5. Modify the [GeekBench profile](https://github.com/microsoft/VirtualClient/blob/main/src/VirtualClient/VirtualClient.Main/profiles/PERF-CPU-GEEKBENCH5.json) as needed. If you are using your own blob storage, you can use the profile as is. If you are copying the zip file locally under `vc/packages`, you can simply remove the DependencyPackageInstallation step.


## What is Being Measured?
Geekbench5 reports four different types of scores, a value that's calculated by comparing the device's performance against a baseline. The baseline score 
is 1000, with higher numbers being better and double the score indicating double the performance.

1. Geekbench score. This is the overall score, calculated using the single-core and multi-core scores.

2. Single-core and multi-core score. This is calculated using the corresponding single-core and multi-core scores from the three subsections detailed below.

3. Subsection score. The workloads are grouped by how the workload exercise the system, and are calculated using the individual workload scores. The subsections are Cryptography, 
   Integer, and Floating-Point workloads.

4. Workload score. Each individual workload reports a score. Additionally, each individual workload reports the numerical result of 
  the workload - 5 GB/sec, 35 images/sec, etc.

Geekbench runs a corresponding single-core and multi-core version for each of the following common CPU-intensive algorithms. 

| Name                  | Description                                                                                                                                  |
|-----------------------|----------------------------------------------------------------------------------------------------------------------------------------------|
| AES-XTS               | Runs the AES encryption algorithm, widely used to secure communication channels                                                              |
| Text Compression      | Compresses and decompress text using the LZMA compression algorithm                                                                          |
| Image Compression     | Compresses and decompress a photograph using libjpeg-turbo for JPEG and libpng for PNG                                                       |
| Navigation            | Computes driving directions with approximated times using Dijkstra's algorithm                                                               |
| HTML5                 | Models DOM creation from both server-side and client-side rendered HTML5 documents                                                           |
| SQLite                | Executes SQL queries against an in-memory database that mimics financial data                                                                |
| PDF Rendering         | Parses and renders a PDF using the PDFium library                                                                                            |
| Text Rendering        | Parses a Markdown-formatted document and renders it as rich text to a bitmap                                                                 |
| Clang                 | Compiles a C source file using AArch64 as the target architecture                                                                            |
| Camera                | Replicates a photo sharing app, including image manipulation, using only the CPU                                                             |
| N-Body Physics        | Computes a 3D gravitation simulation using the Barnes-Hut method                                                                             |
| Rigid Body Physics    | Computes a 2D physics simulation for rigid bodies using Lua and the BoxD physics lib                                                         |
| Gaussian Blur         | Blurs an image using a Gaussian spatial filter                                                                                               |
| Face Detection        | Detects faces with the algorithm presented in “Rapid Object Detection using a Boosted Cascade of Simple Features” (2001) by Viola and Jones. |
| Horizon Detection     | Searches for the horizon line in an image and rotates the image to make horizon line level if found                                          |
| Image Inpainting      | Reconstructs part of an image with data from other parts of the image                                                                        |
| HDR                   | Takes four SDR images and produces an HDR image                                                                                              |
| Ray Tracing           | Generates an image by tracing the path of light through an image plane and simulating the effects of its encounters with virtual objects     |
| Structure from Motion | Takes two 2D images of the same scene and constructs an estimate of the 3D coordinates of the points visible in both images                  |
| Speech Recognition    | Performs recognition of arbitrary English speech using PocketSphinx                                                                          |
| Machine Learning      | Executes a  Convolutional Neural Network to perform an image classification task                                                             |

## Workload Metrics
The following metrics are examples of those captured by the Virtual Client when running the GeekBench5 workload.

| Scenario | Metric Name | Example Value (min) | Example Value (max) | Example Value (avg) | Unit |
|-----------|-------------|---------------------|---------------------|---------------------|------|
| AES-XTS (Multi-Core) | Raw Value | 1.26 | 2.35 | 2.2361644951140167 | GB/sec |
| AES-XTS (Single-Core) | Raw Value | 1.21 | 2.09 | 1.9808686210640743 | GB/sec |
| AES-XTS Score (Multi-Core) | Test Score | 742.0 | 1379.0 | 1311.7852877307276 | score |
| AES-XTS Score (Single-Core) | Test Score | 708.0 | 1226.0 | 1162.0089576547233 | score |
| Camera (Multi-Core) | Raw Value | 5.02 | 9.21 | 8.824011943539614 | images/sec |
| Camera (Single-Core) | Raw Value | 4.58 | 8.76 | 8.354275244299679 | images/sec |
| Camera Score (Multi-Core) | Test Score | 433.0 | 794.0 | 761.2090119435396 | score |
| Camera Score (Single-Core) | Test Score | 395.0 | 756.0 | 720.6886536373507 | score |
| Clang (Multi-Core) | Raw Value | 2.51 | 7.83 | 7.425597176981524 | Klines/sec |
| Clang (Single-Core) | Raw Value | 3.47 | 6.51 | 6.240559174810017 | Klines/sec |
| Clang Score (Multi-Core) | Test Score | 322.0 | 1005.0 | 953.0298588490771 | score |
| Clang Score (Single-Core) | Test Score | 445.0 | 836.0 | 800.9562975027144 | score |
| Cryptography Score (Multi-Core) | Test Score | 742.0 | 1379.0 | 1311.7852877307276 | score |
| Cryptography Score (Single-Core) | Test Score | 708.0 | 1226.0 | 1162.0089576547233 | score |
| Face Detection (Multi-Core) | Raw Value | 5.0 | 7.37 | 7.172499999999988 | images/sec |
| Face Detection (Single-Core) | Raw Value | 3.83 | 6.97 | 6.708667209554847 | images/sec |
| Face Detection Score (Multi-Core) | Test Score | 649.0 | 957.0 | 931.612920738328 | score |
| Face Detection Score (Single-Core) | Test Score | 498.0 | 905.0 | 871.3536916395223 | score |
| FloatingPoint Score (Multi-Core) | Test Score | 875.0 | 1099.0 | 1068.0024429967428 | score |
| FloatingPoint Score (Single-Core) | Test Score | 642.0 | 920.0 | 892.807546145494 | score |
| Gaussian Blur (Multi-Core) | Raw Value | 22.9 | 46.1 | 44.88300760043444 | Mpixels/sec |
| Gaussian Blur (Single-Core) | Raw Value | 26.5 | 39.3 | 37.38417480998918 | Mpixels/sec |
| Gaussian Blur Score (Multi-Core) | Test Score | 416.0 | 839.0 | 816.5222584147666 | score |
| Gaussian Blur Score (Single-Core) | Test Score | 482.0 | 716.0 | 680.0738327904452 | score |
| HDR (Multi-Core) | Raw Value | 14.1 | 24.8 | 24.09500542888169 | Mpixels/sec |
| HDR (Single-Core) | Raw Value | 11.6 | 22.7 | 22.018431053202997 | Mpixels/sec |
| HDR Score (Multi-Core) | Test Score | 1035.0 | 1817.0 | 1768.249457111835 | score |
| HDR Score (Single-Core) | Test Score | 853.0 | 1662.0 | 1615.9367535287732 | score |
| HTML5 (Multi-Core) | Raw Value | 437.1 | 922.7 | 886.5702225841469 | KElements/sec |
| HTML5 (Single-Core) | Raw Value | 383.8 | 951.7 | 892.1498099891434 | KElements/sec |
| HTML5 Score (Multi-Core) | Test Score | 372.0 | 786.0 | 755.0415309446254 | score |
| HTML5 Score (Single-Core) | Test Score | 327.0 | 811.0 | 759.8032030401737 | score |
| Horizon Detection (Multi-Core) | Raw Value | 17.4 | 26.0 | 25.055456026058658 | Mpixels/sec |
| Horizon Detection (Single-Core) | Raw Value | 12.6 | 21.0 | 20.195276872964159 | Mpixels/sec |
| Horizon Detection Score (Multi-Core) | Test Score | 705.0 | 1054.0 | 1016.4828990228014 | score |
| Horizon Detection Score (Single-Core) | Test Score | 510.0 | 853.0 | 819.3455483170467 | score |
| Image Compression (Multi-Core) | Raw Value | 30.1 | 51.5 | 49.88078175895753 | Mpixels/sec |
| Image Compression (Single-Core) | Raw Value | 24.9 | 44.1 | 42.71745385450586 | Mpixels/sec |
| Image Compression Score (Multi-Core) | Test Score | 636.0 | 1088.0 | 1054.4169381107493 | score |
| Image Compression Score (Single-Core) | Test Score | 527.0 | 933.0 | 903.0100434310532 | score |
| Image Inpainting (Multi-Core) | Raw Value | 49.8 | 85.3 | 81.46028773072759 | Mpixels/sec |
| Image Inpainting (Single-Core) | Raw Value | 39.2 | 73.2 | 69.65016286644959 | Mpixels/sec |
| Image Inpainting Score (Multi-Core) | Test Score | 1014.0 | 1738.0 | 1660.501628664495 | score |
| Image Inpainting Score (Single-Core) | Test Score | 799.0 | 1492.0 | 1419.764115092291 | score |
| Integer Score (Multi-Core) | Test Score | 625.0 | 941.0 | 914.0887622149837 | score |
| Integer Score (Single-Core) | Test Score | 485.0 | 832.0 | 804.042345276873 | score |
| Machine Learning (Multi-Core) | Raw Value | 15.9 | 27.1 | 25.364087947882785 | images/sec |
| Machine Learning (Single-Core) | Raw Value | 14.7 | 24.2 | 22.42418566775247 | images/sec |
| Machine Learning Score (Multi-Core) | Test Score | 410.0 | 702.0 | 656.4011943539631 | score |
| Machine Learning Score (Single-Core) | Test Score | 380.0 | 627.0 | 580.3208469055375 | score |
| Multi-Core Score | Test Score | 767.0 | 1005.0 | 980.1767100977198 | score |
| N-Body Physics (Multi-Core) | Raw Value | 752.3 | 992.2 | 927.4999999999999 | Kpairs/sec |
| N-Body Physics (Multi-Core) | Raw Value | 1.01 | 1.25 | 1.1558626328699893 | Mpairs/sec |
| N-Body Physics (Single-Core) | Raw Value | 596.1 | 941.1 | 888.0819761129206 | Kpairs/sec |
| N-Body Physics Score (Multi-Core) | Test Score | 601.0 | 995.0 | 923.1102062975027 | score |
| N-Body Physics Score (Single-Core) | Test Score | 476.0 | 752.0 | 709.78664495114 | score |
| Navigation (Multi-Core) | Raw Value | 1.48 | 2.89 | 2.7101140065146289 | MTE/sec |
| Navigation (Single-Core) | Raw Value | 1.53 | 2.45 | 2.2940499457112084 | MTE/sec |
| Navigation Score (Multi-Core) | Test Score | 524.0 | 1024.0 | 961.0165580890337 | score |
| Navigation Score (Single-Core) | Test Score | 544.0 | 867.0 | 813.499457111835 | score |
| PDF Rendering (Multi-Core) | Raw Value | 23.5 | 54.8 | 51.804044516829538 | Mpixels/sec |
| PDF Rendering (Single-Core) | Raw Value | 25.7 | 43.9 | 42.409690553746077 | Mpixels/sec |
| PDF Rendering Score (Multi-Core) | Test Score | 433.0 | 1010.0 | 954.5415309446254 | score |
| PDF Rendering Score (Single-Core) | Test Score | 473.0 | 809.0 | 781.46335504886 | score |
| Ray Tracing (Multi-Core) | Raw Value | 1.0 | 1.1 | 1.059050544540641 | Mpixels/sec |
| Ray Tracing (Multi-Core) | Raw Value | 770.8 | 999.9 | 962.744660194175 | Kpixels/sec |
| Ray Tracing (Single-Core) | Raw Value | 510.2 | 914.4 | 866.0622964169361 | Kpixels/sec |
| Ray Tracing Score (Multi-Core) | Test Score | 960.0 | 1365.0 | 1315.3683496199784 | score |
| Ray Tracing Score (Single-Core) | Test Score | 635.0 | 1139.0 | 1078.4891422366994 | score |
| Rigid Body Physics (Multi-Core) | Raw Value | 5001.6 | 7430.6 | 7166.121661237785 | FPS |
| Rigid Body Physics (Single-Core) | Raw Value | 3365.6 | 5952.7 | 5784.287106406104 | FPS |
| Rigid Body Physics Score (Multi-Core) | Test Score | 807.0 | 1199.0 | 1156.6815960912052 | score |
| Rigid Body Physics Score (Single-Core) | Test Score | 543.0 | 961.0 | 933.6305646036916 | score |
| SQLite (Multi-Core) | Raw Value | 102.0 | 283.0 | 269.9680238870789 | Krows/sec |
| SQLite (Single-Core) | Raw Value | 131.8 | 258.3 | 246.84967426710126 | Krows/sec |
| SQLite Score (Multi-Core) | Test Score | 326.0 | 903.0 | 861.6783387622149 | score |
| SQLite Score (Single-Core) | Test Score | 421.0 | 825.0 | 787.8889793702497 | score |
| Single-Core Score | Test Score | 605.0 | 875.0 | 848.5890336590662 | score |
| Speech Recognition (Multi-Core) | Raw Value | 28.7 | 42.2 | 39.75352877307272 | Words/sec |
| Speech Recognition (Single-Core) | Raw Value | 14.3 | 29.7 | 28.06102062975032 | Words/sec |
| Speech Recognition Score (Multi-Core) | Test Score | 898.0 | 1319.0 | 1243.3998371335507 | score |
| Speech Recognition Score (Single-Core) | Test Score | 448.0 | 929.0 | 877.7090119435396 | score |
| Structure from Motion (Multi-Core) | Raw Value | 4.54 | 7.3 | 7.1137079261672009 | Kpixels/sec |
| Structure from Motion (Single-Core) | Raw Value | 3.3 | 6.54 | 6.369581976112893 | Kpixels/sec |
| Structure from Motion Score (Multi-Core) | Test Score | 507.0 | 815.0 | 794.0420738327905 | score |
| Structure from Motion Score (Single-Core) | Test Score | 369.0 | 730.0 | 710.978555917481 | score |
| Text Compression (Multi-Core) | Raw Value | 3.87 | 6.27 | 6.006568946796989 | MB/sec |
| Text Compression (Single-Core) | Raw Value | 2.49 | 4.79 | 4.55621064060805 | MB/sec |
| Text Compression Score (Multi-Core) | Test Score | 765.0 | 1241.0 | 1187.6183496199784 | score |
| Text Compression Score (Single-Core) | Test Score | 493.0 | 946.0 | 900.8458197611292 | score |
| Text Rendering (Multi-Core) | Raw Value | 142.5 | 276.8 | 262.3624592833867 | KB/sec |
| Text Rendering (Single-Core) | Raw Value | 128.8 | 268.0 | 251.43379478827428 | KB/sec |
| Text Rendering Score (Multi-Core) | Test Score | 447.0 | 869.0 | 823.4541259500543 | score |
| Text Rendering Score (Single-Core) | Test Score | 404.0 | 841.0 | 789.1682953311618 | score |

# GeekBench6
Geekbench6 is a third party tool that runs its own, pre-defined set of workloads to measure CPU performance. GeekBench6 is often used to compare cloud 
performance, so it's valuable to see how our changes impact GeekBench6 results.

* [GeekBench Documentation](https://www.geekbench.com/)
* [GeekBench Workloads](https://www.geekbench.com/doc/geekbench6-compute-workloads.pdf)


## How to package GeekBench6
:::info
GeekBench6 is a commercial workload. VirtualClient cannot distribute the license and binary. You need to follow the following steps to package this workload and make it available locally or in a storage that you own.
:::
1. GeekBench can be downloaded here https://www.geekbench.com/download/.
2. You need to purchase a license for it to run properly https://www.primatelabs.com/store/.
3. `Geekbench 5.preferences` is the license file you get after purchasing. Package the binaries and license in this structure. You only need to pack the runtimes you are going to run. (You don't need "win-arm64, win-x64" if you don't plan on running on Windows).
  ```treeview
  geekbench-5.1.0
  ├───geekbench6.vcpkg
  │
  ├───linux-arm64/
  │       Geekbench 6.preferences
  │       geekbench.plar
  │       geekbench6
  │       geekbench_aarch64
  │
  ├───linux-x64/
  │       Geekbench 6.preferences
  │       geekbench.plar
  │       geekbench6
  │       geekbench_x86_64
  │
  ├───win-arm64/
  │       Geekbench 6.preferences
  │       geekbench.plar
  │       geekbench6.exe
  │       geekbench_aarch64.exe
  │
  └───win-x64/
          amd_ags_x64.dll
          cpuidsdk64.dll
          Geekbench 6.preferences
          geekbench.plar
          geekbench6.exe
          geekbench_x86_64.exe
          pl_opencl_x86_64.dll
  ```
4. Zip the geekbench-6.2.2 directory into `geekbench-6.2.2.zip`, make sure the runtimes folders are the top level after extraction, and no extra `/geekbench-6.2.2/` top directory is created.
  ```bash
  7z a geekbench-6.2.2.zip ./geekbench-6.2.2/*
  ```
    or 
  ```bash
  cd geekbench-6.2.2; zip -r ../geekbench-6.2.2.zip *
  ```
5. Modify the [GeekBench profile](https://github.com/microsoft/VirtualClient/blob/main/src/VirtualClient/VirtualClient.Main/profiles/PERF-CPU-GEEKBENCH.json) as needed. If you are using your own blob storage, you can use the profile as is. If you are copying the zip file locally under `vc/packages`, you can simply remove the DependencyPackageInstallation step.


## What is Being Measured?
Geekbench6 reports four different types of scores, a value that's calculated by comparing the device's performance against a baseline. The baseline score 
is 1000, with higher numbers being better and double the score indicating double the performance.

1. Geekbench score. This is the overall score, calculated using the single-core and multi-core scores.

2. Single-core and multi-core score. This is calculated using the corresponding single-core and multi-core scores from the two subsections detailed below.

3. Subsection score. The workloads are grouped by how the workload exercise the system, and are calculated using the individual workload scores. The subsections are Integer,
   and Floating-Point workloads.

4. Workload score. Each individual workload reports a score. Additionally, each individual workload reports the numerical result of 
  the workload - 5 GB/sec, 35 images/sec, etc.

Geekbench runs a corresponding single-core and multi-core version for each of the following common CPU-intensive algorithms. 

| Name                  | Description                                                                                                                                  |
|-----------------------|----------------------------------------------------------------------------------------------------------------------------------------------|
| File Copmression      | Compresses and decompresses files using the LZ4 and ZSTD compression codecs																   |
| Text Compression      | Compresses and decompresses text using the LZMA compression algorithm                                                                        |
| Image Compression     | Compresses and decompresses a photograph using libjpeg-turbo for JPEG and libpng for PNG                                                     |
| Navigation            | Computes driving directions with approximated times using Dijkstra's algorithm                                                               |
| HTML5                 | Models DOM creation from both server-side and client-side rendered HTML5 documents                                                           |
| PDF Rendering         | Parses and renders a PDF using the PDFium library                                                                                            |
| Photo Library         | Runs image classification to categorize and tag photos based on the objects they contain													   |
| Clang                 | Compiles a C source file using AArch64 as the target architecture                                                                            |
| Text Processing       | Loads files, parses them with regex, stores metadata in SQLite databse, and exports content to different format							   |
| Asset Compression     | Compresses 3D textural and geometric assets using a variety of popular compression codecs (ASTC, BC7, DXT5)								   |
| Object Detection      | Uses CNN to detect and classify objects in photo and then highlights them in photo														   |
| Background Blur       | Separates background from foreground in a video stream and blurs the background															   |
| Image Editing         | Measures how well you CPU handles making simple and complex image edits																	   |
| Object Remover        | Removes an object from a photo and automatically fills in the gap left behind																   |
| Horizon Detection     | Searches for the horizon line in an image and rotates the image to make horizon line level if found                                          |
| Photo Filter          | Applies filters to photos to enhance their appearance																						   |
| HDR                   | Takes four SDR images and produces an HDR image                                                                                              |
| Image Synthesis       | Measures how well your CPU handles creating artificial images																				   |
| Ray Tracer            | Generates an image by tracing the path of light through an image plane and simulating the effects of its encounters with virtual objects     |
| Structure from Motion | Takes two 2D images of the same scene and constructs an estimate of the 3D coordinates of the points visible in both images                  |

## Workload Metrics
The following metrics are examples of those captured by the Virtual Client when running the GeekBench6 workload.

| MetricName                                | MetricValue | MetricUnit    |
|-------------------------------------------|-------------|---------------|
| SingleCore-File Compression               | 269.8       | MB/sec        |
| SingleCore-Navigation                     | 11.5        | routes/sec    |
| SingleCore-HTML5 Browser                  | 41.7        | pages/sec     |
| SingleCore-PDF Renderer                   | 47.7        | Mpixels/sec   |
| SingleCore-Photo Library                  | 27          | images/sec    |
| SingleCore-Clang                          | 9.71        | Klines/sec    |
| SingleCore-Text Processing                | 152.4       | pages/sec     |
| SingleCore-Asset Compression              | 62.3        | MB/sec        |
| SingleCore-Object Detection               | 59.1        | images/sec    |
| SingleCore-Background Blur                | 9.76        | images/sec    |
| SingleCore-Horizon Detection              | 84.5        | Mpixels/sec   |
| SingleCore-Object Remover                 | 123.2       | Mpixels/sec   |
| SingleCore-HDR                            | 55.8        | Mpixels/sec   |
| SingleCore-Photo Filter                   | 25.3        | images/sec    |
| SingleCore-Ray Tracer                     | 1.5         | Mpixels/sec   |
| SingleCore-Structure from Motion          | 68.7        | Kpixels/sec   |
| SingleCoreScore-File Compression          | 1878        | Score         |
| SingleCoreScore-Navigation                | 1901        | Score         |
| SingleCoreScore-HTML5 Browser             | 2038        | Score         |
| SingleCoreScore-PDF Renderer              | 2069        | Score         |
| SingleCoreScore-Photo Library             | 1988        | Score         |
| SingleCoreScore-Clang                     | 1971        | Score         |
| SingleCoreScore-Text Processing           | 1903        | Score         |
| SingleCoreScore-Asset Compression         | 2012        | Score         |
| SingleCoreScore-Object Detection          | 1976        | Score         |
| SingleCoreScore-Background Blur           | 2358        | Score         |
| SingleCoreScore-Horizon Detection         | 2714        | Score         |
| SingleCoreScore-Object Remover            | 1602        | Score         |
| SingleCoreScore-HDR                       | 1900        | Score         |
| SingleCoreScore-Photo Filter              | 2546        | Score         |
| SingleCoreScore-Ray Tracer                | 1550        | Score         |
| SingleCoreScore-Structure from Motion     | 2170        | Score         |
| MultiCore-File Compression                | 774         | MB/sec        |
| MultiCore-Navigation                      | 44.2        | routes/sec    |
| MultiCore-HTML5 Browser                   | 134.8       | pages/sec     |
| MultiCore-PDF Renderer                    | 114.8       | Mpixels/sec   |
| MultiCore-Photo Library                   | 46.2        | images/sec    |
| MultiCore-Clang                           | 17.8        | Klines/sec    |
| MultiCore-Text Processing                 | 145.2       | pages/sec     |
| MultiCore-Asset Compression               | 127.7       | MB/sec        |
| MultiCore-Object Detection                | 75.5        | images/sec    |
| MultiCore-Background Blur                 | 13.8        | images/sec    |
| MultiCore-Horizon Detection               | 158         | Mpixels/sec   |
| MultiCore-Object Remover                  | 248.5       | Mpixels/sec   |
| MultiCore-HDR                             | 157.5       | Mpixels/sec   |
| MultiCore-Photo Filter                    | 63.2        | images/sec    |
| MultiCore-Ray Tracer                      | 4.91        | Mpixels/sec   |
| MultiCore-Structure from Motion           | 198         | Kpixels/sec   |
| MultiCoreScore-File Compression           | 5390        | Score         |
| MultiCoreScore-Navigation                 | 7332        | Score         |
| MultiCoreScore-HTML5 Browser              | 6586        | Score         |
| MultiCoreScore-PDF Renderer               | 4978        | Score         |
| MultiCoreScore-Photo Library              | 3401        | Score         |
| MultiCoreScore-Clang                      | 3615        | Score         |
| MultiCoreScore-Text Processing            | 1813        | Score         |
| MultiCoreScore-Asset Compression          | 4122        | Score         |
| MultiCoreScore-Object Detection           | 2524        | Score         |
| MultiCoreScore-Background Blur            | 3339        | Score         |
| MultiCoreScore-Horizon Detection          | 5076        | Score         |
| MultiCoreScore-Object Remover             | 3232        | Score         |
| MultiCoreScore-HDR                        | 5368        | Score         |
| MultiCoreScore-Photo Filter               | 6371        | Score         |
| MultiCoreScore-Ray Tracer                 | 5073        | Score         |
| MultiCoreScore-Structure from Motion      | 6254        | Score         |
| SingleCoreSummary-Single-Core Score       | 2007        | Score         |
| SingleCoreSummary-Integer Score           | 1970        | Score         |
| SingleCoreSummary-Floating Point Score    | 2077        | Score         |
| MultiCoreSummary-Multi-Core Score         | 4308        | Score         |
| MultiCoreSummary-Integer Score            | 4061        | Score         |
| MultiCoreSummary-Floating Point Score     | 4808        | Score         |