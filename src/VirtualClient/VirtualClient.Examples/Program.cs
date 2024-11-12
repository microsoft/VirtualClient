// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Reflection;
    using System.Runtime.InteropServices;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Newtonsoft.Json.Linq;
    using VirtualClient.Contracts;

    /// <summary>
    /// The main entry point for the program
    /// </summary>
    public sealed class Program
    {
        private static Assembly ExampleAssembly = Assembly.GetAssembly(typeof(Program));
        private static string ExampleAssemblyDirectory = Path.GetDirectoryName(Program.ExampleAssembly.Location);
        private static string ProfilesDirectory = Path.Combine(Program.ExampleAssemblyDirectory, "profiles");

        /// <summary>
        /// Entry point of VirtualClient.exe
        /// </summary>
        /// <param name="args">Passed in arguments</param>
        public static void Main(string[] args)
        {
            try
            {
                using (CancellationTokenSource tokenSource = new CancellationTokenSource())
                {
                    CancellationToken cancellationToken = tokenSource.Token;
                    Console.CancelKeyPress += (sender, e) =>
                    {
                        Console.WriteLine("Cancelled...");
                        tokenSource.Cancel();
                        e.Cancel = true;
                    };

                    Console.WriteLine("Important:");
                    Console.WriteLine("This application (or Visual Studio) must be ran with administrator privileges.");
                    Console.WriteLine();
                    Console.WriteLine();

                    Console.WriteLine("Select an example from the following:");
                    Console.WriteLine("1 = an example of running a client/server workoad.");
                    Console.WriteLine("2 = an example of running a profiler monitor on an interval.");
                    Console.WriteLine("3 = an example of running a profiler monitor on-demand.");
                    Console.WriteLine();
                    Console.Write("Choice: ");
                    ConsoleKeyInfo selection = Console.ReadKey();
                    Console.WriteLine();
                    Console.WriteLine();

                    // IMPORTANT:
                    // You MUST run as administrator in order for the application to make firewall rule changes.
                    IConfiguration configuration = new ConfigurationBuilder().Build();
                    PlatformSpecifics platformSpecifics = new PlatformSpecifics(Environment.OSVersion.Platform, RuntimeInformation.ProcessArchitecture);

                    ISystemManagement systemManagement = DependencyFactory.CreateSystemManager(
                        Environment.MachineName,
                        Guid.NewGuid().ToString(),
                        platformSpecifics);

                    IServiceCollection dependencies = new ServiceCollection()
                        .AddSingleton<ISystemInfo>(systemManagement)
                        .AddSingleton<ISystemManagement>(systemManagement);

                    if (selection.Key == ConsoleKey.NumPad1 || selection.Key == ConsoleKey.D1)
                    {
                        // 1) Run the API server.
                        // ==================================================
                        Task apiServerHostingTask = ApiHosting.StartApiServer<ApiServerStartup>(configuration, systemManagement.FirewallManager, ApiClientManager.DefaultApiPort)
                            .StartAsync(cancellationToken);

                        // 2) Run the workloads
                        // ==================================================
                        ExampleWorkloadExecutor workloadExecutor = new ExampleWorkloadExecutor();
                        Task workloadExecutionTask = workloadExecutor.ExecuteAsync(cancellationToken);

                        // Allow all background tasks to exit gracefully. Each task checks the state of the
                        // cancellation token and will exit when it indicates cancellation.
                        Task.WhenAll(workloadExecutionTask).GetAwaiter().GetResult();
                    }
                    else if (selection.Key == ConsoleKey.NumPad2 || selection.Key == ConsoleKey.D2)
                    {
                        ComponentTypeCache.Instance.LoadComponentTypes(Program.ExampleAssemblyDirectory);

                        ExecutionProfile workloadProfile = ExecutionProfile.ReadProfileAsync(Path.Combine(Program.ProfilesDirectory, "EXAMPLE-WORKLOAD-PROFILE.json"))
                            .GetAwaiter().GetResult();

                        ExecutionProfile monitorsProfile = ExecutionProfile.ReadProfileAsync(Path.Combine(Program.ProfilesDirectory, "EXAMPLE-MONITORS-PROFILE.json"))
                            .GetAwaiter().GetResult();

                        workloadProfile.Inline();
                        monitorsProfile.Inline();

                        int executionRound = 0;
                        using (ProfileExecutor executor = new ProfileExecutor(workloadProfile.MergeWith(monitorsProfile), dependencies))
                        {
                            executor.IterationBegin += (sender, args) =>
                            {
                                executionRound++;
                                Console.WriteLine();
                                Console.WriteLine($"[Profile Execution Round #{executionRound}]");
                            };

                            executor.ExecuteAsync(ProfileTiming.Forever(), tokenSource.Token)
                                .GetAwaiter().GetResult();
                        }
                    }
                    else if (selection.Key == ConsoleKey.NumPad3 || selection.Key == ConsoleKey.D3)
                    {
                        ComponentTypeCache.Instance.LoadComponentTypes(Program.ExampleAssemblyDirectory);

                        ExecutionProfile workloadProfile = ExecutionProfile.ReadProfileAsync(Path.Combine(Program.ProfilesDirectory, "EXAMPLE-WORKLOAD-PROFILE.json"))
                            .GetAwaiter().GetResult();

                        // Enable On-Demand profiling for each workload
                        workloadProfile.Parameters["OnDemandProfilingEnabled"] = true;

                        ExecutionProfile monitorsProfile = ExecutionProfile.ReadProfileAsync(Path.Combine(Program.ProfilesDirectory, "EXAMPLE-MONITORS-PROFILE.json"))
                            .GetAwaiter().GetResult();

                        // Enable On-Demand profiling on the monitor
                        monitorsProfile.Parameters["ExampleProfilerMode"] = "OnDemand";

                        workloadProfile.Inline();
                        monitorsProfile.Inline();

                        int executionRound = 0;
                        using (ProfileExecutor executor = new ProfileExecutor(workloadProfile.MergeWith(monitorsProfile), dependencies))
                        {
                            executor.IterationBegin += (sender, args) =>
                            {
                                executionRound++;
                                Console.WriteLine();
                                Console.WriteLine($"[Profile Execution Round #{executionRound}]");
                            };

                            executor.ExecuteAsync(ProfileTiming.Forever(), tokenSource.Token)
                                .GetAwaiter().GetResult();
                        }
                    }
                    else
                    {
                        throw new ProcessException($"The selection '{selection.Key.ToString()}' is not a valid selection.");
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // Expected whenever Ctrl-C is issued from the command line.
            }
            catch (Exception exc)
            {
                Console.WriteLine(exc.Message);
            }
        }
    }
}
