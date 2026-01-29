// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Common.ProcessAffinity
{
    using System;
    using System.Linq;
    using NUnit.Framework;

    [TestFixture]
    [Category("Unit")]
    public class WindowsProcessAffinityConfigurationTests
    {
        [Test]
        public void WindowsProcessAffinityConfigurationCalculatesCorrectBitmaskForSingleCore()
        {
            WindowsProcessAffinityConfiguration config = new WindowsProcessAffinityConfiguration(new[] { 0 });
            
            Assert.AreEqual(1L, config.AffinityMask.ToInt64());
        }

        [Test]
        public void WindowsProcessAffinityConfigurationCalculatesCorrectBitmaskForMultipleCores()
        {
            // Cores 0, 1, 2, 3 => 0b1111 = 15
            WindowsProcessAffinityConfiguration config = new WindowsProcessAffinityConfiguration(new[] { 0, 1, 2, 3 });
            
            Assert.AreEqual(15L, config.AffinityMask.ToInt64());
        }

        [Test]
        public void WindowsProcessAffinityConfigurationCalculatesCorrectBitmaskForNonContiguousCores()
        {
            // Cores 0, 2, 4 => 0b10101 = 21
            WindowsProcessAffinityConfiguration config = new WindowsProcessAffinityConfiguration(new[] { 0, 2, 4 });
            
            Assert.AreEqual(21L, config.AffinityMask.ToInt64());
        }

        [Test]
        public void WindowsProcessAffinityConfigurationCalculatesCorrectBitmaskForHighCoreIndices()
        {
            // Core 10 => 0b10000000000 = 1024
            WindowsProcessAffinityConfiguration config = new WindowsProcessAffinityConfiguration(new[] { 10 });
            
            Assert.AreEqual(1024L, config.AffinityMask.ToInt64());
        }

        [Test]
        public void WindowsProcessAffinityConfigurationCalculatesCorrectBitmaskForMixedCores()
        {
            // Cores 0, 5, 10, 15 => 0b1000001000100001 = 33825
            WindowsProcessAffinityConfiguration config = new WindowsProcessAffinityConfiguration(new[] { 0, 5, 10, 15 });
            
            Assert.AreEqual(33825L, config.AffinityMask.ToInt64());
        }

        [Test]
        public void WindowsProcessAffinityConfigurationThrowsOnCoreIndexExceeding63()
        {
            // Windows supports up to 64 cores per group (indices 0-63)
            Assert.Throws<NotSupportedException>(() => new WindowsProcessAffinityConfiguration(new[] { 64 }));
            Assert.Throws<NotSupportedException>(() => new WindowsProcessAffinityConfiguration(new[] { 0, 1, 64 }));
            Assert.Throws<NotSupportedException>(() => new WindowsProcessAffinityConfiguration(new[] { 100 }));
        }

        [Test]
        public void WindowsProcessAffinityConfigurationSupportsMaxCoreIndex63()
        {
            // Core 63 is the maximum supported (0-based indexing)
            WindowsProcessAffinityConfiguration config = new WindowsProcessAffinityConfiguration(new[] { 63 });
            
            Assert.AreEqual(1L << 63, config.AffinityMask.ToInt64());
        }

        [Test]
        public void WindowsProcessAffinityConfigurationToStringIncludesMask()
        {
            WindowsProcessAffinityConfiguration config = new WindowsProcessAffinityConfiguration(new[] { 0, 1, 2, 3 });
            
            string result = config.ToString();
            
            Assert.IsTrue(result.Contains("0,1,2,3"));
            Assert.IsTrue(result.Contains("Mask: 0xF")); // 15 in hex is 0xF
        }

        [Test]
        [Platform(Exclude = "Unix,Linux,MacOsX")]
        public void WindowsProcessAffinityConfigurationApplyAffinityThrowsOnNullProcess()
        {
            WindowsProcessAffinityConfiguration config = new WindowsProcessAffinityConfiguration(new[] { 0, 1 });
            
#pragma warning disable CA1416
            Assert.Throws<ArgumentException>(() => config.ApplyAffinity(null));
#pragma warning restore CA1416
        }

        [Test]
        [Platform(Exclude = "Unix,Linux,MacOsX")]
        public void WindowsProcessAffinityConfigurationApplyAffinityThrowsOnExitedProcess()
        {
            WindowsProcessAffinityConfiguration config = new WindowsProcessAffinityConfiguration(new[] { 0, 1 });
            InMemoryProcess process = new InMemoryProcess();
            process.OnHasExited = () => true;
            
#pragma warning disable CA1416
            Assert.Throws<InvalidOperationException>(() => config.ApplyAffinity(process));
#pragma warning restore CA1416
        }

        [Test]
        [Platform(Exclude = "Unix,Linux,MacOsX")]
        public void WindowsProcessAffinityConfigurationApplyAffinityThrowsOnIncompatibleProcessProxy()
        {
            WindowsProcessAffinityConfiguration config = new WindowsProcessAffinityConfiguration(new[] { 0, 1 });
            InMemoryProcess process = new InMemoryProcess();
            process.OnHasExited = () => false;
            
#pragma warning disable CA1416
            // InMemoryProcess is not a ProcessProxy, so this should throw
            Assert.Throws<NotSupportedException>(() => config.ApplyAffinity(process));
#pragma warning restore CA1416
        }
    }
}
