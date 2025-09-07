// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient
{
    using System;
    using System.Collections.Generic;
    using System.CommandLine;
    using System.IO;
    using System.IO.Abstractions;
    using System.Linq;
    using System.Reflection;
    using System.Text.RegularExpressions;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Hosting;
    using Microsoft.Extensions.Logging;
    using VirtualClient.Common;
    using VirtualClient.Common.Extensions;
    using VirtualClient.Common.Telemetry;
    using VirtualClient.Contracts;
    using VirtualClient.Logging;

    /// <summary>
    /// Command runs the Virtual Client as a controller targeting N-number of target
    /// agent systems through SSH connections.
    /// </summary>
    internal class ExecuteControllerCommand : CommandBase
    {
        private static readonly Option AgentOption = OptionFactory.CreateAgentOption();
        private static readonly Option ExperimentIdOption = OptionFactory.CreateExperimentIdOption();
        private static readonly Option IsolatedOption = OptionFactory.CreateIsolatedFlag();
        private static readonly Option LogDirectoryOption = OptionFactory.CreateLogDirectoryOption();
        private static readonly Option SshOption = OptionFactory.CreateSshOption();
        private static readonly Option StateDirectoryOption = OptionFactory.CreateStateDirectoryOption();
        private static readonly Option TempDirectoryOption = OptionFactory.CreateTempDirectoryOption();
        private static readonly Regex AgentExpression = new Regex(string.Join("|", AgentOption.Aliases.Select(a => $"{a}=")));
        private static readonly Regex ExperimentIdExpression = new Regex(string.Join("|", ExperimentIdOption.Aliases.Select(a => $"{a}=")));
        private static readonly Regex IsolatedExpression = new Regex(string.Join("|", IsolatedOption.Aliases));
        private static readonly Regex LogDirectoryExpression = new Regex(string.Join("|", LogDirectoryOption.Aliases.Select(a => $"{a}=")));
        private static readonly Regex StateDirectoryExpression = new Regex(string.Join("|", StateDirectoryOption.Aliases.Select(a => $"{a}=")));
        private static readonly Regex TempDirectoryExpression = new Regex(string.Join("|", TempDirectoryOption.Aliases.Select(a => $"{a}=")));

        /// <summary>
        /// A command line to execute independently of a profile.
        /// </summary>
        public string Command { get; set; }

        /// <summary>
        /// The target agent SSH connections to use for establishing a session in which to execute
        /// agent operations (e.g. anyuser@192.168.1.15;pass_w_@rd).
        /// </summary>
        public IEnumerable<string> TargetAgents { get; set; }

        /// <summary>
        /// Executes the profile operations.
        /// </summary>
        /// <param name="args">The arguments provided to the application on the command line.</param>
        /// <param name="cancellationTokenSource">Provides a token that can be used to cancel the command operations.</param>
        /// <returns>The exit code for the command operations.</returns>
        public override async Task<int> ExecuteAsync(string[] args, CancellationTokenSource cancellationTokenSource)
        {
            int exitCode = 0;
            ILogger logger = null;
            IPackageManager packageManager = null;
            ProcessManager processManager = null;
            ISystemManagement systemManagement = null;
            IServiceCollection dependencies = null;
            CancellationToken cancellationToken = cancellationTokenSource.Token;

            try
            {
                this.Isolated = true;
                this.SetGlobalTelemetryProperties(args);

                // Setup any dependencies required to execute the workload profile.
                dependencies = this.InitializeDependencies(args);
                logger = dependencies.GetService<ILogger>();
                packageManager = dependencies.GetService<IPackageManager>();
                processManager = dependencies.GetService<ProcessManager>();
                systemManagement = dependencies.GetService<ISystemManagement>();

                EventContext telemetryContext = EventContext.Persisted();

                // Extracts and registers any packages that are pre-existing on the system (e.g. they exist in
                // the 'packages' directory already).
                await this.InitializePackagesAsync(packageManager, cancellationToken);

                using (SemaphoreSlim controlFlowLock = new SemaphoreSlim(1, 1))
                {
                    string baseCommandLine = this.GetBaseCommandArguments(args);

                    List<Task> vcExecutions = new List<Task>();
                    foreach (string targetAgent in this.TargetAgents)
                    {
                        if (SshClientProxy.TryGetSshTargetInformation(targetAgent, out string host, out string username, out string password))
                        {
                            string vcExecutable = VirtualClientRuntime.ExecutableName;
                            string vcCommandArguments = this.GetTargetAgentCommandArguments(baseCommandLine, targetAgent, host, systemManagement.PlatformSpecifics);

                            Console.WriteLine($"Execute on Agent: {host}");

                            Task targetAgentExecution = Task.Run(async () =>
                            {
                                using (IProcessProxy vcProcess = processManager.CreateProcess(vcExecutable, vcCommandArguments, Environment.CurrentDirectory))
                                {
                                    VirtualClientRuntime.CleanupTasks.Add(new Action_(() => vcProcess.SafeKill(Program.Logger, TimeSpan.FromSeconds(30))));

                                    await vcProcess.StartAndWaitAsync(cancellationToken);
                                    await controlFlowLock.WaitAsync();

                                    try
                                    {
                                        Console.WriteLine();
                                        Console.WriteLine($"Agent Results: {host}");
                                        Console.WriteLine($"*************************************************************************");
                                        Console.WriteLine($"{vcProcess.StandardOutput?.ToString()}{Environment.NewLine}{Environment.NewLine}{vcProcess.StandardError?.ToString()}".Trim());
                                    }
                                    finally
                                    {
                                        controlFlowLock.Release();
                                    }
                                }
                            });

                            vcExecutions.Add(targetAgentExecution);
                        }
                    }

                    await Task.WhenAll(vcExecutions);
                }
            }
            catch (OperationCanceledException)
            {
                // Expected when the Ctrl-C is pressed to cancel operation.
            }
            catch (NotSupportedException exc)
            {
                Program.LogErrorMessage(logger, exc, EventContext.Persisted());
                exitCode = (int)ErrorReason.NotSupported;
            }
            catch (VirtualClientException exc)
            {
                Program.LogErrorMessage(logger, exc, EventContext.Persisted());
                exitCode = (int)exc.Reason;
            }
            catch (Exception exc)
            {
                Program.LogErrorMessage(logger, exc, EventContext.Persisted());
                exitCode = 1;
            }
            finally
            {
                // Allow components to handle any final exit operations.
                VirtualClientRuntime.OnExiting();
                Console.WriteLine($"Controller Exit Code: {exitCode}");

                TimeSpan remainingWait = TimeSpan.FromMinutes(2);
                if (this.ExitWaitTimeout != DateTime.MinValue)
                {
                    remainingWait = this.ExitWaitTimeout.SafeSubtract(DateTime.UtcNow);
                }

                if (remainingWait <= TimeSpan.Zero && this.ExitWait > TimeSpan.Zero)
                {
                    remainingWait = TimeSpan.FromMinutes(2);
                }

                DependencyFactory.FlushTelemetry(remainingWait);

                // Allow components to handle any final cleanup operations.
                VirtualClientRuntime.OnCleanup();
            }

            return exitCode;
        }

        private string GetBaseCommandArguments(string[] commandArguments)
        {
            List<string> targetCommandArguments = new List<string>();

            foreach (string argument in commandArguments)
            {
                if (!string.IsNullOrWhiteSpace(this.Command) && argument == this.Command)
                {
                    // The System.CommandLine framework removes the quotes surrounding 1-off commands
                    // on the command line. However, commands on the command line for VC MUST be surrounded
                    // in quotes.
                    targetCommandArguments.Add($"\"{argument}\"");
                }
                else if (!AgentExpression.IsMatch(argument))
                {
                    // Remove the --agent options from the command line.
                    targetCommandArguments.Add(argument);
                }
            }

            return string.Join(" ", targetCommandArguments);
        }

        private string GetTargetAgentCommandArguments(string baseCommandArguments, string targetAgent, string targetAgentHostName, PlatformSpecifics platformSpecifics)
        {
            string vcCommandArguments = $"{baseCommandArguments} {SshOption.Aliases.Last()}=\"{targetAgent}\"";

            if (!LogDirectoryExpression.IsMatch(vcCommandArguments))
            {
                string logDirectory = platformSpecifics.GetLogsPath(targetAgentHostName, "{experimentId}");
                vcCommandArguments += $" {LogDirectoryOption.Aliases.Last()}=\"{logDirectory}\"";
            }

            if (!StateDirectoryExpression.IsMatch(vcCommandArguments))
            {
                string stateDirectory = platformSpecifics.GetStatePath(targetAgentHostName, "{experimentId}");
                vcCommandArguments += $" {StateDirectoryOption.Aliases.Last()}=\"{stateDirectory}\"";
            }

            if (!TempDirectoryExpression.IsMatch(vcCommandArguments))
            {
                string tempDirectory = platformSpecifics.GetTempPath(targetAgentHostName, "{experimentId}");
                vcCommandArguments += $" {TempDirectoryOption.Aliases.Last()}=\"{tempDirectory}\"";
            }

            if (!IsolatedExpression.IsMatch(vcCommandArguments))
            {
                vcCommandArguments += $" {IsolatedOption.Aliases.Last()}";
            }


            return vcCommandArguments;
        }
    }
}