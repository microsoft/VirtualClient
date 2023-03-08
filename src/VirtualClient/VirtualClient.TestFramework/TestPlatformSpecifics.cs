// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient
{
    using System;
    using System.Collections.Generic;
    using System.Runtime.InteropServices;
    using System.Text.RegularExpressions;
    using VirtualClient.Contracts;

    /// <summary>
    /// Provides platform-specific information to help ease inconsistencies
    /// in cross-platform test scenarios (e.g. testing with Linux paths on Windows).
    /// </summary>
    public class TestPlatformSpecifics : PlatformSpecifics
    {
        /// <summary>
        /// Initializes a new version of the <see cref="TestPlatformSpecifics"/> class.
        /// </summary>
        /// <param name="platform">The OS platform (e.g. Windows, Unix).</param>
        /// <remarks>
        /// This constructor is largely used to address challenges with testing code that references
        /// paths on a system that are expected to be in a different format than is typical for the
        /// system on which the test is running. For example, Linux paths use forward slashes. When
        /// testing components on a Windows system, the typical path semantics have to be modified.
        /// </remarks>
        public TestPlatformSpecifics(PlatformID platform)
            : this(platform, Architecture.X64)
        {
        }

        /// <summary>
        /// Initializes a new version of the <see cref="TestPlatformSpecifics"/> class.
        /// </summary>
        /// <param name="platform">The OS platform (e.g. Windows, Unix).</param>
        /// <param name="architecture">The CPU architecture (e.g. x64, arm64).</param>
        /// <remarks>
        /// This constructor is largely used to address challenges with testing code that references
        /// paths on a system that are expected to be in a different format than is typical for the
        /// system on which the test is running. For example, Linux paths use forward slashes. When
        /// testing components on a Windows system, the typical path semantics have to be modified.
        /// </remarks>
        public TestPlatformSpecifics(PlatformID platform, Architecture architecture)
            : base(platform, architecture, platform == PlatformID.Win32NT ? "C:\\users\\any\\tools\\VirtualClient" : "/home/user/tools/VirtualClient")
        {
            this.EnvironmentVariables = new Dictionary<string, string>();
        }

        /// <summary>
        /// Initializes a new version of the <see cref="TestPlatformSpecifics"/> class.
        /// </summary>
        /// <param name="platform">The OS platform (e.g. Windows, Unix).</param>
        /// <param name="currentDirectory">The directory to use as the current working directory.</param>
        /// <remarks>
        /// This constructor is largely used to address challenges with testing code that references
        /// paths on a system that are expected to be in a different format than is typical for the
        /// system on which the test is running. For example, Linux paths use forward slashes. When
        /// testing components on a Windows system, the typical path semantics have to be modified.
        /// </remarks>
        public TestPlatformSpecifics(PlatformID platform, string currentDirectory)
            : base(platform, Architecture.X64, currentDirectory)
        {
            this.EnvironmentVariables = new Dictionary<string, string>();
        }

        /// <summary>
        /// Initializes a new version of the <see cref="TestPlatformSpecifics"/> class.
        /// </summary>
        /// <param name="platform">The OS platform (e.g. Windows, Unix).</param>
        /// <param name="architecture">The CPU architecture (e.g. x64, arm64).</param>
        /// <param name="currentDirectory">The directory to use as the current working directory.</param>
        /// <remarks>
        /// This constructor is largely used to address challenges with testing code that references
        /// paths on a system that are expected to be in a different format than is typical for the
        /// system on which the test is running. For example, Linux paths use forward slashes. When
        /// testing components on a Windows system, the typical path semantics have to be modified.
        /// </remarks>
        public TestPlatformSpecifics(PlatformID platform, Architecture architecture, string currentDirectory)
            : base(platform, architecture, currentDirectory)
        {
            this.EnvironmentVariables = new Dictionary<string, string>();
        }

        /// <summary>
        /// Used to supply fake environment variable values to test operations.
        /// </summary>
        public IDictionary<string, string> EnvironmentVariables { get; }

        /// <summary>
        /// Returns the value of the environment variable in the test 'EnvironmentVariables' set.
        /// </summary>
        /// <param name="name">The name of the environment variable.</param>
        /// <param name="target">The environment variable scope (e.g. Machine, User, Process).</param>
        /// <returns>The value of the environment variable</returns>
        public override string GetEnvironmentVariable(string name, EnvironmentVariableTarget target = EnvironmentVariableTarget.Process)
        {
            this.EnvironmentVariables.TryGetValue(name, out string value);
            return value;
        }

        /// <summary>
        /// Sets the value of the environment variable in the test 'EnvironmentVariables' set.
        /// </summary>
        /// <param name="name">The name of the environment variable to set.</param>
        /// <param name="value">The value to which to set the environment variable or append to the end of the existing value.</param>
        /// <param name="target">The environment variable scope (e.g. Machine, User, Process).</param>
        /// <param name="append">True to append the value to the end of the existing environment variable value. False to replace the existing value.</param>
        public override void SetEnvironmentVariable(string name, string value, EnvironmentVariableTarget target = EnvironmentVariableTarget.Process, bool append = false)
        {
            if (!append)
            {
                this.EnvironmentVariables[name] = value;
            }
            else
            {
                char delimiter = this.Platform == PlatformID.Unix ? ':' : ';';
                this.EnvironmentVariables.TryGetValue(name, out string existingValue);
                this.EnvironmentVariables[name] = string.IsNullOrWhiteSpace(existingValue) ? value : $"{existingValue}{delimiter}{value}";
            }
        }
    }
}
