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
    using System.Threading;
    using System.Threading.Tasks;
    using VirtualClient.Common.Extensions;

    /// <summary>
    /// Extension methods for <see cref="CommandLineBuilder"/> instances.
    /// </summary>
    public static class CommandLineExtensions
    {
        private const string HtmlQuote = "&quot;";
        private static readonly Regex OptionExpression = new Regex(@"^(-[a-z0-9]|--[a-z0-9-_]+)", RegexOptions.IgnoreCase);

        /// <summary>
        /// Parses the command line arguments.
        /// </summary>
        /// <param name="commandBuilder">Defines the supported command line options and subcommands.</param>
        /// <param name="args">The command line arguments provided to the application.</param>
        /// <param name="parsedTokens">Tokens parsed (and normalized) from the command line arguments.</param>
        /// <remarks>
        /// The System.CommandLine library has quirks that causes any \" escaping characters to be removed
        /// from the end of any argument. This method handles this quirk to ensure users can reliably use escaping on
        /// the command line where required (e.g. --command="pwsh Invoke-FirmwareUpdate -Image \"/path/to/image.bin\" -LogDirectory \"/path/to/logs\"")
        /// </remarks>
        /// <returns>The result of the command line parsing.</returns>
        public static ParseResult ParseArguments(this CommandLineBuilder commandBuilder, string[] args, out IEnumerable<Token> parsedTokens)
        {
            parsedTokens = null;
            string[] preprocessedArgs = CommandLineExtensions.PreprocessArguments(args);
            ParseResult parseResult = commandBuilder.Build().Parse(preprocessedArgs);
            parseResult.ThrowOnUsageError();
            parsedTokens = CommandLineExtensions.PostprocessTokens(parseResult.Tokens);

            return parseResult;
        }

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

        private static IEnumerable<Token> PostprocessTokens(IEnumerable<Token> commandLineTokens)
        {
            // System.CommandLine Quirk:
            // The library parsing logic will strip the \" from the end of the command line
            // vs. treating it as an explicit quotation mark to leave in place. There are no
            // hooks in the library implementation to override this behavior.
            //
            // To workaround this we replace the quotes with the HTML encoding. Each option can
            // then handle the HTML decoding as required.

            List<Token> normalizedTokens = new List<Token>();
            foreach (Token token in commandLineTokens)
            {
                string value = token.Value;
                if (!string.IsNullOrWhiteSpace(value))
                {
                    if (value.Contains("&quot;"))
                    {
                        value = value.Replace("&quot;", "\"");
                    }
                }

                normalizedTokens.Add(new Token(value, token.Type));
            }

            return normalizedTokens;
        }

        private static string[] PreprocessArguments(params string[] args)
        {
            string[] preprocessedArgs = new string[args.Length];
            for (int i = 0; i < args.Length; i++)
            {
                if (args[i].Contains("\""))
                {
                    // System.CommandLine Quirk:
                    // The library parsing logic will strip the \" from the end of the command line
                    // vs. treating it as an explicit quotation mark to leave in place. There are no
                    // hooks in the library implementation to override this behavior.
                    //
                    // To workaround this we replace the quotes with the HTML encoding. Each option can
                    // then handle the HTML decoding as required.
                    preprocessedArgs[i] = args[i].Replace("\"", "&quot;");
                    continue;
                }

                preprocessedArgs[i] = args[i];
            }

            return preprocessedArgs;
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
