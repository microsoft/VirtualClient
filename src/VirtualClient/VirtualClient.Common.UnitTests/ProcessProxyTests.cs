namespace VirtualClient.Common
{
    using System;
    using System.Diagnostics;
    using System.Diagnostics.CodeAnalysis;
    using System.IO;
    using System.Threading;
    using System.Threading.Tasks;
    using NUnit.Framework;

    [TestFixture]
    [Category("Unit")]
    [SuppressMessage("Design", "CA1001:Types that own disposable fields should be disposable", Justification = "Disposed in the individual tests.")]
    public class ProcessProxyTests
    {
        private Process mockProcess;

        [SetUp]
        public void SetupTest()
        {
            this.mockProcess = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = @"C:\any\command.exe",
                    Arguments = "--argument1=1234 --argument2=value",
                    RedirectStandardError = true,
                    RedirectStandardOutput = true,
                    UseShellExecute = false
                }
            };
        }

        [TearDown]
        public void CleanupTest()
        {
            this.mockProcess.Dispose();
        }

        [Test]
        public void ProcessProxyRedirectPropertiesSetExpectedPropertiesOnTheStartInfo()
        {
            using (ProcessProxy process = new ProcessProxy(this.mockProcess))
            {
                process.RedirectStandardError = false;
                process.RedirectStandardInput = false;
                process.RedirectStandardOutput = false;

                Assert.IsFalse(process.StartInfo.RedirectStandardError);
                Assert.IsFalse(process.StartInfo.RedirectStandardInput);
                Assert.IsFalse(process.StartInfo.RedirectStandardOutput);

                process.RedirectStandardError = true;
                process.RedirectStandardInput = true;
                process.RedirectStandardOutput = true;

                Assert.IsTrue(process.StartInfo.RedirectStandardError);
                Assert.IsTrue(process.StartInfo.RedirectStandardInput);
                Assert.IsTrue(process.StartInfo.RedirectStandardOutput);
            }
        }

        [Test]
        public void ProcessProxyEnablesSessionEnvironmentVariablesToBeSet()
        {
            using (ProcessProxy process = new ProcessProxy(this.mockProcess))
            {
                Assert.IsNotNull(process.EnvironmentVariables);
            }
        }

        [Test]
        public async Task ProcessProxyStartTimesAreNotAffectedByTheProcessHavingBeenDisposed()
        {
            IProcessProxy process = null;
            ProcessStartInfo startInfo = new ProcessStartInfo
            {
                FileName = "whoami",
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardError = true,
                RedirectStandardOutput = true
            };

            using (process = new ProcessProxy(new Process { StartInfo = startInfo }))
            {
                await process.StartAndWaitAsync(CancellationToken.None);
                await Task.Delay(500);
            }

            // This will throw if the object is disposed.
            DateTime startTime = process.StartTime;
            Assert.IsTrue(startTime != DateTime.MinValue);
        }

        [Test]
        public async Task ProcessProxyExitTimesAreNotAffectedByTheProcessHavingBeenDisposed()
        {
            IProcessProxy process = null;
            ProcessStartInfo startInfo = new ProcessStartInfo
            {
                FileName = "whoami",
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardError = true,
                RedirectStandardOutput = true
            };

            using (process = new ProcessProxy(new Process { StartInfo = startInfo }))
            {
                await process.StartAndWaitAsync(CancellationToken.None);
                await Task.Delay(500);
            }

            // This will throw if the object is disposed.
            DateTime exitTime = process.ExitTime;
            Assert.IsTrue(exitTime != DateTime.MinValue);
        }

        [Test]
        public async Task ProcessProxyWaitForExitAsyncHandlesTimeoutExceptionAsExpected()
        {
            IProcessProxy process = null;
            ProcessStartInfo startInfo = new ProcessStartInfo
            {
                FileName = "ping",
                Arguments = "localhost -n 2", // This will run for about 2 seconds
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardError = true,
                RedirectStandardOutput = true
            };

            using (process = new ProcessProxy(new Process { StartInfo = startInfo }))
            {
                // Test Case 1: When timeout is null, no TimeoutException should be thrown
                // The process will complete normally
                await process.StartAndWaitAsync(CancellationToken.None);
                Assert.IsTrue(process.HasExited);

                // Test Case 2: When timeout is specified and process takes longer, TimeoutException should be caught
                process = new ProcessProxy(new Process { StartInfo = startInfo });
                await process.StartAndWaitAsync(CancellationToken.None, TimeSpan.FromMilliseconds(100));
                // If we get here, the TimeoutException was caught as expected
                Assert.IsTrue(true);
            }
        }
    }
}
