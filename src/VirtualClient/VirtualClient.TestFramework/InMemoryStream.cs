// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.IO;
    using System.Linq;
    using System.Text;

    /// <summary>
    /// A mock/test I/O stream.
    /// </summary>
    public class InMemoryStream : Stream
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="InMemoryStream"/> class.
        /// </summary>
        public InMemoryStream()
        {
            this.Data = new List<byte>();
        }

        /// <summary>
        /// Event handler is invoked when bytes are written to the stream.
        /// </summary>
        public EventHandler<string> BytesWritten { get; set; }
        
        /// <summary>
        /// Contains all data/bytes written to the stream.
        /// </summary>
        public List<byte> Data { get; }

        /// <summary>
        /// True
        /// </summary>
        public override bool CanRead => true;

        /// <summary>
        /// True
        /// </summary>
        public override bool CanSeek => true;

        /// <summary>
        /// True
        /// </summary>
        public override bool CanWrite => true;

        /// <summary>
        /// The length of all bytes in the stream.
        /// </summary>
        public override long Length => this.Data.Count;

        /// <summary>
        /// Not used.
        /// </summary>
        public override long Position { get; set; }

        /// <summary>
        /// Not used.
        /// </summary>
        public override void Flush()
        {
        }

        /// <summary>
        /// Reads bytes from the stream.
        /// </summary>
        public override int Read(byte[] buffer, int offset, int count)
        {
            int bytesRead = 0;
            if (buffer != null)
            {
                for (int i = offset; i < count; i++)
                {
                    buffer[i] = this.Data[i];
                }

                bytesRead = count - offset;
            }

            return bytesRead;
        }

        /// <summary>
        /// Not used.
        /// </summary>
        public override long Seek(long offset, SeekOrigin origin)
        {
            return 0;
        }

        /// <summary>
        /// Not used.
        /// </summary>
        public override void SetLength(long value)
        {
        }

        /// <summary>
        /// Writes bytes to the stream.
        /// </summary>
        public override void Write(byte[] buffer, int offset, int count)
        {
            if (buffer != null)
            {
                for (int i = offset; i < count; i++)
                {
                    this.Data.Add(buffer[i]);
                }

                this.BytesWritten?.Invoke(this, Encoding.UTF8.GetString(buffer, offset, count));
            }
        }
    }
}
