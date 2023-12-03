namespace VirtualClient.Common
{
    using System;
    using System.IO;
    using Renci.SshNet;
    using VirtualClient.Common.Extensions;

    /// <summary>
    /// Acts as a limited proxy to provide information about a SFTP Client.
    /// </summary>
    public class SftpClientProxy : ISftpClientProxy
    {
        private bool disposed;

        /// <summary>
        /// Initializes a new instance of the <see cref="SftpClientProxy"/> class.
        /// </summary>
        public SftpClientProxy(SftpClient sftpClient)
        {
            sftpClient.ThrowIfNull(nameof(sftpClient));
            this.UnderlyingSftpClient = sftpClient;
        }

        /// <inheritdoc />
        public virtual ConnectionInfo ConnectionInfo => this.UnderlyingSftpClient.ConnectionInfo;

        /// <summary>
        /// Gets the underlying SFTP client itself.
        /// </summary>
        protected SftpClient UnderlyingSftpClient { get; }

        /// <inheritdoc />
        public void Connect()
        {
            this.UnderlyingSftpClient.Connect();
        }

        /// <inheritdoc />
        public void UploadFile(Stream input, string remotePath)
        {
            this.UnderlyingSftpClient.UploadFile(input, remotePath);
        }

        /// <inheritdoc />
        public void DownloadFile(string remoteFilePath, Stream output) 
        {
            this.UnderlyingSftpClient.DownloadFile(remoteFilePath, output);
        }

        /// <inheritdoc />
        public void Disconnect()
        {
            this.UnderlyingSftpClient.Disconnect();
        }

        /// <inheritdoc />
        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Disposes of resources used by the proxy.
        /// </summary>
        /// <param name="disposing">True to dispose of unmanaged resources.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!this.disposed)
            {
                if (disposing)
                {
                    this.UnderlyingSftpClient.Dispose();
                }

                this.disposed = true;
            }
        }
    }
}
