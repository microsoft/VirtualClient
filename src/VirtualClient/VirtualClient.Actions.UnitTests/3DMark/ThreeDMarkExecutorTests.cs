// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Actions
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Threading;
    using System.Threading.Tasks;
    using VirtualClient.Common;
    using Microsoft.Extensions.DependencyInjection;
    using Moq;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;
    using NUnit.Framework;
    using VirtualClient.Contracts;
    using VirtualClient.Common.Contracts;
    using VirtualClient.Common.Telemetry;
    using System.Text.RegularExpressions;
    using System.Runtime.InteropServices;

    [TestFixture]
    [Category("Unit")]
    public class ThreeDMarkExecutorTests
    {
        private MockFixture fixture;
        private DependencyPath threeDMockPackage;
        private DependencyPath psToolsMockPackage;

        [SetUp]
        public void SetupDefaultBehavior()
        {
            this.fixture = new MockFixture();
            this.fixture.Setup(PlatformID.Win32NT);

            this.threeDMockPackage = new DependencyPath("3dmark", this.fixture.PlatformSpecifics.GetPackagePath("3dmark"));
            this.psToolsMockPackage = new DependencyPath("pstools", this.fixture.PlatformSpecifics.GetPackagePath("pstools"));

            this.fixture.PackageManager.OnGetPackage("3dmark").ReturnsAsync(this.threeDMockPackage);
            this.fixture.PackageManager.OnGetPackage("pstools").ReturnsAsync(this.psToolsMockPackage);

            this.fixture.Parameters = new Dictionary<string, IConvertible>()
            {
                { nameof(ThreeDMarkExecutor.PackageName), "3dmark" },
                { nameof(ThreeDMarkExecutor.PsExecPackageName), "pstools" },
                { nameof(ThreeDMarkExecutor.PsExecSession), 2 },
                { nameof(ThreeDMarkExecutor.LicenseKey), "someLicense" }
            };
        }

        [Test]
        public async Task ThreeDMarkExecutorRunsTheExpectedWorkloadCommand()
        {
            ProcessStartInfo expectedInfo = new ProcessStartInfo();

            string psToolsMockPackagePath = this.fixture.ToPlatformSpecificPath(this.psToolsMockPackage, PlatformID.Win32NT, Architecture.X64).Path;
            string psToolsExecutablePath = this.fixture.PlatformSpecifics.Combine(psToolsMockPackagePath, "PsExec.exe");
            
            string threeDMockPackagePath = this.fixture.ToPlatformSpecificPath(this.threeDMockPackage, PlatformID.Win32NT, Architecture.X64).Path;
            string threeDExecutablePath = this.fixture.PlatformSpecifics.Combine(threeDMockPackagePath, "3DMark", "3DMarkCmd.exe");
            string threeDDLCPath = this.fixture.PlatformSpecifics.Combine(threeDMockPackagePath, "DLC", "3DMark");

            bool commandExecuted = false;
            int commandNumber = 0;

            string commonArguments = $"{psToolsExecutablePath} -s -i 2 -w {psToolsMockPackagePath} -accepteula -nobanner";

            List<string> expectedCommands = new List<string>()
            {
                $"{commonArguments} {threeDExecutablePath} --path={threeDDLCPath}",
                $"{commonArguments} {threeDExecutablePath} --register=someLicense",
                $"{commonArguments} {threeDExecutablePath} --in={DateTimeOffset.Now.ToUnixTimeSeconds()}.out --export=result.xml"
            };

            this.fixture.ProcessManager.OnCreateProcess = (exe, arguments, workingDir) =>
            {
                string expectedCommand = expectedCommands[commandNumber];

                if (expectedCommand == $"{exe} {arguments}")
                {
                    commandExecuted = true;
                }

                Assert.IsTrue(commandExecuted);
                commandExecuted = false;
                commandNumber += 1;

                return new InMemoryProcess
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = exe,
                        Arguments = arguments
                    },
                    ExitCode = 0,
                    OnStart = () => true,
                    OnHasExited = () => true,
                };
            };

            using (TestThreeDMarkExecutor ThreeDMarkExecutor = new TestThreeDMarkExecutor(this.fixture.Dependencies, this.fixture.Parameters))
            {
                await ThreeDMarkExecutor.ExecuteAsync(CancellationToken.None).ConfigureAwait(false);
            }

            Assert.IsTrue(commandExecuted);
        }

        private class TestThreeDMarkExecutor : ThreeDMarkExecutor
        {
            public TestThreeDMarkExecutor(IServiceCollection dependencies, IDictionary<string, IConvertible> parameters)
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
