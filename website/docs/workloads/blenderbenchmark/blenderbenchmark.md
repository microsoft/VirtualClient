# Blender Benchmark
Blender Benchmark, a new platform to collect and display the results of hardware and software performance tests. This benchmark aims at an optimal comparison between system hardware and installations, and to assist developers to track performance during Blender development.

* [Blender Benchmark] (https://www.blender.org/news/introducing-blender-benchmark/)

## What is Being Measured 
The Blender Benchmark Score is a measure of how quickly cycles can render [path tracing samples](https://docs.blender.org/manual/en/latest/render/cycles/render_settings/sampling.html) using only the device specified. (E.g. CPU or GPU).

**Note**: Integrated GPU may not be detectable by blender and thus may not be used for benchmarking. 

The Blender Benchmark firstly downloads the blender engine and the scenes to be tested. The benchmark then tests how quickly these scenes can be rendered using, and only using, the specified device.

The higher the number, the better. In particular it's the estimated number of samples per minute, summed for all benchmark scenes.

## System Requirements
* Windows 8.1, 10, and 11

## Benchmark License
Blender Benchmark is released under the GNU General Public License (GPL, or “free software”).

Read more about licensing from [this page](https://projects.blender.org/infrastructure/blender-open-data/src/branch/main/LICENSE).

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

#### Example 1: CPU benchmark results metadata
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

#### Example 2: AMD GPU(HIP) benchmark results metadata
```json 
{
	"blender_version": "3.5.0",
    "benchmark_launcher": "3.1.0",
    "scene": "classroom",
    "device_name": "AMD Radeon Pro V620 MxGPU",
    "device_type": "HIP",
    "time_limit": 30
}
```
