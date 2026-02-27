// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Text.RegularExpressions;
    using VirtualClient.Common;
    using VirtualClient.Common.Extensions;

    /// <summary>
    /// Provides tracking of process/command executions and assertion methods
    /// for test validation scenarios.
    /// </summary>
    public class FixtureTracking
    {
        private readonly List<CommandExecutionInfo> commands;

        /// <summary>
        /// Initializes a new instance of the <see cref="FixtureTracking"/> class.
        /// </summary>
        public FixtureTracking()
        {
            this.commands = new List<CommandExecutionInfo>();
        }

        /// <summary>
        /// The set of all commands executed (in order).
        /// </summary>
        public IReadOnlyList<CommandExecutionInfo> Commands => this.commands.AsReadOnly();

        /// <summary>
        /// The set of all process proxies created (derived from <see cref="Commands"/>).
        /// </summary>
        public IReadOnlyList<IProcessProxy> Processes => this.commands.Select(c => c.Process).ToList().AsReadOnly();

        /// <summary>
        /// Clears all tracked commands.
        /// </summary>
        public void Clear()
        {
            this.commands.Clear();
        }

        /// <summary>
        /// Asserts that the specified commands were executed (in any order). Command patterns
        /// are evaluated as regular expressions. If the regex is invalid, an exact match comparison
        /// is used as a fallback.
        /// </summary>
        /// <param name="expectedCommandPatterns">
        /// Regular expression patterns matching the expected commands.
        /// </param>
        public void AssertCommandsExecuted(params string[] expectedCommandPatterns)
        {
            expectedCommandPatterns.ThrowIfNullOrEmpty(nameof(expectedCommandPatterns));

            List<string> notFound = new List<string>();
            List<CommandExecutionInfo> matchedCommands = new List<CommandExecutionInfo>();

            foreach (string pattern in expectedCommandPatterns)
            {
                CommandExecutionInfo match = this.commands.FirstOrDefault(cmd =>
                {
                    try
                    {
                        return Regex.IsMatch(cmd.FullCommand, pattern, RegexOptions.IgnoreCase)
                            && !matchedCommands.Contains(cmd);
                    }
                    catch
                    {
                        // If regex fails, try exact match
                        return string.Equals(cmd.FullCommand, pattern, StringComparison.OrdinalIgnoreCase)
                            && !matchedCommands.Contains(cmd);
                    }
                });

                if (match == null)
                {
                    notFound.Add(pattern);
                }
                else
                {
                    matchedCommands.Add(match);
                }
            }

            if (notFound.Any())
            {
                string errorMessage = this.BuildCommandNotFoundErrorMessage(notFound, expectedCommandPatterns);
                throw new InvalidOperationException(errorMessage);
            }
        }

        /// <summary>
        /// Asserts that the specified commands were executed in the exact order specified. Command
        /// patterns are evaluated as regular expressions. If the regex is invalid, an exact match
        /// comparison is used as a fallback.
        /// </summary>
        /// <param name="expectedCommandPatterns">
        /// Regular expression patterns matching the expected commands in order.
        /// </param>
        public void AssertCommandsExecutedInOrder(params string[] expectedCommandPatterns)
        {
            expectedCommandPatterns.ThrowIfNullOrEmpty(nameof(expectedCommandPatterns));

            List<string> notFound = new List<string>();
            int currentIndex = 0;

            foreach (string pattern in expectedCommandPatterns)
            {
                bool found = false;

                // Search for the pattern starting from currentIndex
                for (int i = currentIndex; i < this.commands.Count; i++)
                {
                    try
                    {
                        if (Regex.IsMatch(this.commands[i].FullCommand, pattern, RegexOptions.IgnoreCase))
                        {
                            currentIndex = i + 1; // Move past this match
                            found = true;
                            break;
                        }
                    }
                    catch
                    {
                        // If regex fails, try exact match
                        if (string.Equals(this.commands[i].FullCommand, pattern, StringComparison.OrdinalIgnoreCase))
                        {
                            currentIndex = i + 1;
                            found = true;
                            break;
                        }
                    }
                }

                if (!found)
                {
                    notFound.Add(pattern);
                }
            }

            if (notFound.Any())
            {
                string errorMessage = this.BuildOrderedCommandNotFoundErrorMessage(
                    notFound,
                    expectedCommandPatterns);
                throw new InvalidOperationException(errorMessage);
            }
        }

        /// <summary>
        /// Asserts that a command matching the pattern was executed exactly the expected
        /// number of times.
        /// </summary>
        /// <param name="commandPattern">A regular expression pattern for the command.</param>
        /// <param name="expectedCount">The expected number of executions.</param>
        public void AssertCommandExecutedTimes(string commandPattern, int expectedCount)
        {
            commandPattern.ThrowIfNullOrWhiteSpace(nameof(commandPattern));

            int actualCount = this.commands.Count(cmd =>
            {
                try
                {
                    return Regex.IsMatch(cmd.FullCommand, commandPattern, RegexOptions.IgnoreCase);
                }
                catch
                {
                    return cmd.FullCommand.Contains(commandPattern, StringComparison.OrdinalIgnoreCase);
                }
            });

            if (actualCount != expectedCount)
            {
                string errorMessage = this.BuildCountMismatchErrorMessage(
                    commandPattern,
                    expectedCount,
                    actualCount);
                throw new InvalidOperationException(errorMessage);
            }
        }

        /// <summary>
        /// Returns a detailed summary of all tracked commands. This is useful for
        /// debugging test failures.
        /// </summary>
        public string GetDetailedSummary()
        {
            StringBuilder summary = new StringBuilder();
            summary.AppendLine($"Total Commands Executed: {this.commands.Count}");
            summary.AppendLine();

            for (int i = 0; i < this.commands.Count; i++)
            {
                CommandExecutionInfo cmd = this.commands[i];
                summary.AppendLine($"[{i + 1}] Command: {cmd.FullCommand}");
                summary.AppendLine($"    Working Dir: {cmd.WorkingDirectory}");
                summary.AppendLine($"    Exit Code: {cmd.ExitCode}");
                summary.AppendLine($"    Executed At: {cmd.ExecutionTime:yyyy-MM-dd HH:mm:ss.fff}");

                if (!string.IsNullOrWhiteSpace(cmd.StandardOutput))
                {
                    string output = cmd.StandardOutput.Length > 200
                        ? cmd.StandardOutput.Substring(0, 200) + "..."
                        : cmd.StandardOutput;
                    summary.AppendLine($"    Output: {output}");
                }

                summary.AppendLine();
            }

            return summary.ToString();
        }

        /// <summary>
        /// Adds a command execution record to the tracking list.
        /// </summary>
        internal void AddCommand(CommandExecutionInfo commandInfo)
        {
            commandInfo.ThrowIfNull(nameof(commandInfo));
            this.commands.Add(commandInfo);
        }

        private string BuildCommandNotFoundErrorMessage(
            List<string> notFound,
            string[] expectedPatterns)
        {
            StringBuilder message = new StringBuilder();
            message.AppendLine("Expected commands were not executed:");
            message.AppendLine();

            message.AppendLine("Missing Commands:");
            foreach (string pattern in notFound)
            {
                message.AppendLine($"  - {pattern}");
            }

            message.AppendLine();
            message.AppendLine("Actual Commands Executed:");
            if (this.commands.Any())
            {
                foreach (var cmd in this.commands)
                {
                    message.AppendLine($"  - {cmd.FullCommand}");
                }
            }
            else
            {
                message.AppendLine("  (No commands executed)");
            }

            message.AppendLine();
            message.AppendLine("Debugging Hints:");
            message.AppendLine("  - Check if the command pattern uses correct regex syntax");
            message.AppendLine("  - Verify the command is actually being executed");
            message.AppendLine("  - Use GetDetailedSummary() for full command details");

            return message.ToString();
        }

        private string BuildOrderedCommandNotFoundErrorMessage(
            List<string> notFound,
            string[] expectedPatterns)
        {
            StringBuilder message = new StringBuilder();
            message.AppendLine("Expected commands were not executed in the specified order:");
            message.AppendLine();

            message.AppendLine("Missing or Out-of-Order Commands:");
            foreach (string pattern in notFound)
            {
                message.AppendLine($"  - {pattern}");
            }

            message.AppendLine();
            message.AppendLine("Expected Order:");
            for (int i = 0; i < expectedPatterns.Length; i++)
            {
                string status = notFound.Contains(expectedPatterns[i]) ? "x" : "ok";
                message.AppendLine($"  {status} [{i + 1}] {expectedPatterns[i]}");
            }

            message.AppendLine();
            message.AppendLine("Actual Execution Order:");
            if (this.commands.Any())
            {
                for (int i = 0; i < this.commands.Count; i++)
                {
                    message.AppendLine($"  [{i + 1}] {this.commands[i].FullCommand}");
                }
            }
            else
            {
                message.AppendLine("  (No commands executed)");
            }

            message.AppendLine();
            message.AppendLine("Debugging Hints:");
            message.AppendLine("  - Commands must appear in the order specified");
            message.AppendLine("  - Check if intermediate commands are missing from expected list");
            message.AppendLine("  - Verify regex patterns match actual command syntax");

            return message.ToString();
        }

        private string BuildCountMismatchErrorMessage(
            string pattern,
            int expectedCount,
            int actualCount)
        {
            StringBuilder message = new StringBuilder();
            message.AppendLine($"Command execution count mismatch for pattern: '{pattern}'");
            message.AppendLine();
            message.AppendLine($"Expected: {expectedCount} execution(s)");
            message.AppendLine($"Actual:   {actualCount} execution(s)");
            message.AppendLine();

            if (actualCount > 0)
            {
                message.AppendLine("Matching Commands Found:");
                var matches = this.commands.Where(cmd =>
                {
                    try
                    {
                        return Regex.IsMatch(cmd.FullCommand, pattern, RegexOptions.IgnoreCase);
                    }
                    catch
                    {
                        return cmd.FullCommand.Contains(pattern, StringComparison.OrdinalIgnoreCase);
                    }
                });

                foreach (var match in matches)
                {
                    message.AppendLine($"  - {match.FullCommand}");
                }
            }

            message.AppendLine();
            message.AppendLine("Debugging Hints:");
            if (actualCount > expectedCount)
            {
                message.AppendLine("  - Command was executed more times than expected");
                message.AppendLine("  - Check for duplicate execution logic");
            }
            else if (actualCount < expectedCount)
            {
                message.AppendLine("  - Command was executed fewer times than expected");
                message.AppendLine("  - Verify all code paths are executing");
            }

            return message.ToString();
        }
    }
}
