// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
namespace VirtualClient.Actions
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Net.Http;
    using System.Reflection;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using VirtualClient.Common;
    using VirtualClient.Contracts;
    using Microsoft.CodeAnalysis;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Moq;
    using NUnit.Framework;
    using Polly;
    using VirtualClient.Common.Contracts;
    using VirtualClient.Common.Telemetry;
    using Architecture = System.Runtime.InteropServices.Architecture;

    [TestFixture]
    [NUnit.Framework.Category("Unit")]
    public class NginxServerExecutorTest
    {
        private MockFixture mockFixture;
        private InMemoryProcess memoryProcess;
        private Item<State> serverState;
        private TimeSpan timeout = TimeSpan.FromMinutes(10);
        private string packageName = "nginxconfiguration";

        [SetUp]
        public void SetupTests()
        {
            this.serverState = new Item<State>(nameof(State), new State());
            this.mockFixture = new MockFixture();
            this.memoryProcess = new InMemoryProcess
            {
                ExitCode = 0,
                OnStart = () => true,
                OnHasExited = () => true,
                StandardError = new ConcurrentBuffer(new StringBuilder($"nginx version: v1\n"))
            };
        }

        [Test]
        [TestCase(PlatformID.Win32NT, Architecture.X64)]
        [TestCase(PlatformID.Win32NT, Architecture.Arm64)]
        public void NginxServerExecutorThrowsErrorIfPlatformIsWrong(PlatformID platform, Architecture architecture)
        {
            this.mockFixture.Setup(platform, architecture, nameof(State));
            this.mockFixture.Parameters.Add(nameof(this.packageName), this.packageName);
            TestNginxServerExecutor executor = new TestNginxServerExecutor(this.mockFixture);
            Assert.ThrowsAsync<NotSupportedException>(async () =>
            {
                await executor.InitializeAsync().ConfigureAwait(false);
            });
        }

        [Test]
        [TestCase(PlatformID.Unix, Architecture.X64)]
        [TestCase(PlatformID.Unix, Architecture.Arm64)]
        public void NginxServerExecutorThrowsErrorPackageIsMissing(PlatformID platform, Architecture architecture)
        {
            this.mockFixture.Setup(platform, architecture, nameof(State));

            TestNginxServerExecutor executor = new TestNginxServerExecutor(this.mockFixture);
            Assert.ThrowsAsync<DependencyException>(async () =>
            {
                await executor.InitializeAsync().ConfigureAwait(false);
            });
        }

        [Test]
        [TestCase(PlatformID.Unix, Architecture.X64)]
        [TestCase(PlatformID.Unix, Architecture.Arm64)]
        public async Task NginxServerExecutorInitializesAsExpected(PlatformID platform, Architecture architecture)
        {
            /*
            When Nginx Server Initialize, these are the expectations:
                1.Set up Api Client for client and server.
                2.Verify packages are installed and shell scripts exist inside.
                3.Set up reset(execute: "setup - reset.sh") - only applicable for first time
                    a.This will ensure, server's sysctl configuration is saved so when virtual client exits, it is able to reset the server back to its original state.
                4.	Set up content. (execute: "setup-content.sh FileSizeInKB")
                    a.This will create a file that can be used during testing.
                5.	Set up config (execute: "setup-config.sh)
                    a.This will make changes to server configuration.
                6.	Delete any states that is saved in Server.
            */

            this.mockFixture.Setup(platform, architecture);
            this.mockFixture.Parameters.Add(nameof(this.packageName), this.packageName);
            this.mockFixture.Parameters.Add("FileSizeInKb", 5);
            TestNginxServerExecutor executor = new TestNginxServerExecutor(this.mockFixture);

            // pkg setup            
            DependencyFixture fixture = new DependencyFixture();
            fixture.Setup(platform, architecture);
            fixture.SetupPackage(this.packageName);
            string packagePath = executor.PlatformSpecifics.ToPlatformSpecificPath(fixture.PackageManager.FirstOrDefault(), platform, architecture).Path;
            
            this.mockFixture.PackageManager
                .Setup(x => x.GetPackageAsync(It.Is<string>(y => y == this.packageName), It.IsAny<CancellationToken>()))
                .ReturnsAsync(fixture.PackageManager.FirstOrDefault());

            string resetFilePath = executor.PlatformSpecifics.Combine(packagePath, "reset.sh");
            string resetOutput = Guid.NewGuid().ToString();
            int processCount = 0;
            
            this.mockFixture.ProcessManager.OnCreateProcess = (command, arguments, workingDir) =>
            {
                if (processCount == 0)
                {
                    Assert.AreEqual(command, "sudo");
                    Assert.AreEqual(workingDir, packagePath);
                    Assert.AreEqual(arguments, "bash setup-reset.sh");
                    this.memoryProcess.StandardOutput = new ConcurrentBuffer(new StringBuilder(resetOutput));
                }
                else if (processCount == 1)
                {
                    Assert.AreEqual(command, "sudo");
                    Assert.AreEqual(workingDir, packagePath);
                    Assert.AreEqual(arguments, "bash setup-content.sh 5");
                }
                else if (processCount == 2)
                {
                    Assert.AreEqual(command, "sudo");
                    Assert.AreEqual(workingDir, packagePath);
                    Assert.AreEqual(arguments, "bash setup-config.sh auto Client 1.2.3.5");
                }
                else
                {
                    Assert.Fail("Only 3 process expected.");
                }

                processCount++;
                return this.memoryProcess;
            };
            
            string[] expectedFiles = new string[]
            {
                executor.PlatformSpecifics.Combine(packagePath, "setup-reset.sh"),
                executor.PlatformSpecifics.Combine(packagePath, "setup-content.sh"),
                executor.PlatformSpecifics.Combine(packagePath,"setup-config.sh")
            };

            this.mockFixture.FileSystem.Setup(x => x.File.Exists(It.Is<string>(x => x == resetFilePath))).Returns(false);

            this.mockFixture.FileSystem
                .Setup(x => x.File.Exists(It.Is<string>(x => x != resetFilePath)))
                .Returns(true)
                .Callback((string fileName) =>
                {
                    if (!expectedFiles.Any(y => y == fileName))
                    {
                        Assert.Fail($"Unexpected File Name: {fileName}. \n{string.Join("\n", expectedFiles)}");
                    }
                });

            // Create file for reset
            Mock<InMemoryFileSystemStream> mockFileStream = new Mock<InMemoryFileSystemStream>();
            this.mockFixture.FileStream.Setup(f => f.New(It.IsAny<string>(), It.IsAny<FileMode>(), It.IsAny<FileAccess>(), It.IsAny<FileShare>()))
                .Returns(mockFileStream.Object)
                .Callback((string path, FileMode mode, FileAccess access, FileShare share) =>
                {
                    Assert.AreEqual(resetFilePath, path);
                    Assert.IsTrue(mode == FileMode.Create);
                    Assert.IsTrue(access == FileAccess.ReadWrite);
                    Assert.IsTrue(share == FileShare.ReadWrite);
                });

            mockFileStream
                .Setup(x => x.Write(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>()))
                .Callback((byte[] data, int offset, int count) =>
                {
                    byte[] byteData = Encoding.Default.GetBytes(resetOutput);
                    Assert.AreEqual(offset, 0);
                    Assert.AreEqual(count, byteData.Length);
                    Assert.AreEqual(data, byteData);
                });

            

            await executor.InitializeAsync().ConfigureAwait(false);
            Assert.AreEqual(processCount, 3);
            this.mockFixture.FileSystem.Verify(x => x.File.Exists(It.IsAny<string>()), Times.Exactly(4));
        }

        [Test]
        [TestCase(PlatformID.Unix, Architecture.X64)]
        [TestCase(PlatformID.Unix, Architecture.Arm64)]
        public async Task NginxServerExecutorInitializesAsExpectedForSecondTime(PlatformID platform, Architecture architecture)
        {            
            this.mockFixture.Setup(platform, architecture, nameof(State));            
            this.mockFixture.Parameters.Add(nameof(this.packageName), this.packageName);
            TestNginxServerExecutor executor = new TestNginxServerExecutor(this.mockFixture);
            DependencyFixture fixture = new DependencyFixture();
            fixture.Setup(platform, architecture);
            fixture.SetupPackage(this.packageName);
            string packagePath = executor.PlatformSpecifics.ToPlatformSpecificPath(fixture.PackageManager.FirstOrDefault(), platform, architecture).Path;

            int processCount = 0;
            this.mockFixture.ProcessManager.OnCreateProcess = (command, arguments, workingDir) =>
            {
                if (processCount == 0)
                {
                    Assert.AreEqual(command, "sudo");
                    Assert.AreEqual(workingDir, packagePath);
                    Assert.AreEqual(arguments, "bash setup-content.sh 1");
                }
                else if (processCount == 1)
                {
                    Assert.AreEqual(command, "sudo");
                    Assert.AreEqual(workingDir, packagePath);
                    Assert.AreEqual(arguments, "bash setup-config.sh auto Client 1.2.3.5");
                }
                else
                {
                    Assert.Fail("Only 2 process expected.");
                }

                processCount++;
                return this.memoryProcess;
            };

            this.mockFixture.PackageManager
               .Setup(x => x.GetPackageAsync(It.Is<string>(y => y == this.packageName), It.IsAny<CancellationToken>()))
               .ReturnsAsync(fixture.PackageManager.FirstOrDefault());

            var expectedFiles = new string[]
            {
                executor.PlatformSpecifics.Combine(packagePath, "setup-reset.sh"),
                executor.PlatformSpecifics.Combine(packagePath, "setup-content.sh"),
                executor.PlatformSpecifics.Combine(packagePath,"setup-config.sh"),
                executor.PlatformSpecifics.Combine(packagePath, "reset.sh")
            };

            this.mockFixture.FileSystem
                .Setup(x => x.File.Exists(It.IsAny<string>()))
                .Returns(true)
                .Callback((string fileName) =>
                {
                    if(!expectedFiles.Any(y => y == fileName))
                    {
                        Assert.Fail($"Unexpected File Name: {fileName}. \n{string.Join("\n", expectedFiles)}");
                    }
                });

            await executor.InitializeAsync().ConfigureAwait(false);
            Assert.AreEqual(processCount, 2);
            this.mockFixture.FileSystem.Verify(x => x.File.Exists(It.IsAny<string>()), Times.Exactly(4));
        }

        [Test]
        [TestCase(PlatformID.Unix, Architecture.X64)]
        [TestCase(PlatformID.Unix, Architecture.Arm64)]
        public async Task NginxServerExecutorResetsServerAsExpected(PlatformID platform, Architecture architecture)
        {
            TimeSpan timeout = TimeSpan.FromMinutes(5);
            this.mockFixture.Setup(platform, architecture, nameof(State));
            int processCount = 0;           

            this.mockFixture.ProcessManager.OnCreateProcess = (command, arguments, workingDir) =>
            {
                if (processCount == 0)
                {
                    // During every reset, nginx will delete existing content.
                    // "delete-content.sh" is downloaded from blob.
                    Assert.AreEqual(arguments, "bash reset.sh");
                }
                else
                {
                    Assert.AreEqual(arguments, "systemctl disable nginx", NginxCommand.Stop.ConvertToCommandArgs());
                }

                Assert.AreEqual(command, "sudo");
                Assert.IsNull(workingDir);
                processCount++;
                return this.memoryProcess;
            };


            TestNginxServerExecutor executor = new TestNginxServerExecutor(this.mockFixture);
            await executor.ResetNginxAsync().ConfigureAwait(false);
            Assert.AreEqual(processCount, 2);
            
            this.mockFixture.ApiClient.Verify(x => x.UpdateStateAsync(
                It.Is<string>(x => x == nameof(State)),
                It.Is<Item<State>>(x => x.Definition.Online(null) == false),
                It.IsAny<CancellationToken>(),
                It.IsAny<IAsyncPolicy<HttpResponseMessage>>()),
                Times.Exactly(1));
        }

        [Test]
        [TestCase(PlatformID.Unix, Architecture.X64)]
        [TestCase(PlatformID.Unix, Architecture.Arm64)]
        public void ResetServerWillSwallowExceptions(PlatformID platform, Architecture architecture)
        {
            this.mockFixture.Setup(platform, architecture, nameof(State));
            this.mockFixture.ProcessManager.OnCreateProcess = (command, arguments, workingDir) => { throw new SchemaException(); };

            TestNginxServerExecutor executor = new TestNginxServerExecutor(this.mockFixture);
            Assert.DoesNotThrowAsync(async () => await executor.ResetNginxAsync().ConfigureAwait(false));
        }

        [Test]
        [TestCase(PlatformID.Unix, Architecture.X64)]
        [TestCase(PlatformID.Unix, Architecture.Arm64)]
        public void NginxServerExecutorWillResetServerDuringDispose(PlatformID platform, Architecture architecture)
        {
            // Dispose will call reset nginx
            TimeSpan timeout = TimeSpan.FromMinutes(5);
            this.mockFixture.Setup(platform, architecture, nameof(State));
            int processCount = 0;

            this.mockFixture.ProcessManager.OnCreateProcess = (command, arguments, workingDir) =>
            {
                if (processCount == 0)
                {
                    // During every reset, nginx will delete existing content.
                    // "delete-content.sh" is downloaded from blob.
                    Assert.AreEqual(arguments, "bash reset.sh");
                }
                else
                {
                    Assert.AreEqual(arguments, "systemctl disable nginx", NginxCommand.Stop.ConvertToCommandArgs());
                }

                Assert.AreEqual(command, "sudo");
                Assert.IsNull(workingDir);
                processCount++;
                return this.memoryProcess;
            };


            TestNginxServerExecutor executor = new TestNginxServerExecutor(this.mockFixture);
            executor.Dispose(true);
            Assert.AreEqual(processCount, 2);
            this.mockFixture.ApiClient.Verify(x => x.UpdateStateAsync(
                    It.Is<string>(x => x == nameof(State)), 
                    It.Is<Item<State>>(x => x.Definition.Online(null) == false), 
                    It.IsAny<CancellationToken>(), 
                    It.IsAny<IAsyncPolicy<HttpResponseMessage>>()), 
                    Times.Exactly(1));
        }

        [Test]
        [TestCase(PlatformID.Unix, Architecture.X64)]
        [TestCase(PlatformID.Unix, Architecture.Arm64)]
        public async Task NginxServerExecutorRunsAsExpected(PlatformID platform, Architecture architecture)
        {
            this.mockFixture.Setup(platform, architecture, nameof(State));
            this.mockFixture.Parameters.Add(nameof(this.packageName), this.packageName);
            this.mockFixture.Parameters.Add(nameof(this.timeout), this.timeout.ToString());
            this.mockFixture.Parameters.Add("pollingInterval", TimeSpan.FromSeconds(1).ToString());

            int nginxServiceCalls = 0;
            int shellScriptCalls = 0;

            TestNginxServerExecutor executor = new TestNginxServerExecutor(this.mockFixture);
            DependencyFixture fixture = new DependencyFixture();
            fixture.Setup(platform, architecture);
            fixture.SetupPackage(packageName);
            
            string packagePath = executor.PlatformSpecifics.ToPlatformSpecificPath(fixture.PackageManager.FirstOrDefault(), platform, architecture).Path;
            this.mockFixture.PackageManager.Setup(x => x.GetPackageAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(fixture.PackageManager.FirstOrDefault());
            this.mockFixture.FileSystem.Setup(x => x.File.Exists(It.IsAny<string>())).Returns(true);
            this.mockFixture.ProcessManager.OnCreateProcess = (command, arguments, workingDir) =>
            {
                Assert.AreEqual(command, "sudo");

                if (new[] {"bash setup-content.sh 1", "bash setup-config.sh", "bash setup-config.sh auto Client 1.2.3.5", "bash reset.sh" }.Contains(arguments, StringComparer.OrdinalIgnoreCase))
                {
                    shellScriptCalls++;
                    Assert.AreEqual(workingDir, packagePath);
                }
                else if (new[] { "systemctl restart nginx", "systemctl disable nginx"}.Contains(arguments, StringComparer.OrdinalIgnoreCase))
                {
                    nginxServiceCalls++;
                    Assert.IsNull(workingDir);
                }
                else if (arguments == "nginx -V")
                {
                    nginxServiceCalls++;
                    string examplesDirectory = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "Examples", "Nginx");
                    string outputPath = Path.Combine(examplesDirectory, @"NginxVersionExample.txt");
                    string rawText = File.ReadAllText(outputPath);
                    this.memoryProcess.StandardError = new ConcurrentBuffer(new StringBuilder(rawText));
                }
                else
                {
                    Assert.Fail($"Unexpected Arguments: {arguments}");
                }
                                
                return this.memoryProcess;
            };


            Item<State> onlineClientState = new Item<State>(nameof(State), new State());
            onlineClientState.Definition.Online(true);

            Item<State> expiredState = new Item<State>(nameof(State), new State());
            expiredState.Definition.Timeout(DateTime.UtcNow.AddDays(-1));

            // Set up for polling online client state.           
            this.mockFixture.ApiClient.SetupSequence(client => client.GetStateAsync(It.IsAny<string>(), It.IsAny<CancellationToken>(), It.IsAny<IAsyncPolicy<HttpResponseMessage>>()))
                .ReturnsAsync(this.mockFixture.CreateHttpResponse(System.Net.HttpStatusCode.OK, onlineClientState)) // Polling for online state
                .ReturnsAsync(this.mockFixture.CreateHttpResponse(System.Net.HttpStatusCode.OK, expiredState)); // Get client state
                                                                                                                        
            this.mockFixture.ApiClient.Setup(x => x.CreateStateAsync(It.IsAny<string>(), It.IsAny<State>(), It.IsAny<CancellationToken>(), It.IsAny<IAsyncPolicy<HttpResponseMessage>>()))
                .ReturnsAsync(this.mockFixture.CreateHttpResponse(System.Net.HttpStatusCode.OK));

            this.mockFixture.ApiClient.Setup(x => x.UpdateStateAsync(It.IsAny<string>(), It.IsAny<Item<State>>(), It.IsAny<CancellationToken>(), It.IsAny<IAsyncPolicy<HttpResponseMessage>>()))
                .ReturnsAsync(this.mockFixture.CreateHttpResponse(System.Net.HttpStatusCode.OK));

            await executor.InitializeAsync().ConfigureAwait(false);
            await executor.ExecuteAsync().ConfigureAwait(false);

            Assert.AreEqual(shellScriptCalls, 3);
            Assert.AreEqual(nginxServiceCalls, 3);

            // nginx version and local online state
            this.mockFixture.ApiClient.Verify(x => x.CreateStateAsync(It.IsAny<string>(), It.IsAny<State>(), It.IsAny<CancellationToken>(), It.IsAny<IAsyncPolicy<HttpResponseMessage>>()), Times.Exactly(2));

            // first loop and reset
            this.mockFixture.ApiClient.Verify(x => x.UpdateStateAsync(It.IsAny<string>(), It.IsAny<Item<State>>(), It.IsAny<CancellationToken>(), It.IsAny<IAsyncPolicy<HttpResponseMessage>>()), Times.Exactly(2));
        }

        [Test]
        [TestCase(PlatformID.Unix, Architecture.X64)]
        [TestCase(PlatformID.Unix, Architecture.Arm64)]
        public void NginxServerExecutorWillResetServerIfFailure(PlatformID platform, Architecture architecture)
        {
            this.mockFixture.Setup(platform, architecture, nameof(State));
            int processCount = 0;
            this.mockFixture.ProcessManager.OnCreateProcess = (command, arguments, workingDir) =>
            {
                if (arguments == "nginx -V")
                {
                    // This will ensure nginx version is empty therefore ArgumentException will be thrown.
                    this.memoryProcess.StandardError = new ConcurrentBuffer(new StringBuilder(string.Empty));
                }
                else if (arguments == "systemctl disable nginx" || arguments == "bash reset.sh")
                {
                }
                else
                {
                    Assert.Fail();
                }

                processCount++;
                return this.memoryProcess;
            };

            TestNginxServerExecutor executor = new TestNginxServerExecutor(this.mockFixture);
            Assert.ThrowsAsync<ArgumentException>(async () => await executor.ExecuteAsync().ConfigureAwait(false));
            Assert.AreEqual(processCount, 3);

            // Expected to delete local server state & will create new offline state.
            this.mockFixture.ApiClient.Verify(x => x.UpdateStateAsync(
                It.Is<string>(x => x == nameof(State)),
                It.Is<Item<State>>(x => x.Definition.Online(null) == false),
                It.IsAny<CancellationToken>(),
                It.IsAny<IAsyncPolicy<HttpResponseMessage>>()),
                Times.Exactly(1));
        }

        [Test]
        [TestCase(PlatformID.Unix, Architecture.X64)]
        [TestCase(PlatformID.Unix, Architecture.Arm64)]
        public async Task NginxExecutorParsesNginxVersion(PlatformID platform, Architecture architecture)
        {
            this.mockFixture.Setup(platform, architecture, nameof(State));
            string examplesDirectory = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "Examples", "Nginx");
            string outputPath = Path.Combine(examplesDirectory, @"NginxVersionExample.txt");
            string rawText = File.ReadAllText(outputPath);

            this.mockFixture.ProcessManager.OnCreateProcess = (command, arguments, workingDir) =>
            {
                if (arguments == "nginx -V")
                {
                    // This will ensure nginx version is empty therefore ArgumentException will be thrown.
                    this.memoryProcess.StandardError = new ConcurrentBuffer(new StringBuilder(rawText));                    
                }
                else
                {
                    Assert.Fail();
                }

                return this.memoryProcess;
            };

            TestNginxServerExecutor executor = new TestNginxServerExecutor(this.mockFixture);
            Dictionary<string, string> version = await executor.GetNginxVersionAsync().ConfigureAwait(false);
                        
            Assert.AreEqual(version["nginxVersion"], "nginx/1.18.0 (Ubuntu)");
            Assert.IsTrue(version["sslVersion"].Contains("1.1.1f", StringComparison.OrdinalIgnoreCase));
            Assert.AreEqual(version["serverNameIndicationSupport"], "TLS SNI support enabled");
            Assert.IsTrue(version["arguments"].StartsWith("--with-cc-opt='-g -O2"));
        }

        private class TestNginxServerExecutor : NginxServerExecutor
        {
            public TestNginxServerExecutor(MockFixture mockFixture, IDictionary<string, IConvertible> parameters = null)
                : base(mockFixture.Dependencies, mockFixture.Parameters)
            {
                this.ServerApi = mockFixture.ApiClient.Object;
                this.ClientApi = mockFixture.ApiClient.Object;
            }

            public new void Dispose(bool disposing)
            {
                base.Dispose(disposing);
            }

            public async Task InitializeAsync()
            {
                await base.InitializeAsync(EventContext.None, CancellationToken.None).ConfigureAwait(false);
            }

            public async Task ExecuteAsync()
            {
                await base.ExecuteAsync(EventContext.None, CancellationToken.None).ConfigureAwait(false);
            }

            public async Task ResetNginxAsync()
            {
                await base.ResetNginxAsync(EventContext.None, CancellationToken.None).ConfigureAwait(false);
            }

            public async Task<Dictionary<string, string>> GetNginxVersionAsync()
            {
                return await base.GetNginxVersionAsync(EventContext.None, CancellationToken.None).ConfigureAwait(false);
            }
        }
    }
}
