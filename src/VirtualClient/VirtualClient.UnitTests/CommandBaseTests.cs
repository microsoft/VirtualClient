// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.UnitTests
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Runtime.InteropServices;
    using System.Security.Cryptography.X509Certificates;
    using System.Threading;
    using System.Threading.Tasks;
    using AutoFixture;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.Logging;
    using Moq;
    using NUnit.Framework;
    using VirtualClient.Common.Telemetry;
    using VirtualClient.Configuration;
    using VirtualClient.Contracts;
    using VirtualClient.Identity;
    using VirtualClient.TestExtensions;

    [TestFixture]
    [Category("Unit")]
    internal class CommandBaseTests : CommandBase
    {
        private MockFixture mockFixture;

        // Note:
        // Property and method overrides are defined at the bottom of this class below
        // the unit tests.

        public void SetupTest(PlatformID platform, Architecture architecture)
        {
            this.mockFixture = new MockFixture();
            this.mockFixture.Setup(platform, architecture);
            this.mockFixture.SetupCertificateMocks();
        }

        [Test]
        [TestCase(PlatformID.Unix, Architecture.X64)]
        [TestCase(PlatformID.Unix, Architecture.Arm64)]
        [TestCase(PlatformID.Win32NT, Architecture.X64)]
        [TestCase(PlatformID.Win32NT, Architecture.Arm64)]
        public void ApplicationUsesTheExpectedDefaultLogDirectoryLocation(PlatformID platform, Architecture architecture)
        {
            this.SetupTest(platform, architecture);

            string expectedDirectory = this.mockFixture.Combine(this.mockFixture.PlatformSpecifics.CurrentDirectory, "logs");
            string defaultDirectory = this.mockFixture.PlatformSpecifics.LogsDirectory;

            this.EvaluateDirectoryPathOverrides(this.mockFixture.PlatformSpecifics);
            string actualDirectory = this.mockFixture.PlatformSpecifics.LogsDirectory;

            Assert.AreEqual(expectedDirectory, defaultDirectory, "Default logs directory does not match expected.");
            Assert.AreEqual(expectedDirectory, actualDirectory);
        }

        [Test]
        [TestCase(PlatformID.Unix, Architecture.X64)]
        [TestCase(PlatformID.Unix, Architecture.Arm64)]
        [TestCase(PlatformID.Win32NT, Architecture.X64)]
        [TestCase(PlatformID.Win32NT, Architecture.Arm64)]
        public void ApplicationUsesTheExpectedLogDirectoryLocationWhenSpecifiedOnTheCommandLine(PlatformID platform, Architecture architecture)
        {
            this.SetupTest(platform, architecture);

            // Setup:
            // Property set when --log-dir=<path> is used on the command line.
            string expectedDirectory = this.mockFixture.Combine(this.mockFixture.PlatformSpecifics.CurrentDirectory, "alternate_logs_1");
            this.LogDirectory = expectedDirectory;

            this.EvaluateDirectoryPathOverrides(this.mockFixture.PlatformSpecifics);
            string actualDirectory = this.mockFixture.PlatformSpecifics.LogsDirectory;

            Assert.AreEqual(expectedDirectory, actualDirectory);
        }

        [Test]
        [TestCase(PlatformID.Unix, Architecture.X64)]
        [TestCase(PlatformID.Unix, Architecture.Arm64)]
        [TestCase(PlatformID.Win32NT, Architecture.X64)]
        [TestCase(PlatformID.Win32NT, Architecture.Arm64)]
        public void ApplicationUsesTheExpectedLogDirectoryLocationWhenSpecifiedInTheSupportedEnvironmentVariable(PlatformID platform, Architecture architecture)
        {
            this.SetupTest(platform, architecture);

            // Setup:
            // Environment variable 'VC_LOGS_DIR' can be used to define the logs directory.
            string expectedDirectory = this.mockFixture.Combine(this.mockFixture.PlatformSpecifics.CurrentDirectory, "alternate_logs_2");
            this.mockFixture.SetEnvironmentVariable(EnvironmentVariable.VC_LOGS_DIR, expectedDirectory);

            this.EvaluateDirectoryPathOverrides(this.mockFixture.PlatformSpecifics);
            string actualDirectory = this.mockFixture.PlatformSpecifics.LogsDirectory;

            Assert.AreEqual(expectedDirectory, actualDirectory);
        }

        [Test]
        [TestCase(PlatformID.Unix, Architecture.X64)]
        [TestCase(PlatformID.Unix, Architecture.Arm64)]
        [TestCase(PlatformID.Win32NT, Architecture.X64)]
        [TestCase(PlatformID.Win32NT, Architecture.Arm64)]
        public void ApplicationUsesTheExpectedDefaultPackagesDirectoryLocation(PlatformID platform, Architecture architecture)
        {
            this.SetupTest(platform, architecture);

            string expectedDirectory = this.mockFixture.Combine(this.mockFixture.PlatformSpecifics.CurrentDirectory, "packages");
            string defaultDirectory = this.mockFixture.PlatformSpecifics.PackagesDirectory;

            this.EvaluateDirectoryPathOverrides(this.mockFixture.PlatformSpecifics);
            string actualDirectory = this.mockFixture.PlatformSpecifics.PackagesDirectory;

            Assert.AreEqual(expectedDirectory, defaultDirectory, "Default packages directory does not match expected.");
            Assert.AreEqual(expectedDirectory, actualDirectory);
        }

        [Test]
        [TestCase(PlatformID.Unix, Architecture.X64)]
        [TestCase(PlatformID.Unix, Architecture.Arm64)]
        [TestCase(PlatformID.Win32NT, Architecture.X64)]
        [TestCase(PlatformID.Win32NT, Architecture.Arm64)]
        public void ApplicationUsesTheExpectedPackagesDirectoryLocationWhenSpecifiedOnTheCommandLine(PlatformID platform, Architecture architecture)
        {
            this.SetupTest(platform, architecture);

            // Setup:
            // Property set when --package-dir=<path> is used on the command line.
            string expectedDirectory = this.mockFixture.Combine(this.mockFixture.PlatformSpecifics.CurrentDirectory, "alternate_packages_1");
            this.PackageDirectory = expectedDirectory;

            this.EvaluateDirectoryPathOverrides(this.mockFixture.PlatformSpecifics);
            string actualDirectory = this.mockFixture.PlatformSpecifics.PackagesDirectory;

            Assert.AreEqual(expectedDirectory, actualDirectory);
        }

        [Test]
        [TestCase(PlatformID.Unix, Architecture.X64)]
        [TestCase(PlatformID.Unix, Architecture.Arm64)]
        [TestCase(PlatformID.Win32NT, Architecture.X64)]
        [TestCase(PlatformID.Win32NT, Architecture.Arm64)]
        public void ApplicationUsesTheExpectedPackagesDirectoryLocationWhenSpecifiedInTheSupportedEnvironmentVariable(PlatformID platform, Architecture architecture)
        {
            this.SetupTest(platform, architecture);

            // Setup:
            // Environment variable 'VC_PACKAGES_DIR' can be used to define the logs directory.
            string expectedDirectory = this.mockFixture.Combine(this.mockFixture.PlatformSpecifics.CurrentDirectory, "alternate_packages_2");
            this.mockFixture.SetEnvironmentVariable(EnvironmentVariable.VC_PACKAGES_DIR, expectedDirectory);

            this.EvaluateDirectoryPathOverrides(this.mockFixture.PlatformSpecifics);
            string actualDirectory = this.mockFixture.PlatformSpecifics.PackagesDirectory;

            Assert.AreEqual(expectedDirectory, actualDirectory);
        }

        [Test]
        [TestCase(PlatformID.Unix, Architecture.X64)]
        [TestCase(PlatformID.Unix, Architecture.Arm64)]
        [TestCase(PlatformID.Win32NT, Architecture.X64)]
        [TestCase(PlatformID.Win32NT, Architecture.Arm64)]
        public void ApplicationUsesTheExpectedDefaultStateDirectoryLocation(PlatformID platform, Architecture architecture)
        {
            this.SetupTest(platform, architecture);

            PlatformSpecifics platformSpecifics = new PlatformSpecifics(platform, architecture);
            string expectedDirectory = this.mockFixture.Combine(MockFixture.GetDirectory(typeof(CommandBaseTests), "state"));
            string defaultDirectory = platformSpecifics.StateDirectory;

            this.EvaluateDirectoryPathOverrides(platformSpecifics);
            string actualDirectory = platformSpecifics.StateDirectory;

            Assert.AreEqual(expectedDirectory, defaultDirectory, "Default state directory does not match expected.");
            Assert.AreEqual(expectedDirectory, actualDirectory);
        }

        [Test]
        [TestCase(PlatformID.Unix, Architecture.X64)]
        [TestCase(PlatformID.Unix, Architecture.Arm64)]
        [TestCase(PlatformID.Win32NT, Architecture.X64)]
        [TestCase(PlatformID.Win32NT, Architecture.Arm64)]
        public void ApplicationUsesTheExpectedStateDirectoryLocationWhenSpecifiedOnTheCommandLine(PlatformID platform, Architecture architecture)
        {
            this.SetupTest(platform, architecture);

            // Setup:
            // Property set when --state-dir=<path> is used on the command line.
            string expectedDirectory = this.mockFixture.Combine(this.mockFixture.PlatformSpecifics.CurrentDirectory, "alternate_state_1");
            this.StateDirectory = expectedDirectory;

            this.EvaluateDirectoryPathOverrides(this.mockFixture.PlatformSpecifics);
            string actualDirectory = this.mockFixture.PlatformSpecifics.StateDirectory;

            Assert.AreEqual(expectedDirectory, actualDirectory);
        }

        [Test]
        [TestCase(PlatformID.Unix, Architecture.X64)]
        [TestCase(PlatformID.Unix, Architecture.Arm64)]
        [TestCase(PlatformID.Win32NT, Architecture.X64)]
        [TestCase(PlatformID.Win32NT, Architecture.Arm64)]
        public void ApplicationUsesTheExpectedStateDirectoryLocationWhenSpecifiedInTheSupportedEnvironmentVariable(PlatformID platform, Architecture architecture)
        {
            this.SetupTest(platform, architecture);

            // Setup:
            // Environment variable 'VC_STATE_DIR' can be used to define the logs directory.
            string expectedDirectory = this.mockFixture.Combine(this.mockFixture.PlatformSpecifics.CurrentDirectory, "alternate_state_2");
            this.mockFixture.SetEnvironmentVariable(EnvironmentVariable.VC_STATE_DIR, expectedDirectory);

            this.EvaluateDirectoryPathOverrides(this.mockFixture.PlatformSpecifics);
            string actualDirectory = this.mockFixture.PlatformSpecifics.StateDirectory;

            Assert.AreEqual(expectedDirectory, actualDirectory);
        }

        [Test]
        [TestCase(PlatformID.Unix, Architecture.X64)]
        [TestCase(PlatformID.Unix, Architecture.Arm64)]
        [TestCase(PlatformID.Win32NT, Architecture.X64)]
        [TestCase(PlatformID.Win32NT, Architecture.Arm64)]
        public void CommandBaseCanCreateLoggers(PlatformID platform, Architecture architecture)
        {
            this.SetupTest(platform, architecture);

            TestCommandBase testCommand = new TestCommandBase();
            List<string> loggerDefinitions = new List<string>();
            testCommand.Loggers = loggerDefinitions;
            IList<ILoggerProvider> loggers = testCommand.CreateLogger(new ConfigurationBuilder().Build(), this.mockFixture.PlatformSpecifics);
            // 1 console, 3 serilog and 1 csv file logger
            Assert.AreEqual(loggers.Count, 5);
        }

        [Test]
        public void CommandBaseCanCreateEventHubLoggers()
        {
            this.SetupTest(PlatformID.Unix, Architecture.X64);

            TestCommandBase testCommand = new TestCommandBase(this.mockFixture.CertificateManager.Object);
            this.mockFixture.CertificateManager.Setup(mgr => mgr.GetCertificateFromStoreAsync(It.IsAny<string>(), It.IsAny<IEnumerable<StoreLocation>>(), It.IsAny<StoreName>()))
                .ReturnsAsync(this.mockFixture.Create<X509Certificate2>());
            List<string> loggerDefinitions = new List<string>();
            loggerDefinitions.Add("eventHub;sb://any.servicebus.windows.net/?cid=307591a4-abb2-4559-af59-b47177d140cf&tid=985bbc17-e3a5-4fec-b0cb-40dbb8bc5959&crtt=123456789");
            testCommand.Loggers = loggerDefinitions;

            var inMemorySettings = new Dictionary<string, string>
            {
                { "EventHubLogSettings:IsEnabled", "true" },
                { "EventHubLogSettings:EventsHubName", "Events" },
                { "EventHubLogSettings:CountersHubName", "Counters" },
                { "EventHubLogSettings:MetricsHubName", "Metrics" },
                { "EventHubLogSettings:TracesHubName", "Traces" },
            };

            IConfiguration configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(inMemorySettings)
                .Build();

            var eventHubLogSettings = new EventHubLogSettings();
            configuration.GetSection("EventHubLogSettings").Bind(eventHubLogSettings);

            IList<ILoggerProvider> loggers = testCommand.CreateLogger(configuration, this.mockFixture.PlatformSpecifics);
            // 1 console, 3 serilog and 1 csv file logger, 3 eventhub
            Assert.AreEqual(loggers.Count, 8);
        }

        [Test]
        public void CommandBaseCanCreateMultipleLoggers()
        {
            this.SetupTest(PlatformID.Unix, Architecture.X64);

            TestCommandBase testCommand = new TestCommandBase(this.mockFixture.CertificateManager.Object);
            this.mockFixture.CertificateManager.Setup(mgr => mgr.GetCertificateFromStoreAsync(It.IsAny<string>(), It.IsAny<IEnumerable<StoreLocation>>(), It.IsAny<StoreName>()))
                .ReturnsAsync(this.mockFixture.Create<X509Certificate2>());
            List<string> loggerDefinitions = new List<string>();
            loggerDefinitions.Add("eventHub;sb://any.servicebus.windows.net/?cid=307591a4-abb2-4559-af59-b47177d140cf&tid=985bbc17-e3a5-4fec-b0cb-40dbb8bc5959&crtt=123456789");
            loggerDefinitions.Add(@"proxy;https://vc.com");
            loggerDefinitions.Add("console");
            loggerDefinitions.Add("file");
            testCommand.Loggers = loggerDefinitions;

            var inMemorySettings = new Dictionary<string, string>
            {
                { "EventHubLogSettings:IsEnabled", "true" },
                { "EventHubLogSettings:EventsHubName", "Events" },
                { "EventHubLogSettings:CountersHubName", "Counters" },
                { "EventHubLogSettings:MetricsHubName", "Metrics" },
                { "EventHubLogSettings:TracesHubName", "Traces" },
            };

            IConfiguration configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(inMemorySettings)
                .Build();

            var eventHubLogSettings = new EventHubLogSettings();
            configuration.GetSection("EventHubLogSettings").Bind(eventHubLogSettings);

            IList<ILoggerProvider> loggers = testCommand.CreateLogger(configuration, this.mockFixture.PlatformSpecifics);
            // 1 console, 3 serilog and 1 csv file logger, 3 eventhub, 1 proxy
            Assert.AreEqual(loggers.Count, 9);
        }

        /// <summary>
        /// Not implemented yet.
        /// </summary>
        public override Task<int> ExecuteAsync(string[] args, CancellationTokenSource cancellationTokenSource)
        {
            throw new NotImplementedException();
        }

        public new void EvaluateDirectoryPathOverrides(PlatformSpecifics platformSpecifics)
        {
            base.EvaluateDirectoryPathOverrides(platformSpecifics);
        }

        private class TestCommandBase : CommandBase
        {
            public TestCommandBase(ICertificateManager certManager = null)
                : base()
            {
                this.CertificateManager = certManager;
            }

            public IList<ILoggerProvider> CreateLogger(IConfiguration configuration, PlatformSpecifics platformSpecifics)
            {
                return base.CreateLoggerProviders(configuration, platformSpecifics, null);
            }

            public override Task<int> ExecuteAsync(string[] args, CancellationTokenSource cancellationTokenSource)
            {
                throw new NotImplementedException();
            }
        }
    }
}
