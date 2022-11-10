// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Actions
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Newtonsoft.Json;
    using VirtualClient.Contracts;

    /// <summary>
    /// Parser for Sysctl output
    /// </summary>
    public class SysctlParser : TextParser<string>
    {
        /// <summary>
        /// Constructor for <see cref="SysctlParser"/>
        /// </summary>
        /// <param name="rawText">Raw text to parse.</param>
        public SysctlParser(string rawText)
            : base(rawText)
        {
        }

        /// <inheritdoc/>
        public override string Parse()
        {
            try
            {
                Dictionary<string, string> jsonDict = new Dictionary<string, string>();
                string data = this.RawText.Replace('\t', ' ');
                data = data.TrimEnd('\r', '\n');
                List<string> listOfSysctlOptions = data.Split('\n').ToList();

                foreach (string item in listOfSysctlOptions)
                {
                    List<string> keyValuePair = item.Split('=').ToList();
                    jsonDict[keyValuePair[0]] = keyValuePair[1].Trim();
                }

                return JsonConvert.SerializeObject(jsonDict);
            }
            catch (Exception exc)
            {
                throw new WorkloadResultsException("Failed to parse Sysctl results.", exc, ErrorReason.WorkloadResultsParsingFailed);
            }
        }
    }
}