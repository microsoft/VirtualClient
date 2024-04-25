// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient
{
    using System.Threading.Tasks;
    using System.Threading;
    using System;

    internal class InstallCommand
    {
        /// <summary>
        /// Executes the default monitor execution command.
        /// </summary>
        /// <param name="args">The arguments provided to the application on the command line.</param>
        /// <param name="cancellationTokenSource">Provides a token that can be used to cancel the command operations.</param>
        /// <returns>The exit code for the command operations.</returns>
        public async Task<int> ExecuteAsync(string[] args, CancellationTokenSource cancellationTokenSource)
        {

            int exitCode = 0;

            try
            {
                CancellationToken cancellationToken = cancellationTokenSource.Token;
                Guid dependencyId = Guid.NewGuid();

                Console.Out.WriteLine();
                Console.Out.WriteLine($"Setup/Install Dependency: {dependencyId}");
                Console.Out.WriteLine($"Version: 2.0.0");
                Console.Out.WriteLine($"-------------------------------------------------------");

                try
                {
                    Console.Out.Write($"Installing");
                    DateTime randomEndTime = DateTime.UtcNow.AddSeconds(Random.Shared.Next(10, 20));

                    while (DateTime.UtcNow < randomEndTime)
                    {
                        if (cancellationToken.IsCancellationRequested)
                        {
                            break;
                        }

                        Console.Out.Write(".");
                        await Task.Delay(2000);
                    }

                    if (!cancellationToken.IsCancellationRequested)
                    {
                        Console.Out.WriteLine();
                        Console.Out.WriteLine("Installation Successful");
                    }
                }
                finally
                {
                    Console.Out.WriteLine();
                }
            }
            catch (Exception exc)
            {
                Console.Error.WriteLine();
                Console.Error.WriteLine("Installation FAILED");
                Console.Error.WriteLine();
                Console.Error.WriteLine(exc.Message);
                Console.Error.WriteLine(exc.StackTrace);
                Console.Error.WriteLine();
                exitCode = 1;
            }

            return exitCode;
        }
    }
}
