// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Dependencies
{
    using System;
    using System.Runtime.InteropServices;
    using System.Threading;
    using System.Threading.Tasks;
    using NUnit.Framework;

    [TestFixture]
    [Category("Unit")]
    internal class WaitExecutorTests
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
            this.mockFixture.Parameters[nameof(WaitExecutor.Duration)] = new TimeSpan(0, 0, 0, 0, 10).ToString();

            using (TestWaitExecutor testBufferTimeWaiter = new TestWaitExecutor(this.mockFixture))
            {
                DateTime startTime = DateTime.Now;
                await testBufferTimeWaiter.ExecuteAsync(CancellationToken.None).ConfigureAwait(false);
                DateTime endTime = DateTime.Now;

                if ((endTime - startTime) >= TimeSpan.Parse(this.mockFixture.Parameters[nameof(WaitExecutor.Duration)].ToString()))
                {
                    Assert.IsTrue(true);
                }
                else
                {
                    Assert.IsTrue(false);
                }
            }
        }

        private class TestWaitExecutor : WaitExecutor
        {
            public TestWaitExecutor(MockFixture mockFixture)
                : base(mockFixture?.Dependencies, mockFixture?.Parameters)
            {
            }
        }
    }
}
