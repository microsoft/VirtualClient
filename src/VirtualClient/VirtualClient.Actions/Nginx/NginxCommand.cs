// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Actions
{
    /// <summary>
    /// Common Nginx Commands
    /// Explore more here: https://nginx.org/en/docs/switches.html
    /// </summary>
    public enum NginxCommand
    {
        /// <summary>
        /// "service nginx start"
        /// </summary>
        Start,

        /// <summary>
        /// "service nginx stop"
        /// shut down quickly
        /// </summary>
        Stop,

        /// <summary>
        /// "nginx -T";
        /// -V: print nginx version, compiler version, and configure parameters. 
        /// -T: Test configuration file. And dump it.
        /// Use standard output to read
        /// </summary>
        GetVersion,

        /// <summary>
        /// "nginx -T";
        /// -T: Test configuration file. And dump it.
        /// Use standard output to read
        /// </summary>
        GetConfig
    }
}
