// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Actions
{
    using System.Collections.Generic;
    using System.Linq;
    using VirtualClient.Common;
    using VirtualClient.Common.Extensions;

    /// <summary>
    /// Represents a single I/O workload process in execution.
    /// </summary>
    public class DiskWorkloadProcess
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DiskWorkloadProcess"/> class.
        /// </summary>
        public DiskWorkloadProcess(IProcessProxy process, string testedInstance, params string[] testFiles)
        {
            process.ThrowIfNull(nameof(process));
            testedInstance.ThrowIfNull(nameof(testedInstance));

            this.Process = process;
            this.Process.RedirectStandardError = true;
            this.Process.RedirectStandardOutput = true;
            this.Categorization = testedInstance;
            this.TestFiles = new List<string>();
            if (testFiles?.Any() == true)
            {
                (this.TestFiles as List<string>).AddRange(testFiles);
            }
        }

        /// <summary>
        /// The I/O workload command full path.
        /// </summary>
        public string Command
        {
            get
            {
                return this.Process.StartInfo.FileName;
            }
        }

        /// <summary>
        /// The I/O workload command line arguments.
        /// </summary>
        public string CommandArguments
        {
            get
            {
                return this.Process.StartInfo.Arguments;
            }
        }

        /// <summary>
        /// The I/O workload process standard error.
        /// </summary>
        public ConcurrentBuffer StandardError
        {
            get
            {
                return this.Process.StandardError;
            }
        }

        /// <summary>
        /// The I/O workload process standard output.
        /// </summary>
        public ConcurrentBuffer StandardOutput
        {
            get
            {
                return this.Process.StandardOutput;
            }
        }

        /// <summary>
        /// The I/O workload process in execution.
        /// </summary>
        public IProcessProxy Process { get; }

        /// <summary>
        /// The disk instance under test (e.g. remote_disk, remote_disk_premium_lrs).
        /// </summary>
        public string Categorization { get; set; }

        /// <summary>
        /// The test files associated with the I/O workload operations.
        /// </summary>
        public IEnumerable<string> TestFiles { get; }
    }
}
