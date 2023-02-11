// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.TestExtensions
{
    using System.Text.RegularExpressions;

    /// <summary>
    /// Extension methods for strings used in test methods/logic.
    /// </summary>
    public static class TestStringExtensions
    {
        private static Regex asciiHeaderExpression = new Regex(@"^\?{3}", RegexOptions.Compiled);

        /// <summary>
        /// Removes any ASCII headers from the beginning of the text/document
        /// (the headers are formatted like this:  ???{ documentSchema:....}
        /// </summary>
        /// <param name="text">The text/document that contains ASCII headers</param>
        /// <returns>A text/document string without ASCII headers.</returns>
        public static string RemoveAsciiHeader(this string text)
        {
            return TestStringExtensions.asciiHeaderExpression.Replace(text, string.Empty);
        }
    }
}
