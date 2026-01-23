// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Common.ProcessAffinity
{
    using System;
    using System.Linq;
    using System.Text.RegularExpressions;
    using NUnit.Framework;

    [TestFixture]
    [Category("Unit")]
    public class LinuxProcessAffinityConfigurationTests
    {
        [Test]
        public void LinuxProcessAffinityConfigurationGeneratesCorrectNumactlSpecForSingleCore()
        {
            LinuxProcessAffinityConfiguration config = new LinuxProcessAffinityConfiguration(new[] { 0 });
            
            // Verify through GetCommandWithAffinity which uses GetNumactlCoreSpec internally
            string command = config.GetCommandWithAffinity("test", null);
            
            Assert.IsTrue(command.Contains("-C 0"));
        }

        [Test]
        public void LinuxProcessAffinityConfigurationGeneratesCorrectNumactlSpecForContiguousCores()
        {
            LinuxProcessAffinityConfiguration config = new LinuxProcessAffinityConfiguration(new[] { 0, 1, 2, 3 });
            
            string command = config.GetCommandWithAffinity("test", null);
            
            // Should be optimized to range notation
            Assert.IsTrue(command.Contains("-C 0-3"));
        }

        [Test]
        public void LinuxProcessAffinityConfigurationGeneratesCorrectNumactlSpecForNonContiguousCores()
        {
            LinuxProcessAffinityConfiguration config = new LinuxProcessAffinityConfiguration(new[] { 0, 2, 4 });
            
            string command = config.GetCommandWithAffinity("test", null);
            
            Assert.IsTrue(command.Contains("-C 0,2,4"));
        }

        [Test]
        public void LinuxProcessAffinityConfigurationGeneratesCorrectNumactlSpecForMixedCores()
        {
            LinuxProcessAffinityConfiguration config = new LinuxProcessAffinityConfiguration(new[] { 0, 1, 2, 5, 7, 8, 9 });
            
            string command = config.GetCommandWithAffinity("test", null);
            
            // Should optimize ranges: 0-2,5,7-9
            Assert.IsTrue(command.Contains("-C 0-2,5,7-9"));
        }

        [Test]
        public void LinuxProcessAffinityConfigurationGeneratesCorrectNumactlSpecForComplexPattern()
        {
            LinuxProcessAffinityConfiguration config = new LinuxProcessAffinityConfiguration(
                new[] { 0, 1, 2, 5, 6, 10, 12, 13, 14, 15 });
            
            string command = config.GetCommandWithAffinity("test", null);
            
            // 0-2 (3 cores), 5,6 (2 cores), 10 (single), 12-15 (4 cores)
            Assert.IsTrue(command.Contains("-C 0-2,5,6,10,12-15"));
        }

        [Test]
        public void LinuxProcessAffinityConfigurationGeneratesCorrectNumactlSpecForHighCoreIndices()
        {
            LinuxProcessAffinityConfiguration config = new LinuxProcessAffinityConfiguration(new[] { 100, 101, 102 });
            
            string command = config.GetCommandWithAffinity("test", null);
            
            Assert.IsTrue(command.Contains("-C 100-102"));
        }

        [Test]
        public void LinuxProcessAffinityConfigurationGeneratesCorrectNumactlCommandForSingleCore()
        {
            LinuxProcessAffinityConfiguration config = new LinuxProcessAffinityConfiguration(new[] { 0 });
            
            string command = config.GetCommandWithAffinity(null, "myworkload --arg1 --arg2");
            
            Assert.AreEqual("\"numactl -C 0 myworkload --arg1 --arg2\"", command);
        }

        [Test]
        public void LinuxProcessAffinityConfigurationGeneratesCorrectNumactlCommandForMultipleCores()
        {
            LinuxProcessAffinityConfiguration config = new LinuxProcessAffinityConfiguration(new[] { 0, 1, 2 });
            
            string command = config.GetCommandWithAffinity(null, "myworkload --arg1 --arg2");
            
            Assert.AreEqual("\"numactl -C 0-2 myworkload --arg1 --arg2\"", command);
        }

        [Test]
        public void LinuxProcessAffinityConfigurationGeneratesCorrectNumactlCommandWithEmptyArguments()
        {
            LinuxProcessAffinityConfiguration config = new LinuxProcessAffinityConfiguration(new[] { 1, 3, 5 });
            
            string command = config.GetCommandWithAffinity(null, "myworkload");
            
            Assert.AreEqual("\"numactl -C 1,3,5 myworkload\"", command);
        }

        [Test]
        public void LinuxProcessAffinityConfigurationHandlesComplexArguments()
        {
            LinuxProcessAffinityConfiguration config = new LinuxProcessAffinityConfiguration(new[] { 0, 1 });
            
            string command = config.GetCommandWithAffinity(
                null,
                "myworkload --file=\"path with spaces\" --option=value");
            
            // 2 cores use comma notation (0,1), not range (0-1)
            Assert.AreEqual(
                "\"numactl -C 0,1 myworkload --file=\"path with spaces\" --option=value\"", 
                command);
        }

        [Test]
        public void LinuxProcessAffinityConfigurationToStringIncludesNumactlSpec()
        {
            LinuxProcessAffinityConfiguration config = new LinuxProcessAffinityConfiguration(new[] { 0, 1, 2, 5 });
            
            string result = config.ToString();
            
            Assert.IsTrue(result.Contains("0,1,2,5"));
            Assert.IsTrue(result.Contains("numactl: -C 0-2,5"));
        }

        [Test]
        public void LinuxProcessAffinityConfigurationOptimizesRanges()
        {
            // Test various range optimization scenarios by checking the command output
            // Note: 2 consecutive cores use comma notation (0,1), 3+ use range notation (0-2)
            var testCases = new[]
            {
                (new[] { 0 }, "-C 0"),
                (new[] { 0, 1 }, "-C 0,1"), // 2 cores: comma notation
                (new[] { 0, 1, 2 }, "-C 0-2"), // 3+ cores: range notation
                (new[] { 0, 2 }, "-C 0,2"),
                (new[] { 0, 1, 3 }, "-C 0,1,3"), // 2 cores then gap
                (new[] { 0, 1, 2, 4, 5, 6 }, "-C 0-2,4-6"), // Two 3-core ranges
                (new[] { 0, 2, 4, 6, 8 }, "-C 0,2,4,6,8"),
                (new[] { 0, 1, 2, 3, 5, 6, 7, 8, 10 }, "-C 0-3,5-8,10") // 4-core range, 4-core range, single
            };

            foreach (var (cores, expectedSpec) in testCases)
            {
                LinuxProcessAffinityConfiguration config = new LinuxProcessAffinityConfiguration(cores);
                string command = config.GetCommandWithAffinity("test", null);
                Assert.IsTrue(command.Contains(expectedSpec), $"Failed for cores: {string.Join(",", cores)}. Expected '{expectedSpec}' in '{command}'");
            }
        }

        [Test]
        public void LinuxProcessAffinityConfigurationHandlesUnsortedCores()
        {
            // Cores should be sorted before optimization
            LinuxProcessAffinityConfiguration config = new LinuxProcessAffinityConfiguration(new[] { 5, 0, 2, 1, 3 });
            
            string command = config.GetCommandWithAffinity("test", null);
            
            // Should sort and optimize: 0-3,5
            Assert.IsTrue(command.Contains("-C 0-3,5"));
        }
    }
}
