// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Threading;
    using System.Threading.Tasks;
    using VirtualClient.Api;
    using VirtualClient.Contracts;

    internal class ExampleWorkloadExecutor
    {
        public Task ExecuteAsync(CancellationToken cancellationToken)
        {
            List<Task> clientServerTasks = new List<Task>
            {
                // In a real-life scenario, the Virtual Client workload executor would be looking at the
                // "Role" that was supplied to in in the environment layout. Given the role, it would be
                // executing 1 or the other of these client/server executors. For the sake of this example
                // project, we are running both the client and server in the same process so that the developer
                // can debug and see how it works.
                new ExampleWorkloadServerExecutor().ExecuteAsync(cancellationToken),
                new ExampleWorkloadClientExecutor().ExecuteAsync(cancellationToken)
            };

            return Task.WhenAll(clientServerTasks);
        }
    }
}
