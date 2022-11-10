// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient
{
    using System;
    using System.Collections.Generic;
    using System.CommandLine;
    using System.CommandLine.Parsing;
    using System.Diagnostics.CodeAnalysis;
    using System.Linq;
    using System.Net;
    using VirtualClient.Contracts;

    /// <summary>
    /// Provides a factory for the creation of Command Options used by application command line operations.
    /// </summary>
    [SuppressMessage("StyleCop.CSharp.ReadabilityRules", "SA1118:Parameter should not span multiple lines", Justification = "Allow for longer description text.")]
    public static class OptionFactory
    {
        /// <summary>
        /// Command line option defines the duration/timeout for running the operation (e.g. workload execution timeout).
        /// </summary>
        /// <param name="required">Sets this option as required.</param>
        /// <param name="defaultValue">Sets the default value when none is provided.</param>
        public static Option CreateDurationOption(bool required = true, object defaultValue = null)
        {
            Option<TimeSpan> option = new Option<TimeSpan>(
                new string[] { "--duration", "--timeout", "--d" },
                new ParseArgument<TimeSpan>(arg =>
                {
                    string value = arg.Tokens.First().Value;
                    if (int.TryParse(value, out int minutes))
                    {
                        // The value is an integer representing minutes.
                        return TimeSpan.FromMinutes(minutes);
                    }
                    else
                    {
                        // The value is a timespan format: 01.00:00:00.
                        return TimeSpan.Parse(value);
                    }
                }))
            {
                Name = "Duration",
                Description = "Defines the duration/timeout for running the operation (e.g. workload execution timeout). " +
                    $"This may be a valid timespan (e.g. 01.00:00:00) or simple numeric value representing total minutes (e.g. 1440).",
                ArgumentHelpName = "timespan",
                AllowMultipleArgumentsPerToken = false
            };

            option.AddValidator(result =>
            {
                if (result.Tokens?.Any() == true)
                {
                    Token parsedResult = result.Tokens.First();
                    if (!int.TryParse(parsedResult.Value, out int intValue) && !TimeSpan.TryParse(parsedResult.Value, out TimeSpan timespanValue))
                    {
                        throw new ArgumentException(
                            $"Invalid command line usage. The duration/timeout parameter can be either a timespan or numeric value (e.g. 01.00:00:00).");
                    }
                }

                return string.Empty;
            });

            OptionFactory.SetOptionRequirements(option, required, defaultValue);

            return option;
        }

        /// <summary>
        /// An option to set IP address of a Virtual Client API to target/monitor.
        /// </summary>
        /// <param name="required">Sets this option as required.</param>
        /// <param name="defaultValue">Sets the default value when none is provided.</param>
        public static Option CreateIPAddressOption(bool required = true, object defaultValue = null)
        {
            Option<IPAddress> option = new Option<IPAddress>(
                new string[] { "--ipAddress", "--ipaddress", "--ip" },
                new ParseArgument<IPAddress>(arg => IPAddress.Parse(arg.Tokens.First().Value)))
            {
                Name = "IPAddress",
                Description = "The IP address of a remote/target application API instance to monitor.",
                ArgumentHelpName = "address",
                AllowMultipleArgumentsPerToken = false
            };

            option.AddValidator(new ValidateSymbol<OptionResult>(result =>
            {
                Token parsedResult = result.Tokens.First();
                if (!IPAddress.TryParse(parsedResult.Value, out IPAddress address))
                {
                    throw new ArgumentException(
                        $"Invalid command line usage. The IP address provided is not a valid format.");
                }

                return string.Empty;
            }));

            OptionFactory.SetOptionRequirements(option, required, defaultValue);

            return option;
        }

        /// <summary>
        /// Defines the port to host the REST API on.
        /// </summary>
        /// <param name="required">Sets this option as required.</param>
        public static Option CreatePortOption(bool required = false)
        {
            Option<int> option = new Option<int>(new string[] { "--port" })
            {
                Name = "Port",
                Description = "The port on which to host the REST API.",
                AllowMultipleArgumentsPerToken = false
            };

            OptionFactory.SetOptionRequirements(option, required);

            return option;
        }

        /// <summary>
        /// Defines the target server(s) for reverse proxy operations.
        /// </summary>
        /// <param name="required">Sets this option as required.</param>
        public static Option CreateApiServersOption(bool required = false)
        {
            Option<IEnumerable<string>> option = new Option<IEnumerable<string>>(
                new string[] { "--apiServers", "--s" },
                new ParseArgument<IEnumerable<string>>(arg => OptionFactory.ParseDelimitedValues(arg)))
            {
                Name = "ApiServers",
                Description = "The target server(s) to which to proxy requests when running in reverse proxy mode.",
                AllowMultipleArgumentsPerToken = true
            };

            OptionFactory.SetOptionRequirements(option, required);

            return option;
        }

        /// <summary>
        /// An option to display the current build version of the application.
        /// </summary>
        /// <param name="required">Sets this option as required.</param>
        public static Option CreateVersionOption(bool required = false)
        {
            Option<bool> option = new Option<bool>(new string[] { "--version" })
            {
                Name = "Version",
                Description = "The current build version of the application.",
                AllowMultipleArgumentsPerToken = false
            };

            OptionFactory.SetOptionRequirements(option, required);

            return option;
        }

        /// <summary>
        /// Command line option defines the type of workload to run (e.g. Default, ClientServer, ReverseProxyServer).
        /// </summary>
        /// <param name="required">Sets this option as required.</param>
        /// <param name="defaultValue">Sets the default value when none is provided.</param>
        public static Option CreateWorkloadOption(bool required = true, object defaultValue = null)
        {
            Option<string> option = new Option<string>(
                new string[] { "--workload", "--w" },
                new ParseArgument<string>(arg =>
                {
                    string workload = arg.Tokens.First().Value;
                    string workloadLc = arg.Tokens.First().Value.ToLowerInvariant();

                    switch (workloadLc)
                    {
                        case "default":
                            return RunWorkloadCommand.WorkloadDefault;

                        case "clientserver":
                            return RunWorkloadCommand.WorkloadClientServer;

                        default:
                            throw new ArgumentException(
                                $"The workload provided '{workload}' is not supported. Supported workloads include: Default, ClientServer.");
                    }
                }))
            {
                Name = "Workload",
                Description = "Defines the workload to run. Supported options are: Default, ClientServer.",
                ArgumentHelpName = "name",
                AllowMultipleArgumentsPerToken = false
            };

            OptionFactory.SetOptionRequirements(option, required, defaultValue);

            return option;
        }

        private static IList<string> ParseDelimitedValues(ArgumentResult parsedResult)
        {
            List<string> values = new List<string>();
            foreach (Token token in parsedResult.Tokens)
            {
                if (!string.IsNullOrWhiteSpace(token.Value))
                {
                    string[] delimitedValues = token.Value.Split(VirtualClientComponent.CommonDelimiters, StringSplitOptions.RemoveEmptyEntries);

                    if (delimitedValues?.Any() == true)
                    {
                        foreach (string value in delimitedValues)
                        {
                            values.Add(value.Trim());
                        }
                    }
                }
            }

            return values;
        }

        private static Option SetOptionRequirements(Option option, bool required = false, object defaultValue = null, ValidateSymbol<OptionResult> validator = null)
        {
            option.IsRequired = required;

            if (defaultValue != null)
            {
                option.SetDefaultValue(defaultValue);
            }

            if (validator != null)
            {
                option.AddValidator(validator);
            }

            return option;
        }
    }
}
