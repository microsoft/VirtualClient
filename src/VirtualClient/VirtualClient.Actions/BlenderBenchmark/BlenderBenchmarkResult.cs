using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace VirtualClient.Actions
{
    /// <summary>
    /// Class that represents the blender benchmark results.
    /// </summary>
    public class BlenderBenchmarkResult
    {
        /// <summary>
        /// Timestamp of the record creation.
        /// </summary>
        [JsonProperty("timestamp")]
        public DateTime Timestamp { get; set; }

        /// <summary>
        /// Information about the Blender version used.
        /// </summary>
        [JsonProperty("blender_version")]
        public BlenderVersion BlenderVersion { get; set; }

        /// <summary>
        /// Information about the benchmark launcher.
        /// </summary>
        [JsonProperty("benchmark_launcher")]
        public BenchmarkLauncher BenchmarkLauncher { get; set; }

        /// <summary>
        /// Information about the benchmark script.
        /// </summary>
        [JsonProperty("benchmark_script")]
        public BenchmarkScript BenchmarkScript { get; set; }

        /// <summary>
        /// Details of the scene.
        /// </summary>
        [JsonProperty("scene")]
        public Scene Scene { get; set; }

        /// <summary>
        /// System information including device details.
        /// </summary>
        [JsonProperty("system_info")]
        public SystemInfo SystemInfo { get; set; }

        /// <summary>
        /// Information about the device used.
        /// </summary>
        [JsonProperty("device_info")]
        public DeviceInfo DeviceInfo { get; set; }

        /// <summary>
        /// Statistical data related to the rendering process.
        /// </summary>
        [JsonProperty("stats")]
        public Stats Stats { get; set; }
    }

    /// <summary>
    /// Details pertaining to the version of Blender used.
    /// </summary>
    public class BlenderVersion
    {
        /// <summary>
        /// The version number of Blender.
        /// </summary>
        [JsonProperty("version")]
        public string Version { get; set; }

        /// <summary>
        /// The build date of this version of Blender.
        /// </summary>
        [JsonProperty("build_date")]
        public string BuildDate { get; set; } // Changed to string because it's not a standard date format

        /// <summary>
        /// The build time of this version of Blender.
        /// </summary>
        [JsonProperty("build_time")]
        public string BuildTime { get; set; }

        /// <summary>
        /// The date of the build commit for this version of Blender.
        /// </summary>
        [JsonProperty("build_commit_date")]
        public string BuildCommitDate { get; set; } // Changed to string because it's not a standard date format

        /// <summary>
        /// The time of the build commit for this version of Blender.
        /// </summary>
        [JsonProperty("build_commit_time")]
        public string BuildCommitTime { get; set; }

        /// <summary>
        /// The unique hash of the build for this version of Blender.
        /// </summary>
        [JsonProperty("build_hash")]
        public string BuildHash { get; set; }

        /// <summary>
        /// The label associated with this version of Blender.
        /// </summary>
        [JsonProperty("label")]
        public string Label { get; set; }

        /// <summary>
        /// The checksum of this version of Blender.
        /// </summary>
        [JsonProperty("checksum")]
        public string Checksum { get; set; }
    }

    /// <summary>
    /// Contains data about the benchmark launcher used.
    /// </summary>
    public class BenchmarkLauncher
    {
        /// <summary>
        /// The label associated with the benchmark launcher.
        /// </summary>
        [JsonProperty("label")]
        public string Label { get; set; }

        /// <summary>
        /// The checksum of the benchmark launcher.
        /// </summary>
        [JsonProperty("checksum")]
        public string Checksum { get; set; }
    }

    /// <summary>
    /// Contains data about the benchmark script used.
    /// </summary>
    public class BenchmarkScript
    {
        /// <summary>
        /// The label associated with the benchmark script.
        /// </summary>
        [JsonProperty("label")]
        public string Label { get; set; }

        /// <summary>
        /// The checksum of the benchmark script.
        /// </summary>
        [JsonProperty("checksum")]
        public string Checksum { get; set; }
    }

    /// <summary>
    /// Details about the scene involved in the benchmark.
    /// </summary>
    public class Scene
    {
        /// <summary>
        /// The label identifying the scene.
        /// </summary>
        [JsonProperty("label")]
        public string Label { get; set; }

        /// <summary>
        /// The checksum of the scene data.
        /// </summary>
        [JsonProperty("checksum")]
        public string Checksum { get; set; }
    }

    /// <summary>
    /// Information related to the system on which the benchmark is run.
    /// </summary>
    public class SystemInfo
    {
        /// <summary>
        /// The bitness of the operating system.
        /// </summary>
        [JsonProperty("bitness")]
        public string Bitness { get; set; }

        /// <summary>
        /// Type of the machine, e.g., AMD64.
        /// </summary>
        [JsonProperty("machine")]
        public string Machine { get; set; }

        /// <summary>
        /// Name of the system, e.g., Windows.
        /// </summary>
        [JsonProperty("system")]
        public string System { get; set; }

        /// <summary>
        /// Name of the specific distribution, if applicable.
        /// </summary>
        [JsonProperty("dist_name")]
        public string DistName { get; set; }

        /// <summary>
        /// Version of the specific distribution, if applicable.
        /// </summary>
        [JsonProperty("dist_version")]
        public string DistVersion { get; set; }

        /// <summary>
        /// List of devices associated with the system.
        /// </summary>
        [JsonProperty("devices")]
        public List<Device> Devices { get; set; }

        /// <summary>
        /// Number of CPU sockets available in the system.
        /// </summary>
        [JsonProperty("num_cpu_sockets")]
        public int NumCpuSockets { get; set; }

        /// <summary>
        /// Number of CPU cores available in the system.
        /// </summary>
        [JsonProperty("num_cpu_cores")]
        public int NumCpuCores { get; set; }

        /// <summary>
        /// Number of CPU threads available in the system.
        /// </summary>
        [JsonProperty("num_cpu_threads")]
        public int NumCpuThreads { get; set; }
    }

    /// <summary>
    /// Information regarding the computing device used in the benchmark.
    /// </summary>
    public class DeviceInfo
    {
        /// <summary>
        /// Type of the device used, e.g., HIP.
        /// </summary>
        [JsonProperty("device_type")]
        public string DeviceType { get; set; }

        /// <summary>
        /// List of computing devices used in the benchmark.
        /// </summary>
        [JsonProperty("compute_devices")]
        public List<Device> ComputeDevices { get; set; }

        /// <summary>
        /// Number of CPU threads utilized by the device.
        /// </summary>
        [JsonProperty("num_cpu_threads")]
        public int NumCpuThreads { get; set; }
    }

    /// <summary>
    /// Details about a specific device.
    /// </summary>
    public class Device
    {
        /// <summary>
        /// Type of the device.
        /// </summary>
        [JsonProperty("type")]
        public string Type { get; set; }

        /// <summary>
        /// Name or identifier of the device.
        /// </summary>
        [JsonProperty("name")]
        public string Name { get; set; }

        /// <summary>
        /// Indicates if the device is also a display device.
        /// </summary>
        [JsonProperty("is_display")]
        public bool? IsDisplay { get; set; }
    }

    /// <summary>
    /// Contains statistics generated during the benchmarking process.
    /// </summary>
    public class Stats
    {
        /// <summary>
        /// Peak memory usage of the device.
        /// </summary>
        [JsonProperty("device_peak_memory")]
        public double DevicePeakMemory { get; set; }

        /// <summary>
        /// Total number of samples taken during the benchmark.
        /// </summary>
        [JsonProperty("number_of_samples")]
        public int NumberOfSamples { get; set; }

        /// <summary>
        /// Time taken to collect all the samples.
        /// </summary>
        [JsonProperty("time_for_samples")]
        public double TimeForSamples { get; set; }

        /// <summary>
        /// Rate of sample collection per minute.
        /// </summary>
        [JsonProperty("samples_per_minute")]
        public double SamplesPerMinute { get; set; }

        /// <summary>
        /// Total render time during the benchmark.
        /// </summary>
        [JsonProperty("total_render_time")]
        public double TotalRenderTime { get; set; }

        /// <summary>
        /// Render time excluding any synchronization activities.
        /// </summary>
        [JsonProperty("render_time_no_sync")]
        public double RenderTimeNoSync { get; set; }

        /// <summary>
        /// Limit on the time for which the benchmark runs.
        /// </summary>
        [JsonProperty("time_limit")]
        public int TimeLimit { get; set; }
    }
}