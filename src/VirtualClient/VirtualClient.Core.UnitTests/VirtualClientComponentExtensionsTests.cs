// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient
{
    using System;
    using System.Collections.Generic;
    using System.IO.Abstractions;
    using System.Threading;
    using System.Threading.Tasks;
    using Moq;
    using NUnit.Framework;
    using VirtualClient.Common;
    using VirtualClient.Common.Telemetry;
    using VirtualClient.Contracts;

    [TestFixture]
    [Category("Unit")]
    internal class VirtualClientComponentExtensionsTests
    {
        private MockFixture mockFixture;

        public void SetupDefaults(PlatformID platform)
        {
            this.mockFixture = new MockFixture();
            this.mockFixture.Setup(platform);
        }

        [Test]
        public void CreateFileUploadDescriptorExtensionReturnsTheExpectedDefaultDescriptor()
        {
            this.SetupDefaults(PlatformID.Unix);

            Mock<IFileInfo> mockFile = new Mock<IFileInfo>();
            mockFile.Setup(f => f.Name).Returns("file.log");
            mockFile.Setup(f => f.FullName).Returns("/home/any/path/to/file.log");
            mockFile.Setup(f => f.CreationTime).Returns(DateTime.Now);
            mockFile.Setup(f => f.CreationTimeUtc).Returns(DateTime.UtcNow);

            using (TestExecutor component = new TestExecutor(this.mockFixture))
            {
                lock (ComponentTypeCache.LockObject)
                {
                    try
                    {
                        ComponentTypeCache.Instance.Clear();
                        FileContext fileContext = new FileContext(mockFile.Object, "text/plain", "utf-8", component.ExperimentId);
                        FileUploadDescriptor descriptor = component.CreateFileUploadDescriptor(fileContext);

                        Assert.IsNotNull(descriptor);
                        Assert.IsTrue(descriptor.Manifest.TryGetValue("pathFormat", out IConvertible format));
                        Assert.AreEqual(FileUploadDescriptorFactory.Default, format.ToString());
                    }
                    finally
                    {
                        ComponentTypeCache.Instance.Clear();
                        ComponentTypeCache.Instance.DescriptorFactoryCache.Clear();
                    }
                }
            }
        }

        [Test]
        public void CreateFileUploadDescriptorExtensionReturnsTheExpectedDescriptorForASpecificPathStructure()
        {
            this.SetupDefaults(PlatformID.Unix);

            Mock<IFileInfo> mockFile = new Mock<IFileInfo>();
            mockFile.Setup(f => f.Name).Returns("file.log");
            mockFile.Setup(f => f.FullName).Returns("/home/any/path/to/file.log");
            mockFile.Setup(f => f.CreationTime).Returns(DateTime.Now);
            mockFile.Setup(f => f.CreationTimeUtc).Returns(DateTime.UtcNow);

            using (TestExecutor component = new TestExecutor(this.mockFixture))
            {
                component.ContentPathFormat = "Format1234";

                lock (ComponentTypeCache.LockObject)
                {
                    try
                    {
                        ComponentTypeCache.Instance.Clear();
                        ComponentTypeCache.Instance.Add(new ComponentType(typeof(TestFileUploadDescriptorFactory_A)));

                        FileContext fileContext = new FileContext(mockFile.Object, "text/plain", "utf-8", component.ExperimentId);
                        FileUploadDescriptor descriptor = component.CreateFileUploadDescriptor(fileContext);

                        Assert.IsNotNull(descriptor);
                        Assert.IsTrue(descriptor.Manifest.TryGetValue("pathFormat", out IConvertible format));
                        Assert.AreEqual("Format1234", format.ToString());
                    }
                    finally
                    {
                        ComponentTypeCache.Instance.Clear();
                        ComponentTypeCache.Instance.DescriptorFactoryCache.Clear();
                    }
                }
            }
        }

        [Test]
        public async Task ExecuteCommandAsyncExtensionExecutesTheExpectedProcessOnWindowsSystems()
        {
            this.SetupDefaults(PlatformID.Win32NT);

            string command = "anycommand.exe";
            string commandArguments = "--option1=123 --option2=456";
            string workingDirectory = MockFixture.TestAssemblyDirectory;

            using (TestExecutor component = new TestExecutor(this.mockFixture))
            {
                using (IProcessProxy process = await component.ExecuteCommandAsync(command, commandArguments, workingDirectory, EventContext.None, CancellationToken.None))
                {
                    Assert.IsNotNull(process.StartInfo);
                    Assert.AreEqual(command, process.StartInfo.FileName);
                    Assert.AreEqual(commandArguments, process.StartInfo.Arguments);
                    Assert.AreEqual(workingDirectory, process.StartInfo.WorkingDirectory);
                }
            }
        }

        [Test]
        public async Task ExecuteCommandAsyncExtensionExecutesTheExpectedProcessOnWindowsSystemsWhenRunningElevated()
        {
            this.SetupDefaults(PlatformID.Win32NT);

            // There is no different on Windows systems.
            string command = "anycommand.exe";
            string commandArguments = "--option1=123 --option2=456";
            string workingDirectory = MockFixture.TestAssemblyDirectory;

            using (TestExecutor component = new TestExecutor(this.mockFixture))
            {
                using (IProcessProxy process = await component.ExecuteCommandAsync(command, commandArguments, workingDirectory, EventContext.None, CancellationToken.None, runElevated: true))
                {
                    Assert.IsNotNull(process.StartInfo);
                    Assert.AreEqual(command, process.StartInfo.FileName);
                    Assert.AreEqual(commandArguments, process.StartInfo.Arguments);
                    Assert.AreEqual(workingDirectory, process.StartInfo.WorkingDirectory);
                }
            }
        }

        [Test]
        public async Task ExecuteCommandAsyncExtensionExecutesTheExpectedProcessOnUnixSystems()
        {
            this.SetupDefaults(PlatformID.Unix);

            string command = "anycommand";
            string commandArguments = "--option1=123 --option2=456";
            string workingDirectory = MockFixture.TestAssemblyDirectory;

            using (TestExecutor component = new TestExecutor(this.mockFixture))
            {
                using (IProcessProxy process = await component.ExecuteCommandAsync(command, commandArguments, workingDirectory, EventContext.None, CancellationToken.None))
                {
                    Assert.IsNotNull(process.StartInfo);
                    Assert.AreEqual(command, process.StartInfo.FileName);
                    Assert.AreEqual(commandArguments, process.StartInfo.Arguments);
                    Assert.AreEqual(workingDirectory, process.StartInfo.WorkingDirectory);
                }
            }
        }

        [Test]
        public async Task ExecuteCommandAsyncExtensionExecutesTheExpectedProcessOnUnixSystemsWhenRunningElevated()
        {
            this.SetupDefaults(PlatformID.Unix);

            string command = "anycommand";
            string commandArguments = "--option1=123 --option2=456";
            string workingDirectory = MockFixture.TestAssemblyDirectory;

            using (TestExecutor component = new TestExecutor(this.mockFixture))
            {
                using (IProcessProxy process = await component.ExecuteCommandAsync(command, commandArguments, workingDirectory, EventContext.None, CancellationToken.None, runElevated: true))
                {
                    Assert.IsNotNull(process.StartInfo);
                    Assert.AreEqual("sudo", process.StartInfo.FileName);
                    Assert.AreEqual($"{command} {commandArguments}", process.StartInfo.Arguments);
                    Assert.AreEqual(workingDirectory, process.StartInfo.WorkingDirectory);
                }
            }
        }

        [Test]
        public async Task ExecuteCommandAsyncExtensionExecutesTheExpectedProcessOnUnixSystemsWhenRunningElevatedAndAUsernameIsSupplied()
        {
            this.SetupDefaults(PlatformID.Unix);

            string username = "anyuser";
            string command = "anycommand";
            string commandArguments = "--option1=123 --option2=456";
            string workingDirectory = MockFixture.TestAssemblyDirectory;

            using (TestExecutor component = new TestExecutor(this.mockFixture))
            {
                using (IProcessProxy process = await component.ExecuteCommandAsync(command, commandArguments, workingDirectory, EventContext.None, CancellationToken.None, runElevated: true, username: username))
                {
                    Assert.IsNotNull(process.StartInfo);
                    Assert.AreEqual("sudo", process.StartInfo.FileName);
                    Assert.AreEqual($"-u {username} {command} {commandArguments}", process.StartInfo.Arguments);
                    Assert.AreEqual(workingDirectory, process.StartInfo.WorkingDirectory);
                }
            }
        }

        [Test]
        public void ExecuteCommandAsyncExtensionDoesNotSupportAUsernameSuppliedOnWindowsSystems()
        {
            this.SetupDefaults(PlatformID.Win32NT);

            string command = "anycommand.exe";
            string commandArguments = "--option1=123 --option2=456";
            string workingDirectory = MockFixture.TestAssemblyDirectory;

            using (TestExecutor component = new TestExecutor(this.mockFixture))
            {
                Assert.ThrowsAsync<NotSupportedException>(
                    () => component.ExecuteCommandAsync(command, commandArguments, workingDirectory, EventContext.None, CancellationToken.None, username: "notsupported"));
            }
        }

        [Test]
        public void ExecuteCommandAsyncExtensionDoesNotSupportAUsernameSuppliedUnlessRunningElevatedOnUnixSystems()
        {
            this.SetupDefaults(PlatformID.Unix);

            string command = "anycommand";
            string commandArguments = "--option1=123 --option2=456";
            string workingDirectory = MockFixture.TestAssemblyDirectory;

            using (TestExecutor component = new TestExecutor(this.mockFixture))
            {
                Assert.ThrowsAsync<NotSupportedException>(
                    () => component.ExecuteCommandAsync(command, commandArguments, workingDirectory, EventContext.None, CancellationToken.None, username: "notsupported"));
            }
        }

        [ComponentDescription(Id = "Format1234")]
        private class TestFileUploadDescriptorFactory_A : IFileUploadDescriptorFactory
        {
            public FileUploadDescriptor CreateDescriptor(FileContext fileContext, string contentStorePathTemplate, IDictionary<string, IConvertible> parameters = null, IDictionary<string, IConvertible> manifest = null, bool timestamped = true)
            {
                return new FileUploadDescriptor(
                    $"/any/path/to/blob/{fileContext.File.Name}",
                    "anyContainer",
                    "utf-8",
                    "text/plain",
                    fileContext.File.FullName,
                    manifest);
            }
        }
    }
}
