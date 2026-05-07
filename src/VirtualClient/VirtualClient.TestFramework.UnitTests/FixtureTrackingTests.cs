// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient
{
    using System;
    using System.Diagnostics;
    using NUnit.Framework;
    using VirtualClient.Common;

    [TestFixture]
    [Category("Unit")]
    internal class FixtureTrackingTests
    {
        private FixtureTracking tracking;

        [SetUp]
        public void SetupTest()
        {
            this.tracking = new FixtureTracking();
        }

        [Test]
        public void AssertCommandsExecutedThrowsWhenNoCommandsWereExecuted()
        {
            Assert.Throws<InvalidOperationException>(() => this.tracking.AssertCommandsExecuted("any-command"));
        }

        [Test]
        public void AssertCommandsExecutedThrowsWhenExpectedCommandWasNotExecuted()
        {
            this.AddCommand("command1 --arg1");

            Assert.Throws<InvalidOperationException>(() => this.tracking.AssertCommandsExecuted("command2"));
        }

        [Test]
        public void AssertCommandsExecutedDoesNotThrowWhenExpectedCommandWasExecuted()
        {
            this.AddCommand("command1 --arg1");

            Assert.DoesNotThrow(() => this.tracking.AssertCommandsExecuted("command1 --arg1"));
        }

        [Test]
        public void AssertCommandsExecutedMatchesUsingRegularExpressions()
        {
            this.AddCommand("/home/user/bin/workload --threads 4 --size 128");

            Assert.DoesNotThrow(() => this.tracking.AssertCommandsExecuted("workload.*--threads 4"));
        }

        [Test]
        public void AssertCommandsExecutedMatchesRegardlessOfOrder()
        {
            this.AddCommand("command1");
            this.AddCommand("command2");
            this.AddCommand("command3");

            Assert.DoesNotThrow(() => this.tracking.AssertCommandsExecuted("command3", "command1"));
        }

        [Test]
        public void AssertCommandsExecutedMatchesAreCaseInsensitive()
        {
            this.AddCommand("SomeCommand --Arg1");

            Assert.DoesNotThrow(() => this.tracking.AssertCommandsExecuted("somecommand --arg1"));
        }

        [Test]
        public void AssertCommandsExecutedFallsBackToExactMatchWhenRegexIsInvalid()
        {
            // The '[' is an invalid regex pattern by itself.
            this.AddCommand("[invalid-regex");

            Assert.DoesNotThrow(() => this.tracking.AssertCommandsExecuted("[invalid-regex"));
        }

        [Test]
        public void AssertCommandsExecutedDoesNotMatchTheSameCommandTwice()
        {
            this.AddCommand("command1");

            Assert.Throws<InvalidOperationException>(() => this.tracking.AssertCommandsExecuted("command1", "command1"));
        }

        [Test]
        public void AssertCommandsExecutedInOrderThrowsWhenNoCommandsWereExecuted()
        {
            Assert.Throws<InvalidOperationException>(() => this.tracking.AssertCommandsExecuted(true, "any-command"));
        }

        [Test]
        public void AssertCommandsExecutedInOrderThrowsWhenCommandsAreOutOfOrder()
        {
            this.AddCommand("command1");
            this.AddCommand("command2");

            Assert.Throws<InvalidOperationException>(() => this.tracking.AssertCommandsExecuted(true, "command2", "command1"));
        }

        [Test]
        public void AssertCommandsExecutedInOrderDoesNotThrowWhenCommandsAreInOrder()
        {
            this.AddCommand("command1");
            this.AddCommand("command2");
            this.AddCommand("command3");

            Assert.DoesNotThrow(() => this.tracking.AssertCommandsExecuted(true, "command1", "command3"));
        }

        [Test]
        public void AssertCommandsExecutedInOrderMatchesUsingRegularExpressions()
        {
            this.AddCommand("sudo chmod +x /home/user/workload");
            this.AddCommand("sudo /home/user/workload --port 6379");

            Assert.DoesNotThrow(() => this.tracking.AssertCommandsExecuted(true,
                "chmod.*workload",
                "workload.*--port 6379"));
        }

        [Test]
        public void AssertCommandsExecutedInOrderFallsBackToExactMatchWhenRegexIsInvalid()
        {
            this.AddCommand("[first");
            this.AddCommand("[second");

            Assert.DoesNotThrow(() => this.tracking.AssertCommandsExecuted(true, "[first", "[second"));
        }

        [Test]
        public void AssertCommandExecutedTimesThrowsWhenCountDoesNotMatch()
        {
            this.AddCommand("command1");

            Assert.Throws<InvalidOperationException>(() => this.tracking.AssertCommandExecutedTimes("command1", 2));
        }

        [Test]
        public void AssertCommandExecutedTimesDoesNotThrowWhenCountMatches()
        {
            this.AddCommand("command1 --port 6379");
            this.AddCommand("command1 --port 6380");

            Assert.DoesNotThrow(() => this.tracking.AssertCommandExecutedTimes("command1", 2));
        }

        [Test]
        public void AssertCommandExecutedTimesHandlesZeroExpectedExecutions()
        {
            this.AddCommand("command1");

            Assert.DoesNotThrow(() => this.tracking.AssertCommandExecutedTimes("command2", 0));
        }

        [Test]
        public void AssertCommandExecutedTimesMatchesUsingRegularExpressions()
        {
            this.AddCommand("numactl -C 0 redis-server --port 6379");
            this.AddCommand("numactl -C 1 redis-server --port 6380");
            this.AddCommand("chmod +x redis-server");

            Assert.DoesNotThrow(() => this.tracking.AssertCommandExecutedTimes("numactl.*redis-server", 2));
        }

        [Test]
        public void AssertCommandExecutedTimesFallsBackToSubstringMatchWhenRegexIsInvalid()
        {
            this.AddCommand("run [test");
            this.AddCommand("run [test");

            Assert.DoesNotThrow(() => this.tracking.AssertCommandExecutedTimes("[test", 2));
        }

        [Test]
        public void ClearRemovesAllTrackedCommands()
        {
            this.AddCommand("command1");
            this.AddCommand("command2");

            this.tracking.Clear();

            Assert.AreEqual(0, this.tracking.Commands.Count);
            Assert.Throws<InvalidOperationException>(() => this.tracking.AssertCommandsExecuted("command1"));
        }

        [Test]
        public void CommandsPropertyReturnsAllTrackedCommandsInOrder()
        {
            this.AddCommand("first");
            this.AddCommand("second");
            this.AddCommand("third");

            Assert.AreEqual(3, this.tracking.Commands.Count);
            Assert.AreEqual("first", this.tracking.Commands[0].FullCommand);
            Assert.AreEqual("second", this.tracking.Commands[1].FullCommand);
            Assert.AreEqual("third", this.tracking.Commands[2].FullCommand);
        }

        [Test]
        public void GetDetailedSummaryReturnsFormattedSummaryOfTrackedCommands()
        {
            this.AddCommand("command1 --arg1");

            string summary = this.tracking.GetDetailedSummary();

            Assert.IsNotNull(summary);
            StringAssert.Contains("Total Commands Executed: 1", summary);
            StringAssert.Contains("command1 --arg1", summary);
        }

        [Test]
        public void GetDetailedSummaryHandlesNoCommands()
        {
            string summary = this.tracking.GetDetailedSummary();

            Assert.IsNotNull(summary);
            StringAssert.Contains("Total Commands Executed: 0", summary);
        }

        [Test]
        public void ErrorMessageIncludesActualCommandsExecuted()
        {
            this.AddCommand("actual-command --flag");

            InvalidOperationException error = Assert.Throws<InvalidOperationException>(
                () => this.tracking.AssertCommandsExecuted("missing-command"));

            StringAssert.Contains("actual-command --flag", error.Message);
            StringAssert.Contains("Missing Commands:", error.Message);
        }

        [Test]
        public void ErrorMessageForOrderedAssertionIncludesExpectedAndActualOrder()
        {
            this.AddCommand("command2");
            this.AddCommand("command1");

            InvalidOperationException error = Assert.Throws<InvalidOperationException>(
                () => this.tracking.AssertCommandsExecuted(true, "command1", "command2"));

            StringAssert.Contains("Expected Order:", error.Message);
            StringAssert.Contains("Actual Execution Order:", error.Message);
        }

        [Test]
        public void ErrorMessageForCountMismatchIncludesExpectedAndActualCounts()
        {
            this.AddCommand("command1");

            InvalidOperationException error = Assert.Throws<InvalidOperationException>(
                () => this.tracking.AssertCommandExecutedTimes("command1", 3));

            StringAssert.Contains("Expected: 3 execution(s)", error.Message);
            StringAssert.Contains("Actual:   1 execution(s)", error.Message);
        }

        private void AddCommand(string fullCommand)
        {
            // Split on first space to separate command from arguments, matching how
            // InMemoryProcessManager records executions.
            string command = fullCommand;
            string arguments = null;

            int spaceIndex = fullCommand.IndexOf(' ');
            if (spaceIndex >= 0)
            {
                command = fullCommand.Substring(0, spaceIndex);
                arguments = fullCommand.Substring(spaceIndex + 1);
            }

            InMemoryProcess process = new InMemoryProcess
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = command,
                    Arguments = arguments
                },
                OnHasExited = () => true,
                OnStart = () => true
            };

            this.tracking.AddCommand(new CommandExecutionInfo(
                command,
                arguments,
                null,
                process,
                DateTime.UtcNow));
        }
    }
}
