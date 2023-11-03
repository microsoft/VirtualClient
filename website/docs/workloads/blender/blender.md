# Blender Benchmark
Blender Benchmark, a new platform to collect and display the results of hardware and software performance tests. With this benchmark we aim at an optimal comparison between system hardware and installations, and to assist developers to track performance during Blender development.

The benchmark consists of two parts: a downloadable package which runs Blender and renders on several production files, and the Open Data portal on blender.org, where the results will be (optionally) uploaded.

* [Blender benchmark] (https://www.blender.org/news/introducing-blender-benchmark/)

## What is Being Measured 
The Blender benchmark Score is a measure of how quickly Cycles can render path tracing samples on one CPU or GPU device. The higher the number, the better. In particular it's the estimated number of samples per minute, summed for all benchmark scenes.

## System Requirements
* Windows 8.1, 10, and 11
* 64-bit quad core CPU with SSE2 support
* 8 GB RAM
* 2 GB VRAM Graphics Card that supports OpenGL 4.3

## Benchmark License
Blender is released under the GNU General Public License (GPL, or “free software”).

This license grants people a number of freedoms:

* You are free to use Blender, for any purpose
* You are free to distribute Blender
* You can study how Blender works and change it
* You can distribute changed versions of Blender

The Blender benchmark also only uses free and open source software (GNU GPL), the testing content is public domain (CC0), and the test results are being shared anonymized as public domain data – free for anyone to download and to process further.

Read more about licensing from [this page](https://www.blender.org/about/license/).

## Workload Metrics
The following metrics are examples of those captured by the Virtual Client when running the Blender workload.

### Example Metrics

| Name                     | Example            | Unit               |
|--------------------------|--------------------|--------------------|
| number_of_samples	       | 65	                | sample             |
| device_peak_memory	   | 646.81	            | mb                 |
| time_for_samples	       | 34.237828	        | second             |
| total_render_time	       | 34.9036	        | second             |
| samples_per_minute	   | 113.90909493440998	| samples_per_minute |
| render_time_no_sync	   | 34.238	            | second             |
| device_peak_memory	   | 4830.43	        | mb                 |

The best way to measure your device's performance is through the samples_per_minute metric. The more samples rendered per minute, the better the device performance.

number_of_samples = time_for_samples * samples_per_minute

### Example Metadata
More information about the metrics can be found in the metric's metadata
```json
{
	"blender_version": "3.6.0",
	"benchmark_launcher": "3.1.0",
	"scene": "monster",
	"device_name": "AMD EPYC 7763 64-Core Processor",
	"device_type": "CPU",
	"time_limit": 30
}
```
