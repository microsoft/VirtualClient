// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.UnitTests
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using NUnit.Framework;

    [TestFixture]
    [Category("Unit")]
    public class ExecuteRemoteAgentCommandTests
    {
        [Test]
        [TestCase(
            "remote --profile=ANY-PROFILE.json --packages=anystore --timeout=01:00:00 --agent-ssh=user@10.1.2.3;pass",
            "--profile=ANY-PROFILE.json --packages=anystore --timeout=01:00:00")]
        [TestCase(
            "remote --profile=ANY-PROFILE.json --packages=anystore --timeout=01:00:00 --agent-ssh=user@10.1.2.3;pass --agent-ssh=user@10.1.2.4;pass",
            "--profile=ANY-PROFILE.json --packages=anystore --timeout=01:00:00")]
        [TestCase(
            "remote --profile=ANY-PROFILE.json --packages=anystore --timeout=01:00:00 --package-dir=/any/packages --log-dir=/any/logs --state-dir=/any/state --temp-dir=/any/temp --agent-ssh=user@10.1.2.3;pass --agent-ssh=user@10.1.2.4;pass",
            "--profile=ANY-PROFILE.json --packages=anystore --timeout=01:00:00 --package-dir=/any/packages --log-dir=/any/logs --state-dir=/any/state --temp-dir=/any/temp")]
        public void ExecuteRemoteAgentCommandSuppliesTheExpectedCommandToExecuteOnTheTargetAgents_1(string originalCommand, string expectedTargetCommand)
        {
            var command = new TestExecuteRemoteAgentCommand();
            string actualTargetCommand = command.GetTargetCommandArguments(originalCommand.Split(' '));
            Assert.AreEqual(expectedTargetCommand, actualTargetCommand);
        }

        [Test]
        [TestCase(
            "remote \"./packages/custom_scripts.1.0.0/execute_workload.py --log-dir=/any/log/dir\" --packages=anystore --timeout=01:00:00 --agent-ssh=user@10.1.2.3;pass",
            "\"./packages/custom_scripts.1.0.0/execute_workload.py --log-dir=/any/log/dir\" --packages=anystore --timeout=01:00:00")]
        [TestCase(
            "remote \"./packages/custom_scripts.1.0.0/execute_workload.py --log-dir=/any/log/dir\" --packages=anystore --timeout=01:00:00 --packages=anystore --timeout=01:00:00 --agent-ssh=user@10.1.2.3;pass --agent-ssh=user@10.1.2.4;pass",
            "\"./packages/custom_scripts.1.0.0/execute_workload.py --log-dir=/any/log/dir\" --packages=anystore --timeout=01:00:00 --packages=anystore --timeout=01:00:00")]
        [TestCase(
            "remote \"./packages/custom_scripts.1.0.0/execute_workload.py --log-dir=/any/log/dir\" --packages=anystore --timeout=01:00:00 --packages=anystore --timeout=01:00:00 --package-dir=/any/packages --log-dir=/any/logs --state-dir=/any/state --temp-dir=/any/temp --agent-ssh=user@10.1.2.3;pass --agent-ssh=user@10.1.2.4;pass",
            "\"./packages/custom_scripts.1.0.0/execute_workload.py --log-dir=/any/log/dir\" --packages=anystore --timeout=01:00:00 --packages=anystore --timeout=01:00:00 --package-dir=/any/packages --log-dir=/any/logs --state-dir=/any/state --temp-dir=/any/temp")]
        public void ExecuteRemoteAgentCommandSuppliesTheExpectedCommandToExecuteOnTheTargetAgents_2(string originalCommand, string expectedTargetCommand)
        {
            var command = new TestExecuteRemoteAgentCommand();
            string actualTargetCommand = command.GetTargetCommandArguments(originalCommand.Split(' '));
            Assert.AreEqual(expectedTargetCommand, actualTargetCommand);
        }

        private class TestExecuteRemoteAgentCommand : ExecuteRemoteAgentCommand
        {
            public new string GetTargetCommandArguments(string[] commandArguments)
            {
                return base.GetTargetCommandArguments(commandArguments);
            }
        }
    }
}
