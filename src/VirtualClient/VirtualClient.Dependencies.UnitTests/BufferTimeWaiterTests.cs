// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Dependencies
{
    using System;
    using System.Runtime.InteropServices;
    using System.Threading;
    using System.Threading.Tasks;
    using Moq;
    using NUnit.Framework;
    using VirtualClient.Common.Telemetry;
    using VirtualClient.Contracts;

    [TestFixture]
    [Category("Unit")]
    internal class BufferTimeWaiterTests
    {
        private MockFixture mockFixture;

        public void SetupDefaults(PlatformID platform, Architecture architecture = Architecture.X64)
        {
            this.mockFixture = new MockFixture();
            this.mockFixture.Setup(platform, architecture);
        }

        [Test]
        [TestCase(PlatformID.Unix, Architecture.X64)]
        public async Task BufferTimeWaiterWaitsForExpectedAmountOfTime(PlatformID platform, Architecture architecture)
        {
            this.SetupDefaults(platform, architecture);
            this.mockFixture.Parameters[nameof(BufferTimeWaiter.BufferTimeInSec)] = 1;

            using (TestBufferTimeWaiter testBufferTimeWaiter = new TestBufferTimeWaiter(this.mockFixture))
            {
                DateTime startTime = DateTime.Now;
                await testBufferTimeWaiter.ExecuteAsync(CancellationToken.None).ConfigureAwait(false);
                DateTime endTime = DateTime.Now;
                if ((endTime - startTime).TotalSeconds > long.Parse(this.mockFixture.Parameters[nameof(BufferTimeWaiter.BufferTimeInSec)].ToString()))
                {
                    Assert.IsTrue(true);
                }
                else
                {
                    Assert.IsTrue(false);
                }
            }
        }

        private class TestBufferTimeWaiter : BufferTimeWaiter
        {
            public TestBufferTimeWaiter(MockFixture mockFixture)
                : base(mockFixture?.Dependencies, mockFixture?.Parameters)
            {
            }

            public new Task InitializeAsync(EventContext telemetryContext, CancellationToken cancellationToken)
            {
                return base.InitializeAsync(telemetryContext, cancellationToken);
            }

            public new Task ExecuteAsync(EventContext telemetryContext, CancellationToken cancellationToken)
            {
                return base.ExecuteAsync(telemetryContext, cancellationToken);
            }
        }
    }
}
