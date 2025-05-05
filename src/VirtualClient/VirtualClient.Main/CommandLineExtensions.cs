// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient
{
    using System;
    using System.Collections.Generic;
    using System.CommandLine;
    using System.CommandLine.Builder;
    using System.CommandLine.Invocation;
    using System.CommandLine.Parsing;
    using System.Linq;
    using System.Text.RegularExpressions;
    using System.Threading.Tasks;
    using VirtualClient.Common.Extensions;

    /// <summary>
    /// Extension methods for <see cref="CommandLineBuilder"/> instances.
    /// </summary>
    public static class CommandLineExtensions
    {
        /// <summary>
        /// Adds the default settings/configuration to the command line builder.
        /// </summary>
        /// <param name="builder">The command line builder to configure.</param>
        public static CommandLineBuilder WithDefaults(this CommandLineBuilder builder)
        {
            builder.ThrowIfNull(nameof(builder));
            builder.ResponseFileHandling = ResponseFileHandling.ParseArgsAsLineSeparated;
            builder.EnablePosixBundling = true;
            
            builder.AddOption(OptionFactory.CreateVersionOption());
            builder.UseMiddleware((InvocationContext context, Func<InvocationContext, Task> nextTask) =>
            {
                return CommandLineExtensions.OutputVersionAsync(context, nextTask);
            });

            builder.UseDefaults();
            builder.UseHelpBuilder((context) => new CommandHelpBuilder(context.Console));

            return builder;
        }

        /// <summary>
        /// Throws an <see cref="ArgumentException"/> if the parse results indicate an error in
        /// the processing of the command line arguments supplied.
        /// </summary>
        public static void ThrowOnUsageError(this ParseResult parseResults)
        {
            parseResults.ThrowIfNull(nameof(parseResults));

            if (!CommandLineExtensions.ContainsHelpFlag(parseResults) && !CommandLineExtensions.ContainsVersionFlag(parseResults))
            {
                // Scenario 1:
                // General parsing errors for the command line options.
                if (parseResults.Errors?.Any() == true)
                {
                    throw new ArgumentException($"Invalid Usage. {string.Join(" ", parseResults.Errors.Select(e => e.Message))}");
                }

                // The System.CommandLine library does not do a good job of handling unsupported/unrecognized
                // options provided by the user on the command line. Effectively, the parser just handles them
                // even if they are NOT defined on the Command definition. This leads to confusing situations for users
                // where they might have simply misspelled the option.
                //
                // Deeper Context:
                // When the System.CommandLine library encounters an option on the command line that it does not recognize,
                // it parses the option as a positional Argument vs. as an Option.
                //
                // e.g.
                // Option = --profile
                // Argument = PERF-CPU-OPENSSL.json
                // Option = --timeout
                // Argument = 01:00:00
                // Argument = --unrecognized-option
                // Argument = Value for the unrecognized option
                List<string> unsupportedOptions = new List<string>();

                if (parseResults.Tokens?.Any() == true)
                {
                    // Scenario 2:
                    // There are no options supported on the command line at all.
                    ICommand command = parseResults.CommandResult.Command;
                    IReadOnlyList<IOption> supportedOptions = command.Options;
                    if (supportedOptions?.Any() != true)
                    {
                        IEnumerable<Token> userCommandLineOptions = parseResults.Tokens.Where(t => t.Type == TokenType.Option);
                        if (userCommandLineOptions?.Any() == true)
                        {

                            unsupportedOptions.AddRange(userCommandLineOptions.Select(o => o.Value));
                        }
                    }

                    // Scenario 3:
                    // There are no options on the command line that are not valid for the command.
                    IEnumerable<Token> arguments = parseResults.Tokens.Where(t => t.Type == TokenType.Argument);
                    if (arguments?.Any() == true)
                    {
                        Regex optionMatchExpression = new Regex("--[a-z]+", RegexOptions.IgnoreCase);
                        foreach (Token argument in arguments)
                        {
                            if (optionMatchExpression.IsMatch(argument.Value?.Trim()))
                            {
                                unsupportedOptions.Add(argument.Value);
                            }
                        }
                    }

                    if (unsupportedOptions.Any())
                    {
                        throw new ArgumentException(
                            $"Invalid Usage. The following command line options are not supported: {string.Join(", ", unsupportedOptions)}. " +
                            $"Confirm the options are supported and that they are not simply misspelled.");
                    }
                }
            }
        }

        private static bool ContainsHelpFlag(ParseResult parseResults)
        {
            bool containsHelpFlag = false;
            List<string> helpFlags = new List<string>
            {
                "/?",
                "-h",
                "--help"
            };

            if (parseResults?.Tokens?.Where(t => t.Type == TokenType.Option)?.Any(token => helpFlags.Contains(token.Value?.Trim())) == true)
            {
                containsHelpFlag = true;
            }

            return containsHelpFlag;
        }

        private static bool ContainsVersionFlag(ParseResult parseResults)
        {
            bool containsVersionFlag = false;
            Option versionOption = OptionFactory.CreateVersionOption();
            if (parseResults?.Tokens?.Any(token => versionOption.Aliases.Contains(token.Value.ToLower())) == true)
            {
                containsVersionFlag = true;
            }

            return containsVersionFlag;
        }

        private static void StandardizeForSingleQuoteSupport(List<string> args)
        {
            Regex singleQuoteExpression = new Regex(@"\s*'(.*?)'\s*", RegexOptions.IgnoreCase);
            Regex doubleQuoteExpression = new Regex("\"");

            List<string> standardizedArguments = new List<string>();

            foreach (string arg in args)
            {
                Match singleQuotedArgument = singleQuoteExpression.Match(arg);
                if (singleQuotedArgument.Success)
                {
                    string normalizedArgument = singleQuotedArgument.Groups[1].Value;
                    
                    // We have to address any internal double-quotes in the argument to
                    // avoid parsing errors. We just escape them.
                    //
                    // e.g. 
                    {
                        
                    }
                    standardizedArguments.Add($"\"{singleQuotedArgument.Groups[1].Value}\"");
                }
            }
        }

        private static Task OutputVersionAsync(InvocationContext context, Func<InvocationContext, Task> nextTask)
        {
            context.ThrowIfNull(nameof(context));
            nextTask.ThrowIfNull(nameof(nextTask));

            Option versionOption = OptionFactory.CreateVersionOption();

            if (context.ParseResult.Tokens.Any(token => versionOption.Aliases.Contains(token.Value.ToLower())))
            {
                Console.WriteLine(CommandHelpBuilder.GetVersionInfo());
                return Task.CompletedTask;
            }
            else
            {
                return nextTask(context);
            }
        }
    }
}
