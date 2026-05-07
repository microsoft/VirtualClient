// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Dependencies
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using Moq;
    using NUnit.Framework;
    using VirtualClient.Common.Telemetry;

    [TestFixture]
    [Category("Unit")]
    public class GetAccessTokenTests
    {
        private MockFixture mockFixture;

        public void Setup(PlatformID platform)
        {
            this.mockFixture = new MockFixture();
            this.mockFixture.Setup(platform);
            this.mockFixture.Parameters = new Dictionary<string, IConvertible>(StringComparer.OrdinalIgnoreCase)
            {
                { "TenantId", "00000000-0000-0000-0000-000000000000" }
            };
        }

        [Test]
        [TestCase(PlatformID.Unix)]
        [TestCase(PlatformID.Win32NT)]
        public async Task GetAccessTokenDoesNotRequireAFilePath(PlatformID platform)
        {
            this.Setup(platform);

            using (var component = new TestKeyVaultAccessToken(this.mockFixture))
            {
                await component.InitializeAsync(EventContext.None, CancellationToken.None);
                Assert.IsNull(component.FilePath);
            }
        }

        [Test]
        [TestCase(PlatformID.Unix)]
        [TestCase(PlatformID.Win32NT)]
        public async Task GetAccessTokenDeletesAnyExistingAccessTokenFilesBeforeWritingNewOnes(PlatformID platform)
        {
            this.Setup(platform);

            string expectedFilePath = this.mockFixture.Combine(this.mockFixture.CurrentDirectory, "access_token.txt");
            this.mockFixture.Parameters[nameof(GetAccessToken.FilePath)] = expectedFilePath;

            // Setup:
            // Existing token file is present and should be deleted before writing a new one.
            this.mockFixture.File.Setup(f => f.Exists(expectedFilePath))
                .Returns(true);

            using (var component = new TestKeyVaultAccessToken(this.mockFixture))
            {
                await component.InitializeAsync(EventContext.None, CancellationToken.None);
                this.mockFixture.File.Verify(f => f.Delete(expectedFilePath), Times.Once);
            }
        }

        [Test]
        [TestCase(PlatformID.Unix)]
        [TestCase(PlatformID.Win32NT)]
        public async Task GetAccessTokenWritesAccessTokensToTheExpectedFilePath(PlatformID platform)
        {
            this.Setup(platform);

            string expectedFilePath = this.mockFixture.Combine(this.mockFixture.CurrentDirectory, "access_token.txt");
            this.mockFixture.Parameters[nameof(GetAccessToken.FilePath)] = expectedFilePath;

            // Setup:
            // Existing token file is present and should be deleted before writing a new one.
            this.mockFixture.File.Setup(f => f.Exists(expectedFilePath))
                .Returns(true);

            using (var component = new TestKeyVaultAccessToken(this.mockFixture))
            {
                await component.ExecuteAsync(CancellationToken.None);

                this.mockFixture.File.Verify(
                    f => f.WriteAllTextAsync(expectedFilePath, It.IsAny<string>(), It.IsAny<CancellationToken>()), 
                    Times.Once);
            }
        }

        private sealed class TestKeyVaultAccessToken : GetAccessToken
        {
            public TestKeyVaultAccessToken(MockFixture mockFixture)
                : base(mockFixture?.Dependencies, mockFixture?.Parameters)
            {
            }

            public string InteractiveToken { get; set; } = "interactive-token";

            public string DeviceCodeToken { get; set; } = "device-token";

            public bool ThrowOnInteractiveWorkflowAttempt { get; set; }

            public int InteractiveCalls { get; private set; }

            public int DeviceCodeCalls { get; private set; }

            public new Task InitializeAsync(EventContext context, CancellationToken token)
            {
                return base.InitializeAsync(context, token);
            }
        }
    }
}