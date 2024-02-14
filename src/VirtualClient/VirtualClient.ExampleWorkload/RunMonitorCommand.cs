// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Newtonsoft.Json.Linq;

    /// <summary>
    /// Example command to run a fake monitor
    /// </summary>
    internal class RunMonitorCommand
    {
        /// <summary>
        /// The timeout at which point the monitor execution should stop.
        /// </summary>
        public TimeSpan Duration { get; set; }

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
                DateTime finishTime = DateTime.MaxValue;
                if (this.Duration != TimeSpan.Zero)
                {
                    finishTime = DateTime.UtcNow.Add(this.Duration);
                }

                // Mimic the workload doing actual work on the system.
                while (DateTime.UtcNow < finishTime && !cancellationToken.IsCancellationRequested)
                {
                    await Task.Delay(50).ConfigureAwait(false);
                }

                // Emit fake workload metrics.
                Random randomGen = new Random();

                JObject metricsOutput = JObject.Parse(
                    $"{{ " +
                    $"'monitor_metric_1': {randomGen.Next(1000, 2000)}, " +
                    $"'monitor_metric_2': {randomGen.Next(2000, 3000)}, " +
                    $"'monitor_metric_3': {randomGen.Next(3000, 4000)}, " +
                    $"'monitor_metric_4': {randomGen.Next(4000, 5000)}, " +
                    $"'monitor_metric_5': {randomGen.Next(5000, 6000)} " +
                    $"}}");

                Console.WriteLine(metricsOutput.ToString());
            }
            catch
            {
                exitCode = 1;
            }

            return exitCode;
        }
    }
}
