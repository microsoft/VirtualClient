// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient
{
    using System;
    using System.Collections.Generic;
    using System.IO.Abstractions;
    using System.Linq;
    using System.Net;
    using System.Net.Http;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.DependencyInjection;
    using Moq;
    using Moq.Language;
    using Moq.Language.Flow;
    using Newtonsoft.Json.Linq;
    using Polly;
    using VirtualClient.Common.Contracts;
    using VirtualClient.Common.Extensions;
    using VirtualClient.Contracts;

    /// <summary>
    /// Extension methods for test fixture instances.
    /// </summary>
    public static class MockSetupExtensions
    {
        /// <summary>
        /// Removes all instances of services that match the type defined from the dependencies
        /// collectin.
        /// </summary>
        /// <typeparam name="TService">The data type of the services to remove.</typeparam>
        /// <param name="dependencies">The service dependency collection from which the services will be removed.</param>
        public static IServiceCollection RemoveAll<TService>(this IServiceCollection dependencies)
        {
            dependencies.ThrowIfNull(nameof(dependencies));

            List<ServiceDescriptor> descriptorsToRemove = new List<ServiceDescriptor>();
            Type targetType = typeof(TService);
            for (int i = 0; i < dependencies.Count; i++)
            {
                ServiceDescriptor descriptor = dependencies[i];
                if (descriptor.ServiceType == targetType || descriptor.ServiceType.GetInterfaces().Contains(targetType))
                {
                    descriptorsToRemove.Add(descriptor);
                }
            }

            descriptorsToRemove.ForEach(descriptor => dependencies.Remove(descriptor));

            return dependencies;
        }

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
    }
}
