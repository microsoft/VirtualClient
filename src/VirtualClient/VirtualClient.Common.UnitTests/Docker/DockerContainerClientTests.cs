// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Common.Docker
{
    using System;
    using System.Runtime.InteropServices;
    using NUnit.Framework;

    [TestFixture]
    [Category("Unit")]
    public class DockerContainerClientTests
    {
        [Test]
        [TestCase("linux", "amd64", "", PlatformID.Unix, Architecture.X64)]
        [TestCase("linux", "arm64", "", PlatformID.Unix, Architecture.Arm64)]
        [TestCase("linux", "arm", "v8", PlatformID.Unix, Architecture.Arm64)]
        [TestCase("linux", "arm", "v7", PlatformID.Unix, Architecture.Arm)]
        [TestCase("linux", "386", "", PlatformID.Unix, Architecture.X86)]
        [TestCase("linux", "x86_64", "", PlatformID.Unix, Architecture.X64)]
        [TestCase("linux", "aarch64", "", PlatformID.Unix, Architecture.Arm64)]
        [TestCase("windows", "amd64", "", PlatformID.Win32NT, Architecture.X64)]
        public void ParsePlatformFromInspectJsonReturnsExpectedPlatformAndArchitecture(
            string os, string arch, string variant, PlatformID expectedPlatform, Architecture expectedArch)
        {
            string json = $@"[{{""Os"":""{os}"",""Architecture"":""{arch}"",""Variant"":""{variant}""}}]";

            var (platform, architecture) = DockerContainerClient.ParsePlatformFromInspectJson(json);

            Assert.AreEqual(expectedPlatform, platform);
            Assert.AreEqual(expectedArch, architecture);
        }

        [Test]
        public void ParsePlatformFromInspectJsonThrowsOnUnsupportedOs()
        {
            string json = @"[{""Os"":""freebsd"",""Architecture"":""amd64"",""Variant"":""""}]";

            Assert.Throws<NotSupportedException>(() =>
                DockerContainerClient.ParsePlatformFromInspectJson(json));
        }

        [Test]
        public void ParsePlatformFromInspectJsonThrowsOnUnsupportedArchitecture()
        {
            string json = @"[{""Os"":""linux"",""Architecture"":""mips"",""Variant"":""""}]";

            Assert.Throws<NotSupportedException>(() =>
                DockerContainerClient.ParsePlatformFromInspectJson(json));
        }

        [Test]
        public void ParsePlatformFromInspectJsonThrowsOnInvalidJson()
        {
            Assert.Throws<ArgumentException>(() =>
                DockerContainerClient.ParsePlatformFromInspectJson("not json"));
        }

        [Test]
        public void ParsePlatformFromInspectJsonThrowsOnEmptyArray()
        {
            Assert.Throws<ArgumentException>(() =>
                DockerContainerClient.ParsePlatformFromInspectJson("[]"));
        }

        [Test]
        public void ParsePlatformFromInspectJsonHandlesRealUbuntuInspectOutput()
        {
            // Mirrors actual 'docker image inspect ubuntu:24.04' JSON structure
            string json = @"[{
                ""Id"": ""sha256:abc123"",
                ""Os"": ""linux"",
                ""Architecture"": ""amd64"",
                ""Variant"": """",
                ""Config"": {}
            }]";

            var (platform, architecture) = DockerContainerClient.ParsePlatformFromInspectJson(json);

            Assert.AreEqual(PlatformID.Unix, platform);
            Assert.AreEqual(Architecture.X64, architecture);
        }
    }
}
