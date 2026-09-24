// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Actions
{
    using System;
    using System.Collections.Generic;
    using global::VirtualClient.Contracts;
    using Microsoft.Extensions.DependencyInjection;
    using NUnit.Framework;

    [TestFixture]
    [Category("Unit")]
    public class SpecCpuExecutorTests
    {
        private MockFixture mockFixture;

        [SetUp]
        public void SetupTests()
        {
            this.mockFixture = new MockFixture();
            this.mockFixture.Setup(PlatformID.Unix);
        }

        [Test]
        public void SpecCpuExecutorRoutes2017PackagesToThe2017Executor()
        {
            this.mockFixture.Parameters = new Dictionary<string, IConvertible>
            {
                { nameof(SpecCpuExecutor.PackageName), "speccpu2017" },
                { nameof(SpecCpuExecutor.RunPeak), false }
            };

            using (TestSpecCpuExecutor dispatcher = new TestSpecCpuExecutor(this.mockFixture.Dependencies, this.mockFixture.Parameters))
            using (SpecCpuExecutor executor = dispatcher.CreateVersionExecutor())
            {
                Assert.IsInstanceOf<SpecCpu2017Executor>(executor);
            }
        }

        [Test]
        public void SpecCpuExecutorRoutes2026PackagesToThe2026ExecutorCaseInsensitively()
        {
            this.mockFixture.Parameters = new Dictionary<string, IConvertible>
            {
                { nameof(SpecCpuExecutor.PackageName), "SPECCPU2026" },
                { nameof(SpecCpuExecutor.RunPeak), false }
            };

            using (TestSpecCpuExecutor dispatcher = new TestSpecCpuExecutor(this.mockFixture.Dependencies, this.mockFixture.Parameters))
            using (SpecCpuExecutor executor = dispatcher.CreateVersionExecutor())
            {
                Assert.IsInstanceOf<SpecCpu2026Executor>(executor);
            }
        }

        [Test]
        public void SpecCpuExecutorRejectsPackagesWithoutASupportedVersion()
        {
            this.mockFixture.Parameters = new Dictionary<string, IConvertible>
            {
                { nameof(SpecCpuExecutor.PackageName), "speccpu" },
                { nameof(SpecCpuExecutor.RunPeak), false }
            };

            using (TestSpecCpuExecutor dispatcher = new TestSpecCpuExecutor(this.mockFixture.Dependencies, this.mockFixture.Parameters))
            {
                WorkloadException error = Assert.Throws<WorkloadException>(() => dispatcher.CreateVersionExecutor());
                StringAssert.Contains("must contain '2017' or '2026'", error.Message);
            }
        }

        private class TestSpecCpuExecutor : SpecCpuExecutor
        {
            public TestSpecCpuExecutor(IServiceCollection dependencies, IDictionary<string, IConvertible> parameters)
                : base(dependencies, parameters)
            {
            }

            public SpecCpuExecutor CreateVersionExecutor()
            {
                return base.CreateExecutor();
            }
        }
    }
}
