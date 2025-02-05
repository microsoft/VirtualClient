// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient
{
    using System;
    using System.Collections.Generic;
    using System.CommandLine;
    using System.CommandLine.Help;
    using System.Linq;
    using System.Reflection;
    using VirtualClient.Common.Extensions;

    /// <summary>
    /// Builds help/usage information to display in console standard output.
    /// </summary>
    public class CommandHelpBuilder : IHelpBuilder
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CommandHelpBuilder"/> class.
        /// </summary>
        /// <param name="console">Provides access to the console/terminal display.</param>
        public CommandHelpBuilder(IConsole console)
        {
            console.ThrowIfNull(nameof(console));
            this.Console = console;
        }

        /// <summary>
        /// The console interface used to write text out to standard output.
        /// </summary>
        protected IConsole Console { get; }

        /// <summary>
        /// Returns text containing the version information for the application
        /// (e.g. VirtualClient (v1.0.0).
        /// </summary>
        /// <returns></returns>
        public static string GetVersionInfo()
        {
            AssemblyName assemblyName = Assembly.GetExecutingAssembly().GetName();
            Version version = assemblyName.Version;
            string projectName = assemblyName.Name;

            return $"{projectName} (v{version.Major}.{version.Minor}.{version.Build}.{version.Revision})";
        }

        /// <summary>
        /// Writes the usage/help content to standard output on the console.
        /// </summary>
        /// <param name="command">Provides the command details, options and description information.</param>
        public void Write(ICommand command)
        {
            this.WriteLine();
            this.WriteVersion(command);
            this.WriteLine();
            this.WriteLine();
            this.WriteSynopsis(command);
            this.WriteLine(2);
            this.WriteUsage(command);
            this.WriteLine(2);

            if (command.Options?.Any() == true)
            {
                this.WriteOptions(command.Options);
            }

            RootCommand defaultCommand = command as RootCommand;
            if (defaultCommand != null && defaultCommand.Children?.Any() == true)
            {
                this.WriteLine(2);
                this.WriteSubCommands(command);
            }

            this.WriteLine(2);
        }

        /// <summary>
        /// Writes the sub-commands for the command to console standard output.
        /// </summary>
        /// <param name="command">Describes the command details.</param>
        protected virtual void WriteSubCommands(ICommand command)
        {
            RootCommand defaultCommand = command as RootCommand;
            if (defaultCommand.Children?.Any() == true)
            {
                IEnumerable<ISymbol> childCommands = defaultCommand.Children.Where(child => child is Command);
                if (childCommands?.Any() == true)
                {
                    this.Write($"[SubCommands]:");

                    foreach (ISymbol childCommand in childCommands.OrderBy(cmd => cmd.Name))
                    {
                        this.WriteLine();
                        this.Write($"  {childCommand.Name}: {childCommand.Description}");
                    }
                }
            }
        }

        /// <summary>
        /// Writes a new line/line break to the console standard output.
        /// </summary>
        protected void WriteLine(int count = 1)
        {
            for (int lineCount = 0; lineCount < count; lineCount++)
            {
                this.Console.Out.Write(Environment.NewLine);
            }
        }

        /// <summary>
        /// Writes the option descriptions to console standard output.
        /// </summary>
        /// <param name="options">The set of options available on the command line.</param>
        protected virtual void WriteOptions(IEnumerable<IOption> options)
        {
            this.Write("[Options]:");
            this.WriteLine();

            // Remove version and help options. They will be added in at the end.
            IEnumerable<IOption> versionAndHelp = options.Where(opt => opt.Name == "Version" || opt.Name == "help");
            IEnumerable<IOption> otherOptions = options.Except(versionAndHelp);

            List<Tuple<string, string>> optionDescriptions = new List<Tuple<string, string>>();
            IEnumerable<IOption> requiredOptions = otherOptions.Where(opt => opt.IsRequired);

            foreach (IOption option in requiredOptions.OrderBy(opt => opt.Name))
            {
                CommandHelpBuilder.AddOptionDescription(optionDescriptions, option);
            }

            IEnumerable<IOption> optionalOptions = otherOptions.Where(opt => !opt.IsRequired);
            foreach (IOption option in optionalOptions.OrderBy(opt => opt.Name))
            {
                CommandHelpBuilder.AddOptionDescription(optionDescriptions, option);
            }

            int column1Width = optionDescriptions.Select(opt => opt.Item1).Max(desc => desc.Length);

            foreach (Tuple<string, string> description in optionDescriptions.Take(optionDescriptions.Count - 1))
            {
                this.WriteOptionDescription(description.Item1, description.Item2, column1Width);
                this.WriteLine(2);
            }

            this.WriteOptionDescription(optionDescriptions.Last().Item1, optionDescriptions.Last().Item2, column1Width);

            // Add back in the version option.
            IOption versionOption = versionAndHelp.FirstOrDefault(opt => opt.Name == "Version");
            if (versionOption != null)
            {
                this.WriteLine(2);
                optionDescriptions.Clear();
                CommandHelpBuilder.AddOptionDescription(optionDescriptions, versionOption);
                this.WriteOptionDescription(optionDescriptions[0].Item1, optionDescriptions[0].Item2, column1Width);
            }

            // Add back in the help option.
            IOption helpOption = versionAndHelp.FirstOrDefault(opt => opt.Name == "help");
            if (helpOption != null)
            {
                this.WriteLine(2);
                optionDescriptions.Clear();
                CommandHelpBuilder.AddOptionDescription(optionDescriptions, helpOption);
                this.WriteOptionDescription(optionDescriptions[0].Item1, optionDescriptions[0].Item2, column1Width);
            }
        }

        /// <summary>
        /// Writes the usage synopsis header to console standard output.
        /// </summary>
        /// <param name="command">Describes the command details.</param>
        protected virtual void WriteSynopsis(ICommand command)
        {
            this.Write($"[Description]:");
            this.WriteLine();
            this.Write($"  {command.Description}");
        }

        /// <summary>
        /// Writes the help usage to console standard output.
        /// </summary>
        /// <param name="command">Describes the command details.</param>
        protected virtual void WriteUsage(ICommand command)
        {
            AssemblyName assemblyName = Assembly.GetExecutingAssembly().GetName();

            this.Write($"[Usage]:");
            this.WriteLine();

            if (command is RootCommand)
            {
                this.Console.Out.Write($"  {assemblyName.Name}.exe [options]");
            }
            else
            {
                this.Console.Out.Write($"  {assemblyName.Name}.exe {command.Name} [options]");
            }
        }

        /// <summary>
        /// Writes version information to the console standard output.
        /// </summary>
        /// <param name="command">Describes the command details.</param>
        protected virtual void WriteVersion(ICommand command)
        {
            this.Console.Out.Write(CommandHelpBuilder.GetVersionInfo());
        }

        private static void AddOptionDescription(List<Tuple<string, string>> optionDescriptions, IOption option)
        {
            string argumentHelpName = (option as Option)?.ArgumentHelpName;

            // We ONLY display up to 3 of the aliases.
            string orderedAliases = string.Join(",", CommandHelpBuilder.GetDistinctAliases(option).Where(alias => alias.Contains("-"))
                .Take(3));

            bool isFlag = string.Equals(argumentHelpName, "Flag", StringComparison.OrdinalIgnoreCase);

            if (option.IsRequired && !isFlag)
            {
                optionDescriptions.Add(new Tuple<string, string>($"{orderedAliases} <{argumentHelpName}>", $"{option.Name}: {option.Description}"));
            }
            else if (option.IsRequired && isFlag)
            {
                optionDescriptions.Add(new Tuple<string, string>($"{orderedAliases}", $"{option.Name}: {option.Description}"));
            }
            else if (!option.IsRequired && !isFlag)
            {
                optionDescriptions.Add(new Tuple<string, string>($"[{orderedAliases} <{argumentHelpName}>]", $"{option.Name}: {option.Description}"));
            }
            else if (!option.IsRequired && isFlag)
            {
                optionDescriptions.Add(new Tuple<string, string>($"[{orderedAliases}]", $"{option.Name}: {option.Description}"));
            }
        }

        private static IEnumerable<string> GetDescriptionLines(string description, int maxWidth = 150)
        {
            List<string> lines = new List<string>();
            string[] words = description.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            string currentLine = words[0];

            for (int i = 1; i < words.Length; i++)
            {
                if (currentLine.Length + words[i].Length + 1 > maxWidth)
                {
                    lines.Add(currentLine);
                    currentLine = words[i];
                    continue;
                }

                currentLine += $" {words[i]}";
                if (i == words.Length - 1)
                {
                    lines.Add(currentLine);
                }
            }

            return lines;
        }

        private static IEnumerable<string> GetDistinctAliases(IOption option)
        {
            HashSet<string> distinctAliases = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (string alias in option.Aliases)
            {
                distinctAliases.Add(alias);
            }

            return distinctAliases;
        }

        private void Write(string output)
        {
            this.Console.Out.Write(output);
        }

        private void WriteOptionDescription(string parameters, string description, int column1Width)
        {
            string parameterDescription = string.Format($"  {{0,-{column1Width}}}", parameters);

            int column2Width = System.Console.BufferWidth - column1Width - 10;
            IEnumerable<string> lines = CommandHelpBuilder.GetDescriptionLines(description, column2Width);
            this.Write($"{parameterDescription} {lines.First()}");

            if (lines.Count() > 1)
            {
                foreach (string line in lines.Skip(1))
                {
                    this.WriteLine();
                    this.Write(string.Format($"{{0,-{column1Width + 3}}}{{1}}", string.Empty, line));
                }
            }
        }
    }
}
