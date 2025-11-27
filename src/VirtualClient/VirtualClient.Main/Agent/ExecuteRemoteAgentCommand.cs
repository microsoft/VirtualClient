// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Agent
{
    using System;
    using System.Collections.Generic;
    using System.CommandLine;
    using System.CommandLine.Parsing;
    using System.Linq;
    using System.Text.RegularExpressions;
    using VirtualClient.Contracts;

    /// <summary>
    /// Command runs the full SDK Agent command line on a target system.
    /// </summary>
    internal class ExecuteRemoteAgentCommand : ExecuteProfileCommand
    {
        private const string ExecuteOnRemoteProfile = "EXECUTE-ON-REMOTE.json";

        /// <summary>
        /// Initializes the command state before execution.
        /// </summary>
        protected override void Initialize(string[] args, PlatformSpecifics platformSpecifics)
        {
            if (this.Profiles?.Any() != true && string.IsNullOrWhiteSpace(this.Command))
            {
                throw new NotSupportedException("Command line usage is not supported. The intended command or profile execution intentions are unclear.");
            }

            this.Profiles = new List<DependencyProfileReference>
            {
                new DependencyProfileReference(ExecuteRemoteAgentCommand.ExecuteOnRemoteProfile)
            };

            this.Iterations = ProfileTiming.OneIteration();

            // To avoid confusing situations, remote command execution DOES NOT support
            // the following features on the controller/local system. The options (if defined) however
            // will be passed to the target system:
            // - Dependency installation on the controller.
            // - Targeting specific scenarios (e.g. --scenarios=Scenario01).
            this.InstallDependencies = false;
            this.Scenarios = null;

            string agentCommand = this.GetTargetCommandArguments(Program.CommandLineTokens);

            this.Parameters = new Dictionary<string, IConvertible>(StringComparer.OrdinalIgnoreCase)
            {
                { nameof(this.Command), agentCommand }
            };
        }

        /// <summary>
        /// Returns the command line arguments to execute on the target/remote system.
        /// </summary>
        /// <param name="commandLineTokens">Tokens parsed from the command line provided by the user.</param>
        protected string GetTargetCommandArguments(IEnumerable<Token> commandLineTokens)
        {
            List<string> targetCommandArguments = new List<string>();
            Option targetOption = OptionFactory.CreateTargetAgentOption();
            Option commandOption = OptionFactory.CreateCommandOption();
            Regex pathExpression = new Regex(@"[\s\/]+", RegexOptions.IgnoreCase);

            IEnumerable<string> optionsAlwaysExluded = new HashSet<string>(targetOption.Aliases);
            IEnumerable<string> optionsAlwaysQuoted = new HashSet<string>(commandOption.Aliases);
            IEnumerable<Token> commandTokens = commandLineTokens.Where(t => t.Type == TokenType.Option || t.Type == TokenType.Argument);

            for (int t = 0; t < commandTokens.Count() - 1; t++)
            {
                Token token = commandTokens.ElementAt(t);


                if (token.Type == TokenType.Option && !optionsAlwaysExluded.Contains(token.Value))
                {
                    // Exclude target agent (--target=) options from the command line sent to the
                    // agent system. Command definitions (e.g. --command) are always surrounded in quotes.
                    Token nextToken = commandTokens.ElementAt(t + 1);
                    if ((nextToken.Type == TokenType.Argument && optionsAlwaysQuoted.Contains(token.Value)) || pathExpression.IsMatch(nextToken.Value))
                    {
                        targetCommandArguments.Add($"{token.Value}=\"{nextToken.Value}\"");
                    }
                    else if (nextToken.Type == TokenType.Argument)
                    {
                        targetCommandArguments.Add($"{token.Value}={nextToken.Value}");
                    }
                    else
                    {
                        targetCommandArguments.Add(token.Value);
                    }
                }
            }

            Token lastToken = commandTokens.Last();
            if (lastToken.Type == TokenType.Option)
            {
                targetCommandArguments.Add(lastToken.Value);
            }

            return string.Join(' ', targetCommandArguments);
        }

        /// <summary>
        /// Returns the command line arguments to execute on the target/remote system.
        /// </summary>
        /// <param name="commandArguments">The original/full command line arguments.</param>
        protected string GetTargetCommandArguments(string[] commandArguments)
        {
            List<string> targetCommandArguments = new List<string>();
            Option targetOption = OptionFactory.CreateTargetAgentOption();
            Option commandOption = OptionFactory.CreateCommandOption();

            // We use the regex to match usages of the --command option that include an equal (=) sign
            // vs. a space (e.g. --command=/any/command.sh vs. --command /any/command.sh). The .NET command
            // line parsing treats them differently in the arguments array.
            // Regex commandOptionExpression = new Regex($"^{string.Join('|', OptionFactory.GetAliases(commandOption))}=");
            // Regex targetOptionExpression = new Regex($"^{string.Join('|', OptionFactory.GetAliases(targetOption))}=");
            // Regex flagExpression = new Regex("^(-[a-z0-9]+)=([\x20-\x7E]+)", RegexOptions.IgnoreCase);

            Regex optionExpression = new Regex("^(-{1,2}[a-z0-9_-]+)=*([\x20-\x7E]+)*", RegexOptions.IgnoreCase);
            Regex pathExpression = new Regex(@"[\s\/]+", RegexOptions.IgnoreCase);

            // Skip the first argument, the subcommand 'remote'
            for (int i = 1; i < commandArguments.Length; i++)
            {
                string effectiveArgument = commandArguments[i].Trim();
                if (effectiveArgument.StartsWith("@"))
                {
                    // Do not add response files. They are not supported for the controller during
                    // remote execution as this would require the same response file to exist on the
                    // target system. Additionally, the options within the response file could be confusing
                    // (e.g. --target={ssh_target}).
                    continue;
                }

                

                if (OptionFactory.ContainsOption(targetOption, effectiveArgument))
                {
                    Match targetMatch = optionExpression.Match(effectiveArgument);
                    if (targetMatch.Success && targetMatch.Groups.Count == 3)
                    {
                        // target contains a space vs. an equal sign. The .NET command line
                        // parsing will separate these into individual entries in the array. We need
                        // to skip both indexes in the array.
                        //
                        // e.g.
                        // --target "any@10.3.4.5;pwd"
                        i += 2;
                    }

                    continue;
                }

                ////if (OptionFactory.ContainsOption(commandOption, effectiveArgument))
                ////{
                ////    Match commandMatch = optionExpression.Match(effectiveArgument);

                ////    if (commandMatch.Success && commandMatch.Groups.Count < 3)
                ////    {
                ////        string option = commandMatch.Groups[1].Value;
                ////        string argument = null;

                ////        if (commandMatch.Groups.Count == 3)
                ////        {
                ////            argument = optionMatch.Groups[2].Value;
                ////        }

                ////        // command contains a space vs. an equal sign. The .NET command line
                ////        // parsing will separate these into individual entries in the array. We need
                ////        // to skip both indexes in the array.
                ////        //
                ////        // e.g.
                ////        // --command /any/command.sh
                ////        targetCommandArguments.Add($"{commandOption.Aliases.Where(a => a.StartsWith("-")).Last()}=\"{argument}\"");
                ////    }

                ////    continue;
                ////}

                Match optionMatch = optionExpression.Match(effectiveArgument);
                if (optionMatch.Success)
                {
                    string option = optionMatch.Groups[1].Value;
                    string argument = null;

                    if (optionMatch.Groups.Count == 3)
                    {
                        argument = optionMatch.Groups[2].Value;
                    }

                    if (argument != null && pathExpression.IsMatch(argument))
                    {
                        // If any arguments contain spaces, URI characters or path delimiters
                        // we surround in quotes.
                        targetCommandArguments.Add($"{option}=\"{argument}\"");
                    }
                    else
                    {
                        targetCommandArguments.Add($"{option}={argument}");
                    }
                }
                else if (pathExpression.IsMatch(effectiveArgument))
                {
                    targetCommandArguments.Add($"\"{effectiveArgument}\"");
                }
                else
                {
                    targetCommandArguments.Add(effectiveArgument);
                }

                ////if (OptionFactory.ContainsOption(commandOption, effectiveArgument))
                ////{
                ////    if (!optionExpression.IsMatch(effectiveArgument))
                ////    {
                ////        // target contains a space vs. an equal sign. The .NET command line
                ////        // parsing will separate these into individual entries in the array. We need
                ////        // to skip both indexes in the array.
                ////        //
                ////        // e.g.
                ////        // --target "any@10.3.4.5;pwd"
                ////        targetCommandArguments.Add($"{option}=\"{argument}\"");
                ////        i += 2;
                ////    }
                ////    else
                ////    {
                ////    }

                ////    continue;
                ////}
            }

            return string.Join(" ", targetCommandArguments);
        }
    }
}