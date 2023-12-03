namespace VirtualClient
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.IO;
    using System.Linq;
    using Renci.SshNet;
    using VirtualClient.Common;

    /// <summary>
    /// Represents a fake sftp client.
    /// </summary>
    [SuppressMessage("Design", "CA1063:Implement IDisposable Correctly", Justification = "This is a test/mock class with no real resources.")]
    public class InMemorySftpClient : Dictionary<string, IConvertible>, ISftpClientProxy
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="InMemorySftpClient"/>
        /// </summary>
        public InMemorySftpClient()
        {
            this.UploadPaths = new List<string>();
            this.DownloadPaths = new List<string>();
        }

        /// <inheritdoc />
        public ConnectionInfo ConnectionInfo { get; set; }

        /// <summary>
        /// Delegate allows user/test to define the logic to execute when the 
        /// 'Dispose' method is called.
        /// </summary>
        public Action OnDispose { get; set; }

        /// <summary>
        /// Delegate allows user/test to define the logic to execute when the 
        /// 'Connect' property is called.
        /// </summary>
        public Action OnConnect { get; set; }

        /// <summary>
        /// Delegate allows user/test to define the logic to execute when the 
        /// 'Disconnect' method is called.
        /// </summary>
        public Action OnDisconnect { get; set; }

        /// <summary>
        /// Delegate allows user/test to define the logic to execute when the 
        /// 'UploadFile' method is called.
        /// </summary>
        public Action<Stream, string> OnUploadFile { get; set; }

        /// <summary>
        /// Delegate allows user/test to define the logic to execute when the 
        /// 'DownloadFile' method is called.
        /// </summary>
        public Action<string, Stream> OnDownloadFile { get; set; }

        /// <summary>
        /// The set of upload paths by Sftp Client.
        /// </summary>
        public IEnumerable<string> UploadPaths { get; }

        /// <summary>
        /// The set of download paths by Sftp Client.
        /// </summary>
        public IEnumerable<string> DownloadPaths { get; }

        /// <inheritdoc />
        public void Connect()
        {
            if (this.OnConnect != null)
            {
                this.OnConnect?.Invoke();
            }
        }

        /// <inheritdoc />
        public void UploadFile(Stream input, string path)
        {
            if (this.OnUploadFile != null)
            {
                this.OnUploadFile?.Invoke(input, path);
            }

            this.UploadPaths.Append(path);
        }

        /// <inheritdoc />
        public void DownloadFile(string path, Stream output)
        {
            if (this.OnDownloadFile != null)
            {
                this.OnDownloadFile?.Invoke(path, output);
            }

            this.DownloadPaths?.Append(path);
        }

        /// <inheritdoc />
        public void Disconnect()
        {
            if (this.OnDisconnect != null)
            {
                this.OnDisconnect?.Invoke();
            }
        }

        /// <summary>
        /// Dispose of resources.
        /// </summary>
        [SuppressMessage("Design", "CA1063:Implement IDisposable Correctly", Justification = "This is a test/mock class with no real resources.")]
        public void Dispose()
        {
            this.OnDispose?.Invoke();
        }
    }
}
