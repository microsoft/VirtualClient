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
    using Microsoft.Extensions.DependencyInjection;
    using NUnit.Framework;
    using VirtualClient.Common.Telemetry;

    [TestFixture]
    [Category("Unit")]
    public class CreateResponseFileTests
    {
        private string originalCurrentDirectory;
        private string tempDirectory;
        private MockFixture mockFixture;

        [SetUp]
        public void SetUp()
        {
            this.originalCurrentDirectory = Environment.CurrentDirectory;
            this.tempDirectory = Path.Combine(Path.GetTempPath(), "VirtualClient", "UnitTests", Guid.NewGuid().ToString("n"));
            Directory.CreateDirectory(this.tempDirectory);

            Environment.CurrentDirectory = this.tempDirectory;
            this.mockFixture = new MockFixture();
        }

        [TearDown]
        public void TearDown()
        {
            Environment.CurrentDirectory = this.originalCurrentDirectory;

            try
            {
                if (Directory.Exists(this.tempDirectory))
                {
                    Directory.Delete(this.tempDirectory, recursive: true);
                }
            }
            catch
            {
                // Best-effort cleanup for test runs.
            }
        }

        [Test]
        public async Task ExecuteAsync_DoesNotCreateFile_WhenNoOptionsAreSupplied()
        {
            IServiceCollection services = new ServiceCollection();
            services.AddSingleton<IFileSystem>(new FileSystem());

            var parameters = new Dictionary<string, IConvertible>
            {
                ["FileName"] = "resource_access.rsp"
            };

            TestCreateResponseFile executor = new TestCreateResponseFile(this.mockFixture);
            await executor.ExecuteAsync().ConfigureAwait(false);

            string expectedPath = Path.Combine(this.tempDirectory, "resource_access.rsp");
            Assert.False(File.Exists(expectedPath), "The response file should not be created when no options are supplied.");
        }

        [Test]
        public async Task ExecuteAsync_CreatesResponseFile_WithExpectedContent()
        {
            IServiceCollection services = new ServiceCollection();
            services.AddSingleton<IFileSystem>(new FileSystem());

            var parameters = new Dictionary<string, IConvertible>
            {
                ["FileName"] = "resource_access.rsp",
                ["Option2"] = "--KeyVaultUri=\"https://crc-partner-vault.vault.azure.net\"",
                ["Option1"] = "--System=\"Testing\""
            };

            TestCreateResponseFile executor = new TestCreateResponseFile(this.mockFixture);
            await executor.ExecuteAsync().ConfigureAwait(false);

            string expectedPath = Path.Combine(this.tempDirectory, "resource_access.rsp");
            Assert.True(File.Exists(expectedPath), "The response file should be created when options are supplied.");

            // Current implementation orders keys lexically (OrdinalIgnoreCase): Option1 then Option2.
            string expectedContent = "--System=\"Testing\" --KeyVaultUri=\"https://crc-partner-vault.vault.azure.net\"";
            string actualContent = await File.ReadAllTextAsync(expectedPath, Encoding.UTF8);

            Assert.AreEqual(expectedContent, actualContent);
        }

        [Test]
        public async Task ExecuteAsync_UsesAbsoluteFileName_WhenRootedPathProvided()
        {
            IServiceCollection services = new ServiceCollection();
            services.AddSingleton<IFileSystem>(new FileSystem());

            string absolutePath = Path.Combine(this.tempDirectory, "sub", "my.rsp");
            Directory.CreateDirectory(Path.GetDirectoryName(absolutePath)!);

            var parameters = new Dictionary<string, IConvertible>
            {
                ["FileName"] = absolutePath,
                ["Option1"] = "--System=\"Testing\""
            };

            TestCreateResponseFile executor = new TestCreateResponseFile(this.mockFixture);
            await executor.ExecuteAsync().ConfigureAwait(false);

            Assert.True(File.Exists(absolutePath), "The response file should be created at the rooted path.");
            string actualContent = await File.ReadAllTextAsync(absolutePath, Encoding.UTF8);
            Assert.AreEqual("--System=\"Testing\"", actualContent);
        }

        private class TestCreateResponseFile : CreateResponseFile
        {
            public TestCreateResponseFile(MockFixture mockFixture)
                : base(mockFixture.Dependencies, mockFixture.Parameters)
            {
            }

            public async Task ExecuteAsync()
            {
                await base.ExecuteAsync(EventContext.None, CancellationToken.None).ConfigureAwait(false);
            }
        }
    }
}