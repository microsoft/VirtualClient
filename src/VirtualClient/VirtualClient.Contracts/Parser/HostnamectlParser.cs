// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Contracts
{
    using System.Collections.Generic;
    using System.Text.RegularExpressions;

    /// <summary>
    /// Not a metric parser. It is a hostnamectl output parser for determining Linux distribution
    /// </summary>
    public class HostnamectlParser : TextParser<LinuxDistributionInfo>
    {
        /// <summary>
        /// Constructor for <see cref="HostnamectlParser"/>
        /// </summary>
        /// <param name="rawText">Raw text to parse.</param>
        public HostnamectlParser(string rawText)
            : base(rawText)
        {
        }

        /// <summary>
        /// Determining the linux distribution.
        /// </summary>
        /// <returns>Linux Distribution class.</returns>
        public override LinuxDistributionInfo Parse()
        {
            this.Preprocess();
            Regex ubuntuRegex = new Regex("Operating System: Ubuntu", RegexOptions.Multiline | RegexOptions.IgnoreCase);
            Regex debianRegex = new Regex("Operating System: Debian", RegexOptions.Multiline | RegexOptions.IgnoreCase);
            Regex rhel7Regex = new Regex(@"Operating System: Red Hat Enterprise Linux 7", RegexOptions.Multiline | RegexOptions.IgnoreCase);
            Regex rhel8Regex = new Regex(@"Operating System: Red Hat Enterprise Linux (8|9).", RegexOptions.Multiline | RegexOptions.IgnoreCase);
            Regex flatcarRegex = new Regex("Operating System: Flatcar", RegexOptions.Multiline | RegexOptions.IgnoreCase);
            Regex centos7Regex = new Regex("Operating System: CentOS Linux 7", RegexOptions.Multiline | RegexOptions.IgnoreCase);
            Regex centos8Regex = new Regex("Operating System: CentOS Linux 8", RegexOptions.Multiline | RegexOptions.IgnoreCase);
            Regex marinerRegex = new Regex("Operating System: CBL-Mariner", RegexOptions.Multiline | RegexOptions.IgnoreCase);
            Regex suseRegex = new Regex("Operating System: SUSE", RegexOptions.Multiline | RegexOptions.IgnoreCase);

            Dictionary<Regex, LinuxDistribution> distroMapping = new Dictionary<Regex, LinuxDistribution>()
            {
                { ubuntuRegex, LinuxDistribution.Ubuntu },
                { debianRegex, LinuxDistribution.Debian },
                { rhel7Regex, LinuxDistribution.RHEL7 },
                { rhel8Regex, LinuxDistribution.RHEL8 },
                { flatcarRegex, LinuxDistribution.Flatcar },
                { centos7Regex, LinuxDistribution.CentOS7 },
                { centos8Regex, LinuxDistribution.CentOS8 },
                { suseRegex, LinuxDistribution.SUSE },
                { marinerRegex, LinuxDistribution.Mariner }
            };

            LinuxDistribution distribution = LinuxDistribution.Unknown;
            foreach (var distro in distroMapping)
            {
                if (Regex.IsMatch(this.PreprocessedText, distro.Key.ToString(), distro.Key.Options))
                {
                    distribution = distro.Value;
                    break;
                }
            }

            string osFullName = string.Empty;
            Regex osNameRegex = new Regex(@"Operating System: (.+)", RegexOptions.Multiline | RegexOptions.IgnoreCase);
            Match match = Regex.Match(this.PreprocessedText, osNameRegex.ToString(), osNameRegex.Options);
            osFullName = match.Groups[1].Value.Trim();

            return new LinuxDistributionInfo()
            {
                OperationSystemFullName = osFullName,
                LinuxDistribution = distribution
            };
        }
    }
}