// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace CRC.VirtualClient.Actions
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.IO.Abstractions;
    using System.Linq;
    using System.Net;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.Threading;
    using System.Threading.Tasks;

    internal class ParallelDownloadHandler
    {
        /// <summary>
        /// Defines an interface for handling parallel file downloads, providing access to HTTP and file system
        /// operations.
        /// </summary>
        /// <remarks>
        /// Implementations of this interface provide the necessary HTTP client and file system abstractions
        /// to support parallel downloading of files.
        /// Unit test implementations can mock this interface to simulate different download scenarios.
        /// </remarks>
        public interface IParallelDownloadHandler
        {
            public HttpClient? HttpClient { get; set; }

            public bool FileExists { get; set; }

            public long FileLength { get; set; }

            FileStream CreateFileStream(string destinationPath);
        }

        /// <summary>
        /// Downloads a file from the specified URL to the destination path with optional parallel download support.
        /// Automatically detects if the server supports range requests and switches between parallel and single-threaded download modes.
        /// </summary>
        /// <param name="url">The URL of the file to download.</param>
        /// <param name="destinationPath">The local file path where the downloaded file will be saved.</param>
        /// <param name="cancellationToken">A cancellation token to cancel the download operation.</param>
        /// <param name="parallel">The maximum number of parallel connections to use for downloading. Default is 8. Only applies if the server supports range requests.</param>
        /// <param name="timeout">The HTTP request timeout in seconds. Default is 30 seconds.</param>
        /// <param name="chunkMb">The size of each download chunk in megabytes when using parallel download. Default is 10 MB.</param>
        /// <param name="parallelDownloadHandler">An optional parallel download handler instance to use for the download. If not provided, a new HttpClient will be created.</param>
        /// <returns>A task representing the asynchronous download operation.</returns>
        public static async Task DownloadFile(
            string url, 
            string destinationPath, 
            CancellationToken cancellationToken,
            int parallel = 8,
            int timeout = 30,
            int chunkMb = 10,
            IParallelDownloadHandler? parallelDownloadHandler = null)
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

            var sw = Stopwatch.StartNew();
            try
            {
                HttpClient? httpClient = parallelDownloadHandler?.HttpClient;
                HttpClient http;
                
                if (httpClient == null)
                {
                    // ---- Build a tuned SocketsHttpHandler ----
                    var handler = new SocketsHttpHandler
                    {
                        AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate, // save bandwidth
                        MaxConnectionsPerServer = Math.Max(1, parallel), // lift per-host concurrency for ranges
                        AllowAutoRedirect = false,
                        PooledConnectionLifetime = TimeSpan.FromMinutes(5)
                    };
                    http = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(timeout) };
                }
                else
                {
                    http = httpClient;
                }

                // HEAD probe
                var headReq = new HttpRequestMessage(HttpMethod.Head, url);
                using var head = await http.SendAsync(headReq, HttpCompletionOption.ResponseHeadersRead, cts.Token);
                head.EnsureSuccessStatusCode();

                long? length = head.Content.Headers.ContentLength;
                bool supportsRanges = head.Headers.AcceptRanges.Contains("bytes");
                string serverName = head.Headers.Server?.ToString() ?? string.Empty;

                bool fileExists;
                long existingLength;

                if (parallelDownloadHandler == null)
                {
                    fileExists = File.Exists(destinationPath);
                    existingLength = fileExists ? new FileInfo(destinationPath).Length : 0;
                }
                else
                {
                    fileExists = parallelDownloadHandler.FileExists;
                    existingLength = fileExists ? parallelDownloadHandler.FileLength : 0;
                }

                // Decide parallel vs single
                if (supportsRanges && length > 0 && parallel > 1)
                {
                    int chunkSize = Math.Max(1, chunkMb) * 1024 * 1024;
                    await DownloadParallelAsync(http, url, destinationPath, length.Value, chunkSize, parallel, cts.Token);
                }
                else
                {
                    await DownloadSingleAsync(http, url, destinationPath, cts.Token, parallelDownloadHandler);
                }
            }
            catch (OperationCanceledException)
            {
                return;
            }
        }

        private static async Task DownloadSingleAsync(
            HttpClient http, 
            string url, 
            string destinationPath,
            CancellationToken ct,
            IParallelDownloadHandler? parallelDownloadHandler)
        {
            using var req = new HttpRequestMessage(HttpMethod.Get, url);

            using var resp = await http.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, ct);
            resp.EnsureSuccessStatusCode();

            var total = resp.Content.Headers.ContentLength ?? 0;
            using var src = await resp.Content.ReadAsStreamAsync(ct);

            using FileStream dst =
                parallelDownloadHandler == null ?
                new FileStream(destinationPath, FileMode.Create, FileAccess.Write, FileShare.None, 1 << 20, useAsync: true) :
                parallelDownloadHandler.CreateFileStream(destinationPath);

            var buffer = new byte[1 << 20]; // 1MB buffer
            long written = 0;
            int read;

            while ((read = await src.ReadAsync(buffer.AsMemory(0, buffer.Length), ct)) > 0)
            {
                await dst.WriteAsync(buffer.AsMemory(0, read), ct);
                written += read;
            }
        }

        private static async Task DownloadParallelAsync(
            HttpClient http, 
            string url, 
            string destinationPath,
            long totalLength, 
            int chunkSize, 
            int parallel, 
            CancellationToken ct)
        {
            var ranges = BuildRanges(totalLength, chunkSize);

            // Create temp part files
            var tempDir = Path.Combine(Path.GetDirectoryName(Path.GetFullPath(destinationPath)) !, $".{Path.GetFileName(destinationPath)}.parts");
            Directory.CreateDirectory(tempDir);

            object gate = new ();

            var throttler = new SemaphoreSlim(parallel);
            var tasks = ranges.Select(async (r, i) =>
            {
                await throttler.WaitAsync(ct);
                try
                {
                    var partPath = Path.Combine(tempDir, $"{i:D8}.part");
                    var success = await DownloadRangeWithRetryAsync(http, url, r, partPath, ct, progress: bytes => { /* in progress */ });
                    // if (!success) throw new Exception($"Failed to download range {r.start}-{r.end}");
                }
                finally 
                { 
                    throttler.Release();
                }
            });

            await Task.WhenAll(tasks);

            // wrap up
            using (var dst = new FileStream(destinationPath, FileMode.Create, FileAccess.Write, FileShare.None, 1 << 20, useAsync: true))
            {
                for (int i = 0; i < ranges.Count; i++)
                {
                    var part = Path.Combine(tempDir, $"{i:D8}.part");
                    using var src = new FileStream(part, FileMode.Open, FileAccess.Read, FileShare.Read, 1 << 20, useAsync: true);
                    await src.CopyToAsync(dst, 1 << 20, ct);
                }
            }

            Directory.Delete(tempDir, recursive: true);
        }

        private static async Task<bool> DownloadRangeWithRetryAsync(
            HttpClient http, 
            string url, 
            (long start, long end) range,
            string partPath, 
            CancellationToken ct, 
            Action<long>? progress)
        {
            const int maxAttempts = 5;
            var rng = new Random();

            for (int attempt = 1; attempt <= maxAttempts; attempt++)
            {
                try
                {
                    using var req = new HttpRequestMessage(HttpMethod.Get, url);
                    req.Headers.Range = new RangeHeaderValue(range.start, range.end);

                    using var resp = await http.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, ct);
                    resp.EnsureSuccessStatusCode();

                    using var src = await resp.Content.ReadAsStreamAsync(ct);
                    using var dst = new FileStream(partPath, FileMode.Create, FileAccess.Write, FileShare.None, 1 << 20, useAsync: true);

                    var buffer = new byte[1 << 20];
                    int read;
                    long written = 0;
                    while ((read = await src.ReadAsync(buffer.AsMemory(0, buffer.Length), ct)) > 0)
                    {
                        await dst.WriteAsync(buffer.AsMemory(0, read), ct);
                        written += read;
                        progress?.Invoke(read);
                    }

                    return true;
                }
                catch when (attempt < maxAttempts)
                {
                    // jittered exponential backoff
                    var delayMs = (int)Math.Min(30000, (Math.Pow(2, attempt) * 250) + rng.Next(0, 250));
                    await Task.Delay(delayMs, ct);
                }
            }

            return false;
        }

        private static List<(long start, long end)> BuildRanges(long total, int size)
        {
            var ranges = new List<(long, long)>();
            for (long start = 0; start < total; start += size)
            {
                long end = Math.Min(total - 1, start + size - 1);
                ranges.Add((start, end));
            }

            return ranges;
        }
    }
}
