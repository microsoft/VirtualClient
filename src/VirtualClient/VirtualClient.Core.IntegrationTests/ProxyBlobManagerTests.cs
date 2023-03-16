// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using NUnit.Framework;
    using VirtualClient.Common.Rest;
    using VirtualClient.Contracts;
    using VirtualClient.Contracts.Proxy;
    using VirtualClient.Proxy;

    [TestFixture]
    [Category("Integration")]
    internal class ProxyBlobManagerTests
    {
        private static Uri proxyApiUri;
        private static DependencyProxyStore packageStore;

        [OneTimeSetUp]
        public void SetupFixture()
        {
            // Note:
            // The API URI must be updated to point at an appropriate Proxy API endpoint.
            ProxyBlobManagerTests.proxyApiUri = new Uri("https://proxyagent.azurewebsites.net/api/blobs");
            ProxyBlobManagerTests.packageStore = new DependencyProxyStore("Packages", ProxyBlobManagerTests.proxyApiUri);
        }


        [Test]
        [TestCase("CSIToolkit.4.8.1.zip")]
        public async Task ProxyBlobManagerCanHandleLargeFileDownloads(string largePackageName)
        {
            // string downloadDirectory = Path.Combine(MockFixture.TestAssemblyDirectory, "downloads");
            string downloadDirectory = @"S:\\Downloads";
            string downloadPath = Path.Combine(downloadDirectory, largePackageName);

            try
            {
                // WARNING:
                // The packages we are downloading are very big (1+ GB in size). Be careful to ensure you have
                // enough drive space. It is recommended that you delete the downloaded content afterwards as well.
                IRestClient restClient = new RestClientBuilder(TimeSpan.FromMinutes(10))
                   .AlwaysTrustServerCertificate()
                   .AddAuthorizationHeader("Hql8Q~nCUoSXQGV2.ZWB3V3QDnW-3SfdcmUDbaS1", "ApiKey")
                   .Build();

                VirtualClientProxyApiClient apiClient = new VirtualClientProxyApiClient(restClient, proxyApiUri);
                apiClient.ChunkSize = 1024 * 1024 * 100;

                // VirtualClientProxyApiClient apiClient = DependencyFactory.CreateVirtualClientProxyApiClient(ProxyBlobManagerTests.proxyApiUri, TimeSpan.FromHours(2));
                ProxyBlobManager blobManager = new Proxy.ProxyBlobManager(ProxyBlobManagerTests.packageStore, apiClient);

                BlobDescriptor descriptor = new BlobDescriptor
                {
                    Name = largePackageName,
                    ContainerName = "packages",
                    ContentEncoding = Encoding.UTF8,
                    ContentType = "application/octet-stream"
                };

                if (!Directory.Exists(downloadDirectory))
                {
                    Directory.CreateDirectory(downloadDirectory);
                }

                File.Delete(downloadPath);

                using (FileStream stream = new FileStream(downloadPath, FileMode.Create, FileAccess.ReadWrite, FileShare.ReadWrite))
                {
                    await blobManager.DownloadBlobAsync(descriptor, stream, CancellationToken.None)
                        .ConfigureAwait(false);
                }
            }
            catch
            {
                throw;
            }
            finally
            {
                File.Delete(downloadPath);
            }
        }
    }
}
