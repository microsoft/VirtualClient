// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Contracts
{
    using System;
    using System.Collections.Generic;
    using NUnit.Framework;

    [TestFixture]
    [Category("Unit")]
    internal class FileContextTests
    {
        private MockFixture mockFixture;

        public void SetupDefaults(PlatformID platform = PlatformID.Unix)
        {
            this.mockFixture = new MockFixture();
            this.mockFixture.Setup(platform);
        }

        [Test]
        [TestCase("/home/user/logs/{experimentId}", "/home/user/logs/ab43a99d-eddd-44f8-ac25-f368f02dbc21")]
        [TestCase("/home/user/{experimentId}/logs", "/home/user/ab43a99d-eddd-44f8-ac25-f368f02dbc21/logs")]
        [TestCase("/home/user/logs/{experimentId}/{agentId}/{toolName}", "/home/user/logs/ab43a99d-eddd-44f8-ac25-f368f02dbc21/agent01/tool01")]
        [TestCase("/home/user/{experimentId}/logs/{agentId}/{toolName}", "/home/user/ab43a99d-eddd-44f8-ac25-f368f02dbc21/logs/agent01/tool01")]
        [TestCase("/home/user/{experimentId}/logs/{agentId}/{toolName}/", "/home/user/ab43a99d-eddd-44f8-ac25-f368f02dbc21/logs/agent01/tool01")]
        public void FileContextResolvesPlaceholdersAsExpected_Unix_Systems(string pathTemplate, string expectedResolvedPath)
        {
            this.SetupDefaults(PlatformID.Unix);

            IDictionary<string, IConvertible> replacements = new Dictionary<string, IConvertible>
            {
                ["experimentId"] = "ab43a99d-eddd-44f8-ac25-f368f02dbc21",
                ["agentId"] = "agent01",
                ["toolName"] = "tool01"
            };

            string actualResolvedPath = FileContext.ResolvePathTemplate(pathTemplate, replacements);
            Assert.AreEqual(expectedResolvedPath, actualResolvedPath);
        }

        [Test]
        [TestCase(@"C:\Users\Any\Logs\{experimentId}", @"C:\Users\Any\Logs\ab43a99d-eddd-44f8-ac25-f368f02dbc21")]
        [TestCase(@"C:\Users\Any\{experimentId}\Logs", @"C:\Users\Any\ab43a99d-eddd-44f8-ac25-f368f02dbc21\Logs")]
        [TestCase(@"C:\Users\Any\Logs\{experimentId}\{agentId}\{toolName}", @"C:\Users\Any\Logs\ab43a99d-eddd-44f8-ac25-f368f02dbc21\agent01\tool01")]
        [TestCase(@"C:\Users\Any\{experimentId}\Logs\{agentId}\{toolName}", @"C:\Users\Any\ab43a99d-eddd-44f8-ac25-f368f02dbc21\Logs\agent01\tool01")]
        public void FileContextResolvesPlaceholdersAsExpected_Windows_Systems(string pathTemplate, string expectedResolvedPath)
        {
            this.SetupDefaults(PlatformID.Win32NT);

            IDictionary<string, IConvertible> replacements = new Dictionary<string, IConvertible>
            {
                ["experimentId"] = "ab43a99d-eddd-44f8-ac25-f368f02dbc21",
                ["agentId"] = "agent01",
                ["toolName"] = "tool01"
            };

            string actualResolvedPath = FileContext.ResolvePathTemplate(pathTemplate, replacements);
            Assert.AreEqual(expectedResolvedPath, actualResolvedPath);
        }

        [Test]
        [TestCase("/home/user/logs", "/home/user/logs")]
        [TestCase("/home/user/logs/{does_not_exist}", "/home/user/logs")]
        [TestCase("/home/user/{does_not_exist}/logs", "/home/user/logs")]
        [TestCase("/home/user/{does_not_exist}/logs/", "/home/user/logs")]
        [TestCase("/home/user/{does_not_exist}/logs//", "/home/user/logs")]
        [TestCase("/{does_not_exist}/user/logs", "/user/logs")]
        [TestCase("/home/user/logs/{experimentId}/{does_not_exist}", "/home/user/logs/ab43a99d-eddd-44f8-ac25-f368f02dbc21")]
        [TestCase("/home/user/logs/{experimentId}/{does_not_exist}/", "/home/user/logs/ab43a99d-eddd-44f8-ac25-f368f02dbc21")]
        public void FileContextHandlesCasesWhenUnmatchedPlaceholdersExist_Unix_Systems(string pathTemplate, string expectedResolvedPath)
        {
            this.SetupDefaults(PlatformID.Unix);

            IDictionary<string, IConvertible> replacements = new Dictionary<string, IConvertible>
            {
                ["experimentId"] = "ab43a99d-eddd-44f8-ac25-f368f02dbc21",
                ["agentId"] = "agent01",
                ["toolName"] = "tool01"
            };

            string actualResolvedPath = FileContext.ResolvePathTemplate(pathTemplate, replacements);
            Assert.AreEqual(expectedResolvedPath, actualResolvedPath);
        }

        [Test]
        [TestCase(@"C:\Users\Any\Logs", @"C:\Users\Any\Logs")]
        [TestCase(@"C:\Users\Any\Logs\{does_not_exist}", @"C:\Users\Any\Logs")]
        [TestCase(@"C:\Users\Any\{does_not_exist}\Logs", @"C:\Users\Any\Logs")]
        [TestCase(@"C:\Users\Any\{does_not_exist}\Logs\", @"C:\Users\Any\Logs")]
        [TestCase(@"C:\Users\Any\{does_not_exist}\Logs\\", @"C:\Users\Any\Logs")]
        [TestCase(@"C:\{does_not_exist}\Any\Logs\\", @"C:\Any\Logs")]
        [TestCase(@"{does_not_exist}\Any\Logs", @"Any\Logs")]
        [TestCase(@"C:\Users\Any\Logs\{experimentId}\{does_not_exist}", @"C:\Users\Any\Logs\ab43a99d-eddd-44f8-ac25-f368f02dbc21")]
        [TestCase(@"C:\Users\Any\Logs\{experimentId}\{does_not_exist}\", @"C:\Users\Any\Logs\ab43a99d-eddd-44f8-ac25-f368f02dbc21")]
        public void FileContextHandlesCasesWhenUnmatchedPlaceholdersExist_Windows_Systems(string pathTemplate, string expectedResolvedPath)
        {
            this.SetupDefaults(PlatformID.Win32NT);

            IDictionary<string, IConvertible> replacements = new Dictionary<string, IConvertible>
            {
                ["experimentId"] = "ab43a99d-eddd-44f8-ac25-f368f02dbc21",
                ["agentId"] = "agent01",
                ["toolName"] = "tool01"
            };

            string actualResolvedPath = FileContext.ResolvePathTemplate(pathTemplate, replacements);
            Assert.AreEqual(expectedResolvedPath, actualResolvedPath);
        }

        [Test]
        public void FileContextThrowsWhenUnmatchedPlaceholdersExistWhenValidationIsRequested()
        {
            this.SetupDefaults();

            IDictionary<string, IConvertible> replacements = new Dictionary<string, IConvertible>
            {
                ["experimentId"] = "ab43a99d-eddd-44f8-ac25-f368f02dbc21",
                ["agentId"] = "agent01",
                ["toolName"] = "tool01"
            };

            ArgumentException error = Assert.Throws<ArgumentException>(() => FileContext.ResolvePathTemplate(
                "/home/user/logs/{does_not_exist}", 
                replacements, 
                throwIfNotMatched: true));

            Assert.AreEqual(
                "Invalid path placeholder reference. The placeholder '{does_not_exist}' does not have a corresponding replacement. " +
                "This placeholder is either not a supported out-of-box option or is not defined in the metadata provided to the application " +
                "on the command line.",
                error.Message);
        }
    }
}
