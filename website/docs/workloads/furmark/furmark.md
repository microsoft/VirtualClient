# Furmark
FurMark is a lightweight but very intensive graphics card / GPU stress test on Windows platform. It's a quick OpenGL benchmark as well.
FurMark is simple to use and is free. 

It is the popular GPU stress test utility for graphics cards (NVIDIA GeForce, AMD Radeon and Intel Arc GPUs).Currently VC supports 
running Furmark on AMD GPU.

* [Furmark website](https://geeks3d.com/furmark/)

## What is Being Measured?
The Furmark workload measures FPS(Frames per second) which also gives scores by rendering a GUI which stresses out GPU to the maximum extend.
Only single GPU will be stressed out with Furmark. It also gives GPU power, temperature and more details about GPU.

## Workload Metrics
The following metrics are examples of those captured by the Virtual Client when running the Furmark workload. This set of metrics 
will be captured for each one of the distinct scenarios that are part of the profile (Parameters that are varied are time for
which the tool is run for stressing out GPU, resolution(width and height) of the GUI that is to be rendered). 

| Metric Name |  Example Value | Unit | Description |
|-------------|---------------------|------|-------------|
|Score	      |15863 | None| Furmark scores reflect GPU performance and stress levels effectively.|
|DurationInMs	|60000 | ms(milliseconds)| Time for which we are stressing the GPU using Furmark Tool|
|GPU1_AvgTemperatur | 	0| celsius | Avergae Furmark temperature output indicates GPU's heat level during stress.|
|GPU1_MaxTemperatur |	0 | celsius | Max Furmark temperature output indicates GPU's heat level during stress.|
|GPU1_AvgFPS |	255.69696969697 |None| Average FPS (Frames Per Second) denotes the measure of how many frames (images) the GPU is capable of rendering and displaying per second during the benchmarking process |
|GPU1_MaxFPS |	268 |None| Max FPS (Frames Per Second) denotes the measure of how many frames (images) the GPU is capable of rendering and displaying per second during the benchmarking process |
|GPU1_Vddc |	0 |None| VDDs in Furmark output represents GPU's core voltage during benchmarking. |
|GPU1_AvgCoreLoad |	0 | None | Average Core load in Furmark output indicates GPU's utilization during benchmarking process. |
|GPU1_MaxCoreLoad |	0 | None | Max Core load in Furmark output indicates GPU's utilization during benchmarking process. |
|GPU2_AvgTemperatur |	43.6969696969697 | celsius | Avergae Furmark temperature output indicates GPU's heat level during stress.|
|GPU2_MaxTemperatur |	51 | celsius | Max Furmark temperature output indicates GPU's heat level during stress.|
|GPU2_AvgFPS |	255.69696969697 |None| Average FPS (Frames Per Second) denotes the measure of how many frames (images) the GPU is capable of rendering and displaying per second during the benchmarking process |
|GPU2_MaxFPS |	268 | None | Max FPS (Frames Per Second) denotes the measure of how many frames (images) the GPU is capable of rendering and displaying per second during the benchmarking process|
|GPU2_Vddc |	0.914424242424242 |None| VDDs in Furmark output represents GPU's core voltage during benchmarking. |
|GPU2_AvgCoreLoad |	96 | None | Average Core load in Furmark output indicates GPU's utilization during benchmarking process. |
|GPU2_MaxCoreLoad |	99 | None | Max Core load in Furmark output indicates GPU's utilization during benchmarking process. |
