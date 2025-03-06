// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Diagnostics.CodeAnalysis;
    using System.IO;
    using System.IO.Abstractions;
    using System.Net.Http;
    using System.Threading;
    using System.Threading.Tasks;
    using Moq;
    using Moq.Language;
    using Moq.Language.Flow;
    using Newtonsoft.Json.Linq;
    using Polly;
    using VirtualClient.Common;
    using VirtualClient.Common.Contracts;
    using VirtualClient.Common.Extensions;
    using VirtualClient.Contracts;

    /// <summary>
    /// Extension methods for test fixture instances.
    /// </summary>
    public static class MockSetupExtensions
    {
        private static Random randomGen = new Random();

        /// <summary>
        /// Setup default behavior for creating/saving state objects using the <see cref="IApiClient"/>.
        /// </summary>
        public static ISetup<IApiClient, Task<HttpResponseMessage>> OnCreateState(this Mock<IApiClient> apiClient, string stateId = null)
        {
            apiClient.ThrowIfNull(nameof(apiClient));

            if (stateId == null)
            {
                return apiClient.Setup(client => client.CreateStateAsync(It.IsAny<string>(), It.IsAny<JObject>(), It.IsAny<CancellationToken>(), It.IsAny<IAsyncPolicy<HttpResponseMessage>>()));
            }
            else
            {
                return apiClient.Setup(client => client.CreateStateAsync(stateId, It.IsAny<JObject>(), It.IsAny<CancellationToken>(), It.IsAny<IAsyncPolicy<HttpResponseMessage>>()));
            }
        }

        /// <summary>
        /// Setup default behavior for creating/saving state objects using the <see cref="IApiClient"/>.
        /// </summary>
        public static ISetup<IApiClient, Task<HttpResponseMessage>> OnCreateState<TState>(this Mock<IApiClient> apiClient, string stateId = null)
            where TState : State
        {
            apiClient.ThrowIfNull(nameof(apiClient));

            if (stateId == null)
            {
                return apiClient.Setup(client => client.CreateStateAsync<TState>(It.IsAny<string>(), It.IsAny<TState>(), It.IsAny<CancellationToken>(), It.IsAny<IAsyncPolicy<HttpResponseMessage>>()));
            }
            else
            {
                return apiClient.Setup(client => client.CreateStateAsync<TState>(stateId, It.IsAny<TState>(), It.IsAny<CancellationToken>(), It.IsAny<IAsyncPolicy<HttpResponseMessage>>()));
            }
        }

        /// <summary>
        /// Setup default behavior for retrieving disks using an <see cref="IDiskManager"/> instance.
        /// </summary>
        public static ISetup<IDiskManager, Task<IEnumerable<Disk>>> OnGetDisks(this Mock<IDiskManager> diskManager)
        {
            diskManager.ThrowIfNull(nameof(diskManager));

            return diskManager.Setup(mgr => mgr.GetDisksAsync(It.IsAny<CancellationToken>()));
        }

        /// <summary>
        /// Setup default behavior for getting API heartbeats using the <see cref="IApiClient"/>.
        /// </summary>
        public static ISetup<IApiClient, Task<HttpResponseMessage>> OnGetHeartbeat(this Mock<IApiClient> apiClient)
        {
            apiClient.ThrowIfNull(nameof(apiClient));
            return apiClient.Setup(client => client.GetHeartbeatAsync(It.IsAny<CancellationToken>(), It.IsAny<IAsyncPolicy<HttpResponseMessage>>()));
        }

        /// <summary>
        /// Setup default behavior for checking if a server-side component is online using the <see cref="IApiClient"/>.
        /// </summary>
        public static ISetup<IApiClient, Task<HttpResponseMessage>> OnGetServerOnline(this Mock<IApiClient> apiClient)
        {
            apiClient.ThrowIfNull(nameof(apiClient));
            return apiClient.Setup(client => client.GetServerOnlineStatusAsync(It.IsAny<CancellationToken>(), It.IsAny<IAsyncPolicy<HttpResponseMessage>>()));
        }

        /// <summary>
        /// Setup default behavior for retrieving state objects using the <see cref="IApiClient"/>.
        /// </summary>
        public static ISetup<IApiClient, Task<HttpResponseMessage>> OnGetState(this Mock<IApiClient> apiClient, string stateId = null)
        {
            apiClient.ThrowIfNull(nameof(apiClient));

            if (stateId == null)
            {
                return apiClient.Setup(client => client.GetStateAsync(It.IsAny<string>(), It.IsAny<CancellationToken>(), It.IsAny<IAsyncPolicy<HttpResponseMessage>>()));
            }
            else
            {
                return apiClient.Setup(client => client.GetStateAsync(stateId, It.IsAny<CancellationToken>(), It.IsAny<IAsyncPolicy<HttpResponseMessage>>()));
            }
        }

        /// <summary>
        /// Setup default behavior for retrieving state objects using the <see cref="IApiClient"/>.
        /// </summary>
        public static ISetupSequentialResult<Task<HttpResponseMessage>> OnGetStateSequence(this Mock<IApiClient> apiClient, string stateId = null)
        {
            apiClient.ThrowIfNull(nameof(apiClient));

            if (stateId == null)
            {
                return apiClient.SetupSequence(client => client.GetStateAsync(It.IsAny<string>(), It.IsAny<CancellationToken>(), It.IsAny<IAsyncPolicy<HttpResponseMessage>>()));
            }
            else
            {
                return apiClient.SetupSequence(client => client.GetStateAsync(stateId, It.IsAny<CancellationToken>(), It.IsAny<IAsyncPolicy<HttpResponseMessage>>()));
            }
        }

        /// <summary>
        /// Setup default behavior for updating state objects using the <see cref="IApiClient"/>.
        /// </summary>
        public static ISetup<IApiClient, Task<HttpResponseMessage>> OnUpdateState(this Mock<IApiClient> apiClient, string stateId = null)
        {
            apiClient.ThrowIfNull(nameof(apiClient));

            if (stateId == null)
            {
                return apiClient.Setup(client => client.UpdateStateAsync(It.IsAny<string>(), It.IsAny<JObject>(), It.IsAny<CancellationToken>(), It.IsAny<IAsyncPolicy<HttpResponseMessage>>()));
            }
            else
            {
                return apiClient.Setup(client => client.UpdateStateAsync(stateId, It.IsAny<JObject>(), It.IsAny<CancellationToken>(), It.IsAny<IAsyncPolicy<HttpResponseMessage>>()));
            }
        }

        /// <summary>
        /// Setup default behavior for updating state objects using the <see cref="IApiClient"/>.
        /// </summary>
        public static ISetup<IApiClient, Task<HttpResponseMessage>> OnUpdateState<TState>(this Mock<IApiClient> apiClient, string stateId = null)
            where TState : State
        {
            apiClient.ThrowIfNull(nameof(apiClient));

            if (stateId == null)
            {
                return apiClient.Setup(client => client.UpdateStateAsync(It.IsAny<string>(), It.IsAny<Item<TState>>(), It.IsAny<CancellationToken>(), It.IsAny<IAsyncPolicy<HttpResponseMessage>>()));
            }
            else
            {
                return apiClient.Setup(client => client.UpdateStateAsync(stateId, It.IsAny<Item<TState>>(), It.IsAny<CancellationToken>(), It.IsAny<IAsyncPolicy<HttpResponseMessage>>()));
            }
        }

        /// <summary>
        /// Setup default behavior for validation if a file exists.
        /// </summary>
        public static ISetup<IFile, bool> OnFileExists(this Mock<IFile> fileIntegration)
        {
            fileIntegration.ThrowIfNull(nameof(fileIntegration));
            return fileIntegration.Setup(file => file.Exists(It.IsAny<string>()));
        }

        /// <summary>
        /// Setup default behavior for validation for file writing.
        /// </summary>
        public static ISetup<IFile, Task> OnWriteAllTextAsync(this Mock<IFile> fileIntegration, string filePath)
        {
            fileIntegration.ThrowIfNull(nameof(fileIntegration));
            return fileIntegration.Setup(file => file.WriteAllTextAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()));
        }

        /// <summary>
        /// Setup default behavior for installing a Blob package using the <see cref="IPackageManager"/>.
        /// </summary>
        public static ISetup<IPackageManager, Task<string>> OnInstallDependencyPackage(this Mock<IPackageManager> packageManager)
        {
            packageManager.ThrowIfNull(nameof(packageManager));

            return packageManager.Setup(mgr => mgr.InstallPackageAsync(
                It.IsAny<IBlobManager>(),
                It.IsAny<DependencyDescriptor>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<string>(),
                It.IsAny<IAsyncPolicy>()));
        }

        /// <summary>
        /// Setup default behavior for installing a Blob package using the <see cref="IPackageManager"/>.
        /// </summary>
        public static IReturnsThrows<IPackageManager, Task<string>> OnInstallPackage(this Mock<IPackageManager> packageManager, Action<DependencyDescriptor, string> evaluate)
        {
            packageManager.ThrowIfNull(nameof(packageManager));

            return packageManager.OnInstallDependencyPackage()
                .Callback<IBlobManager, DependencyDescriptor, CancellationToken, string, IAsyncPolicy>((blobManager, descriptor, token, installationPath, retryPolicy) =>
                {
                    evaluate.Invoke(descriptor, installationPath);
                });
        }

        /// <summary>
        /// Setup default behavior for retrieving a package from the <see cref="IPackageManager"/>.
        /// </summary>
        public static ISetup<IPackageManager, Task<DependencyPath>> OnGetPackage(this Mock<IPackageManager> packageManager, string packageName = null)
        {
            packageManager.ThrowIfNull(nameof(packageManager));

            if (packageName == null)
            {
                return packageManager.Setup(mgr => mgr.GetPackageAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()));
            }
            else
            {
                return packageManager.Setup(mgr => mgr.GetPackageAsync(packageName, It.IsAny<CancellationToken>()));
            }
        }

        /// <summary>
        /// Setup default behavior for retrieving a package from the <see cref="IPackageManager"/>.
        /// </summary>
        public static IReturnsThrows<IPackageManager, Task<DependencyPath>> OnGetPackage(this Mock<IPackageManager> packageManager, Action<string> evaluate)
        {
            packageManager.ThrowIfNull(nameof(packageManager));

            return packageManager.OnGetPackage()
                .Callback<string, CancellationToken>((packageName, token) =>
                {
                    evaluate.Invoke(packageName);
                });
        }

        /// <summary>
        /// Setup default behavior for retrieving state objects from the <see cref="IStateManager"/>.
        /// </summary>
        public static ISetup<IStateManager, Task<JObject>> OnGetState(this Mock<IStateManager> stateManager, string stateId = null)
        {
            stateManager.ThrowIfNull(nameof(stateManager));

            if (stateId == null)
            {
                return stateManager.Setup(mgr => mgr.GetStateAsync(It.IsAny<string>(), It.IsAny<CancellationToken>(), It.IsAny<IAsyncPolicy>()));
            }
            else
            {
                return stateManager.Setup(mgr => mgr.GetStateAsync(stateId, It.IsAny<CancellationToken>(), It.IsAny<IAsyncPolicy>()));
            }
        }

        /// <summary>
        /// Setup default behavior for retrieving a package from the <see cref="IPackageManager"/>.
        /// </summary>
        public static ISetup<IPackageManager, Task> OnRegisterPackage(this Mock<IPackageManager> packageManager, DependencyPath package = null)
        {
            packageManager.ThrowIfNull(nameof(packageManager));

            if (package == null)
            {
                return packageManager.Setup(mgr => mgr.RegisterPackageAsync(It.IsAny<DependencyPath>(), It.IsAny<CancellationToken>()));
            }
            else
            {
                return packageManager.Setup(mgr => mgr.RegisterPackageAsync(package, It.IsAny<CancellationToken>()));
            }
        }

        /// <summary>
        /// Setup default behavior for retrieving a package from the <see cref="IPackageManager"/>.
        /// </summary>
        public static IReturnsThrows<IPackageManager, Task> OnRegisterPackage(this Mock<IPackageManager> packageManager, Action<DependencyPath> evaluate)
        {
            packageManager.ThrowIfNull(nameof(packageManager));

            return packageManager.OnRegisterPackage()
                .Callback<DependencyPath, CancellationToken>((description, token) =>
                {
                    evaluate.Invoke(description);
                });
        }

        /// <summary>
        /// Setup default behavior for retrieving state objects from the <see cref="IStateManager"/>.
        /// </summary>
        public static ISetup<IStateManager, Task> OnSaveState(this Mock<IStateManager> stateManager, string stateId = null)
        {
            stateManager.ThrowIfNull(nameof(stateManager));

            if (stateId == null)
            {
                return stateManager.Setup(mgr => mgr.SaveStateAsync(
                    It.IsAny<string>(),
                    It.IsAny<JObject>(),
                    It.IsAny<CancellationToken>(),
                    It.IsAny<IAsyncPolicy>()));
            }
            else
            {
                return stateManager.Setup(mgr => mgr.SaveStateAsync(
                    stateId,
                    It.IsAny<JObject>(),
                    It.IsAny<CancellationToken>(),
                    It.IsAny<IAsyncPolicy>()));
            }
        }

        /// <summary>
        /// Setup default behavior for retrieving state objects from the <see cref="IStateManager"/>.
        /// </summary>
        public static IReturnsThrows<IStateManager, Task> OnSaveState(this Mock<IStateManager> stateManager, Action<string, JObject> evaluate)
        {
            stateManager.ThrowIfNull(nameof(stateManager));

            return stateManager.OnSaveState()
                .Callback<string, JObject, CancellationToken, IAsyncPolicy>((stateId, state, token, retryPolicy) =>
                {
                    evaluate.Invoke(stateId, state);
                });
        }

        /// <summary>
        /// Setup default properties and behaviors for a mock <see cref="IFileInfo"/> object.
        /// </summary>
        public static Mock<IFileInfo> Setup(this Mock<IFileInfo> mockFileInfo, string filePath, bool exists = true, long length = 12345, DateTime? creationTime = null, DateTime? lastModified = null)
        {
            string directoryPath = MockFixture.GetDirectoryName(filePath);

            Mock<IDirectoryInfo> mockDirectoryInfo = new Mock<IDirectoryInfo>();
            mockDirectoryInfo.Setup(dir => dir.Name).Returns(Path.GetFileName(filePath));
            mockDirectoryInfo.Setup(dir => dir.Exists).Returns(exists);
            mockDirectoryInfo.Setup(dir => dir.FullName).Returns(directoryPath);
            mockDirectoryInfo.Setup(dir => dir.CreationTime).Returns(creationTime != null ? creationTime.Value : DateTime.Now.AddMinutes(-5));
            mockDirectoryInfo.Setup(dir => dir.CreationTimeUtc).Returns(creationTime != null ? creationTime.Value : DateTime.UtcNow.AddMinutes(-5));
            mockDirectoryInfo.Setup(dir => dir.LastAccessTime).Returns(lastModified != null ? lastModified.Value : DateTime.Now);
            mockDirectoryInfo.Setup(dir => dir.LastAccessTimeUtc).Returns(lastModified != null ? lastModified.Value : DateTime.UtcNow);
            mockDirectoryInfo.Setup(dir => dir.LastWriteTime).Returns(lastModified != null ? lastModified.Value : DateTime.Now);
            mockDirectoryInfo.Setup(dir => dir.LastWriteTimeUtc).Returns(lastModified != null ? lastModified.Value : DateTime.UtcNow);

            mockFileInfo.Setup(file => file.Name).Returns(Path.GetFileName(filePath));
            mockFileInfo.Setup(file => file.Exists).Returns(exists);
            mockFileInfo.Setup(file => file.FullName).Returns(filePath);
            mockFileInfo.Setup(file => file.Directory).Returns(mockDirectoryInfo.Object);
            mockFileInfo.Setup(file => file.DirectoryName).Returns(directoryPath);
            mockFileInfo.Setup(file => file.Extension).Returns(Path.GetExtension(filePath));
            mockFileInfo.Setup(file => file.CreationTime).Returns(creationTime != null ? creationTime.Value : DateTime.Now.AddMinutes(-5));
            mockFileInfo.Setup(file => file.CreationTimeUtc).Returns(creationTime != null ? creationTime.Value : DateTime.UtcNow.AddMinutes(-5));
            mockFileInfo.Setup(file => file.LastAccessTime).Returns(lastModified != null ? lastModified.Value : DateTime.Now);
            mockFileInfo.Setup(file => file.LastAccessTimeUtc).Returns(lastModified != null ? lastModified.Value : DateTime.UtcNow);
            mockFileInfo.Setup(file => file.LastWriteTime).Returns(lastModified != null ? lastModified.Value : DateTime.Now);
            mockFileInfo.Setup(file => file.LastWriteTimeUtc).Returns(lastModified != null ? lastModified.Value : DateTime.UtcNow);
            mockFileInfo.Setup(file => file.Length).Returns(12345);

            return mockFileInfo;
        }

        /// <summary>
        /// Setup default property values and behaviors for a mock system/OS process.
        /// </summary>
        [SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope", Justification = "Mock object support.")]
        public static Mock<IProcessProxy> Setup(
            this Mock<IProcessProxy> mockProcess,
            string command = null,
            string commandArguments = null,
            string workingDirectory = null,
            string standardOutput = null,
            string standardError = null,
            int? processId = null,
            int? exitCode = null,
            bool hasExited = true)
        {
            mockProcess.ThrowIfNull(nameof(mockProcess));

            ProcessStartInfo mockStartInfo = MockSetupExtensions.CreateMockProcessStartInfo(command, commandArguments, workingDirectory);
            
            ConcurrentBuffer standardOut = new ConcurrentBuffer();
            if (!string.IsNullOrWhiteSpace(standardOutput))
            {
                standardOut.Append(standardOutput);
            }

            ConcurrentBuffer standardErr = new ConcurrentBuffer();
            if (!string.IsNullOrWhiteSpace(standardError))
            {
                standardOut.Append(standardError);
            }

            int effectiveProcessId = processId ?? MockSetupExtensions.randomGen.Next(100, 10000000);

            mockProcess.SetupGet(p => p.ExitCode).Returns(exitCode ?? 0);
            mockProcess.SetupGet(p => p.HasExited).Returns(hasExited);
            mockProcess.SetupGet(p => p.Id).Returns(effectiveProcessId);
            mockProcess.SetupGet(p => p.StartInfo).Returns(mockStartInfo);
            mockProcess.SetupGet(p => p.Name).Returns(Path.GetFileNameWithoutExtension(mockStartInfo.FileName));
            mockProcess.SetupGet(p => p.StandardError).Returns(standardErr);
            mockProcess.SetupGet(p => p.StandardOutput).Returns(standardOut);
            mockProcess.SetupGet(p => p.StandardInput).Returns(new StreamWriter(new MemoryStream()));
            mockProcess.Setup(p => p.Start()).Returns(true);
            mockProcess.SetupGet(p => p.ProcessDetails).Returns(new ProcessDetails
            {
                Id = effectiveProcessId,
                CommandLine = $"{command} {commandArguments}",
                ExitCode = exitCode ?? 0,
                StandardError = standardError,
                StandardOutput = standardOutput,
                ToolName = Path.GetFileNameWithoutExtension(command),
                WorkingDirectory = workingDirectory
            });

            return mockProcess;
        }

        private static ProcessStartInfo CreateMockProcessStartInfo(string command = null, string commandArguments = null, string workingDirectory = null)
        {
            return new ProcessStartInfo
            {
                RedirectStandardError = true,
                RedirectStandardOutput = true,
                Arguments = commandArguments ?? "--option1=value --option2=1234",
                FileName = command ?? "SomeCommand.exe",
                UseShellExecute = false,
                WindowStyle = ProcessWindowStyle.Hidden,
                WorkingDirectory = workingDirectory ?? "./Any/Working/Directory"
            };
        }
    }
}
