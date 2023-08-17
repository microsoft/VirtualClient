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
        /////// <summary>
        /////// Creates a descriptor that represents the path location and content information for a file 
        /////// to upload to a blob store.
        /////// </summary>
        /////// <param name="component">The component that produced the file.</param>
        /////// <param name="file">The file to be uploaded.</param>
        /////// <param name="toolname">The name of the component/toolset that produced the file (e.g. GeekbenchExecutor, Geekbench5).</param>
        /////// <param name="contentType">The type of content (e.g. application/octet-stream).</param>
        /////// <param name="contentEncoding">The web encoding name for the contents of the file (e.g. utf-8).</param>
        /////// <param name="fileTimestamp">A timestamp to include in the file name (e.g. 2023-05-21t09-23-30-23813z-file.log).</param>
        /////// <param name="manifest">Information and metadata related to the blob/file.</param>
        ////FileUploadDescriptor CreateDescriptor(VirtualClientComponent component, IFileInfo file, string toolname, string contentType, string contentEncoding,  DateTime? fileTimestamp = null, IDictionary<string, IConvertible> manifest = null);

        /// <summary>
        /// Creates a descriptor that represents the path location and content information for a file 
        /// to upload to a blob store.
        /// </summary>
        /// <param name="fileContext">Provides context about a file to be uploaded.</param>
        /// <param name="contentPathPattern">Content path template to use when uploading content to target storage resources.</param>
        /// <param name="parameters">Parameters related to the component that produced the file (e.g. the parameters from the component).</param>
        /// <param name="manifest">Additional information and metadata related to the blob/file to include in the descriptor alongside the default manifest information.</param>
        /// <param name="timestamped">
        /// True to to include the file creation time in the file name (e.g. 2023-05-21t09-23-30-23813z-file.log). This is explicit to allow for cases where modification of the 
        /// file name is not desirable. Default = true (timestamped file names).
        /// </param>
        FileUploadDescriptor CreateDescriptor(FileContext fileContext, string contentPathPattern, IDictionary<string, IConvertible> parameters = null, IDictionary<string, IConvertible> manifest = null, bool timestamped = true);
    }
}