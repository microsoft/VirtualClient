// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Common
{
    using System;
    using System.Linq;
    using System.Text;
    using System.Text.RegularExpressions;
    using VirtualClient.Common.Extensions;

    /// <summary>
    /// Extension methods for <see cref="ProcessExecutionResult"/> objects.
    /// </summary>
    public static class ProcessExecutionResultExtensions
    {
        /// <summary>
        /// Returns True if the exit code of the <see cref="ProcessExecutionResult"/> is non-zero
        /// or if there is any standard error present. Returns False otherwise.
        /// </summary>
        /// <param name="result">True/False if the <see cref="ProcessExecutionResult"/> signals an error.</param>
        public static bool IsErrored(this ProcessExecutionResult result)
        {
            result.ThrowIfNull(nameof(result));
            return (result.Error?.Any() == true || (result?.ExitCode ?? 0) != 0);
        }

        /// <summary>
        /// Returns true/false whether the output of the process execution contains the text specified.
        /// </summary>
        /// <param name="result">The process execution result.</param>
        /// <param name="text">The text to look for in the output.</param>
        /// <param name="comparison">The text comparison type.</param>
        /// <param name="encoding">The expected encoding for the output text.</param>
        /// <returns>True/False that output contains specified text.</returns>
        public static bool OutputContains(this ProcessExecutionResult result, string text, StringComparison comparison, Encoding encoding = null)
        {
            result.ThrowIfNull(nameof(result));
            text.ThrowIfNull(nameof(text));

            string output = string.Join(" ", result.Output);
            if (encoding != null)
            {
                output = encoding.GetString(encoding.GetBytes(output));
            }

            return output.Contains(text, comparison);
        }

        /// <summary>
        /// Throws an exception if the result indicates the command timed out.
        /// </summary>
        /// <typeparam name="TError">The type of error to throw.</typeparam>
        /// <param name="result">The result of a command execution that may have timed out.</param>
        /// <param name="errorMessage">The error message for the exception.</param>
        public static void ThrowIfTimedOut<TError>(this ProcessExecutionResult result, string errorMessage = null)
            where TError : Exception
        {
            result.ThrowIfNull(nameof(result));

            if (result.TimedOut)
            {
                TError exception = result.ToException<TError>(errorMessage ?? "The command execution timed out.");
                throw exception;
            }
        }

        /// <summary>
        /// Throws an exception if the result indicates errored.
        /// </summary>
        /// <typeparam name="TError">The type of error to throw.</typeparam>
        /// <param name="result">The result of a command execution that may have errored.</param>
        /// <param name="errorMessage">The error message for the exception.</param>
        public static void ThrowIfErrored<TError>(this ProcessExecutionResult result, string errorMessage = null)
            where TError : Exception
        {
            result.ThrowIfNull(nameof(result));

            if (result.IsErrored())
            {
                throw result.ToException<TError>(errorMessage);
            }
        }

        /// <summary>
        /// Throws an exception if the result contains error information.
        /// </summary>
        /// <param name="result"> The process execution result containing the error information from which the exception will be created. </param>
        /// <param name="errorMessage"> Optional message to prepend to the error message in the returned exception</param>
        /// <returns> The constructed exception of TError type </returns>
        public static TError ToException<TError>(this ProcessExecutionResult result, string errorMessage = null)
            where TError : Exception
        {
            result.ThrowIfNull(nameof(result));

            TError exception = default(TError);
            StringBuilder errorBuilder = new StringBuilder();

            if (result.Error?.Any() == true)
            {
                foreach (string line in result.Error)
                {
                    if (errorBuilder.Length == 0)
                    {
                        errorBuilder.Append(line);
                    }
                    else
                    {
                        errorBuilder.Append($" {line}");
                    }
                }

                errorBuilder.Append($" (error code = {result.ExitCode})");
            }
            else
            {
                errorBuilder.Append($"Process command execution failed with error code '{result.ExitCode}'.");
            }

            string combinedErrorMessage = Regex.Replace(errorBuilder.ToString(), @"\x20{2,}", " ");
            if (errorMessage != null)
            {
                combinedErrorMessage = combinedErrorMessage.Insert(0, errorMessage + Environment.NewLine);
            }

            try
            {
                exception = (TError)Activator.CreateInstance(typeof(TError), combinedErrorMessage);
            }
            catch (MissingMethodException)
            {
                throw new MissingMethodException(
                    $"The error type provided '{typeof(TError).FullName}' does not have a constructor that takes in a single 'message' parameter.");
            }

            return exception;
        }
    }
}