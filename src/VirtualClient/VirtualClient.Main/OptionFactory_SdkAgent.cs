// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient
{
    using System;
    using System.CommandLine;
    using System.CommandLine.Parsing;
    using System.IO;
    using System.Linq;
    using System.Text.RegularExpressions;
    using Microsoft.AspNetCore.Mvc;

    public partial class OptionFactory
    {
        // NOTE:
        // The following are options specific to SDK Agent command lines.

        /// <summary>
        /// Command line option defines an identifier used to group/correlate a set of experiments.
        /// </summary>
        /// <param name="required">Sets this option as required.</param>
        /// <param name="defaultValue">Sets the default value when none is provided.</param>
        public static Option CreateExperimentNameOption(bool required = true, object defaultValue = null)
        {
            Option<string> option = new Option<string>(new string[] { "--experiment-name" })
            {
                Name = "ExperimentName",
                Description = "An identifier used to group/correlate a set of experiments.",
                ArgumentHelpName = "name",
                AllowMultipleArgumentsPerToken = false
            };

            OptionFactory.SetOptionRequirements(option, required, defaultValue);

            return option;
        }

        /// <summary>
        /// Command line option defines the name of the folder within the default logs directory to 
        /// which log files should be written.
        /// </summary>
        /// <param name="required">Sets this option as required.</param>
        /// <param name="defaultValue">Sets the default value when none is provided.</param>
        public static Option CreateLogSubdirectoryOption(bool required = true, object defaultValue = null)
        {
            Option<string> option = new Option<string>(new string[] { "--log-dir" })
            {
                Name = "LogSubdirectory",
                Description = "Defines the name of the folder within the default logs directory to which log files should be written." +
                $"(e.g. folder1, folder1/folder2).",
                ArgumentHelpName = "path",
                AllowMultipleArgumentsPerToken = false
            };

            OptionFactory.SetOptionRequirements(option, required, defaultValue, new ValidateSymbol<OptionResult>(result =>
            {
                string path = result.Tokens?.FirstOrDefault().Value.Trim();
                if (!string.IsNullOrWhiteSpace(path))
                {
                    if (Path.IsPathRooted(path) || Regex.IsMatch(path, "^([/\\.]+|file:)", RegexOptions.IgnoreCase))
                    {
                        throw new ArgumentException(
                            "Invalid usage. The log directory value is not a valid subdirectory name. The log directory name must be specified in a form similar to the following: " +
                            "folder1, folder1/folder2, folder1/folder2/folder3 etc..");
                    }
                }

                return string.Empty;
            }));

            return option;
        }
    }
}
