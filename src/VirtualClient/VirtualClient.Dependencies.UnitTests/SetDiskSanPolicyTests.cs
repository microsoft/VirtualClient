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
    using VirtualClient.Contracts;

    [TestFixture]
    [Category("Unit")]
    public class SetDiskSanPolicyTests
    {
        private MockFixture mockFixture;

        [Test]
        public async Task SetDiskSanPolicyCallsDiskManagerSetSanPolicyOnWindows()
        {
            this.mockFixture = new MockFixture();
            this.mockFixture.Setup(PlatformID.Win32NT);

            using (SetDiskSanPolicy component = new SetDiskSanPolicy(this.mockFixture.Dependencies, this.mockFixture.Parameters))
            {
                await component.ExecuteAsync(CancellationToken.None);

                this.mockFixture.DiskManager.Verify(
                    mgr => mgr.SetSanPolicyAsync(It.IsAny<CancellationToken>()),
                    Times.Once);
            }
        }

        [Test]
        public async Task SetDiskSanPolicyDoesNotCallDiskManagerSetSanPolicyOnLinux()
        {
            this.mockFixture = new MockFixture();
            this.mockFixture.Setup(PlatformID.Unix);

            using (SetDiskSanPolicy component = new SetDiskSanPolicy(this.mockFixture.Dependencies, this.mockFixture.Parameters))
            {
                await component.ExecuteAsync(CancellationToken.None);

                this.mockFixture.DiskManager.Verify(
                    mgr => mgr.SetSanPolicyAsync(It.IsAny<CancellationToken>()),
                    Times.Never);
            }
        }

        [Test]
        public void SetDiskSanPolicyPropagatesExceptionsThrownByDiskManagerOnWindows()
        {
            this.mockFixture = new MockFixture();
            this.mockFixture.Setup(PlatformID.Win32NT);

            this.mockFixture.DiskManager
                .Setup(mgr => mgr.SetSanPolicyAsync(It.IsAny<CancellationToken>()))
                .ThrowsAsync(new ProcessException("DiskPart SAN policy command failed.", ErrorReason.DiskFormatFailed));

            using (SetDiskSanPolicy component = new SetDiskSanPolicy(this.mockFixture.Dependencies, this.mockFixture.Parameters))
            {
                ProcessException exc = Assert.ThrowsAsync<ProcessException>(
                    () => component.ExecuteAsync(CancellationToken.None));

                Assert.AreEqual(ErrorReason.DiskFormatFailed, exc.Reason);
            }
        }
    }
}
