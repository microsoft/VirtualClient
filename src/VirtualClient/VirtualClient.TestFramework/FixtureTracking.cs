// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using VirtualClient.Common;

    /// <summary>
    /// Centralized tracking for mock fixture interactions during testing.
    /// Provides visibility into processes, file operations, and package operations
    /// executed during test runs.
    /// </summary>
    public class FixtureTracking
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="FixtureTracking"/> class.
        /// </summary>
        public FixtureTracking()
        {
            this.Processes = new List<IProcessProxy>();
            this.Commands = new List<CommandExecutionInfo>();
            this.FileOperations = new List<FileOperationInfo>();
            this.PackageOperations = new List<PackageOperationInfo>();
        }

        /// <summary>
        /// All processes created during the test execution.
        /// </summary>
        public List<IProcessProxy> Processes { get; }

        /// <summary>
        /// Detailed command execution information.
        /// </summary>
        public List<CommandExecutionInfo> Commands { get; }

        /// <summary>
        /// File system operations performed during the test.
        /// </summary>
        public List<FileOperationInfo> FileOperations { get; }

        /// <summary>
        /// Package manager operations performed during the test.
        /// </summary>
        public List<PackageOperationInfo> PackageOperations { get; }

        /// <summary>
        /// Clears all tracked data.
        /// </summary>
        public void Clear()
        {
            this.Processes.Clear();
            this.Commands.Clear();
            this.FileOperations.Clear();
            this.PackageOperations.Clear();
        }

        /// <summary>
        /// Returns a summary of tracked operations.
        /// </summary>
        public string GetSummary()
        {
            return $"Processes: {this.Processes.Count}, " +
                   $"Commands: {this.Commands.Count}, " +
                   $"FileOps: {this.FileOperations.Count}, " +
                   $"PackageOps: {this.PackageOperations.Count}";
        }

        /// <summary>
        /// Returns a detailed summary of all tracked operations for debugging.
        /// </summary>
        public string GetDetailedSummary()
        {
            var sb = new StringBuilder();
            sb.AppendLine("=== Fixture Tracking Summary ===");
            sb.AppendLine($"Total Processes: {this.Processes.Count}");
            sb.AppendLine($"Total Commands: {this.Commands.Count}");
            sb.AppendLine($"Total File Operations: {this.FileOperations.Count}");
            sb.AppendLine($"Total Package Operations: {this.PackageOperations.Count}");
            sb.AppendLine();

            if (this.Commands.Any())
            {
                sb.AppendLine("Commands Executed:");
                foreach (var cmd in this.Commands)
                {
                    sb.AppendLine($"  [{cmd.ExecutedAt:HH:mm:ss.fff}] {cmd.FullCommand}");
                    if (cmd.ExitCode.HasValue)
                    {
                        sb.AppendLine($"    Exit Code: {cmd.ExitCode}");
                    }

                    if (!string.IsNullOrEmpty(cmd.StandardOutput))
                    {
                        sb.AppendLine($"    Output: {cmd.StandardOutput.Substring(0, Math.Min(100, cmd.StandardOutput.Length))}...");
                    }
                }

                sb.AppendLine();
            }

            if (this.FileOperations.Any())
            {
                sb.AppendLine("File Operations:");
                foreach (var op in this.FileOperations.GroupBy(f => f.Operation))
                {
                    sb.AppendLine($"  {op.Key}: {op.Count()} operations");
                }

                sb.AppendLine();
            }

            if (this.PackageOperations.Any())
            {
                sb.AppendLine("Package Operations:");
                foreach (var op in this.PackageOperations)
                {
                    sb.AppendLine($"  [{op.OccurredAt:HH:mm:ss.fff}] {op.Operation}: {op.PackageName}");
                }
            }

            return sb.ToString();
        }
    }

    /// <summary>
    /// Detailed information about a command execution.
    /// </summary>
    public class CommandExecutionInfo
    {
        /// <summary>
        /// The full command including arguments.
        /// </summary>
        public string FullCommand { get; set; }

        /// <summary>
        /// The executable/command name.
        /// </summary>
        public string FileName { get; set; }

        /// <summary>
        /// The command line arguments.
        /// </summary>
        public string Arguments { get; set; }

        /// <summary>
        /// The working directory where the command was executed.
        /// </summary>
        public string WorkingDirectory { get; set; }

        /// <summary>
        /// The timestamp when the command was executed.
        /// </summary>
        public DateTime ExecutedAt { get; set; }

        /// <summary>
        /// The exit code of the process (if available).
        /// </summary>
        public int? ExitCode { get; set; }

        /// <summary>
        /// The standard output from the process.
        /// </summary>
        public string StandardOutput { get; set; }

        /// <summary>
        /// The standard error from the process.
        /// </summary>
        public string StandardError { get; set; }

        /// <summary>
        /// The process ID.
        /// </summary>
        public int ProcessId { get; set; }

        /// <summary>
        /// Returns a string representation of the command.
        /// </summary>
        public override string ToString() => this.FullCommand;
    }

    /// <summary>
    /// Information about a file operation.
    /// </summary>
    public class FileOperationInfo
    {
        /// <summary>
        /// The type of operation (Read, Write, Delete, Exists, etc.).
        /// </summary>
        public string Operation { get; set; }

        /// <summary>
        /// The file path involved in the operation.
        /// </summary>
        public string FilePath { get; set; }

        /// <summary>
        /// The timestamp when the operation occurred.
        /// </summary>
        public DateTime OccurredAt { get; set; }
    }

    /// <summary>
    /// Information about a package operation.
    /// </summary>
    public class PackageOperationInfo
    {
        /// <summary>
        /// The type of operation (Install, GetPackage, Register, etc.).
        /// </summary>
        public string Operation { get; set; }

        /// <summary>
        /// The name of the package.
        /// </summary>
        public string PackageName { get; set; }

        /// <summary>
        /// The path to the package.
        /// </summary>
        public string PackagePath { get; set; }

        /// <summary>
        /// The timestamp when the operation occurred.
        /// </summary>
        public DateTime OccurredAt { get; set; }
    }
}
