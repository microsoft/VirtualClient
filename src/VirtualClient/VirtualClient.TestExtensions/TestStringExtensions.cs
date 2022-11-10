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
        private static Regex whitespaceExpression = new Regex(@"\s+", RegexOptions.Compiled);

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

        /// <summary>
        /// Removes all whitespace from the text.  This is useful when comparing
        /// one text string to another for equality.
        /// </summary>
        /// <param name="text">The text/string from which to remove the whitespace.</param>
        /// <returns>
        /// A text string without any whitespace.
        /// </returns>
        public static string RemoveWhitespace(this string text)
        {
            return TestStringExtensions.whitespaceExpression.Replace(text, string.Empty);
        }
    }
}
