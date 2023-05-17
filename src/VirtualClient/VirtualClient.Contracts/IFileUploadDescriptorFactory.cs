namespace VirtualClient.Contracts
{
    using System;
    using System.Collections.Generic;
    using System.IO.Abstractions;

    /// <summary>
    /// Provides features for defining descriptors associated with file uploads.
    /// </summary>
    public interface IFileUploadDescriptorFactory
    {
        /// <summary>
        /// Creates a descriptor that represents the path location and content information for a file 
        /// to upload to a blob store.
        /// </summary>
        /// <param name="component">The component that produced the file.</param>
        /// <param name="file">The file to be uploaded.</param>
        /// <param name="contentType">The type of content (e.g. application/octet-stream).</param>
        /// <param name="contentEncoding">The web encoding name for the contents of the file (e.g. utf-8).</param>
        /// <param name="toolname">The name of the component/toolset that produced the file (e.g. GeekbenchExecutor, Geekbench5).</param>
        /// <param name="fileTimestamp">A timestamp to include in the file name (e.g. 2023-05-21t09-23-30-23813z-file.log).</param>
        /// <param name="manifest">Information and metadata related to the blob/file.</param>
        FileUploadDescriptor CreateDescriptor(VirtualClientComponent component, IFileInfo file, string contentType, string contentEncoding, string toolname = null, DateTime? fileTimestamp = null, IDictionary<string, IConvertible> manifest = null);
    }
}