// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.UnitTests
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Threading;
    using System.Threading.Tasks;
    using VirtualClient.Common.Platform;
    using Microsoft.Extensions.DependencyInjection;
    using VirtualClient.Contracts;
    using VirtualClient.Common.Telemetry;

    public class TestAction : VirtualClientComponent
    {
        public static string FilePath = Path.Combine(Path.GetTempPath(), "testaction.txt");

        public TestAction(IServiceCollection dependencies, IDictionary<string, IConvertible> parameters)
           : base(dependencies, parameters)
        {
        }

        protected override Task ExecuteAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            return Task.Run(() =>
            {
                var file = File.Create(FilePath);
                file.Close();
            });
        }

        public static bool Executed
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
                FileInfo file = new FileInfo(FilePath);
                file.Delete();
            }
        }

    }
}