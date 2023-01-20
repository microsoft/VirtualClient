// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Monitors
{
    using System;
    using System.Collections.Generic;
    using System.Data;
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
                
                // The section name will be first line
                // 0001:00:00.0 3D controller: NVIDIA Corporation TU104GL [Tesla T4] (rev a1)
                string address = section.Key;
                string name

            }

            return devices;
        }
    }
}
