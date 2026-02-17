// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.TestExtensions
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Text.RegularExpressions;
    using NUnit.Framework;
    using VirtualClient.Common;

    /// <summary>
    /// Fluent assertion extensions for MockFixture tracking.
    /// </summary>
    public static class MockFixtureTrackingAssertions
    {
        /// <summary>
        /// Asserts that specific commands were executed in the exact order specified.
        /// Supports regex patterns for flexible matching.
        /// </summary>
        /// <param name="tracking">The fixture tracking instance.</param>
        /// <param name="expectedCommands">The expected commands in order (supports regex patterns).</param>
        public static void AssertCommandsExecutedInOrder(this FixtureTracking tracking, params string[] expectedCommands)
        {
            var actualCommands = tracking.Commands.Select(c => c.FullCommand).ToList();

            if (expectedCommands.Length > actualCommands.Count)
            {
                Assert.Fail(
                    $"Expected {expectedCommands.Length} commands but only {actualCommands.Count} were executed.\n\n" +
                    FormatCommandMismatch(expectedCommands, actualCommands));
            }

            for (int i = 0; i < expectedCommands.Length; i++)
            {
                bool matches = TryMatchCommand(actualCommands[i], expectedCommands[i]);
                
                if (!matches)
                {
                    Assert.Fail(
                        $"Command mismatch at position {i}.\n\n" +
                        $"Expected: {expectedCommands[i]}\n" +
                        $"Actual:   {actualCommands[i]}\n\n" +
                        FormatCommandMismatch(expectedCommands, actualCommands, i));
                }
            }

            if (actualCommands.Count > expectedCommands.Length)
            {
                var extraCommands = actualCommands.Skip(expectedCommands.Length);
                Assert.Fail(
                    $"Unexpected additional commands executed:\n  {string.Join("\n  ", extraCommands)}\n\n" +
                    FormatCommandMismatch(expectedCommands, actualCommands));
            }
        }

        /// <summary>
        /// Asserts that specific commands were executed (order-independent).
        /// Supports regex patterns for flexible matching.
        /// </summary>
        /// <param name="tracking">The fixture tracking instance.</param>
        /// <param name="expectedCommands">The expected commands (supports regex patterns).</param>
        public static void AssertCommandsExecuted(this FixtureTracking tracking, params string[] expectedCommands)
        {
            var actualCommands = tracking.Commands.Select(c => c.FullCommand).ToList();
            var unmatchedExpected = new List<string>(expectedCommands);
            var matchedActual = new HashSet<string>();

            foreach (string expected in expectedCommands)
            {
                var match = actualCommands.FirstOrDefault(actual =>
                    !matchedActual.Contains(actual) && TryMatchCommand(actual, expected));

                if (match != null)
                {
                    unmatchedExpected.Remove(expected);
                    matchedActual.Add(match);
                }
            }

            if (unmatchedExpected.Any())
            {
                Assert.Fail(
                    $"Expected commands not executed:\n  {string.Join("\n  ", unmatchedExpected)}\n\n" +
                    $"Actual commands executed:\n  {string.Join("\n  ", actualCommands)}\n\n" +
                    $"Debugging: {unmatchedExpected.Count} expected command(s) did not match any of the {actualCommands.Count} commands executed.");
            }
        }

        /// <summary>
        /// Asserts that a command was executed exactly N times.
        /// Supports regex patterns for flexible matching.
        /// </summary>
        /// <param name="tracking">The fixture tracking instance.</param>
        /// <param name="commandPattern">The command pattern (supports regex).</param>
        /// <param name="expectedCount">The expected number of executions.</param>
        public static void AssertCommandExecutedTimes(this FixtureTracking tracking, string commandPattern, int expectedCount)
        {
            int actualCount = tracking.Commands.Count(c => TryMatchCommand(c.FullCommand, commandPattern));

            if (actualCount != expectedCount)
            {
                var matches = tracking.Commands
                    .Where(c => TryMatchCommand(c.FullCommand, commandPattern))
                    .Select(c => c.FullCommand);

                Assert.Fail(
                    $"Expected '{commandPattern}' to be executed {expectedCount} time(s), but was executed {actualCount} time(s).\n\n" +
                    $"Matching commands:\n  {string.Join("\n  ", matches)}\n\n" +
                    $"All commands:\n  {string.Join("\n  ", tracking.Commands.Select(c => c.FullCommand))}");
            }
        }

        /// <summary>
        /// Asserts that a file operation occurred.
        /// </summary>
        /// <param name="tracking">The fixture tracking instance.</param>
        /// <param name="operation">The operation type (e.g., "Read", "Write", "Exists").</param>
        /// <param name="filePath">The file path pattern (supports regex).</param>
        public static void AssertFileOperation(this FixtureTracking tracking, string operation, string filePath)
        {
            bool occurred = tracking.FileOperations.Any(fo =>
                fo.Operation == operation &&
                TryMatchCommand(fo.FilePath, filePath));

            if (!occurred)
            {
                var relevantOps = tracking.FileOperations
                    .Where(fo => fo.Operation == operation)
                    .Select(fo => fo.FilePath);

                Assert.Fail(
                    $"Expected {operation} operation on '{filePath}' but it did not occur.\n\n" +
                    $"Actual {operation} operations:\n  {string.Join("\n  ", relevantOps)}");
            }
        }

        /// <summary>
        /// Asserts that a package operation occurred.
        /// </summary>
        /// <param name="tracking">The fixture tracking instance.</param>
        /// <param name="operation">The operation type (e.g., "GetPackage", "InstallPackage").</param>
        /// <param name="packageName">The package name.</param>
        public static void AssertPackageOperation(this FixtureTracking tracking, string operation, string packageName)
        {
            bool occurred = tracking.PackageOperations.Any(po =>
                po.Operation == operation &&
                string.Equals(po.PackageName, packageName, StringComparison.OrdinalIgnoreCase));

            if (!occurred)
            {
                var relevantOps = tracking.PackageOperations
                    .Where(po => po.Operation == operation)
                    .Select(po => po.PackageName);

                Assert.Fail(
                    $"Expected {operation} operation on package '{packageName}' but it did not occur.\n\n" +
                    $"Actual {operation} operations:\n  {string.Join("\n  ", relevantOps)}");
            }
        }

        /// <summary>
        /// Tries to match a command against a pattern (supports regex).
        /// </summary>
        private static bool TryMatchCommand(string actualCommand, string expectedPattern)
        {
            try
            {
                return Regex.IsMatch(actualCommand, expectedPattern, RegexOptions.IgnoreCase);
            }
            catch
            {
                // If regex fails, try exact match
                return string.Equals(actualCommand, expectedPattern, StringComparison.OrdinalIgnoreCase);
            }
        }

        /// <summary>
        /// Formats a command mismatch for display.
        /// </summary>
        private static string FormatCommandMismatch(string[] expected, List<string> actual, int? highlightIndex = null)
        {
            var sb = new StringBuilder();
            
            sb.AppendLine("Expected commands:");
            for (int i = 0; i < expected.Length; i++)
            {
                string marker = (highlightIndex.HasValue && i == highlightIndex.Value) ? " >>> " : "     ";
                sb.AppendLine($"{marker}{i + 1}. {expected[i]}");
            }

            sb.AppendLine();

            sb.AppendLine("Actual commands executed:");
            for (int i = 0; i < actual.Count; i++)
            {
                string marker = (highlightIndex.HasValue && i == highlightIndex.Value) ? " >>> " : "     ";
                sb.AppendLine($"{marker}{i + 1}. {actual[i]}");
            }

            return sb.ToString();
        }
    }
}
