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
        private static readonly Regex OptionExpression = new Regex(@"^(-[a-z0-9]|--[a-z0-9-_]+)", RegexOptions.IgnoreCase);

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
        /// Throws an <see cref="ArgumentException"/> if there are unsupported options defined on
        /// in the arguments (command line) provided to the application.
        /// </summary>
        public static void WithOptionValidation(this Command command, string[] args)
        {
            // The System.CommandLine library does not do a good job of handling unsupported/unrecognized
            // options provided by the user on the command line. Effectively, the parser just handles them
            // even if they are NOT defined on the Command definition. This leads to confusing situations for users
            // where they might have simply misspelled the option.
            command.TreatUnmatchedTokensAsErrors = true;

            command.AddValidator(result =>
            {
                List<string> suppliedOptions = new List<string>();
                foreach (var arg in args)
                {
                    Match optionMatch = CommandLineExtensions.OptionExpression.Match(arg);
                    if (optionMatch.Success)
                    {
                        suppliedOptions.Add(optionMatch.Groups[1].Value);
                    }
                }

                if (suppliedOptions.Any())
                {
                    IEnumerable<string> supportedOptions = result.Command.Options
                        .SelectMany(opt => opt.Aliases)
                        .Where(alias => alias.StartsWith('-'));

                    IEnumerable<string> unsupportedOptions = suppliedOptions.Except(supportedOptions);
                    if (unsupportedOptions?.Any() == true)
                    {
                        throw new ArgumentException(
                            $"Invalid Usage. The following command line options are not supported: {string.Join(", ", unsupportedOptions)}. " +
                            $"Confirm the command line options used and that they are not simply misspelled.");
                    }
                }

                return string.Empty;
            });
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
