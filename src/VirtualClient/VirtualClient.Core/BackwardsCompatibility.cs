// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;

    /// <summary>
    /// Provides features required for backwards compatibility support.
    /// </summary>
    public static class BackwardsCompatibility
    {
        private static readonly Dictionary<string, string> ProfileMappings = new Dictionary<string, string>()
        {
            // Profiles that were renamed.
            { "PERF-IO-FIO-STRESS.json", "PERF-IO-FIO.json" },
            { "PERF-IO-DISKSPD-STRESS.json", "PERF-IO-DISKSPD.json" },
            { "PERF-CPU-SPEC-FPRATE.json", "PERF-SPECCPU-FPRATE.json" },
            { "PERF-CPU-SPEC-FPSPEED.json", "PERF-SPECCPU-FPSPEED.json" },
            { "PERF-CPU-SPEC-INTRATE.json", "PERF-SPECCPU-INTRATE.json" },
            { "PERF-CPU-SPEC-INTSPEED.json", "PERF-SPECCPU-INTSPEED.json" },
        };

        /// <summary>
        /// Returns the correct agent ID that should be used in order to support the transition 
        /// from older instances of the Virtual Client where the ID was passed in the --metadata vs. in the
        /// --agentId command line option.
        /// </summary>
        /// <param name="idFromCommandLine">The agent ID supplied via the --agentId option on the command line.</param>
        /// <param name="metadataFromCommandLine">The metadata supplied via the --metadata option on the command line.</param>
        public static string GetAgentId(string idFromCommandLine, IDictionary<string, IConvertible> metadataFromCommandLine)
        {
            string finalAgentId = idFromCommandLine;
            if (metadataFromCommandLine?.Any() == true)
            {
                if (metadataFromCommandLine.TryGetValue("agentId", out IConvertible agentIdInMetadata))
                {
                    // The experiment ID supplied in the metadata takes precedence.
                    if (!string.Equals(idFromCommandLine, agentIdInMetadata.ToString(), StringComparison.OrdinalIgnoreCase))
                    {
                        finalAgentId = agentIdInMetadata.ToString();
                    }
                }
            }

            return finalAgentId;
        }

        /// <summary>
        /// Returns the correct experiment ID that should be used in order to support the transition 
        /// from older instances of the Virtual Client where the ID was passed in the --metadata vs. in the
        /// --experimentId command line option.
        /// </summary>
        /// <param name="idFromCommandLine">The experiment ID supplied via the --experimentId option on the command line.</param>
        /// <param name="metadataFromCommandLine">The metadata supplied via the --metadata option on the command line.</param>
        public static string GetExperimentId(string idFromCommandLine, IDictionary<string, IConvertible> metadataFromCommandLine)
        {
            string finalExperimentId = idFromCommandLine;
            if (metadataFromCommandLine?.Any() == true)
            {
                if (metadataFromCommandLine.TryGetValue("experimentId", out IConvertible experimentIdInMetadata))
                {
                    // The experiment ID supplied in the metadata takes precedence.
                    if (!string.Equals(idFromCommandLine, experimentIdInMetadata.ToString(), StringComparison.OrdinalIgnoreCase))
                    {
                        finalExperimentId = experimentIdInMetadata.ToString();
                    }
                }
            }

            return finalExperimentId;
        }

        /// <summary>
        /// Returns true if the particular profile supplied is mapped to another profile.
        /// </summary>
        /// <param name="profile">The original workload profile name.</param>
        /// <param name="otherProfile">The name of the workload profile to which it should be mapped.</param>
        /// <returns>True if the profile is mapped to another profile. False if not.</returns>
        public static bool TryMapProfile(string profile, out string otherProfile)
        {
            bool isMapped = false;
            otherProfile = null;

            string profileName = Path.GetFileName(profile);
            if (BackwardsCompatibility.ProfileMappings.TryGetValue(profileName, out string mappedProfile))
            {
                isMapped = true;
                otherProfile = profile.Replace(profileName, mappedProfile);
            }

            return isMapped;
        }
    }
}
