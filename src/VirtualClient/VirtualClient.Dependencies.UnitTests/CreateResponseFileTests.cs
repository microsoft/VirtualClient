// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Dependencies.UnitTests
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.IO.Abstractions;
    using System.Linq;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.CodeAnalysis.Options;
    using Microsoft.Extensions.DependencyInjection.Extensions;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Moq;
    using NUnit.Framework;
    using Org.BouncyCastle.Utilities;
    using Polly.Retry;
    using Renci.SshNet.Security;
    using VirtualClient;
    using VirtualClient.Common.Extensions;
    using VirtualClient.Common.Telemetry;
    using static System.Net.WebRequestMethods;

    [TestFixture]
    [Category("Unit")]
    public class CreateResponseFileTests
    {
        private string tempDirectory;
        private MockFixture mockFixture;
        private IFileSystem fileSystem;
        private Mock<IFileSystemEntity> fileStreamMock;

        [SetUp]
        public void SetUp()
        {
            this.tempDirectory = Path.Combine(Path.GetTempPath(), "VirtualClient", "UnitTests", Guid.NewGuid().ToString("n"));
            Directory.CreateDirectory(this.tempDirectory);

            Environment.CurrentDirectory = this.tempDirectory;

            this.mockFixture = new MockFixture();
            this.mockFixture.SetupMocks(true);
            this.fileSystem = this.mockFixture.Dependencies.GetService<IFileSystem>();
            this.fileStreamMock = new Mock<IFileSystemEntity>();
        }

        [Test]
        public async Task ExecuteAsyncDoesNotCreateFileWhenNoOptionsAreSupplied()
        {
            this.mockFixture.Parameters = new Dictionary<string, IConvertible>
            {
                ["FileName"] = "resource_access.rsp"
            };

            var executor = new TestCreateResponseFile(this.mockFixture);

            await executor.ExecuteAsync().ConfigureAwait(false);

            string expectedPath = Path.Combine(this.tempDirectory, "resource_access.rsp");
            Assert.False(this.fileSystem.File.Exists(expectedPath), "The response file should not be created when no options are supplied.");

            this.mockFixture.FileSystem.Verify(x => x.File.Delete(It.IsAny<string>()), Times.Never);
        }

        [Test]
        [TestCase("")]
        [TestCase(" ")]
        [TestCase(null)]
        [TestCase("C:\\repos\\VirtualClient\\out\\bin\\Debug\\x64\\VirtualClient.Main\\net9.0\\test.rsp")]
        [TestCase("/home/vmadmin/VirtualClient/out/bin/debug/x64/VirtualClient.Main/net9.0/test.rsp")]
        [TestCase("test.rsp")]
        [TestCase("test.txt")]
        public async Task ExecuteAsyncCreatesResponseFileAsExpected(string inputFilePath)
        {
            string expectedFilePath = string.IsNullOrWhiteSpace(inputFilePath)
                ? this.mockFixture.Combine(Environment.CurrentDirectory, "resource_access.rsp")
                : inputFilePath;

            this.mockFixture.Parameters["FileName"] = inputFilePath;
            this.mockFixture.Parameters["Option2"] = "--KeyVaultUri=\"https://crc-partner-vault.vault.azure.net\"";
            this.mockFixture.Parameters["Option1"] = "--System=\"Testing\"";

            string expectedContent = string.Join(' ', this.mockFixture.Parameters
                .Where(p => p.Key.StartsWith("Option", StringComparison.OrdinalIgnoreCase))
                .Select(x => x.Value.ToString().Trim())
                .ToArray());

            var executor = new TestCreateResponseFile(this.mockFixture);

            Mock<InMemoryFileSystemStream> mockFileStream = new Mock<InMemoryFileSystemStream>();
            this.mockFixture.FileStream.Setup(f => f.New(It.IsAny<string>(), It.IsAny<FileMode>(), It.IsAny<FileAccess>(), It.IsAny<FileShare>()))
                .Returns(mockFileStream.Object)
                .Callback((string path, FileMode mode, FileAccess access, FileShare share) =>
                {
                    Assert.AreEqual(expectedFilePath, path);
                    Assert.IsTrue(mode == FileMode.Create);
                    Assert.IsTrue(access == FileAccess.ReadWrite);
                    Assert.IsTrue(share == FileShare.ReadWrite);
                });

            await executor.ExecuteAsync().ConfigureAwait(false);
            byte[] bytes = Encoding.UTF8.GetBytes(expectedContent);
            mockFileStream.Verify(x => x.WriteAsync(It.Is<ReadOnlyMemory<byte>>(x => (x.Length == bytes.Length)), It.IsAny<CancellationToken>()), Times.Exactly(1));
        }

        private class TestCreateResponseFile : CreateResponseFile
        {
            public TestCreateResponseFile(MockFixture mockFixture)
                : base(mockFixture.Dependencies, mockFixture.Parameters)
            {
            }

            public Task ExecuteAsync()
            {
                return this.ExecuteAsync(EventContext.None, CancellationToken.None);
            }
        }
    }
}