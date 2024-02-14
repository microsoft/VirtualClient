// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Common.Extensions
{
    using System;
    using System.Globalization;
    using System.Text;

    /// <summary>
    /// Extension methods for <see cref="Exception"/> class objects.
    /// </summary>
    public static class ExceptionExtensions
    {
        /// <summary>
        /// Combines all of the error messages (including inner exceptions) into
        /// a single error message.
        /// </summary>
        /// <param name="exc">The exception containing the error messages to combine</param>
        /// <param name="withCallStack">True to include the callstack, false otherwise.</param>
        /// <param name="withErrorTypes">True if the error data type should be included with each individual exception error message
        /// (ex:  [System.ArgumentException]Argument 'source' is required.)</param>
        /// <returns>The combined message.</returns>
        /// <exception cref="ArgumentNullException">The exception is null.</exception>
        public static string ToString(this Exception exc, bool withCallStack = true, bool withErrorTypes = false)
        {
            if (exc == null)
            {
                throw new ArgumentNullException(nameof(exc));
            }

            StringBuilder combinedErrorMessage = new StringBuilder();
            Exception currentExc = exc;

            while (currentExc != null)
            {
                if (!string.IsNullOrWhiteSpace(currentExc.Message))
                {
                    ExceptionExtensions.AppendPeriodToMessageIfMissing(combinedErrorMessage);
                    ExceptionExtensions.AppendExceptionInfo(combinedErrorMessage, currentExc, withErrorTypes);
                }

                currentExc = currentExc.InnerException;
            }

            string errorMessage = combinedErrorMessage.ToString();
            if (withCallStack)
            {
                errorMessage = $"{errorMessage};;;{exc.StackTrace}";
            }

            return errorMessage;
        }

        private static void AppendExceptionInfo(StringBuilder messageBuilder, Exception exc, bool includeErrorTypes)
        {
            if (messageBuilder.Length > 0)
            {
                messageBuilder.Append(";;;");
            }

            if (includeErrorTypes)
            {
                messageBuilder.AppendFormat(
                    CultureInfo.InvariantCulture,
                    "[{0}]{1}",
                    exc.GetType().FullName,
                    exc.Message.Trim());
            }
            else
            {
                messageBuilder.AppendFormat(
                    CultureInfo.InvariantCulture,
                    "{0}",
                    exc.Message.Trim());
            }
        }

        private static void AppendPeriodToMessageIfMissing(StringBuilder messageBuilder)
        {
            if (messageBuilder.Length > 0)
            {
                if (messageBuilder[messageBuilder.Length - 1] != '.')
                {
                    messageBuilder.Append(".");
                }
            }
        }
    }
}