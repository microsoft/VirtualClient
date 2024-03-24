// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient
{
    using System;
    using System.CommandLine;
    using System.CommandLine.Builder;
    using System.CommandLine.Invocation;
    using System.CommandLine.Parsing;
    using System.IO;
    using System.Reflection;
    using System.Threading;

    /// <summary>
    /// The main entry point for the program
    /// </summary>
    public sealed class Program
    {
        private static Assembly ExampleAssembly = Assembly.GetAssembly(typeof(Program));

        /// <summary>
        /// Entry point of VirtualClient.exe
        /// </summary>
        /// <param name="args">Passed in arguments</param>
        public static int Main(string[] args)
        {
            int exitCode = 0;

            try
            {
                // This helps ensure that relative paths (paths relative to the Virtual Client application) are
                // handled correctly. This is required for response file support where relative paths are used.
                Environment.CurrentDirectory = Path.GetDirectoryName(Program.ExampleAssembly.Location);

                using (CancellationTokenSource cancellationTokenSource = new CancellationTokenSource())
                {
                    CancellationToken cancellationToken = cancellationTokenSource.Token;
                    Console.CancelKeyPress += (sender, e) =>
                    {
                        cancellationTokenSource.Cancel();
                        e.Cancel = true;
                    };

                    CommandLineBuilder commandBuilder = Program.SetupCommandLine(args, cancellationTokenSource);
                    ParseResult parseResult = commandBuilder.Build().Parse(args);
                    parseResult.ThrowOnUsageError();

                    exitCode = parseResult.InvokeAsync().GetAwaiter().GetResult();
                }
            }
            catch (OperationCanceledException)
            {
                // Expected when the Ctrl-C is pressed to cancel operation.
            }
            catch (Exception exc)
            {
                exitCode = 1;
                Console.WriteLine(exc.Message);
                Console.WriteLine(exc.StackTrace);
            }

            return exitCode;
        }

        private static CommandLineBuilder SetupCommandLine(string[] args, CancellationTokenSource cancellationTokenSource)
        {
            RootCommand rootCommand = new RootCommand("Executes a fake workload or monitor on the system.");

            Command runWorkloadCommand = new Command("Workload", "Runs the application as an example workload.")
            {
                // --duration
                OptionFactory.CreateDurationOption(required: true),

                // --workload
                OptionFactory.CreateWorkloadOption(required: false, RunWorkloadCommand.WorkloadDefault),

                // --ipAddress
                OptionFactory.CreateIPAddressOption(required: false),

                // --port
                OptionFactory.CreatePortOption(required: false)
            };

            runWorkloadCommand.Handler = CommandHandler.Create<RunWorkloadCommand>(cmd => cmd.ExecuteAsync(args, cancellationTokenSource));

            Command runMonitorCommand = new Command("Monitor", "Runs the application as an example monitor.")
            {
                // --duration | --timeout | --d
                OptionFactory.CreateDurationOption(required: true)
            };

            runMonitorCommand.Handler = CommandHandler.Create<RunMonitorCommand>(cmd => cmd.ExecuteAsync(args, cancellationTokenSource));

            Command runApiCommand = new Command("Api", "Runs the self-hosted REST API.")
            {
                // --port
                OptionFactory.CreatePortOption(required: true),

                // --apiServers
                OptionFactory.CreateApiServersOption(required: false)
            };

            runApiCommand.Handler = CommandHandler.Create<RunApiCommand>(cmd => cmd.ExecuteAsync(args, cancellationTokenSource));

            rootCommand.AddCommand(runWorkloadCommand);
            rootCommand.AddCommand(runMonitorCommand);
            rootCommand.AddCommand(runApiCommand);

            return new CommandLineBuilder(rootCommand).WithDefaults();
        }
    }
}
