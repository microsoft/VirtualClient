// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Dependencies
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.DependencyInjection;
    using Moq;
    using NUnit.Framework;
    using VirtualClient.Common;
    using VirtualClient.Common.Telemetry;

    [TestFixture]
    [Category("Unit")]
    public class GitRepoCloneTests
    {
        private MockFixture mockFixture;

        [Test]
        public async Task GitRepoCloneRunsTheExpectedCommand()
        {
            this.mockFixture = new MockFixture();
            this.mockFixture.File.Reset();
            this.mockFixture.File.Setup(f => f.Exists(It.IsAny<string>()))
                .Returns(true);
            this.mockFixture.Parameters = new Dictionary<string, IConvertible>()
            {
                { nameof(GitRepoClone.PackageName), "aspnetbenchmarks" },
                { nameof(GitRepoClone.RepoUri), "https://github.com/aspnet/Benchmarks.git" }
            };

            ProcessStartInfo expectedInfo = new ProcessStartInfo();
            List<string> expectedCommands = new List<string>()
            {
                $@"git clone {this.mockFixture.Parameters[$"{nameof(GitRepoClone.RepoUri)}"]} {this.mockFixture.GetPackagePath()}\{this.mockFixture.Parameters[$"{nameof(GitRepoClone.PackageName)}"]}"
            };

            int commandExecuted = 0;
            this.mockFixture.ProcessManager.OnCreateProcess = (exe, arguments, workingDir) =>
            {
                if (expectedCommands.Any(c => c == $"{exe} {arguments}"))
                {
                    commandExecuted++;
                }

                IProcessProxy process = new InMemoryProcess()
                {
                    ExitCode = 0,
                    OnStart = () => true,
                    OnHasExited = () => true
                };
                return process;
            };

            using (TestGitRepoClone installation = new TestGitRepoClone(this.mockFixture.Dependencies, this.mockFixture.Parameters))
            {
                await installation.ExecuteAsync(CancellationToken.None).ConfigureAwait(false);
            }

            Assert.AreEqual(1, commandExecuted);
        }

        [Test]
        public async Task GitRepoCloneRunsTheExpectedCommandWithCheckout()
        {
            this.mockFixture = new MockFixture();
            this.mockFixture.File.Reset();
            this.mockFixture.File.Setup(f => f.Exists(It.IsAny<string>()))
                .Returns(true);

            // The parameter Checkout can be a branch-name, tag or a commit id.
            this.mockFixture.Parameters = new Dictionary<string, IConvertible>()
            {
                { nameof(GitRepoClone.PackageName), "aspnetbenchmarks" },
                { nameof(GitRepoClone.RepoUri), "https://github.com/aspnet/Benchmarks.git" },
                { nameof(GitRepoClone.Checkout), "Checkout-string" }
            };

            ProcessStartInfo expectedInfo = new ProcessStartInfo();
            List<string> expectedCommands = new List<string>()
            {
                $@"git clone {this.mockFixture.Parameters[$"{nameof(GitRepoClone.RepoUri)}"]} {this.mockFixture.GetPackagePath()}\{this.mockFixture.Parameters[$"{nameof(GitRepoClone.PackageName)}"]}",
                $@"git -C {this.mockFixture.GetPackagePath()}\{this.mockFixture.Parameters[$"{nameof(GitRepoClone.PackageName)}"]} checkout {this.mockFixture.Parameters[$"{nameof(GitRepoClone.Checkout)}"]}",
            };

            int commandExecuted = 0;
            this.mockFixture.ProcessManager.OnCreateProcess = (exe, arguments, workingDir) =>
            {
                if (expectedCommands.Any(c => c == $"{exe} {arguments}"))
                {
                    commandExecuted++;
                }

                IProcessProxy process = new InMemoryProcess()
                {
                    ExitCode = 0,
                    OnStart = () => true,
                    OnHasExited = () => true
                };
                return process;
            };

            using (TestGitRepoClone installation = new TestGitRepoClone(this.mockFixture.Dependencies, this.mockFixture.Parameters))
            {
                await installation.ExecuteAsync(CancellationToken.None).ConfigureAwait(false);
            }

            Assert.AreEqual(2, commandExecuted);
        }

        private class TestGitRepoClone : GitRepoClone
        {
            public TestGitRepoClone(IServiceCollection dependencies, IDictionary<string, IConvertible> parameters)
                : base(dependencies, parameters)
            {
            }

            public new Task ExecuteAsync(EventContext context, CancellationToken cancellationToken)
            {
                return base.ExecuteAsync(context, cancellationToken);
            }
        }
    }
}