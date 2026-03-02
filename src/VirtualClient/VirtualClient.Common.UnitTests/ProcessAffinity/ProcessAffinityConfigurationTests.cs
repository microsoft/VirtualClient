// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Common.ProcessAffinity
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using NUnit.Framework;

    [TestFixture]
    [Category("Unit")]
    public class ProcessAffinityConfigurationTests
    {
        [Test]
        public void ProcessAffinityConfigurationParsesCommaSeparatedCoreSpecViaCreateMethod()
        {
            ProcessAffinityConfiguration config = ProcessAffinityConfiguration.Create(PlatformID.Unix, "0,1,2,3");
            
            Assert.IsNotNull(config.Cores);
            Assert.AreEqual(4, config.Cores.Count());
            CollectionAssert.AreEqual(new[] { 0, 1, 2, 3 }, config.Cores);
        }

        [Test]
        public void ProcessAffinityConfigurationParsesRangeCoreSpecViaCreateMethod()
        {
            ProcessAffinityConfiguration config = ProcessAffinityConfiguration.Create(PlatformID.Unix, "0-3");
            
            Assert.IsNotNull(config.Cores);
            Assert.AreEqual(4, config.Cores.Count());
            CollectionAssert.AreEqual(new[] { 0, 1, 2, 3 }, config.Cores);
        }

        [Test]
        public void ProcessAffinityConfigurationParsesMixedCoreSpecViaCreateMethod()
        {
            ProcessAffinityConfiguration config = ProcessAffinityConfiguration.Create(PlatformID.Unix, "0,2-4,6");
            
            Assert.IsNotNull(config.Cores);
            Assert.AreEqual(5, config.Cores.Count());
            CollectionAssert.AreEqual(new[] { 0, 2, 3, 4, 6 }, config.Cores);
        }

        [Test]
        public void ProcessAffinityConfigurationParsesComplexCoreSpecViaCreateMethod()
        {
            ProcessAffinityConfiguration config = ProcessAffinityConfiguration.Create(PlatformID.Unix, "0-2,5,7-9,12");
            
            Assert.IsNotNull(config.Cores);
            Assert.AreEqual(8, config.Cores.Count());
            CollectionAssert.AreEqual(new[] { 0, 1, 2, 5, 7, 8, 9, 12 }, config.Cores);
        }

        [Test]
        public void ProcessAffinityConfigurationParsesSingleCoreSpecViaCreateMethod()
        {
            ProcessAffinityConfiguration config = ProcessAffinityConfiguration.Create(PlatformID.Unix, "5");
            
            Assert.IsNotNull(config.Cores);
            Assert.AreEqual(1, config.Cores.Count());
            CollectionAssert.AreEqual(new[] { 5 }, config.Cores);
        }

        [Test]
        public void ProcessAffinityConfigurationThrowsOnInvalidCoreSpec()
        {
            Assert.Throws<ArgumentException>(() => ProcessAffinityConfiguration.Create(PlatformID.Unix, "invalid"));
            Assert.Throws<ArgumentException>(() => ProcessAffinityConfiguration.Create(PlatformID.Unix, "0-"));
            Assert.Throws<ArgumentException>(() => ProcessAffinityConfiguration.Create(PlatformID.Unix, "-5"));
            Assert.Throws<ArgumentException>(() => ProcessAffinityConfiguration.Create(PlatformID.Unix, "a-b"));
        }

        [Test]
        public void ProcessAffinityConfigurationThrowsOnNullOrEmptyCoreSpec()
        {
            Assert.Throws<ArgumentException>(() => ProcessAffinityConfiguration.Create(PlatformID.Unix, (string)null));
            Assert.Throws<ArgumentException>(() => ProcessAffinityConfiguration.Create(PlatformID.Unix, string.Empty));
            Assert.Throws<ArgumentException>(() => ProcessAffinityConfiguration.Create(PlatformID.Unix, "  "));
        }

        [Test]
        public void ProcessAffinityConfigurationCreatesLinuxConfiguration()
        {
            ProcessAffinityConfiguration config = ProcessAffinityConfiguration.Create(
                PlatformID.Unix, 
                new[] { 0, 1, 2 });

            Assert.IsNotNull(config);
            Assert.IsInstanceOf<LinuxProcessAffinityConfiguration>(config);
            CollectionAssert.AreEqual(new[] { 0, 1, 2 }, config.Cores);
        }

        [Test]
        public void ProcessAffinityConfigurationCreatesLinuxConfigurationFromSpec()
        {
            ProcessAffinityConfiguration config = ProcessAffinityConfiguration.Create(
                PlatformID.Unix, 
                "0-2");

            Assert.IsNotNull(config);
            Assert.IsInstanceOf<LinuxProcessAffinityConfiguration>(config);
            CollectionAssert.AreEqual(new[] { 0, 1, 2 }, config.Cores);
        }

        [Test]
        public void ProcessAffinityConfigurationThrowsOnUnsupportedPlatform()
        {
            Assert.Throws<NotSupportedException>(() => ProcessAffinityConfiguration.Create(
                PlatformID.Other, 
                new[] { 0, 1, 2 }));

            Assert.Throws<NotSupportedException>(() => ProcessAffinityConfiguration.Create(
                PlatformID.MacOSX, 
                "0,1,2"));
        }

        [Test]
        public void ProcessAffinityConfigurationThrowsOnNegativeCoreIndexInCoreSpec()
        {
            // Negative indices are validated when parsing core list strings
            Assert.Throws<ArgumentException>(() => ProcessAffinityConfiguration.Create(
                PlatformID.Unix, 
                "-1,0,1"));

            Assert.Throws<ArgumentException>(() => ProcessAffinityConfiguration.Create(
                PlatformID.Unix, 
                "0,-5,2"));
        }

        [Test]
        public void ProcessAffinityConfigurationThrowsOnEmptyCores()
        {
            Assert.Throws<ArgumentException>(() => ProcessAffinityConfiguration.Create(
                PlatformID.Unix, 
                Array.Empty<int>()));

            Assert.Throws<ArgumentException>(() => ProcessAffinityConfiguration.Create(
                PlatformID.Unix, 
                new List<int>()));
        }

        [Test]
        public void ProcessAffinityConfigurationRemovesDuplicateCores()
        {
            ProcessAffinityConfiguration config = ProcessAffinityConfiguration.Create(
                PlatformID.Unix, 
                new[] { 0, 1, 1, 2, 2, 2, 3 });

            Assert.AreEqual(4, config.Cores.Count());
            CollectionAssert.AreEqual(new[] { 0, 1, 2, 3 }, config.Cores);
        }

        [Test]
        public void ProcessAffinityConfigurationToStringReturnsExpectedFormat()
        {
            // Linux configuration includes numactl spec
            ProcessAffinityConfiguration linuxConfig = ProcessAffinityConfiguration.Create(
                PlatformID.Unix, 
                new[] { 0, 1, 2, 5 });
            string linuxString = linuxConfig.ToString();
            Assert.IsTrue(linuxString.Contains("0,1,2,5"));
            Assert.IsTrue(linuxString.Contains("numactl:"));
        }
    }
}
