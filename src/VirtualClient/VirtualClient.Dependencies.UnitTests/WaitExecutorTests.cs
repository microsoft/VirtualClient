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
        private MockFixture fixture;

        public void SetupDefaults(PlatformID platform, Architecture architecture = Architecture.X64)
        {
            this.fixture = new MockFixture();
            this.fixture.Setup(platform, architecture);
        }

        [Ignore("Flaky test that sometimes fail.")]
        [Test]
        [TestCase(PlatformID.Unix, Architecture.X64)]
        public async Task BufferTimeWaiterWaitsForExpectedAmountOfTime(PlatformID platform, Architecture architecture)
        {
            this.SetupDefaults(platform, architecture);
            this.fixture.Parameters[nameof(WaitExecutor.Duration)] = new TimeSpan(0, 0, 0, 0, 10).ToString();

            using (TestWaitExecutor testBufferTimeWaiter = new TestWaitExecutor(this.fixture))
            {
                DateTime startTime = DateTime.Now;
                await testBufferTimeWaiter.ExecuteAsync(CancellationToken.None).ConfigureAwait(false);
                DateTime endTime = DateTime.Now;

                if ((endTime - startTime) >= TimeSpan.Parse(this.fixture.Parameters[nameof(WaitExecutor.Duration)].ToString()))
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
            public TestWaitExecutor(MockFixture fixture)
                : base(fixture?.Dependencies, fixture?.Parameters)
            {
            }
        }
    }
}
