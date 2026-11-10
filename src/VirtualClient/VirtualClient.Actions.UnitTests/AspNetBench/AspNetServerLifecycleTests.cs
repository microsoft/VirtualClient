// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Actions
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Net;
    using System.Net.Sockets;
    using System.Threading;
    using System.Threading.Tasks;
    using Moq;
    using Newtonsoft.Json.Linq;
    using NUnit.Framework;
    using Polly;
    using VirtualClient.Common;
    using VirtualClient.Common.Telemetry;
    using VirtualClient.Contracts;

    [TestFixture]
    [Category("Unit")]
    public class AspNetServerLifecycleTests
    {
        [TestCase(PlatformID.Win32NT)]
        [TestCase(PlatformID.Unix)]
        public async Task ReplacingServerStopsPreviousInstanceWithoutRestartingIt(PlatformID platform)
        {
            MockFixture fixture = CreateFixture(platform);
            List<ServerProcess> servers = new List<ServerProcess>();
            TaskCompletionSource<bool> retryStarted = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

            fixture.ProcessManager.OnCreateProcess = (command, arguments, directory) =>
            {
                if ((command == "taskkill" || command == "pkill") && servers.Count == 1)
                {
                    servers[0].Stop(1);
                    retryStarted.Task.WaitAsync(TimeSpan.FromSeconds(5)).GetAwaiter().GetResult();
                }

                if (arguments?.Contains("Benchmarks.dll") == true)
                {
                    ServerProcess server = new ServerProcess();
                    server.StartInfo = new ProcessStartInfo(command, arguments);
                    server.StandardOutput.Append("Now listening on: http://localhost:9876");
                    server.StandardError.Append("Server diagnostic output");
                    server.OnStart = () =>
                    {
                        if (servers.Count == 2)
                        {
                            retryStarted.TrySetResult(true);
                        }

                        return true;
                    };
                    servers.Add(server);
                    return server;
                }

                return new InMemoryProcess { StartInfo = new ProcessStartInfo(command, arguments ?? string.Empty) };
            };

            using (CancellationTokenSource cancellation = new CancellationTokenSource(TimeSpan.FromSeconds(15)))
            using (TestServer executor = new TestServer(fixture, retry: true))
            {
                try
                {
                    await executor.ExecuteAsync(cancellation.Token);
                    await executor.ExecuteAsync(cancellation.Token);

                    Assert.That(servers.Count, Is.EqualTo(2), "Replacement must create one new server, not restart the previous instance.");
                    Assert.That(servers[0].HasExited, Is.True);
                    Assert.That(servers[1].HasExited, Is.False);
                    Assert.That(fixture.Logger.MessagesLogged($"{executor.TypeName}.KillServerInstances").Count(), Is.EqualTo(2));
                    var processLogs = fixture.Logger.MessagesLogged($"{executor.TypeName}.AspNetBenchmarks.ProcessDetails").ToArray();
                    Assert.That(processLogs.Length, Is.EqualTo(1), "Replacing the server must capture its process output.");
                    EventContext processContext = (EventContext)processLogs[0].Item3;
                    JObject processDetails = JObject.FromObject(processContext.Properties["process"]);
                    Assert.That(processDetails.Value<string>("standardOutput"), Does.Contain("Now listening on:"));
                    Assert.That(processDetails.Value<string>("standardError"), Is.EqualTo("Server diagnostic output"));
                    Assert.That(fixture.ProcessManager.Commands.Any(command =>
                        command.StartsWith("taskkill") || command.StartsWith("pkill") || command.StartsWith("fuser")), Is.False);
                }
                finally
                {
                    cancellation.Cancel();
                    foreach (ServerProcess server in servers)
                    {
                        server.Stop(0);
                    }
                }

                Assert.That(fixture.Logger.MessagesLogged("TestServer.AspNetBenchmarks.ProcessDetails").Count(), Is.EqualTo(2),
                    "Disposal must capture the final server's process output exactly once.");
            }
        }

        [Test]
        public async Task ReplacingServerCancelsAPendingRetryDelay()
        {
            MockFixture fixture = CreateFixture(PlatformID.Win32NT);
            List<ServerProcess> servers = new List<ServerProcess>();
            TaskCompletionSource<bool> retryPending = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            fixture.ProcessManager.OnCreateProcess = (command, arguments, directory) =>
            {
                if (arguments?.Contains("Benchmarks.dll") == true)
                {
                    ServerProcess server = new ServerProcess { StartInfo = new ProcessStartInfo(command, arguments) };
                    servers.Add(server);
                    return server;
                }

                return new InMemoryProcess { StartInfo = new ProcessStartInfo(command, arguments ?? string.Empty) };
            };

            using (CancellationTokenSource cancellation = new CancellationTokenSource(TimeSpan.FromSeconds(10)))
            using (TestServer executor = new TestServer(fixture))
            {
                executor.SetRetryPolicy(Policy.Handle<Exception>(exception => !(exception is OperationCanceledException))
                    .WaitAndRetryAsync(1, attempt => TimeSpan.FromSeconds(10), (exception, delay) => retryPending.TrySetResult(true)));
                await executor.ExecuteAsync(cancellation.Token);
                servers[0].Stop(1);
                await retryPending.Task.WaitAsync(cancellation.Token);
                Assert.That(fixture.Logger.MessagesLogged($"{executor.TypeName}.StartServerInstanceError").Count(), Is.EqualTo(1),
                    "A failed server attempt must be logged even when a retry is pending.");
                await executor.ExecuteAsync(cancellation.Token).WaitAsync(TimeSpan.FromSeconds(2));

                Assert.That(servers.Count, Is.EqualTo(2));
                Assert.That(servers[0].HasExited, Is.True);
                cancellation.Cancel();
            }
        }

        [Test]
        public void AnOccupiedPortDoesNotMakeAFailedServerAppearReady()
        {
            MockFixture fixture = CreateFixture(PlatformID.Win32NT);
            using (TcpListener listener = new TcpListener(IPAddress.Loopback, 0))
            {
                listener.Start();
                fixture.Parameters["ServerPort"] = ((IPEndPoint)listener.LocalEndpoint).Port.ToString();
                fixture.ProcessManager.OnCreateProcess = (command, arguments, directory) =>
                    new InMemoryProcess
                    {
                        StartInfo = new ProcessStartInfo(command, arguments ?? string.Empty),
                        ExitCode = arguments?.Contains("Benchmarks.dll") == true ? 1 : 0
                    };

                using (TestServer executor = new TestServer(fixture, realReadiness: true))
                {
                    Assert.ThrowsAsync<WorkloadException>(() => executor.ExecuteAsync(CancellationToken.None));
                }
            }
        }

        [Test]
        public void AServerFailureWhileSavingReadinessIsNotReportedAsSuccess()
        {
            MockFixture fixture = CreateFixture(PlatformID.Win32NT);
            ServerProcess server = null;
            fixture.ProcessManager.OnCreateProcess = (command, arguments, directory) =>
            {
                if (arguments?.Contains("Benchmarks.dll") == true)
                {
                    server = new ServerProcess { StartInfo = new ProcessStartInfo(command, arguments) };
                    return server;
                }

                return new InMemoryProcess { StartInfo = new ProcessStartInfo(command, arguments ?? string.Empty) };
            };

            using (TestServer executor = new TestServer(fixture))
            {
                fixture.ApiClient.OnUpdateState<State>(nameof(State)).Returns(async () =>
                {
                    server.Stop(1);
                    await executor.GetServerTask().ContinueWith(
                        task => Assert.That(task.Exception.InnerException, Is.InstanceOf<WorkloadException>()),
                        TaskScheduler.Default);
                    return fixture.CreateHttpResponse(HttpStatusCode.OK);
                });

                Assert.ThrowsAsync<WorkloadException>(() => executor.ExecuteAsync(CancellationToken.None));
            }
        }

        [Test]
        public void ANativeProcessStartFailureUsesTheStandardWorkloadErrorClassification()
        {
            MockFixture fixture = CreateFixture(PlatformID.Unix);
            fixture.ProcessManager.OnCreateProcess = (command, arguments, directory) =>
                new InMemoryProcess
                {
                    StartInfo = new ProcessStartInfo(command, arguments ?? string.Empty),
                    OnStart = arguments?.Contains("Benchmarks.dll") == true
                        ? () => throw new System.ComponentModel.Win32Exception(2, "No such file or directory")
                        : () => true
                };

            using (TestServer executor = new TestServer(fixture))
            {
                WorkloadException failure = Assert.ThrowsAsync<WorkloadException>(() => executor.ExecuteAsync(CancellationToken.None));
                Assert.That(failure.Reason, Is.EqualTo(ErrorReason.WorkloadFailed));
                Assert.That(failure.InnerException, Is.InstanceOf<System.ComponentModel.Win32Exception>());
                Assert.That(failure.Message, Does.Contain("Benchmarks.dll"));
                var outcomes = fixture.Logger.MessagesLogged($"{executor.TypeName}.SucceededOrFailed").ToArray();
                Assert.That(outcomes.Length, Is.EqualTo(1));
                Assert.That(((EventContext)outcomes[0].Item3).Properties["metricName"], Is.EqualTo("Failed"));
            }
        }

        [Test]
        public void AnUnexpectedCancellationIsReportedAsAWorkloadFailure()
        {
            MockFixture fixture = CreateFixture(PlatformID.Win32NT);
            fixture.ProcessManager.OnCreateProcess = (command, arguments, directory) =>
                arguments?.Contains("Benchmarks.dll") == true
                    ? new ServerProcess { StartInfo = new ProcessStartInfo(command, arguments), ThrowOnWait = true }
                    : new InMemoryProcess { StartInfo = new ProcessStartInfo(command, arguments ?? string.Empty) };

            using (TestServer executor = new TestServer(fixture))
            {
                Assert.ThrowsAsync<WorkloadException>(() => executor.ExecuteAsync(CancellationToken.None));
            }
        }

        [Test]
        public void MultiRoleServerPropagatesBackgroundProcessFailure()
        {
            MockFixture fixture = CreateFixture(PlatformID.Win32NT);
            fixture.Layout = new EnvironmentLayout(new[]
            {
                new ClientInstance(Environment.MachineName, "127.0.0.1", ClientRole.Server),
                new ClientInstance("client", "127.0.0.2", ClientRole.Client)
            });
            fixture.ProcessManager.OnCreateProcess = (command, arguments, directory) =>
                new InMemoryProcess
                {
                    StartInfo = new ProcessStartInfo(command, arguments ?? string.Empty),
                    ExitCode = arguments?.Contains("Benchmarks.dll") == true ? 1 : 0
                };

            using (TestServer executor = new TestServer(fixture))
            {
                Assert.ThrowsAsync<WorkloadException>(() => executor.ExecuteAsync(CancellationToken.None));
            }
        }

        [Test]
        public async Task ABackgroundFailureIsNotDiscardedByTheNextIteration()
        {
            MockFixture fixture = CreateFixture(PlatformID.Win32NT);
            ServerProcess firstServer = null;
            TaskCompletionSource<bool> exited = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            fixture.ProcessManager.OnCreateProcess = (command, arguments, directory) =>
            {
                if (arguments?.Contains("Benchmarks.dll") == true)
                {
                    ServerProcess server = new ServerProcess { StartInfo = new ProcessStartInfo(command, arguments) };
                    if (firstServer == null)
                    {
                        firstServer = server;
                        server.OnDispose = () => exited.TrySetResult(true);
                    }

                    return server;
                }

                return new InMemoryProcess { StartInfo = new ProcessStartInfo(command, arguments ?? string.Empty) };
            };

            using (CancellationTokenSource cancellation = new CancellationTokenSource(TimeSpan.FromSeconds(10)))
            using (TestServer executor = new TestServer(fixture))
            {
                await executor.ExecuteAsync(cancellation.Token);
                firstServer.Stop(1);
                await exited.Task.WaitAsync(cancellation.Token);
                await executor.GetServerTask().ContinueWith(
                    task => Assert.That(task.Exception.InnerException, Is.InstanceOf<WorkloadException>()),
                    TaskScheduler.Default);
                Assert.That(fixture.Logger.MessagesLogged($"{executor.TypeName}.SucceededOrFailed").Count(), Is.EqualTo(1),
                    "A background error must not emit a second outcome for a completed action.");
                Assert.ThrowsAsync<WorkloadException>(() => executor.ExecuteAsync(cancellation.Token));

                EventContext[] outcomes = fixture.Logger.MessagesLogged($"{executor.TypeName}.SucceededOrFailed")
                    .Select(entry => (EventContext)entry.Item3).ToArray();
                Assert.That(outcomes.Length, Is.EqualTo(2), "Each action execution must emit exactly one outcome.");
                Assert.That(outcomes.Select(outcome => outcome.Properties["metricName"]), Is.EqualTo(new[] { "Succeeded", "Failed" }));
                EventContext failure = outcomes[1];
                Assert.That(failure.Properties["metricValue"], Is.EqualTo(1));
                Assert.That(failure.Properties["toolName"], Is.EqualTo(executor.TypeName));
                Assert.That(failure.Properties["scenarioName"], Is.EqualTo(executor.Scenario));
                Assert.That(failure.Properties["scenarioStartTime"], Is.EqualTo(executor.StartTime));
                Assert.That(failure.Properties["scenarioEndTime"], Is.InRange(executor.StartTime, executor.EndTime));
                Assert.That(failure.Properties["metricUnit"], Is.EqualTo(string.Empty));
                Assert.That(failure.Properties["metricRelativity"], Is.EqualTo(MetricRelativity.LowerIsBetter));
                Assert.That(failure.Properties["metricVerbosity"], Is.EqualTo(1));
                cancellation.Cancel();
            }
        }

        private static MockFixture CreateFixture(PlatformID platform)
        {
            MockFixture fixture = new MockFixture();
            fixture.Setup(platform);
            fixture.Layout = new EnvironmentLayout(new[]
            {
                new ClientInstance(Environment.MachineName, "127.0.0.1", ClientRole.Client)
            });
            fixture.Parameters["PackageName"] = "aspnetbenchmarks";
            fixture.Parameters["TargetFramework"] = "net8.0";
            fixture.Parameters["ServerPort"] = "9876";
            fixture.PackageManager.OnGetPackage("aspnetbenchmarks")
                .ReturnsAsync(new DependencyPath("aspnetbenchmarks", fixture.GetPackagePath("aspnetbenchmarks")));
            fixture.PackageManager.OnGetPackage("dotnetsdk")
                .ReturnsAsync(new DependencyPath("dotnetsdk", fixture.GetPackagePath("dotnetsdk")));
            fixture.ApiClient.OnUpdateState<State>(nameof(State))
                .ReturnsAsync(fixture.CreateHttpResponse(HttpStatusCode.OK));
            return fixture;
        }

        internal class ServerProcess : InMemoryProcess, IProcessProxy
        {
            private readonly TaskCompletionSource<bool> exit = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

            public ServerProcess()
            {
                this.OnHasExited = () => this.exit.Task.IsCompleted;
                this.OnKill = () => this.Stop(1);
            }

            public void Stop(int exitCode)
            {
                this.ExitCode = exitCode;
                this.exit.TrySetResult(true);
            }

            public bool ThrowOnWait { get; set; }

            Task IProcessProxy.WaitForExitAsync(CancellationToken cancellationToken, TimeSpan? timeout)
            {
                if (this.ThrowOnWait)
                {
                    this.ThrowOnWait = false;
                    return Task.FromException(new OperationCanceledException());
                }

                return this.exit.Task.WaitAsync(cancellationToken);
            }
        }

        private class TestServer : AspNetServerExecutor
        {
            private readonly bool realReadiness;

            public TestServer(MockFixture fixture, bool retry = false, bool realReadiness = false)
                : base(fixture.Dependencies, fixture.Parameters)
            {
                this.realReadiness = realReadiness;
                this.ServerRetryPolicy = retry
                    ? Policy.Handle<Exception>(exception => !(exception is OperationCanceledException)).RetryAsync(1)
                    : Policy.NoOpAsync();
            }

            protected override Task WaitForPortReadyAsync(EventContext context, CancellationToken cancellationToken)
            {
                return this.realReadiness
                    ? base.WaitForPortReadyAsync(context, cancellationToken)
                    : Task.CompletedTask;
            }

            public void SetRetryPolicy(IAsyncPolicy policy)
            {
                this.ServerRetryPolicy = policy;
            }

            public Task GetServerTask()
            {
                object process = typeof(AspNetServerExecutor).GetField(
                    "serverProcess",
                    System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).GetValue(this);
                return (Task)process.GetType().GetProperty("Completion").GetValue(process);
            }
        }
    }
}
