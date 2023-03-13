// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Monitors
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.RegularExpressions;
    using VirtualClient.Contracts;

    /// <summary>
    /// Parser for NvidiaSmi output document.
    /// </summary>
    public class LspciParser : TextParser<IList<PciDevice>>
    {
        /// <summary>
        /// Sectionize by one or more empty lines.
        /// </summary>
        private static readonly Regex SectionDelimiter = new Regex(@"(\n)(\s)*(\n)", RegexOptions.ExplicitCapture);

        /// <summary>
        /// Captures the address and name
        /// Address: c93f:00:02.0
        /// Name: Ethernet controller: Mellanox Technologies MT27710 Family [ConnectX-4 Lx Virtual Function] (rev 80)
        /// c93f:00:02.0 Ethernet controller: Mellanox Technologies MT27710 Family [ConnectX-4 Lx Virtual Function] (rev 80)
        /// </summary>
        private static readonly Regex PciAddressRegex = new Regex(@"((?:[\da-f]+:)*[\da-f]{2}:[\da-f]{2}.[\d]+) (.*)", RegexOptions.None);

        /// <summary>
        /// Level 1 indent using 8 spaces and a colon
        /// </summary>
        private static readonly Regex IndentLevel1 = new Regex(@"^(?: ){8}(\w+[^:]*):", RegexOptions.Multiline);

        /// <summary>
        /// Constructor for <see cref="LspciParser"/>
        /// </summary>
        /// <param name="rawText">Raw text to parse.</param>
        public LspciParser(string rawText)
            : base(rawText)
        {
        }

        /// <inheritdoc/>
        public override IList<PciDevice> Parse()
        {
            this.Preprocess();
            this.Sections = TextParsingExtensions.Sectionize(this.PreprocessedText, LspciParser.SectionDelimiter);

            List<PciDevice> devices = new List<PciDevice>();

            foreach (KeyValuePair<string, string> section in this.Sections)
            {
                PciDevice device = new PciDevice();

                // The section name will be first line
                // 0001:00:00.0 3D controller: NVIDIA Corporation TU104GL [Tesla T4] (rev a1)
                Match match = Regex.Match(section.Key, LspciParser.PciAddressRegex.ToString(), LspciParser.PciAddressRegex.Options);
                device.Address = match.Groups[1].Value;
                device.Name = match.Groups[2].Value;

                int firstCapabilityIndex = section.Value.IndexOf("Capabilities:");
                int kernelDriverInUseIndex = section.Value.IndexOf("Kernel driver in use:");

                string propertiesString = section.Value;
                string capabilitiesString = string.Empty;
                if (firstCapabilityIndex != -1)
                {
                    propertiesString = section.Value.Substring(0, firstCapabilityIndex);
                    capabilitiesString = section.Value.Substring(firstCapabilityIndex);

                    if (kernelDriverInUseIndex != -1)
                    {
                        string additionalProperties = section.Value.Substring(kernelDriverInUseIndex);
                        propertiesString = propertiesString + additionalProperties;
                        capabilitiesString = capabilitiesString.Replace(additionalProperties, string.Empty);
                    }
                }

                string[] properties = propertiesString.Split(Environment.NewLine, StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
                device.Properties = this.ParseDictionary(properties);

                if (firstCapabilityIndex != -1)
                {
                    string[] capabilities = capabilitiesString.Split("Capabilities:", StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
                    foreach (string capability in capabilities)
                    {
                        Dictionary<string, string> capabilitiesDic = new Dictionary<string, string>();
                        string[] lines = capability.Split(Environment.NewLine, StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);

                        string capabilityName = lines[0];

                        device.Capabilities.Add(new PciDevice.PciDeviceCapability()
                        {
                            Name = capabilityName,
                            Properties = this.ParseDictionary(lines.Skip(1).ToArray())
                        });
                    }
                }

                devices.Add(device);
            }

            return devices;
        }

        private Dictionary<string, object> ParseDictionary(string[] lines) 
        {
            Dictionary<string, object> result = new Dictionary<string, object>();

            for (int index = 0; index < lines.Length; index++)
            {
                string text = lines[index];
                while (index < lines.Length - 1 && !lines[index + 1].Contains(":"))
                {
                    text = text + " " + lines[++index];
                }

                int colonIndex = text.IndexOf(':');
                string key = text.Substring(0, colonIndex);
                string value = text.Substring(colonIndex + 1);
                result[key.Trim()] = value.Trim();
            }

            return result;
        }
    }
}
