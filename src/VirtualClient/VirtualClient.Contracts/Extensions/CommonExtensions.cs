// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Contracts.Extensions
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using VirtualClient.Common.Extensions;

    /// <summary>
    /// Extensions to use for common scenarios in the operations
    /// of the Virtual Client.
    /// </summary>
    public static class CommonExtensions
    {
        /// <summary>
        /// Writes the lines of content to the file specified surrounded by section comment markers or replaces existing content.
        /// </summary>
        /// <param name="originalContent">The original content within which the marked section content will be replaced.</param>
        /// <param name="newContent">The lines of text/content to use for replacing the original marked content.</param>
        /// <param name="sectionBeginMarker">A comment/marker to use to designate the beginning of the section.</param>
        /// <param name="sectionEndMarker">A comment/marker to use to designate the end of the section.</param>
        public static IEnumerable<string> AddOrReplaceSectionContentAsync(this IEnumerable<string> originalContent, IEnumerable<string> newContent, string sectionBeginMarker, string sectionEndMarker)
        {
            originalContent.ThrowIfNullOrEmpty(nameof(newContent));
            newContent.ThrowIfNullOrEmpty(nameof(newContent));
            sectionBeginMarker.ThrowIfNullOrWhiteSpace(nameof(sectionBeginMarker));
            sectionEndMarker.ThrowIfNullOrWhiteSpace(nameof(sectionEndMarker));

            List<string> cleanedContent = new List<string>();

            if (originalContent?.Any() == true)
            {
                int sectionBeginLine = -1;
                int sectionEndLine = -1;
                int contentLength = originalContent.Count();
                for (int lineNum = 0; lineNum < contentLength; lineNum++)
                {
                    if (string.Equals(originalContent.ElementAt(lineNum), sectionBeginMarker, StringComparison.OrdinalIgnoreCase))
                    {
                        sectionBeginLine = lineNum;
                    }
                    else if (string.Equals(originalContent.ElementAt(lineNum), sectionEndMarker, StringComparison.OrdinalIgnoreCase))
                    {
                        sectionEndLine = lineNum;
                    }
                }

                if (sectionBeginLine >= 0 && sectionEndLine >= 0)
                {
                    // Keep only the content that is not VC-specific settings.
                    cleanedContent.AddRange(originalContent.Take(sectionBeginLine));
                    if (sectionEndLine < contentLength - 1)
                    {
                        cleanedContent.AddRange(originalContent.Skip(sectionEndLine + 1));
                    }
                }
                else
                {
                    // There are no VC settings within the content. Leave it as is.
                    cleanedContent.AddRange(originalContent);
                }
            }

            // Add the new sectioned content in at the end.
            cleanedContent.Add(sectionBeginMarker);
            cleanedContent.AddRange(newContent);
            cleanedContent.Add(sectionEndMarker);

            return cleanedContent;
        }

        /// <summary>
        /// Throws an exception if any one of the runtime tasks are in a faulted/errored state.
        /// </summary>
        public static void ThrowIfErrored(this Task runtimeTask)
        {
            if (runtimeTask.IsFaulted && runtimeTask.Exception != null)
            {
                if (runtimeTask.Exception.InnerExceptions?.Any() == true)
                {
                    throw runtimeTask.Exception.InnerExceptions.First();
                }

                throw runtimeTask.Exception;
            }
        }

        /// <summary>
        /// Formats the information in the exception into a display-friendly representation.
        /// </summary>
        /// <param name="exc">The exception to format.</param>
        /// <param name="withErrorType">True if the data type of the error(s) should be included.</param>
        /// <param name="withCallStack">True if the error callstack should be included.</param>
        /// <returns>
        /// A string representation of the exception in a display-friendly format.
        /// </returns>
        public static string ToDisplayFriendlyString(this Exception exc, bool withErrorType = true, bool withCallStack = false)
        {
            StringBuilder combinedErrorMessage = new StringBuilder(Environment.NewLine);
            Exception currentExc = exc;

            int errorCount = 0;
            while (currentExc != null)
            {
                if (!string.IsNullOrWhiteSpace(currentExc.Message))
                {
                    errorCount++;
                    if (errorCount > 1)
                    {
                        combinedErrorMessage.Append(" ");
                    }

                    if (withErrorType)
                    {
                        combinedErrorMessage.AppendFormat(
                           CultureInfo.InvariantCulture,
                           "[{0}] {1}",
                           currentExc.GetType().FullName,
                           currentExc.Message.Trim());
                    }
                    else
                    {
                        combinedErrorMessage.Append(currentExc.Message.Trim());
                    }

                    if (combinedErrorMessage[combinedErrorMessage.Length - 1] != '.')
                    {
                        combinedErrorMessage.Append(".");
                    }
                }

                currentExc = currentExc.InnerException;
            }

            string errorMessage = combinedErrorMessage.ToString();
            if (withCallStack)
            {
                errorMessage = $"{errorMessage}{Environment.NewLine}{Environment.NewLine}{exc.StackTrace}{Environment.NewLine}";
            }

            return errorMessage;
        }
    }
}
