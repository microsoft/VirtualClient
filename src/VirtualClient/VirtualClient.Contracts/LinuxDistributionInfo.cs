// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Contracts
{
    using System.Collections.Generic;
    using System.Text.RegularExpressions;

    /// <summary>
    /// Defines the Linux distribution information.
    /// </summary>
    public class LinuxDistributionInfo
    {
        // Great examples could be found at https://github.com/chef/os_release
        private static readonly IEnumerable<KeyValuePair<Regex, LinuxDistribution>> DistroMapping = new List<KeyValuePair<Regex, LinuxDistribution>>()
        {
            // Note that order matters here. Upstream/base distros should be evaluated first before downstream distros.
            {
                new KeyValuePair<Regex, LinuxDistribution>(
                    new Regex("NAME=(\")?Debian|Operating\\s+System:\\sDebian", RegexOptions.IgnoreCase),
                    LinuxDistribution.Debian)
            },
            {
                new KeyValuePair<Regex, LinuxDistribution>(
                    new Regex("NAME=(\")?CentOS|Operating\\s+System:\\sCentOS", RegexOptions.IgnoreCase),
                    LinuxDistribution.CentOS)
            },
            {
                new KeyValuePair<Regex, LinuxDistribution>(
                    new Regex("NAME=(\")?openSUSE|Operating\\s+System:\\sopenSUSE", RegexOptions.IgnoreCase),
                    LinuxDistribution.OpenSuse)
            },
            {
                new KeyValuePair<Regex, LinuxDistribution>(
                    new Regex("NAME=(\")?Fedora|Operating\\s+System:\\sFedora", RegexOptions.IgnoreCase),
                    LinuxDistribution.Fedora)
            },
            {
                new KeyValuePair<Regex, LinuxDistribution>(
                    new Regex("NAME=(\")?Ubuntu|Operating\\s+System:\\sUbuntu", RegexOptions.IgnoreCase), 
                    LinuxDistribution.Ubuntu)
            },   
            {
                new KeyValuePair<Regex, LinuxDistribution>(
                    new Regex("NAME=(\")?Red\\s+Hat|Operating\\s+System:\\sRed\\s+Hat", RegexOptions.IgnoreCase), 
                    LinuxDistribution.RedHat) 
            },
            {
                new KeyValuePair<Regex, LinuxDistribution>(
                    new Regex("PRETTY_NAME=(\")?[a-z\\s]*Azure\\s+Linux|Operating\\s+System:[a-z\\s]*Azure\\s+Linux", RegexOptions.IgnoreCase),
                    LinuxDistribution.AzureLinux)
            },
            {
                new KeyValuePair<Regex, LinuxDistribution>(
                    new Regex("NAME=(\")?Amazon Linux|Amazon\\s+Linux", RegexOptions.IgnoreCase),
                    LinuxDistribution.AmazonLinux)
            },
            {
                new KeyValuePair<Regex, LinuxDistribution>(
                    new Regex("Flatcar", RegexOptions.IgnoreCase), 
                    LinuxDistribution.Flatcar)
            },
            {
                new KeyValuePair<Regex, LinuxDistribution>(
                    new Regex("Gentoo", RegexOptions.IgnoreCase),
                    LinuxDistribution.Gentoo)
            }
        };

        private static readonly IDictionary<LinuxDistribution, LinuxUpstreamDistribution> UpstreamDistroMapping = new Dictionary<LinuxDistribution, LinuxUpstreamDistribution>()
        {
            { LinuxDistribution.AmazonLinux, LinuxUpstreamDistribution.Fedora },
            { LinuxDistribution.AzureLinux, LinuxUpstreamDistribution.Fedora },
            { LinuxDistribution.CentOS, LinuxUpstreamDistribution.Fedora },
            { LinuxDistribution.Debian, LinuxUpstreamDistribution.Debian },
            { LinuxDistribution.Fedora, LinuxUpstreamDistribution.Fedora },
            { LinuxDistribution.Flatcar, LinuxUpstreamDistribution.Gentoo },
            { LinuxDistribution.Gentoo, LinuxUpstreamDistribution.Gentoo },
            { LinuxDistribution.OpenSuse, LinuxUpstreamDistribution.OpenSuse },
            { LinuxDistribution.RedHat, LinuxUpstreamDistribution.Fedora },
            { LinuxDistribution.Ubuntu, LinuxUpstreamDistribution.Debian },
        };

        /// <summary>
        /// Full name of the operating system distro.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Full release details for the operating system distro.
        /// </summary>
        public string Details { get; set; }

        /// <summary>
        /// The distribution name/category.
        /// </summary>
        public LinuxDistribution Distribution { get; set; }

        /// <summary>
        /// The upstream distribution base/ecosystem.
        /// </summary>
        public LinuxUpstreamDistribution UpstreamDistribution { get; set; }

        /// <summary>
        /// Creates a <see cref="LinuxDistributionInfo"/> based on the release information provided.
        /// </summary>
        /// <param name="releaseInfo">Release information (e.g. /etc/os-release, hostnamectl).</param>
        public static LinuxDistributionInfo Create(string releaseInfo)
        {
            LinuxDistributionInfo info = new LinuxDistributionInfo
            {
                Details = releaseInfo
            };

            LinuxDistribution distribution = LinuxDistribution.Unknown;
            foreach (var mapping in LinuxDistributionInfo.DistroMapping)
            {
                Regex matchExpression = mapping.Key;
                LinuxDistribution matchingDistro = mapping.Value;

                if (matchExpression.IsMatch(releaseInfo))
                {
                    distribution = matchingDistro;
                    break;
                }
            }

            string osFullName = null;
            Regex osNameRegex = new Regex(@"(?:PRETTY_NAME=|Operating\s+System:)[\s+""]*(.+)[""]*", RegexOptions.IgnoreCase);
            Match osNameMatch = osNameRegex.Match(releaseInfo);

            if (osNameMatch.Success)
            {
                osFullName = osNameMatch.Groups[1].Value.Trim().Trim('"');
            }

            LinuxUpstreamDistribution upstreamDistribution = LinuxUpstreamDistribution.Unknown;
            if (LinuxDistributionInfo.UpstreamDistroMapping.TryGetValue(distribution, out LinuxUpstreamDistribution matchingUpstreamDistro))
            {
                upstreamDistribution = matchingUpstreamDistro;
            }

            info.Name = osFullName ?? distribution.ToString();
            info.Distribution = distribution;
            info.UpstreamDistribution = upstreamDistribution;

            return info;
        }
    }
}
