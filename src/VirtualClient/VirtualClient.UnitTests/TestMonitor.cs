// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.UnitTests
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.DependencyInjection;
    using VirtualClient.Common.Platform;
    using VirtualClient.Common.Telemetry;
    using VirtualClient.Contracts;

    [PlatformAgnostic]
    public class TestMonitor : VirtualClientIntervalBasedMonitor
    {
        public static string FilePath = Path.Combine(Path.GetTempPath(), "testmonitor.txt");

        public TestMonitor(IServiceCollection dependencies, IDictionary<string, IConvertible> parameters)
            : base(dependencies, parameters)
        {
        }

        protected override Task ExecuteAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            var file = File.Create(FilePath);
            file.Close();

            return Task.CompletedTask;
        }

        public static bool Monitored
        {
            get
            {
                return File.Exists(FilePath);
            }
        }

        public static void ResetExecution()
        {
            if (File.Exists(FilePath))
            {
                File.Delete(FilePath);
            }
        }
    }
}