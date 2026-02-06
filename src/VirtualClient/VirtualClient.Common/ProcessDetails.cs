// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;

namespace VirtualClient.Common
{
    /// <summary>
    /// Process details.
    /// </summary>
    public class ProcessDetails
    {
        /// <summary>
        /// Id of the process that ran.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Command line executed.
        /// </summary>
        public string CommandLine { get; set; }

        /// <summary>
        /// The amount of time elapsed between the process/operation start time
        /// and end time.
        /// </summary>
        public TimeSpan? ElapsedTime
        {
            get
            {
                TimeSpan? elapsedTime = null;
                if (this.StartTime != null && this.ExitTime != null)
                {
                    elapsedTime = this.ExitTime.Value - this.StartTime.Value;
                }

                return elapsedTime;
            }
        }

        /// <summary>
        /// The end time for the process or operation.
        /// </summary>
        public DateTime? ExitTime { get; set; }

        /// <summary>
        /// Exit code of the command executed.
        /// </summary>
        public int ExitCode { get; set; }

        /// <summary>
        /// Generated Results of the command.
        /// </summary>
        public IEnumerable<KeyValuePair<string, string>> Results { get; set; }

        /// <summary>
        /// Standard output of the command.
        /// </summary>
        public string StandardOutput { get; set; }

        /// <summary>
        /// Standard error of the command.
        /// </summary>
        public string StandardError { get; set; }

        /// <summary>
        /// The start time for the process or operation.
        /// </summary>
        public DateTime? StartTime { get; set; }

        /// <summary>
        /// Tool Name ran by command.
        /// </summary>
        public string ToolName { get; set; }

        /// <summary>
        /// Working Directory.
        /// </summary>
        public string WorkingDirectory { get; set; }

        /// <summary>
        /// Returns a clone of the current instance.
        /// </summary>
        /// <returns>
        /// A clone of the current instance.
        /// </returns>
        public virtual ProcessDetails Clone()
        {
            ProcessDetails clonedDetails = new ProcessDetails
            {
                Id = this.Id,
                CommandLine = this.CommandLine,
                ExitTime = this.ExitTime,
                ExitCode = this.ExitCode,
                StandardOutput = this.StandardOutput,
                StandardError = this.StandardError,
                StartTime = this.StartTime,
                ToolName = this.ToolName,
                WorkingDirectory = this.WorkingDirectory,
                Results = new List<KeyValuePair<string, string>>()
            };

            // Always create a new collection instance when Results is non-null.
            if (this.Results != null)
            {                
                clonedDetails.Results = this.Results.ToList();
            }

            return clonedDetails;
        }
    }
}