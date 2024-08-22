// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.IO.Abstractions;
    using System.Linq;
    using System.Runtime.InteropServices;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Logging;
    using Moq;
    using NUnit.Framework;
    using VirtualClient.Common;
    using VirtualClient.Common.Telemetry;
    using VirtualClient.Contracts;
    using VirtualClient.Monitors;

    [TestFixture]
    [Category("Integration")]
    internal class WindowsPerformanceCounterMonitorTests
    {
        private IServiceCollection dependencies;
        private InMemoryLogger logger = new InMemoryLogger();

        [SetUp]
        public void InitializeTest()
        {
            ISystemManagement systemManagement = DependencyFactory.CreateSystemManager(
                Environment.MachineName,
                Guid.NewGuid().ToString(),
                new PlatformSpecifics(Environment.OSVersion.Platform, RuntimeInformation.ProcessArchitecture),
                logger);

            this.dependencies = new ServiceCollection()
                .AddSingleton<ISystemManagement>(systemManagement)
                .AddSingleton<ISystemInfo>(systemManagement)
                .AddSingleton<IFileSystem>(systemManagement.FileSystem)
                .AddSingleton<IPackageManager>(systemManagement.PackageManager)
                .AddSingleton<PlatformSpecifics>(systemManagement.PlatformSpecifics)
                .AddSingleton<ProcessManager>(systemManagement.ProcessManager)
                .AddSingleton<IStateManager>(systemManagement.StateManager)
                .AddSingleton<ILogger>(logger);
        }

        [Test]
        public async Task ExecuteWindowsPerformanceCounterMonitorOnLocalSystem()
        {
            using (CancellationTokenSource cancellationSource = new CancellationTokenSource())
            {
                IDictionary<string, IConvertible> parameters = new Dictionary<string, IConvertible>
                {
                    [nameof(WindowsPerformanceCounterMonitor.Scenario)] = "RunMonitorTesting",
                    [nameof(WindowsPerformanceCounterMonitor.CounterDiscoveryInterval)] = TimeSpan.FromSeconds(10).ToString(),
                    [nameof(WindowsPerformanceCounterMonitor.CounterCaptureInterval)] = TimeSpan.FromSeconds(1).ToString(),
                    [nameof(WindowsPerformanceCounterMonitor.MonitorFrequency)] = TimeSpan.FromSeconds(20).ToString(),
                    //
                    // Counter Definitions
                    //
                    ["Counters01"] = "Hyper-V Hypervisor Virtual Processor=\\(_Total\\)\\\\(% (Guest|Hypervisor|Total) Run Time)",
                    ["Counters02"] = "IPv4=.",
                    ["Counters03"] = "Memory=(Available|Cache|Committed) Bytes",
                    ["Counters04"] = "Memory=Faults/sec",
                    ["Counters05"] = "Memory=(Pages/sec|Page Reads/sec|Page Writes/sec|Pages Input/sec|Pages Output/sec)",
                    ["Counters06"] = "PhysicalDisk=\\(_Total\\)",
                    ["Counters07"] = "Processor=\\(_Total\\)",
                    ["Counters08"] = "Processor=\\([0-9]+\\)\\\\% (Idle|Interrupt|Privileged|Processor|User) Time",
                    ["Counters09"] = "System=."
                };

                // So that the counters can be viewed in a spreadsheet format, we are writing them out to the
                // file system in CSV format.
                StringBuilder csvBuilder = new StringBuilder();
                csvBuilder.AppendLine("MetricName,MetricValue,ScenarioStartTime,ScenarioEndTime");

                using (var monitor = new WindowsPerformanceCounterMonitor(this.dependencies, parameters))
                {
                    this.logger.OnLog = (level, eventId, state, exc) =>
                    {
                        if (eventId.Name == "PerformanceCounter")
                        {
                            EventContext context = state as EventContext;
                            object counterName = context.Properties["metricName"];
                            object counterValue = context.Properties["metricValue"];
                            object startTime = ((DateTime)context.Properties["scenarioStartTime"]).ToString("o");
                            object endTime = ((DateTime)context.Properties["scenarioEndTime"]).ToString("o");

                            csvBuilder.AppendLine($"{counterName},{counterValue},{startTime},{endTime}");
                        }
                    };

                    Task executeTask = monitor.ExecuteAsync(cancellationSource.Token);

                    // Allow the task to run for period of time before forcing a timeout.
                    TimeSpan monitorRuntime = TimeSpan.FromSeconds(70);
                    await Task.WhenAny(executeTask, Task.Delay(monitorRuntime));

                    cancellationSource.Cancel();
                    await executeTask;

                    System.IO.File.WriteAllText(
                        Path.Combine(MockFixture.TestAssemblyDirectory, "WindowsPerformanceCounterMonitor_Counters.csv"),
                        csvBuilder.ToString());
                }
            }
        }
    }
}
