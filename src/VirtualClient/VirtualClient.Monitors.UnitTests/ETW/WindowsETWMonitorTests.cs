// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Monitors
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Diagnostics.Tracing.Session;
    using NUnit.Framework;
    using VirtualClient;
    using VirtualClient.Common;
    using VirtualClient.Common.Telemetry;

    [TestFixture]
    [Category("Unit")]
    [Platform(Exclude = "Unix,Linux,MacOsX")]
    public class WindowsETWMonitorTests : MockFixture
    {
        private MockFixture mockFixture;

        [SetUp]
        public void SetupTest()
        {
            this.mockFixture = this;
            this.mockFixture.Setup(PlatformID.Win32NT);

            this.mockFixture.Parameters = new Dictionary<string, IConvertible>()
            {
                { "ProfilingEnabled", true}
            };


            this.mockFixture.ProcessManager.OnCreateProcess = (command, arguments, workingDir) =>
            {
                InMemoryProcess process = new InMemoryProcess()
                {
                    StartInfo = new System.Diagnostics.ProcessStartInfo()
                    {
                        FileName = command,
                        Arguments = arguments,
                        WorkingDirectory = workingDir
                    },
                    OnHasExited = () => true,
                    ExitCode = 0,
                    OnStart = () => true,
                    StandardOutput = new ConcurrentBuffer()
                };

                return process;
            };
        }

        [Test]
        public void WindowsETWMonitorThrowsWhenRunningWithoutAdminPrivileges()
        {
            if (TraceEventSession.IsElevated() == false)
            {
                using (TestWindowsETWMonitor monitor = new TestWindowsETWMonitor(this.mockFixture))
                {
                    this.mockFixture.Parameters["JsonFilters"] = "testing";
                    MonitorException exception = Assert.Throws<MonitorException>(() => monitor.InitializeAsync(EventContext.None, CancellationToken.None));
                    Assert.AreEqual(ErrorReason.Unauthorized, exception.Reason);
                }
            }
        }

        [Test]
        public void WindowsETWMonitorThrowsWhenJsonFiltersParameterIsInImproperFormat()
        {
            this.mockFixture.Parameters["JsonFilters"] = "testing";
            if (TraceEventSession.IsElevated() == true)
            {
                using (TestWindowsETWMonitor monitor = new TestWindowsETWMonitor(this.mockFixture))
                {
                    MonitorException exception = Assert.Throws<MonitorException>(() => monitor.InitializeAsync(EventContext.None, CancellationToken.None));
                    Assert.AreEqual(ErrorReason.InvalidProfileDefinition, exception.Reason);
                }
            }
        }

        [Test]
        public void WindowsETWMonitorThrowsOnValidateWhenProviderTypeParameterIsEmpty()
        {
            this.mockFixture.Parameters["ProviderType"] = string.Empty;
            using (TestWindowsETWMonitor monitor = new TestWindowsETWMonitor(this.mockFixture))
            {
                MonitorException exception = Assert.Throws<MonitorException>(() => monitor.Validate());
                Assert.AreEqual(ErrorReason.InvalidProfileDefinition, exception.Reason);
            }
        }

        [Test]
        public void WindowsETWMonitorThrowsOnValidateWhenProviderParameterIsEmpty()
        {
            this.mockFixture.Parameters["ProviderType"] = "NonKernel";
            this.mockFixture.Parameters["Provider"] = string.Empty;
            using (TestWindowsETWMonitor monitor = new TestWindowsETWMonitor(this.mockFixture))
            {
                MonitorException exception = Assert.Throws<MonitorException>(() => monitor.Validate());
                Assert.AreEqual(ErrorReason.InvalidProfileDefinition, exception.Reason);
            }
        }

        [Test]
        public void WindowsETWMonitorThrowsOnValidateWhenProviderTypeIsKernelAndKernelProviderKeywordsAreEmpty()
        {
            this.mockFixture.Parameters["ProviderType"] = "Kernel";
            this.mockFixture.Parameters["Provider"] = "Windows Kernel";
            this.mockFixture.Parameters["ProviderKeywords"] = string.Empty;
            using (TestWindowsETWMonitor monitor = new TestWindowsETWMonitor(this.mockFixture))
            {
                MonitorException exception = Assert.Throws<MonitorException>(() => monitor.Validate());
                Assert.AreEqual(ErrorReason.InvalidProfileDefinition, exception.Reason);
            }
        }


        [Test]
        [Ignore("Invalid setup. This test checks to see if the application is running with elevated privileges. This should not be required for a unit test.")]
        public async Task WindowsETWMonitorExecuteProfilingOnDemand()
        {
            int count = 0;
            this.mockFixture.Parameters["ProfilingMode"] = "OnDemand";
            using (CancellationTokenSource cancellationSource = new CancellationTokenSource())
            {
                Task timeoutTask = Task.Run(async () =>
                {
                    DateTime finishTime = DateTime.UtcNow.AddSeconds(20);
                    while (DateTime.UtcNow < finishTime)
                    {
                        await Task.Delay(100);
                    }

                    cancellationSource.Cancel();
                });

                Task executorTask = Task.Run(async () =>
                {
                    await Task.Delay(2000).ConfigureAwait(false);
                    using (TestExecutor component = new TestExecutor(this.mockFixture))
                    {
                        using (BackgroundOperations profiling = BackgroundOperations.BeginProfiling(component, cancellationSource.Token))
                        {
                            while (!cancellationSource.IsCancellationRequested)
                            {
                                await Task.Delay(500);
                            }
                        }
                    }
                });

                Task etwMonitorTask = Task.Run(async () =>
                {
                    this.mockFixture.Parameters["ProviderType"] = "NonKernel";
                    this.mockFixture.Parameters["Provider"] = "Microsoft-Windows-DotNETRuntime";
                    this.mockFixture.Parameters["ProfilingWarmUpPeriod"] = "00:00:00";
                    this.mockFixture.Parameters["JsonFilters"] = "";

                    using (TestWindowsETWMonitor monitor = new TestWindowsETWMonitor(this.mockFixture))
                    {
                        await monitor.ExecuteAsync(cancellationSource.Token);
                        count = monitor.etwData != null ? monitor.etwData.Count : 0;
                    }
                });

                Task createEventTask = Task.Run(async () =>
                {
                    using (IProcessProxy process = this.mockFixture.ProcessManager.CreateElevatedProcess(this.mockFixture.Platform, "cmd.exe"))
                    {
                        await Task.Delay(10000);
                        await process.StartAndWaitAsync(cancellationSource.Token, TimeSpan.FromSeconds(30))
                            .ConfigureAwait(false);
                    }
                });

                await Task.WhenAll(timeoutTask, executorTask, etwMonitorTask, createEventTask).ConfigureAwait(false);

                // await Task.WhenAll(etwMonitorTask, etwMonitorTask2, executorTask, timeoutTask);

            }

            Assert.True(count > 0);
        }

        [Test]
        [Ignore("Invalid setup. This test checks to see if the application is running with elevated privileges. This should not be required for a unit test.")]
        public async Task WindowsETWMonitorExecuteProfilingInterval()
        {
            int count = 0;
            this.mockFixture.Parameters["ProfilingMode"] = "Interval";
            this.mockFixture.Parameters["ProfilingPeriod"] = "00:00:20";
            this.mockFixture.Parameters["ProfilingInterval"] = "00:00:00";
            using (CancellationTokenSource cancellationSource = new CancellationTokenSource())
            {
                Task timeoutTask = Task.Run(async () =>
                {
                    DateTime finishTime = DateTime.UtcNow.AddSeconds(20);
                    while (DateTime.UtcNow < finishTime)
                    {
                        await Task.Delay(100);
                    }

                    cancellationSource.Cancel();
                });

                Task etwMonitorTask = Task.Run(async () =>
                {
                    this.mockFixture.Parameters["ProviderType"] = "Kernel";
                    this.mockFixture.Parameters["Provider"] = "Windows Kernel";
                    this.mockFixture.Parameters["JsonFilters"] = "";
                    this.mockFixture.Parameters["ProviderKeywords"] = "ImageLoad,Process";
                    this.mockFixture.Parameters["ProviderEvents"] = "ImageLoad,ProcessStart,ProcessStop";
                    using (TestWindowsETWMonitor monitor = new TestWindowsETWMonitor(this.mockFixture))
                    {
                        await monitor.ExecuteAsync(cancellationSource.Token);
                        count = monitor.etwData != null ? monitor.etwData.Count : 0;
                    }
                });


                Task createEventTask = Task.Run(async () =>
                {
                    using (IProcessProxy process = this.mockFixture.ProcessManager.CreateElevatedProcess(this.mockFixture.Platform, "cmd.exe"))
                    {
                        await Task.Delay(4000);
                        await process.StartAndWaitAsync(cancellationSource.Token, TimeSpan.FromSeconds(30))
                            .ConfigureAwait(false);
                    }
                });

                await Task.WhenAll(timeoutTask, etwMonitorTask, createEventTask).ConfigureAwait(false);

            }

            Assert.True(count > 0);
        }

        private class TestWindowsETWMonitor : WindowsETWMonitor
        {
            public TestWindowsETWMonitor(MockFixture mockFixture)
                : base(mockFixture.Dependencies, mockFixture.Parameters)
            {
            }

            public new Task InitializeAsync(EventContext telemetryContext, CancellationToken cancellationToken)
            {
                return base.InitializeAsync(telemetryContext, cancellationToken);
            }

            public new void Validate()
            {
                base.Validate();
            }

            public new Task ExecuteAsync(EventContext telemetryContext, CancellationToken cancellationToken)
            {
                return base.ExecuteAsync(telemetryContext, cancellationToken);
            }

            public Dictionary<string, object> etwData => ETWData;
        }
    }
}
