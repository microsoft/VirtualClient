// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Contracts
{
    using System;
    using System.Globalization;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    /// <summary>
    /// Extensions to use for common scenarios in the operations
    /// of the Virtual Client.
    /// </summary>
    public static class CommonExtensions
    {
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
