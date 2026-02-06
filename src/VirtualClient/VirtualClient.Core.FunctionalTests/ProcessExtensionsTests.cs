// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient
{
    using System;
    using System.Collections.Generic;
    using System.IO.Abstractions;
    using System.Linq;
    using System.Runtime.InteropServices;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Logging.Abstractions;
    using NUnit.Framework;
    using VirtualClient.Common;
    using VirtualClient.Common.Telemetry;
    using VirtualClient.Contracts;

    [TestFixture]
    [Category("Functional")]
    internal class ProcessExtensionsTests
    {
        private IServiceCollection dependencies;

        // There is a race condition-style flaw in the .NET implementation of the
        // WaitForExit() method. The race condition allows for the process to exit after
        // completion but for a period of time to pass before the kernel completes all finalization
        // and cleanup steps (e.g. setting an exit code). To help prevent downstream issues that
        // happen when attempting to access properties on the process during this race condition period
        // of time, we are adding in an extra check on the process HasExited.
        //
        // Example of error hit during race condition period of time:
        // Process must exit before requested information can be determined.
        //
        // Note:
        // We are running actual processes here as part of the functional test and this
        // is entirely unusual. However, there is no way to evaluate working order of this feature
        // (and the issues we found) without engaging the OS kernel in the process. The processes
        // we are running are very minimal so as to cause little issues during the test runner process.

        [OneTimeSetUp]
        public void SetupFixture()
        {
            PlatformSpecifics platformSpecifics = new PlatformSpecifics(
                Environment.OSVersion.Platform, 
                RuntimeInformation.ProcessArchitecture);

            ISystemManagement systemManagement = DependencyFactory.CreateSystemManager(
                Environment.MachineName, 
                Guid.NewGuid().ToString(), 
                platformSpecifics);

            this.dependencies = new ServiceCollection();
            this.dependencies.AddSingleton<IFileSystem>(systemManagement.FileSystem);
            this.dependencies.AddSingleton<ILogger>(NullLogger.Instance);
            this.dependencies.AddSingleton<PlatformSpecifics>(platformSpecifics);
            this.dependencies.AddSingleton<ISystemInfo>(systemManagement);
            this.dependencies.AddSingleton<ISystemManagement>(systemManagement);
        }

        [Test]
        [Platform("Win")]
        public async Task StartAndWaitAsyncExtensionToLogProcessWorkflowWorksAsExpected_Windows_Systems_1()
        {
            try
            {
                // Scenario:
                // Default workflow. Process exits gracefully and the details are logged.

                var parameters = new Dictionary<string, IConvertible>
                {
                    { "Scenario", "Process_Workflow_Win_1" }
                };

                using (var executor = new TestExecutor(this.dependencies, parameters))
                {
                    using (var tokenSource = new CancellationTokenSource())
                    {
                        var processManager = new WindowsProcessManager();
                        using (IProcessProxy process = processManager.CreateProcess("ipconfig", "/all"))
                        {
                            await process.StartAndWaitAsync(tokenSource.Token, withExitConfirmation: true);
                            await executor.LogProcessDetailsAsync(process, EventContext.None, "ipconfig");
                        }
                    }
                }
            }
            catch (Exception exc)
            {
                Assert.Fail($"Process workflow error unhandled: {exc.Message}");
            }
        }

        [Test]
        [Platform("Win")]
        public async Task StartAndWaitAsyncExtensionToLogProcessWorkflowWorksAsExpected_Windows_Systems_2()
        {
            try
            {
                // Scenario:
                // A cancellation request is issued before/during the process execution.

                var parameters = new Dictionary<string, IConvertible>
                {
                    { "Scenario", "Process_Workflow_Win_2" }
                };

                using (var executor = new TestExecutor(this.dependencies, parameters))
                {
                    using (var tokenSource = new CancellationTokenSource())
                    {
                        var processManager = new WindowsProcessManager();
                        using (IProcessProxy process = processManager.CreateProcess("ipconfig", "/all"))
                        {
                            await tokenSource.CancelAsync();
                            await process.StartAndWaitAsync(tokenSource.Token, withExitConfirmation: true);
                            await executor.LogProcessDetailsAsync(process, EventContext.None, "ipconfig");
                        }
                    }
                }
            }
            catch (Exception exc)
            {
                Assert.Fail($"Process workflow error unhandled: {exc.Message}");
            }
        }

        [Test]
        [Platform("Win")]
        public async Task StartAndWaitAsyncExtensionToLogProcessWorkflowWorksAsExpected_Windows_Systems_3()
        {
            try
            {
                // Scenario:
                // A timeout is hit before the process exits. A TimeoutException should be thrown.

                var parameters = new Dictionary<string, IConvertible>
                {
                    { "Scenario", "Process_Workflow_Win_3" }
                };

                using (var executor = new TestExecutor(this.dependencies, parameters))
                {
                    using (var tokenSource = new CancellationTokenSource())
                    {
                        var processManager = new WindowsProcessManager();
                        using (IProcessProxy process = processManager.CreateProcess("ipconfig", "/all"))
                        {
                            await process.StartAndWaitAsync(tokenSource.Token, timeout: TimeSpan.Zero, withExitConfirmation: true);
                        }
                    }
                }
            }
            catch (TimeoutException)
            {
                // This is the expected exception.
            }
            catch (Exception exc)
            {
                Assert.Fail($"Process workflow error on timeout unhandled: {exc.Message}");
            }
        }

        [Test]
        [Platform("Unix")]
        public async Task StartAndWaitAsyncExtensionToLogProcessWorkflowWorksAsExpected_Unix_Systems_1()
        {
            try
            {
                // Scenario:
                // Default workflow. Process exits gracefully and the details are logged.

                var parameters = new Dictionary<string, IConvertible>
                {
                    { "Scenario", "Process_Workflow_Unix_1" }
                };

                using (var executor = new TestExecutor(this.dependencies, parameters))
                {
                    using (var tokenSource = new CancellationTokenSource())
                    {
                        var processManager = new UnixProcessManager();
                        using (IProcessProxy process = processManager.CreateProcess("bash", "--version"))
                        {
                            await process.StartAndWaitAsync(tokenSource.Token, withExitConfirmation: true);
                            await executor.LogProcessDetailsAsync(process, EventContext.None, "bash");
                        }
                    }
                }
            }
            catch (Exception exc)
            {
                Assert.Fail($"Process workflow error unhandled: {exc.Message}");
            }
        }

        [Test]
        [Platform("Unix")]
        public async Task StartAndWaitAsyncExtensionToLogProcessWorkflowWorksAsExpected_Unix_Systems_2()
        {
            try
            {
                // Scenario:
                // A cancellation request is issued before/during the process execution.

                var parameters = new Dictionary<string, IConvertible>
                {
                    { "Scenario", "Process_Workflow_Unix_2" }
                };

                using (var executor = new TestExecutor(this.dependencies, parameters))
                {
                    using (var tokenSource = new CancellationTokenSource())
                    {
                        var processManager = new UnixProcessManager();
                        using (IProcessProxy process = processManager.CreateProcess("bash", "--version"))
                        {
                            await tokenSource.CancelAsync();
                            await process.StartAndWaitAsync(tokenSource.Token, withExitConfirmation: true);
                            await executor.LogProcessDetailsAsync(process, EventContext.None, "bash");
                        }
                    }
                }
            }
            catch (Exception exc)
            {
                Assert.Fail($"Process workflow error unhandled: {exc.Message}");
            }
        }

        [Test]
        [Platform("Unix")]
        public async Task StartAndWaitAsyncExtensionToLogProcessWorkflowWorksAsExpected_Unix_Systems_3()
        {
            try
            {
                // Scenario:
                // A timeout is hit before the process exits. A TimeoutException should be thrown.

                var parameters = new Dictionary<string, IConvertible>
                {
                    { "Scenario", "Process_Workflow_Unix_3" }
                };

                using (var executor = new TestExecutor(this.dependencies, parameters))
                {
                    using (var tokenSource = new CancellationTokenSource())
                    {
                        var processManager = new UnixProcessManager();
                        using (IProcessProxy process = processManager.CreateProcess("bash", "--version"))
                        {
                            await process.StartAndWaitAsync(tokenSource.Token, timeout: TimeSpan.Zero, withExitConfirmation: true);
                        }
                    }
                }
            }
            catch (TimeoutException)
            {
                // This is the expected exception.
            }
            catch (Exception exc)
            {
                Assert.Fail($"Process workflow error on timeout unhandled: {exc.Message}");
            }
        }
    }
}
