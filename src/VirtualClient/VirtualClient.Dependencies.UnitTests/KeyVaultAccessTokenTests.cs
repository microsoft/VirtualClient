// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Dependencies
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using Azure.Core;
    using Azure.Identity;
    using Moq;
    using NUnit.Framework;
    using Polly;
    using VirtualClient.Common.Telemetry;

    [TestFixture]
    [Category("Unit")]
    public class KeyVaultAccessTokenTests
    {
        private MockFixture mockFixture;

        [SetUp]
        public void Setup()
        {
            this.mockFixture = new MockFixture();
        }

        [Test]
        [TestCase(PlatformID.Unix)]
        [TestCase(PlatformID.Win32NT)]
        public async Task InitializeWillNotDoAnythingIfLogFileNameIsNotProvided(PlatformID platform)
        {
            this.mockFixture.Setup(platform);

            this.SetupWorkingDirectory(platform, out _);

            using (TestKeyVaultAccessToken component = new TestKeyVaultAccessToken(this.mockFixture.Dependencies, this.CreateDefaultParameters()))
            {
                await component.InitializeAsyncInternal(EventContext.None, CancellationToken.None).ConfigureAwait(false);

                Assert.IsNull(component.AccessTokenPathInternal);
            }
        }

        [Test]
        [TestCase(PlatformID.Unix)]
        [TestCase(PlatformID.Win32NT)]
        public async Task InitializeWillEnsureAccessTokenPathIsReadyIfLogFileNameIsProvided(PlatformID platform)
        {
            this.mockFixture.Setup(platform);

            this.SetupWorkingDirectory(platform, out string workingDir);

            string expectedPath = this.Combine(workingDir, "AccessToken.txt");

            // Setup: file does not exist initially
            this.mockFixture.File.Setup(f => f.Exists(expectedPath)).Returns(false);

            using (TestKeyVaultAccessToken component = new TestKeyVaultAccessToken(this.mockFixture.Dependencies, this.CreateDefaultParameters()))
            {
                component.Parameters["LogFileName"] = "AccessToken.txt";

                await component.InitializeAsyncInternal(EventContext.None, CancellationToken.None).ConfigureAwait(false);

                Assert.AreEqual(expectedPath, component.AccessTokenPathInternal);
                Assert.IsFalse(this.mockFixture.File.Object.Exists(component.AccessTokenPathInternal), "File should not be created during Initialize.");
            }
        }

        [Test]
        [TestCase(PlatformID.Unix)]
        [TestCase(PlatformID.Win32NT)]
        public async Task InitializeWillEnsureOldFileIsDeletedIfPresent(PlatformID platform)
        {
            this.mockFixture.Setup(platform);

            this.SetupWorkingDirectory(platform, out string workingDir);

            string tokenPath = this.Combine(workingDir, "AccessToken.txt");

            // Setup: existing token file is present and should be deleted during Initialize.
            this.mockFixture.File.Setup(f => f.Exists(tokenPath)).Returns(true);

            bool deleteCalled = false;
            this.mockFixture.FileSystem
                .Setup(f => f.File.Delete(It.IsAny<string>()))
                .Callback(() => deleteCalled = true);

            using (TestKeyVaultAccessToken component = new TestKeyVaultAccessToken(this.mockFixture.Dependencies, this.CreateDefaultParameters()))
            {
                component.Parameters["LogFileName"] = "AccessToken.txt";

                await component.InitializeAsyncInternal(EventContext.None, CancellationToken.None).ConfigureAwait(false);

                Assert.IsTrue(deleteCalled, "Existing token file should be deleted.");
                this.mockFixture.File.Verify(f => f.Delete(It.IsAny<string>()), Times.Once);
            }
        }

        [Test]
        [TestCase(PlatformID.Unix)]
        [TestCase(PlatformID.Win32NT)]
        public void ExecuteAsyncValidatesRequiredParameters(PlatformID platform)
        {
            this.mockFixture.Setup(platform);

            var parameters = new Dictionary<string, IConvertible>(StringComparer.OrdinalIgnoreCase);

            using (TestKeyVaultAccessToken component = new TestKeyVaultAccessToken(this.mockFixture.Dependencies, parameters))
            {
                Assert.ThrowsAsync<KeyNotFoundException>(() => component.ExecuteAsync(CancellationToken.None));
            }
        }

        [Test]
        [TestCase(PlatformID.Unix)]
        [TestCase(PlatformID.Win32NT)]
        public async Task ExecuteAsyncWillWriteTokenToFileWhenLogFileNameIsProvided(PlatformID platform)
        {
            this.mockFixture.Setup(platform);
            this.SetupWorkingDirectory(platform, out string workingDir);

            string tokenContent = Guid.NewGuid().ToString();
            string expectedPath = this.Combine(workingDir, "AccessToken.txt");

            Mock<InMemoryFileSystemStream> mockFileStream = new Mock<InMemoryFileSystemStream>();
            this.mockFixture.FileStream.Setup(f => f.New(It.IsAny<string>(), It.IsAny<FileMode>(), It.IsAny<FileAccess>(), It.IsAny<FileShare>()))
                .Returns(mockFileStream.Object)
                .Callback((string path, FileMode mode, FileAccess access, FileShare share) =>
                {
                    Assert.AreEqual(expectedPath, path);
                    Assert.IsTrue(mode == FileMode.Create);
                    Assert.IsTrue(access == FileAccess.ReadWrite);
                    Assert.IsTrue(share == FileShare.ReadWrite);
                });

            mockFileStream
                .Setup(x => x.Write(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>()))
                .Callback((byte[] data, int offset, int count) =>
                {
                    byte[] byteData = Encoding.Default.GetBytes(tokenContent);
                    Assert.AreEqual(offset, 0);
                    Assert.AreEqual(count, byteData.Length);
                    Assert.AreEqual(data, byteData);
                });

            using (TestKeyVaultAccessToken component = new TestKeyVaultAccessToken(this.mockFixture.Dependencies, this.CreateDefaultParameters()))
            {
                component.Parameters["LogFileName"] = "AccessToken.txt";
                component.InteractiveTokenToReturn = tokenContent;

                await component.InitializeAsyncInternal(EventContext.None, CancellationToken.None).ConfigureAwait(false);
                await component.ExecuteAsync(CancellationToken.None).ConfigureAwait(false);
            }
        }

        [Test]
        [TestCase(PlatformID.Unix)]
        [TestCase(PlatformID.Win32NT)]
        public void GetTokenRequestContextWillReturnCorrectValue(PlatformID platform)
        {
            this.mockFixture.Setup(platform);

            using (TestKeyVaultAccessToken component = new TestKeyVaultAccessToken(this.mockFixture.Dependencies, this.CreateDefaultParameters()))
            {
                TokenRequestContext ctx = component.GetTokenRequestContextInternal();

                Assert.IsNotNull(ctx);
                Assert.AreEqual(1, ctx.Scopes.Length);
                Assert.AreEqual("https://myvault.vault.azure.net/.default", ctx.Scopes[0]);
            }
        }

        [Test]
        [TestCase(PlatformID.Unix)]
        [TestCase(PlatformID.Win32NT)]
        public async Task ExecuteAsyncWillUseInteractiveTokenFirst(PlatformID platform)
        {
            this.mockFixture.Setup(platform);

            using (TestKeyVaultAccessToken component = new TestKeyVaultAccessToken(this.mockFixture.Dependencies, this.CreateDefaultParameters()))
            {
                component.InteractiveTokenToReturn = "interactive-ok";

                await component.ExecuteAsync(CancellationToken.None).ConfigureAwait(false);

                Assert.AreEqual(1, component.InteractiveCalls);
                Assert.AreEqual(0, component.DeviceCodeCalls);
            }
        }

        [Test]
        [TestCase(PlatformID.Unix)]
        [TestCase(PlatformID.Win32NT)]
        public async Task ExecuteAsyncWillUseDeviceLoginIfInteractiveFailsWithExactError(PlatformID platform)
        {
            this.mockFixture.Setup(platform);

            using (TestKeyVaultAccessToken component = new TestKeyVaultAccessToken(this.mockFixture.Dependencies, this.CreateDefaultParameters()))
            {
                component.ThrowBrowserUnavailableAuthenticationFailedException = true;
                component.DeviceCodeTokenToReturn = "device-code-ok";

                await component.ExecuteAsync(CancellationToken.None).ConfigureAwait(false);

                Assert.AreEqual(1, component.InteractiveCalls);
                Assert.AreEqual(1, component.DeviceCodeCalls);
            }
        }

        [Test]
        [TestCase(PlatformID.Unix, null)]
        [TestCase(PlatformID.Win32NT, null)]
        [TestCase(PlatformID.Unix, "")]
        [TestCase(PlatformID.Win32NT, "")]
        [TestCase(PlatformID.Unix, "     ")]
        [TestCase(PlatformID.Win32NT, "     ")]
        [TestCase(PlatformID.Unix, "validToken")]
        [TestCase(PlatformID.Win32NT, "validToken")]
        public void ExecuteAsyncWillCheckIfValidTokenIsGenerated(PlatformID platform, string token)
        {
            this.mockFixture.Setup(platform);

            using (TestKeyVaultAccessToken component = new TestKeyVaultAccessToken(this.mockFixture.Dependencies, this.CreateDefaultParameters()))
            {
                component.InteractiveTokenToReturn = token;

                if (string.IsNullOrWhiteSpace(token))
                {
                    Assert.ThrowsAsync<AuthenticationFailedException>(() => component.ExecuteAsync(CancellationToken.None));
                }
                else
                {
                    Assert.DoesNotThrowAsync(() => component.ExecuteAsync(CancellationToken.None), string.Empty);
                }
            }
        }

        private void SetupWorkingDirectory(PlatformID platform, out string workingDir)
        {
            workingDir = platform == PlatformID.Win32NT ? @"C:\home\user" : "/home/user";

            // KeyVaultAccessToken uses ISystemManagement.FileSystem internally, which in unit tests is MockFixture.FileSystem
            this.mockFixture.Directory.Setup(d => d.GetCurrentDirectory()).Returns(workingDir);
        }

        private string Combine(string left, string right)
        {
            // Avoid relying on host OS behavior; use the path separator expected by the test platform.
            char sep = this.mockFixture.Platform == PlatformID.Win32NT ? '\\' : '/';
            return $"{left.TrimEnd(sep)}{sep}{right.TrimStart(sep)}";
        }

        private IDictionary<string, IConvertible> CreateDefaultParameters()
        {
            return new Dictionary<string, IConvertible>(StringComparer.OrdinalIgnoreCase)
            {
                { "TenantId", "00000000-0000-0000-0000-000000000000" },
                { "KeyVaultUri", "https://myvault.vault.azure.net/" }
            };
        }

        private sealed class TestKeyVaultAccessToken : KeyVaultAccessToken
        {
            public TestKeyVaultAccessToken(Microsoft.Extensions.DependencyInjection.IServiceCollection dependencies, IDictionary<string, IConvertible> parameters)
                : base(dependencies, parameters)
            {
            }

            public string InteractiveTokenToReturn { get; set; } = "interactive-token";

            public string DeviceCodeTokenToReturn { get; set; } = "device-token";

            public bool ThrowBrowserUnavailableAuthenticationFailedException { get; set; }

            public int InteractiveCalls { get; private set; }

            public int DeviceCodeCalls { get; private set; }

            public string AccessTokenPathInternal => this.AccessTokenPath;

            public Task InitializeAsyncInternal(EventContext context, CancellationToken token)
            {
                return this.InitializeAsync(context, token);
            }

            public TokenRequestContext GetTokenRequestContextInternal()
            {
                return this.GetTokenRequestContext();
            }

            protected override async Task<string> AcquireInteractiveTokenAsync(
                TokenCredential credential,
                TokenRequestContext requestContext,
                CancellationToken cancellationToken)
            {
                this.InteractiveCalls++;

                if (this.ThrowBrowserUnavailableAuthenticationFailedException)
                {
                    throw new AuthenticationFailedException("Unable to open a web page");
                }

                await Task.Yield();
                return this.InteractiveTokenToReturn;
            }

            protected override async Task<string> AcquireDeviceCodeTokenAsync(
                TokenCredential credential,
                TokenRequestContext requestContext,
                CancellationToken cancellationToken)
            {
                this.DeviceCodeCalls++;
                await Task.Yield();
                return this.DeviceCodeTokenToReturn;
            }
        }
    }
}