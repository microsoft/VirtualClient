// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Common
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.RegularExpressions;
    using VirtualClient.Common.Extensions;

    /// <summary>
    /// Provides methods for working with sensitive data.
    /// </summary>
    public static class SensitiveData
    {
        /// <summary>
        /// The set of regular expressions used to match sensitive data or secrets in text strings. This is an
        /// extensibility point to allow for additional match expressions to be supplied to handle other
        /// types of sensitive data/secrets. Example regular expression format = /AccountKey=([\x21\x23-\x7E]+)/.
        /// </summary>
        public static readonly List<Regex> Expressions = new List<Regex>
        {
             new Regex(@"AccessKey[=\x20]+""*([\x21\x23-\x7E]+)""*", RegexOptions.IgnoreCase),
             new Regex(@"AccountKey[=\x20]+""*([\x21\x23-\x7E]+)""*", RegexOptions.IgnoreCase),
             new Regex(@"Token[=\x20]+""*([\x21\x23-\x7E]+)""*", RegexOptions.IgnoreCase),
             new Regex(@"sig[=\x20]+""*([\x21\x23-\x7E]+)""*", RegexOptions.IgnoreCase),

             // Passwords are tricky because they can contain any character. We are not using capture groups  (e.g. ?:) for these type
             // of expressions to allow the handling of special cases (e.g. delimited key/value pair groups (e.g. Password=s@me,val;ue,,,Property1=Value1).
             new Regex("(?<=Password[=\x20]+\"*)(?:,{0,2}[\x21\x23-\x2B\x2D-\x7E]+,{0,2}[\x21\x23-\x2B\x2D-\x7E]+)+\"*", RegexOptions.IgnoreCase),
             new Regex("(?<=Pwd[=\x20]+\"*)(?:,{0,2}[\x21\x23-\x2B\x2D-\x7E]+,{0,2}[\x21\x23-\x2B\x2D-\x7E]+)+\"*", RegexOptions.IgnoreCase),

             // Agent SSH connections allow for passwords
             // (e.g. user@10.2.3.5;pass_;wor;d).
             new Regex(@"[0-9a-z_\-\. ]+@[^;]+;([\x20-\x7E]+)", RegexOptions.IgnoreCase)
        };

        /// <summary>
        /// Obscures known secrets in the original string.
        /// </summary>
        /// <param name="originalString">The original string containing the secrets.</param>
        /// <param name="percentage">The percentage of the original string to obfuscate (1% - 100%)</param>
        /// <returns>
        /// The original string having the secrets to obscure.
        /// </returns>
        public static string ObscureSecrets(string originalString, float percentage = 100)
        {
            string obscuredString = originalString;
            if (!string.IsNullOrWhiteSpace(originalString))
            {
                // The method is meant to allow us to link together any number of algorithms for
                // obscuring secrets in string/text data.
                foreach (Regex expression in SensitiveData.Expressions)
                {
                    obscuredString = SensitiveData.ObscureSecrets(expression, obscuredString, percentage);
                }
            }

            return obscuredString;
        }

        /// <summary>
        /// Obscures any account keys in the original string so that they can be verified
        /// but not fully. Note that the match expression passed in is expected to contain
        /// 1 capture group and no more. For example: Secret=([a-z0-9]+). If additional capture 
        /// groups are provided, they will be ignored. To operate on more than 1 secret, call this
        /// method once for each distinct secret to obscure passing in the appropriate regular 
        /// expression containing a single capture group.
        /// </summary>
        /// <param name="matchExpression">
        /// The regular expression used to match the secret. Note that this expression is expected to contain a single capture group only that
        /// defines the portion of the secret that needs to be obscured. Example: Secret=([a-z0-9]+).
        /// </param>
        /// <param name="originalString">The original string containing one or more secrets to obscure.</param>
        /// <param name="percentage">The percentage of the original string to obfuscate (1% - 100%)</param>
        /// <returns>
        /// The original string having the secrets obscured. 
        /// </returns>
        public static string ObscureSecrets(Regex matchExpression, string originalString, float percentage = 100)
        {
            matchExpression.ThrowIfNull(nameof(matchExpression));
            originalString.ThrowIfNull(nameof(originalString));
            percentage.ThrowIfInvalid(nameof(percentage), pct => pct >= 0);

            string obscuredString = originalString;
            MatchCollection matches = matchExpression.Matches(originalString);

            if (matches?.Any() == true)
            {
                foreach (Match match in matches)
                {
                    // There are cases where it is easier to match a particular part of an expression without using capture
                    // groups. Depending upon how the regular expressions are defined above, we use either the match itself or
                    // the capture groups.
                    if (match.Groups.Count <= 1)
                    {
                        string key = match.Value;
                        int substringLength = (int)(key.Length * ((100 - percentage) / 100));

                        obscuredString = obscuredString.Replace(match.Value, $"{key.Substring(0, substringLength)}...", StringComparison.OrdinalIgnoreCase);
                    }
                    else
                    {
                        foreach (Group group in ((IList<Group>)match.Groups).Skip(1))
                        {
                            if (group.Success)
                            {
                                string key = group.Value;
                                int substringLength = (int)(key.Length * ((100 - percentage) / 100));

                                obscuredString = obscuredString.Replace(key, $"{key.Substring(0, substringLength)}...", StringComparison.OrdinalIgnoreCase);
                            }
                        }
                    }
                }
            }

            return obscuredString;
        }
    }
}
