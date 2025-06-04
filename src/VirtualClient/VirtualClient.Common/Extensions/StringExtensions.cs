// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Common.Extensions
{
    using System;
    using System.Collections;
    using System.Diagnostics.CodeAnalysis;
    using System.Linq;
    using System.Runtime.InteropServices;
    using System.Security;
    using System.Text.RegularExpressions;

    /// <summary>
    /// Collection of ENUM extensions methods.
    /// </summary>
    public static class StringExtensions
    {
        private static Regex whitespaceExpression = new Regex(@"\s+", RegexOptions.Compiled);

        /// <summary>
        /// Returns an array with the individual string value as an item within.
        /// </summary>
        /// <param name="value">The value to insert into the array.</param>
        /// <returns>A string array containing the string value.</returns>
        public static string[] AsArray(this string value)
        {
            return new string[] { value };
        }

        /// <summary>
        /// Return true/false whether the string value contains text.
        /// </summary>
        /// <param name="value">Value to be searched</param>
        /// <param name="text">The text to look for in the output.</param>
        /// <param name="comparison">The text comparison type.</param>
        /// <returns>True if the given string contains text with the string comparison.</returns>
        public static bool Contains(this string value, string text, StringComparison comparison)
        {
            value.ThrowIfNull(nameof(value));
            text.ThrowIfNull(nameof(text));
            return value.IndexOf(text, comparison) >= 0;
        }

        /// <summary>
        /// Remove white spaces in text.
        /// </summary>
        /// <param name="value">Original value</param>
        /// <returns>Value after spaces removed.</returns>
        public static string RemoveWhitespace(this string value)
        {
            return StringExtensions.whitespaceExpression.Replace(value, string.Empty);
        }

        /// <summary>
        /// Convert a <see cref="SecureString"/> to a <see cref="string"/>.
        /// </summary>
        /// <param name="value">The secure string to be converted.</param>
        /// <returns>The provided secure string converted to a string.</returns>
        public static string ToOriginalString(this SecureString value)
        {
            var unmanagedString = IntPtr.Zero;
            try
            {
                unmanagedString = Marshal.SecureStringToGlobalAllocUnicode(value);
                return Marshal.PtrToStringUni(unmanagedString);
            }
            finally
            {
                Marshal.ZeroFreeGlobalAllocUnicode(unmanagedString);
            }
        }

        /// <summary>
        /// Convert a <see cref="string"/> to a <see cref="SecureString"/>.
        /// </summary>
        /// <param name="value">The string to be converted.</param>
        /// <returns>The provided string converted to a secure string.  The caller is responsible for disposal of the SecureString.</returns>
        [SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope", Justification = "Caller must dispose as the disposable object is a return value.")]
        public static SecureString ToSecureString(this string value)
        {
            value.ThrowIfNull(nameof(value));

            SecureString secret = new SecureString();

            try
            {
                foreach (char c in value)
                {
                    secret.AppendChar(c);
                }

                secret.MakeReadOnly();
            }
            catch (Exception)
            {
                secret.Dispose();
                throw;
            }

            return secret;
        }
    }
}