// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient
{
    using System;
    using System.CommandLine;
    using System.CommandLine.Builder;
    using System.CommandLine.Invocation;
    using System.CommandLine.Parsing;
    using System.Linq;
    using System.Reflection;
    using System.Threading.Tasks;

    /// <summary>
    /// Extension methods for <see cref="CommandLineBuilder"/> instances.
    /// </summary>
    internal static class CommandLineExtensions
    {
        /// <summary>
        /// Adds the default settings/configuration to the command line builder.
        /// </summary>
        /// <param name="builder">The command line builder to configure.</param>
        public static CommandLineBuilder WithDefaults(this CommandLineBuilder builder)
        {
            builder.ResponseFileHandling = ResponseFileHandling.ParseArgsAsLineSeparated;
            builder.EnablePosixBundling = true;

            builder.AddOption(OptionFactory.CreateVersionOption());
            builder.UseMiddleware(
                (InvocationContext context, Func<InvocationContext, Task> nextTask) =>
                {
                    return CommandLineExtensions.OutputVersionAsync(context, nextTask);
                });

            builder.UseDefaults();

            return builder;
        }

        /// <summary>
        /// Throws an <see cref="ArgumentException"/> if the parse results indicate an error in
        /// the processing of the command line arguments supplied.
        /// </summary>
        public static void ThrowOnUsageError(this ParseResult parseResults)
        {
            if (parseResults.Errors?.Any() == true)
            {
                Option versionOption = OptionFactory.CreateVersionOption();

                if (!parseResults.Tokens.Any(token => versionOption.Aliases.Contains(token.Value.ToLower())))
                {
                    throw new ArgumentException($"Invalid command line usage: {string.Join(" ", parseResults.Errors.Select(e => e.Message))}");
                }
            }
        }

        private static Task OutputVersionAsync(InvocationContext context, Func<InvocationContext, Task> nextTask)
        {
            Option versionOption = OptionFactory.CreateVersionOption();

            if (context.ParseResult.Tokens.Any(token => versionOption.Aliases.Contains(token.Value.ToLower())))
            {
                AssemblyName assemblyName = Assembly.GetExecutingAssembly().GetName();
                Version version = assemblyName.Version;
                string projectName = assemblyName.Name;
                Console.WriteLine($"{projectName} (v{version.Major}.{version.Minor}.{version.Build})");
                return Task.CompletedTask;
            }
            else
            {
                return nextTask(context);
            }
        }
    }
}
