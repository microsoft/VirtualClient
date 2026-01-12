// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Actions
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Net;
    using System.Net.Http;
    using System.Reflection;
    using System.Threading;
    using System.Threading.Tasks;
    using CRC.VirtualClient.Actions;
    using Moq;
    using Moq.Protected;
    using NUnit.Framework;

    [TestFixture]
    [Category("Unit")]
    public class ParallelDownloadHandlerTests : MockFixture
    {
        private MockParallelDownloadHandler mockParallelDownloadHandler;
        private Mock<HttpMessageHandler> mockHttpMessageHandler;
        private string testUrl;
        private string testDestinationPath;

        [SetUp]
        public void SetupTest()
        {
            this.Setup(PlatformID.Unix);

            this.testUrl = "https://example.com/testfile.zip";
            
            string tempPath = Path.GetTempPath();

            this.testDestinationPath = this.Combine(tempPath, "testfile.zip");

            this.mockHttpMessageHandler = new Mock<HttpMessageHandler>();

            this.mockParallelDownloadHandler = new MockParallelDownloadHandler()
            {
                HttpClient = new HttpClient(this.mockHttpMessageHandler.Object),
                FileExists = false,
                FileLength = 0
            };
            
            this.File.Reset();
            this.File.Setup(f => f.Exists(It.IsAny<string>())).Returns(false);
            
            this.Directory.Setup(d => d.Exists(It.IsAny<string>())).Returns(true);
            this.Directory.Setup(d => d.CreateDirectory(It.IsAny<string>())).Returns(this.DirectoryInfo.Object);
            this.Directory.Setup(d => d.Delete(It.IsAny<string>(), It.IsAny<bool>()));

            this.FileSystem.SetupGet(fs => fs.File).Returns(this.File.Object);
            this.FileSystem.SetupGet(fs => fs.Directory).Returns(this.Directory.Object);
        }

        [Test]
        public async Task DownloadFile_PerformsSingleThreadedDownload_WhenServerDoesNotSupportRangeRequests()
        {
            long fileSize = 1024 * 1024; // 1 MB
            byte[] fileContent = new byte[fileSize];
            new Random().NextBytes(fileContent);

            // Setup HEAD response - no range support
            var headResponse = new HttpResponseMessage(HttpStatusCode.OK);
            headResponse.Content = new ByteArrayContent(Array.Empty<byte>());
            headResponse.Content.Headers.ContentLength = fileSize;
            headResponse.Headers.AcceptRanges.Clear(); // No range support

            // Setup GET response
            var getResponse = new HttpResponseMessage(HttpStatusCode.OK);
            getResponse.Content = new ByteArrayContent(fileContent);
            getResponse.Content.Headers.ContentLength = fileSize;

            int requestCount = 0;
            this.mockHttpMessageHandler.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync((HttpRequestMessage request, CancellationToken token) =>
                {
                    if (request.Method == HttpMethod.Head)
                    {
                        return headResponse;
                    }
                    
                    requestCount++;
                    return getResponse;
                });

            MemoryStream downloadedStream = new MemoryStream();
            this.FileStream.Setup(fs => fs.New(It.IsAny<string>(), It.IsAny<FileMode>(), It.IsAny<FileAccess>(), It.IsAny<FileShare>(), It.IsAny<int>(), It.IsAny<bool>()))
                .Returns((string path, FileMode mode, FileAccess access, FileShare share, int bufferSize, bool useAsync) =>
                {
                    var stream = new ESMockFileSystemStream();
                    return stream;
                });

            this.mockParallelDownloadHandler.FileExists = true;
            this.mockParallelDownloadHandler.FileLength = fileSize;

            await ParallelDownloadHandler.DownloadFile(
                this.testUrl,
                this.testDestinationPath,
                CancellationToken.None,
                parallel: 8,
                timeout: 30,
                chunkMb: 10,
                parallelDownloadHandler: this.mockParallelDownloadHandler);

            // Should use single-threaded download (1 GET request)
            Assert.AreEqual(1, requestCount);
        }

        [Test]
        public async Task DownloadFile_PerformsParallelDownload_WhenServerSupportsRangeRequests()
        {
            long fileSize = 10 * 1024 * 1024; // 10 MB
            byte[] fileContent = new byte[fileSize];
            new Random().NextBytes(fileContent);

            // Setup HEAD response with range support
            var headResponse = new HttpResponseMessage(HttpStatusCode.OK);
            headResponse.Content = new ByteArrayContent(Array.Empty<byte>());
            headResponse.Content.Headers.ContentLength = fileSize;
            headResponse.Headers.AcceptRanges.Add("bytes");

            var rangeRequests = new List<string>();
            this.mockHttpMessageHandler.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync((HttpRequestMessage request, CancellationToken token) =>
                {
                    if (request.Method == HttpMethod.Head)
                    {
                        return headResponse;
                    }

                    // Track range requests
                    if (request.Headers.Range != null)
                    {
                        rangeRequests.Add(request.Headers.Range.ToString());
                        
                        var range = request.Headers.Range.Ranges.GetEnumerator();
                        range.MoveNext();
                        long start = range.Current.From.Value;
                        long end = range.Current.To.Value;
                        int length = (int)(end - start + 1);

                        var response = new HttpResponseMessage(HttpStatusCode.PartialContent);
                        response.Content = new ByteArrayContent(fileContent, (int)start, length);
                        return response;
                    }

                    return new HttpResponseMessage(HttpStatusCode.BadRequest);
                });

            var tempDir = this.Combine(this.GetPackagePath(), $".testfile.zip.parts");
            this.Directory.Setup(d => d.Exists(tempDir)).Returns(true);

            var partFiles = new Dictionary<string, ESMockFileSystemStream>();
            this.FileStream.Setup(fs => fs.New(It.IsAny<string>(), It.IsAny<FileMode>(), It.IsAny<FileAccess>(), It.IsAny<FileShare>(), It.IsAny<int>(), It.IsAny<bool>()))
                .Returns((string path, FileMode mode, FileAccess access, FileShare share, int bufferSize, bool useAsync) =>
                {
                    if (path.Contains(".part"))
                    {
                        var stream = new ESMockFileSystemStream();
                        partFiles[path] = stream;
                        return stream;
                    }
                    return new ESMockFileSystemStream();
                });

            this.mockParallelDownloadHandler.FileExists = true;
            this.mockParallelDownloadHandler.FileLength = fileSize;

            await ParallelDownloadHandler.DownloadFile(
                this.testUrl,
                this.testDestinationPath,
                CancellationToken.None,
                parallel: 2,
                timeout: 30,
                chunkMb: 5,
                parallelDownloadHandler: this.mockParallelDownloadHandler);

            // Should have made multiple range requests
            Assert.Greater(rangeRequests.Count, 1);
        }

        [Test]
        public async Task DownloadFile_HandlesCancellation_Gracefully()
        {
            var headResponse = new HttpResponseMessage(HttpStatusCode.OK);
            headResponse.Content = new ByteArrayContent(Array.Empty<byte>());
            headResponse.Content.Headers.ContentLength = 1024;
            headResponse.Headers.AcceptRanges.Clear();

            this.mockHttpMessageHandler.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(headResponse);

            using (var cts = new CancellationTokenSource())
            {
                cts.Cancel();

                // Should not throw, just return
                await ParallelDownloadHandler.DownloadFile(
                    this.testUrl,
                    this.testDestinationPath,
                    cts.Token,
                    parallel: 8,
                    timeout: 30,
                    chunkMb: 10,
                    parallelDownloadHandler: this.mockParallelDownloadHandler);

                Assert.Pass("Cancellation handled gracefully");
            }
        }

        [Test]
        public async Task DownloadFile_UsesDefaultParameters_WhenNotSpecified()
        {
            long fileSize = 1024;
            var headResponse = new HttpResponseMessage(HttpStatusCode.OK);
            headResponse.Content = new ByteArrayContent(Array.Empty<byte>());
            headResponse.Content.Headers.ContentLength = fileSize;
            headResponse.Headers.AcceptRanges.Clear();

            var getResponse = new HttpResponseMessage(HttpStatusCode.OK);
            getResponse.Content = new ByteArrayContent(new byte[fileSize]);

            this.mockHttpMessageHandler.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync((HttpRequestMessage request, CancellationToken token) =>
                {
                    return request.Method == HttpMethod.Head ? headResponse : getResponse;
                });

            this.FileStream.Setup(fs => fs.New(It.IsAny<string>(), It.IsAny<FileMode>(), It.IsAny<FileAccess>(), It.IsAny<FileShare>(), It.IsAny<int>(), It.IsAny<bool>()))
                .Returns((string path, FileMode mode, FileAccess access, FileShare share, int bufferSize, bool useAsync) =>
                {
                    var stream = new ESMockFileSystemStream();
                    return stream;
                });

            this.mockParallelDownloadHandler.FileExists = true;
            this.mockParallelDownloadHandler.FileLength = fileSize;

            // Should use default parameters: parallel=8, timeout=30, chunkMb=10
            await ParallelDownloadHandler.DownloadFile(
                this.testUrl,
                this.testDestinationPath,
                CancellationToken.None,
                parallelDownloadHandler: this.mockParallelDownloadHandler);
            Assert.Pass("Default parameters used successfully");
        }

        [Test]
        public async Task DownloadFile_FallsBackToSingleThreaded_WhenContentLengthIsUnknown()
        {
            // Setup HEAD response with no content length
            var headResponse = new HttpResponseMessage(HttpStatusCode.OK);
            headResponse.Content = new ByteArrayContent(Array.Empty<byte>());
            headResponse.Headers.AcceptRanges.Add("bytes");

            var getResponse = new HttpResponseMessage(HttpStatusCode.OK);
            byte[] content = new byte[1024];
            getResponse.Content = new ByteArrayContent(content);

            int getRequestCount = 0;
            this.mockHttpMessageHandler.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync((HttpRequestMessage request, CancellationToken token) =>
                {
                    if (request.Method == HttpMethod.Head)
                    {
                        return headResponse;
                    }
                    
                    getRequestCount++;
                    return getResponse;
                });

            this.FileStream.Setup(fs => fs.New(It.IsAny<string>(), It.IsAny<FileMode>(), It.IsAny<FileAccess>(), It.IsAny<FileShare>(), It.IsAny<int>(), It.IsAny<bool>()))
                .Returns((string path, FileMode mode, FileAccess access, FileShare share, int bufferSize, bool useAsync) =>
                {
                    var stream = new ESMockFileSystemStream();
                    return stream;
                });

            this.mockParallelDownloadHandler.FileExists = true;

            await ParallelDownloadHandler.DownloadFile(
                this.testUrl,
                this.testDestinationPath,
                CancellationToken.None,
                parallel: 8,
                parallelDownloadHandler: this.mockParallelDownloadHandler);

            // Should fall back to single-threaded download
            Assert.AreEqual(1, getRequestCount);
        }

        [Test]
        public async Task DownloadFile_FallsBackToSingleThreaded_WhenParallelIsOne()
        {
            long fileSize = 10 * 1024 * 1024;
            var headResponse = new HttpResponseMessage(HttpStatusCode.OK);
            headResponse.Content = new ByteArrayContent(Array.Empty<byte>());
            headResponse.Content.Headers.ContentLength = fileSize;
            headResponse.Headers.AcceptRanges.Add("bytes");

            var getResponse = new HttpResponseMessage(HttpStatusCode.OK);
            getResponse.Content = new ByteArrayContent(new byte[fileSize]);

            int getRequestCount = 0;
            this.mockHttpMessageHandler.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync((HttpRequestMessage request, CancellationToken token) =>
                {
                    if (request.Method == HttpMethod.Head)
                    {
                        return headResponse;
                    }
                    
                    getRequestCount++;
                    return getResponse;
                });

            this.FileStream.Setup(fs => fs.New(It.IsAny<string>(), It.IsAny<FileMode>(), It.IsAny<FileAccess>(), It.IsAny<FileShare>(), It.IsAny<int>(), It.IsAny<bool>()))
                .Returns((string path, FileMode mode, FileAccess access, FileShare share, int bufferSize, bool useAsync) =>
                {
                    var stream = new ESMockFileSystemStream();
                    return stream;
                });

            this.mockParallelDownloadHandler.FileExists = true;
            this.mockParallelDownloadHandler.FileLength = fileSize;

            await ParallelDownloadHandler.DownloadFile(
                this.testUrl,
                this.testDestinationPath,
                CancellationToken.None,
                parallel: 1,
                parallelDownloadHandler: this.mockParallelDownloadHandler);

            // Should use single-threaded download even though server supports ranges
            Assert.AreEqual(1, getRequestCount);
        }

        [Test]
        public void DownloadFile_ThrowsHttpRequestException_WhenHeadRequestFails()
        {
            this.mockHttpMessageHandler.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.NotFound));

            Assert.ThrowsAsync<HttpRequestException>(async () =>
            {
                await ParallelDownloadHandler.DownloadFile(
                    this.testUrl,
                    this.testDestinationPath,
                    CancellationToken.None);
            });
        }

        /// <summary>
        /// Mock file system stream for Elasticsearch Rally download tests.
        /// </summary>
        private class ESMockFileSystemStream : InMemoryFileSystemStream
        {
            /// <summary>
            /// Initializes a new instance of the <see cref="ESMockFileSystemStream"/> class.
            /// </summary>
            public ESMockFileSystemStream()
                : base()
            {
            }

            /// <summary>
            /// Initializes a new instance of the <see cref="ESMockFileSystemStream"/> class.
            /// </summary>
            /// <param name="stream">The underlying stream.</param>
            /// <param name="path">The file path.</param>
            /// <param name="isAsync">Whether the stream is asynchronous.</param>
            public ESMockFileSystemStream(Stream stream, string path, bool isAsync)
                : base(stream, path, isAsync)
            {
            }
        }

        private class MockParallelDownloadHandler : ParallelDownloadHandler.IParallelDownloadHandler
        {
            public HttpClient? HttpClient { get; set; }

            public bool FileExists { get; set; }

            public long FileLength { get; set; }

            public FileStream CreateFileStream(string destinationPath)
            {
                return new MockFileStream();
            }
        }

        private class MockFileStream : FileStream
        {
            public MockFileStream()
                : base("mockfile.txt", FileMode.Create)
            {
            }
        }
    }
}