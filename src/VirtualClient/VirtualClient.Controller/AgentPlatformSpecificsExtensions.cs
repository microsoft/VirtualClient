// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Controller
{
    using System;
    using System.Runtime.InteropServices;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using VirtualClient.Common.Extensions;
    using VirtualClient.Contracts;

    /// <summary>
    /// Provides platform-specific information for SDK agent runtime scenarios.
    /// </summary>
    public static class AgentPlatformSpecificsExtensions
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="platformSpecifics"></param>
        /// <param name="agentId"></param>
        /// <param name="experimentId"></param>
        /// <param name="experimentName"></param>
        /// <returns></returns>
        public static void ApplyAgentDefaults(this PlatformSpecifics platformSpecifics, string agentId, string experimentId, string experimentName = null)
        {
            platformSpecifics.ThrowIfNull(nameof(platformSpecifics));
            agentId.ThrowIfNull(nameof(agentId));
            experimentId.ThrowIfNull(nameof(experimentId));

            if (!string.IsNullOrEmpty(experimentName))
            {
                // e.g. 
                // /home/user/sdkagent/logs/10.1.15.1/dc_cycle
                platformSpecifics.LogsDirectory = platformSpecifics.Combine(platformSpecifics.CurrentDirectory, "/../logs", agentId.ToLowerInvariant(), experimentName.ToLowerInvariant());
            }
            else
            {
                // e.g. 
                // /home/user/sdkagent/logs/10.1.15.1/6ab6fbb1-ab4f-472b-878a-5efb295cb4bc
                platformSpecifics.LogsDirectory = platformSpecifics.Combine(platformSpecifics.CurrentDirectory, "/../logs", agentId.ToLowerInvariant(), experimentId.ToLowerInvariant());
            }

            // e.g. 
            // /home/user/sdkagent/logs
            platformSpecifics.PackagesDirectory = platformSpecifics.Combine(platformSpecifics.CurrentDirectory, "/../packages");
        }
    }
}
