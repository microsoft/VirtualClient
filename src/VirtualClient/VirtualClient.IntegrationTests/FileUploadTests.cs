// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.IO.Abstractions;
    using System.Linq;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.DependencyInjection.Extensions;
    using NUnit.Framework;
    using VirtualClient.Contracts;
    using VirtualClient.Monitors;

    [TestFixture]
    [Category("Integration")]
    public class FileUploadTests
    {
        private MockFixture mockFixture;
        private FileSystem fileSystem;
        private Random randomGen;

        [SetUp]
        public void SetupTest()
        {
            this.mockFixture = new MockFixture();
            this.mockFixture.Setup(PlatformID.Win32NT);
            this.fileSystem = new FileSystem();
            this.randomGen = new Random();

            // **IMPORTANT**
            // Make sure to define the connection string for the BlobManager relevant to the target storage account where the test
            // data should be uploaded. You can define the connection string in an environment variable named "VCContentStore". This
            // can be a system-level environment variable or an environment variable or a user-level environment variable if not defined
            // here directly.
            string storageAccountConnectionString = Environment.GetEnvironmentVariable("VCContentStore", EnvironmentVariableTarget.Machine);
            if (storageAccountConnectionString == null)
            {
                // If the connection string is not defined in the environment variable, it can be defined directly here.
                storageAccountConnectionString = "{Put_Connection_String_Here}";
            }

            IBlobManager blobManager = new BlobManager(new DependencyBlobStore(DependencyStore.Content, storageAccountConnectionString));

            this.mockFixture.Dependencies.RemoveAll<IFileSystem>();
            this.mockFixture.Dependencies.RemoveAll<IEnumerable<IBlobManager>>();
            this.mockFixture.Dependencies.AddSingleton<IFileSystem>(this.fileSystem);
            this.mockFixture.Dependencies.AddSingleton<IEnumerable<IBlobManager>>(new List<IBlobManager> { blobManager });
        }

        [Test]
        [TestCase(10)]
        public async Task ProcessRepeatFileUploads(int totalMinutesToRun)
        {
            // **IMPORTANT**
            // Make sure to define the connection string above for the BlobManager relevant to the target storage account where the test
            // data should be uploaded.

            // Scenario:
            // Upload the same files to the same locations over and over and over again. The blob upload logic is
            // expected to simply overwrite the existing files.

            Random randomGen = new Random();
            IEnumerable<string> resultsFiles = Directory.GetFiles(Path.Combine(MockFixture.TestAssemblyDirectory, "Resources"), "results*.*");
            
            using (TestExecutor component = new TestExecutor(this.mockFixture))
            {
                if (!component.TryGetContentStoreManager(out IBlobManager contentStore))
                {
                    Assert.Fail("Content store blob manager not defined. Ensure that the setting above in the [SetUp] are correct.");
                }

                DateTime finishTime = DateTime.UtcNow.AddMinutes(totalMinutesToRun);
                while (DateTime.UtcNow < finishTime)
                {
                    int randomFileNumber = randomGen.Next(0, resultsFiles.Count());
                    string randomFile = resultsFiles.ElementAt(randomFileNumber);
                    string fileExtension = Path.GetExtension(randomFile).ToLowerInvariant();

                    string contentType = "text/plain";
                    switch (fileExtension)
                    {
                        case ".xml":
                            contentType = "text/xml";
                            break;

                        case ".json":
                            contentType = "application/json";
                            break;
                    }

                    string toolname = Path.GetFileNameWithoutExtension(randomFile).Replace("results-", string.Empty);

                    FileContext fileContext = new FileContext(
                        this.fileSystem.FileInfo.New(randomFile),
                        contentType,
                        Encoding.UTF8.WebName,
                        component.ExperimentId,
                        component.AgentId,
                        toolname,
                        component.Scenario);

                    FileUploadDescriptor descriptor = component.CreateFileUploadDescriptor(fileContext, component.Parameters, component.Metadata);

                    await component.UploadFileAsync(contentStore, this.fileSystem, descriptor, CancellationToken.None, deleteFile: false);
                }
            }
        }

        [Test]
        [TestCase(10)]
        public async Task ProcessRandomFileUploads(int totalMinutesToRun)
        {
            // **IMPORTANT**
            // Make sure to define the connection string above for the BlobManager relevant to the target storage account where the test
            // data should be uploaded.

            // Scenario:
            // Upload random files for a given experiment.

            IEnumerable<string> resultsFiles = Directory.GetFiles(Path.Combine(MockFixture.TestAssemblyDirectory, "Resources"), "results*.*");
            await this.ProcessRandomFileUploadsAsync(Guid.NewGuid(), "Agent01", totalMinutesToRun, resultsFiles);
        }

        [Test]
        [TestCase(10)]
        public async Task ProcessRandomFileUploadsWithClientServerRoles(int totalMinutesToRun)
        {
            // **IMPORTANT**
            // Make sure to define the connection string above for the BlobManager relevant to the target storage account where the test
            // data should be uploaded.

            // Scenario:
            // Upload random files for a given experiment where client and server roles are used.

            Guid experimentId = Guid.NewGuid();
            IEnumerable<string> resultsFiles = Directory.GetFiles(Path.Combine(MockFixture.TestAssemblyDirectory, "Resources"), "results*.*");

            await this.ProcessRandomFileUploadsAsync(experimentId, "Agent01", totalMinutesToRun, resultsFiles, "Client");
            await this.ProcessRandomFileUploadsAsync(experimentId, "Agent02", totalMinutesToRun, resultsFiles, "Server");
        }

        [Test]
        [TestCase(2)]
        public async Task ProcessRandomFileUploadsWithMonitor(int totalMinutesToRun)
        {
            // **IMPORTANT**
            // Make sure to define the connection string above for the BlobManager relevant to the target storage account where the test
            // data should be uploaded.

            IEnumerable<string> resultsFiles = Directory.GetFiles(Path.Combine(MockFixture.TestAssemblyDirectory, "Resources"), "results*.*");

            string requestDirectory = Path.Combine(MockFixture.TestAssemblyDirectory, "contentuploads");
            if (this.fileSystem.Directory.Exists(requestDirectory))
            {
                this.fileSystem.Directory.Delete(requestDirectory, true);
            }

            this.fileSystem.Directory.CreateDirectory(requestDirectory);

            using (CancellationTokenSource cancellationSource = new CancellationTokenSource())
            {
                Task timeoutTask = Task.Run(async () =>
                {
                    DateTime finishTime = DateTime.UtcNow.AddMinutes(totalMinutesToRun);
                    while (DateTime.UtcNow < finishTime)
                    {
                        await Task.Delay(100);
                    }

                    cancellationSource.Cancel();
                });

                // Background thread creating files to mimic the behavior of a workload or monitor
                // producing results.
                Task fileCreationTask = Task.Run(async () =>
                {
                    using (TestExecutor component = new TestExecutor(this.mockFixture))
                    {
                        while (!cancellationSource.IsCancellationRequested)
                        {
                            for (int fileCount = 0; fileCount < 50; fileCount++)
                            {
                                FileUploadDescriptor descriptor = this.GetRandomDescriptor(component, resultsFiles);
                                await component.RequestFileUploadAsync(descriptor, requestDirectory);
                            }

                            await Task.Delay(10000);
                        }
                    }
                });

                // Background thread running the file upload monitor to process the files created
                // by the workload or monitor.
                Task fileUploadTask = Task.Run(async () =>
                {
                    IDictionary<string, IConvertible> parameters = new Dictionary<string, IConvertible>
                    {
                    { nameof(FileUploadMonitor.Scenario), "UploadContent" },
                    { nameof(FileUploadMonitor.RequestsDirectory), requestDirectory }
                    };

                    using (FileUploadMonitor monitor = new FileUploadMonitor(this.mockFixture.Dependencies, parameters))
                    {
                        if (!monitor.TryGetContentStoreManager(out IBlobManager contentStore))
                        {
                            Assert.Fail("Content store blob manager not defined. Ensure that the setting above in the [SetUp] are correct.");
                        }

                        monitor.Metadata.Add("experimentName", "Example_Experiment_01");
                        monitor.Metadata.Add("executionGoal", "Example_Experiment_01.ExecutionGoal.json");
                        monitor.Metadata.Add("targetGoal", "PERF-ANY-WORKLOAD-linux-x64");
                        monitor.Metadata.Add("owner", "crc_fte@microsoft.com");

                        await monitor.ExecuteAsync(cancellationSource.Token);
                    }
                });

                await Task.WhenAll(fileCreationTask, fileUploadTask, timeoutTask);
            }
        }

        private async Task ProcessRandomFileUploadsAsync(Guid experimentId, string agentId, int totalMinutesToRun, IEnumerable<string> resultsFiles, string role = null)
        {
            if (!string.IsNullOrWhiteSpace(role))
            {
                this.mockFixture.Parameters["Role"] = role;
            }

            this.mockFixture.SystemManagement.Setup(mgr => mgr.AgentId).Returns(agentId);
            this.mockFixture.SystemManagement.Setup(mgr => mgr.ExperimentId).Returns(experimentId.ToString());

            
            using (TestExecutor component = new TestExecutor(this.mockFixture))
            {
                if (!component.TryGetContentStoreManager(out IBlobManager contentStore))
                {
                    Assert.Fail("Content store blob manager not defined. Ensure that the setting above in the [SetUp] are correct.");
                }

                component.Metadata.Add("experimentName", "Example_Experiment_01");
                component.Metadata.Add("executionGoal", "Example_Experiment_01.ExecutionGoal.json");
                component.Metadata.Add("targetGoal", "PERF-ANY-WORKLOAD-linux-x64");
                component.Metadata.Add("owner", "crc_fte@microsoft.com");

                DateTime finishTime = DateTime.UtcNow.AddMinutes(totalMinutesToRun);
                while (DateTime.UtcNow < finishTime)
                {
                    FileUploadDescriptor descriptor = this.GetRandomDescriptor(component, resultsFiles);
                    await component.UploadFileAsync(contentStore, this.fileSystem, descriptor, CancellationToken.None, deleteFile: false);
                }
            }
        }

        private FileUploadDescriptor GetRandomDescriptor(VirtualClientComponent component, IEnumerable<string> resultsFiles)
        {
            int randomFileNumber = this.randomGen.Next(0, resultsFiles.Count());
            string randomFile = resultsFiles.ElementAt(randomFileNumber);
            string fileExtension = Path.GetExtension(randomFile).ToLowerInvariant();

            string contentType = "text/plain";
            switch (fileExtension)
            {
                case ".xml":
                    contentType = "text/xml";
                    break;

                case ".json":
                    contentType = "application/json";
                    break;
            }

            string toolname = Path.GetFileNameWithoutExtension(randomFile).Replace("results-", string.Empty);
            string scenario = $"{toolname}_Scenario_{randomFileNumber}".ToLowerInvariant();

            component.Parameters[nameof(VirtualClientComponent.Scenario)] = scenario;

            FileContext fileContext = new FileContext(
                        this.fileSystem.FileInfo.New(randomFile),
                        contentType,
                        Encoding.UTF8.WebName,
                        component.ExperimentId,
                        component.AgentId,
                        toolname,
                        component.Scenario);

            FileUploadDescriptor descriptor = component.CreateFileUploadDescriptor(fileContext, component.Parameters, component.Metadata);

            return descriptor;
        }
    }
}
