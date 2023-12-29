
namespace VirtualClient.Actions
{
    using Moq;
    using NUnit.Framework;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Runtime.InteropServices;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using VirtualClient.Common;
    using VirtualClient.Common.Telemetry;
    using VirtualClient.Contracts;

    [TestFixture]
    [Category("Unit")]
    public class MongoDBExecutorTests
    {
        private MockFixture mockFixture;
        private DependencyPath mockPackage, mockYCSBPackage;
        private ConcurrentBuffer defaultOutput = new ConcurrentBuffer();


        [SetUp]
        public void Setup()
        {

            this.mockFixture = new MockFixture();
            this.mockFixture.Setup(PlatformID.Unix);
            this.mockPackage = new DependencyPath("mongodb", this.mockFixture.PlatformSpecifics.GetPackagePath("mongodb"));
            this.mockYCSBPackage = new DependencyPath("ycsb", this.mockFixture.PlatformSpecifics.GetPackagePath("ycsb"));
            this.mockFixture.PackageManager.OnGetPackage().ReturnsAsync(this.mockPackage);
            this.mockFixture.Parameters = new Dictionary<string, IConvertible>()
            {
                { nameof(MongoDBExecutor.Scenario), "" },
                { nameof(MongoDBExecutor.PackageName), "mongodb" },
                { nameof(MongoDBExecutor.YCSBPackageName), "ycsb" }
            };

            string currentDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string resultsPath = Path.Combine(currentDirectory, "Examples", "MongoDB", "MongoInsertData01.txt");
            string results = File.ReadAllText(resultsPath);
            this.defaultOutput.Clear();
            this.defaultOutput.Append(results);
        }

        [Test]
        [TestCase(PlatformID.Unix, Architecture.X64)]
        public async Task MongoDBExecutorInitializesItsDependenciesAsExpected(PlatformID platform, Architecture architecture)
        {
            this.Setup();
            using (TestMongoDBExecutor executor = new TestMongoDBExecutor(this.mockFixture))
            {
                this.mockFixture.ProcessManager.OnCreateProcess = (command, arguments, workingDirectory) =>
                {
                    return this.mockFixture.Process;
                };

                await executor.InitializeAsync(EventContext.None, CancellationToken.None)
                    .ConfigureAwait(false);

                string mongoDBExpectedPath = this.mockFixture.PlatformSpecifics.Combine(this.mockPackage.Path) ;

                Assert.AreEqual(mongoDBExpectedPath, executor.GetMongoProcessPath);
            }
        }



        private class TestMongoDBExecutor : MongoDBExecutor
        {
            public TestMongoDBExecutor(MockFixture fixture)
                : base(fixture.Dependencies, fixture.Parameters)
            {
            }

            public new Task InitializeAsync(EventContext telemetryContext, CancellationToken cancellationToken)
            {
                return base.InitializeAsync(telemetryContext, cancellationToken);
            }

            public new Task ExecuteAsync(EventContext context, CancellationToken cancellationToken)
            {
                this.InitializeAsync(context, cancellationToken).GetAwaiter().GetResult();
                return base.ExecuteAsync(context, cancellationToken);
            }

            public string GetMongoProcessPath => base.MongoDBPackagePath;



        }

    }


}
