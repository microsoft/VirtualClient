// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;

namespace VirtualClient.Contracts
{
    [TestFixture]
    [Category("Unit")]
    public class OsReleaseFileParserUnitTests
    {
        private string rawText;
        private OsReleaseFileParser testParser;

        private string ExamplePath
        {
            get
            {
                // Great examples could be found at https://github.com/chef/os_release
                string workingDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                return Path.Combine(workingDirectory, "Examples", "os-release");
            }
        }

        [Test]
        public void OsReleaseFileParserrRecognizeFedoraOS()
        {
            string outputPath = Path.Combine(this.ExamplePath, "FedoraOSExample.txt");
            this.rawText = File.ReadAllText(outputPath);
            this.testParser = new OsReleaseFileParser(this.rawText);
            Assert.AreEqual(LinuxDistribution.Fedora, this.testParser.Parse().LinuxDistribution);
            Assert.AreEqual("Fedora 32 (Workstation Edition)", this.testParser.Parse().OperationSystemFullName);
        }

        [Test]
        public void OsReleaseFileParserrRecognizeUbuntu22()
        {
            string outputPath = Path.Combine(this.ExamplePath, "Ubuntu22Example.txt");
            this.rawText = File.ReadAllText(outputPath);
            this.testParser = new OsReleaseFileParser(this.rawText);
            Assert.AreEqual(LinuxDistribution.Ubuntu, this.testParser.Parse().LinuxDistribution);
            Assert.AreEqual("Ubuntu 22.04 LTS", this.testParser.Parse().OperationSystemFullName);
        }

        [Test]
        public void OsReleaseFileParserrRecognizeDebian11()
        {
            string outputPath = Path.Combine(this.ExamplePath, "Debian11Example.txt");
            this.rawText = File.ReadAllText(outputPath);
            this.testParser = new OsReleaseFileParser(this.rawText);
            Assert.AreEqual(LinuxDistribution.Debian, this.testParser.Parse().LinuxDistribution);
            Assert.AreEqual("Debian GNU/Linux 11 (bullseye)", this.testParser.Parse().OperationSystemFullName);
        }

        [Test]
        public void OsReleaseFileParserrRecognizeRHEL93()
        {
            string outputPath = Path.Combine(this.ExamplePath, "RHEL93Example.txt");
            this.rawText = File.ReadAllText(outputPath);
            this.testParser = new OsReleaseFileParser(this.rawText);
            Assert.AreEqual(LinuxDistribution.RHEL8, this.testParser.Parse().LinuxDistribution);
            Assert.AreEqual("Red Hat Enterprise Linux 9.3 (Plow)", this.testParser.Parse().OperationSystemFullName);
        }
    }
}