using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;

// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Contracts
{
    [TestFixture]
    [Category("Unit")]
    public class HostnamectlParserUnitTests
    {
        private string rawText;
        private HostnamectlParser testParser;

        private string ExamplePath
        {
            get
            {
                string workingDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                return Path.Combine(workingDirectory, "Examples", "hostnamectl");
            }
        }

        [Test]
        public void HostnamectlParserRecognizeUbuntu1804()
        {
            string outputPath = Path.Combine(this.ExamplePath, "Ubuntu1804Example.txt");
            this.rawText = File.ReadAllText(outputPath);
            this.testParser = new HostnamectlParser(this.rawText);
            Assert.AreEqual(LinuxDistribution.Ubuntu, this.testParser.Parse().LinuxDistribution);
            Assert.AreEqual("Ubuntu 18.04.6 LTS", this.testParser.Parse().OperationSystemFullName);
        }

        [Test]
        public void HostnamectlParserRecognizeUbuntu2004()
        {
            string outputPath = Path.Combine(this.ExamplePath, "Ubuntu2004Example.txt");
            this.rawText = File.ReadAllText(outputPath);
            this.testParser = new HostnamectlParser(this.rawText);
            Assert.AreEqual(LinuxDistribution.Ubuntu, this.testParser.Parse().LinuxDistribution);
            Assert.AreEqual("Ubuntu 20.04.4 LTS", this.testParser.Parse().OperationSystemFullName);
        }

        [Test]
        public void HostnamectlParserRecognizeDebian10()
        {
            string outputPath = Path.Combine(this.ExamplePath, "Debian10Example.txt");
            this.rawText = File.ReadAllText(outputPath);
            this.testParser = new HostnamectlParser(this.rawText);
            Assert.AreEqual(LinuxDistribution.Debian, this.testParser.Parse().LinuxDistribution);
            Assert.AreEqual("Debian GNU/Linux 10 (buster)", this.testParser.Parse().OperationSystemFullName);
        }

        [Test]
        public void HostnamectlParserRecognizeRedhat83()
        {
            string outputPath = Path.Combine(this.ExamplePath, "RHEL8Example.txt");
            this.rawText = File.ReadAllText(outputPath);
            this.testParser = new HostnamectlParser(this.rawText);
            Assert.AreEqual(LinuxDistribution.RHEL8, this.testParser.Parse().LinuxDistribution);
            Assert.AreEqual("Red Hat Enterprise Linux 8.3 (Ootpa)", this.testParser.Parse().OperationSystemFullName);
        }

        [Test]
        public void HostnamectlParserRecognizeFlatcar()
        {
            string outputPath = Path.Combine(this.ExamplePath, "FlatcarExample.txt");
            this.rawText = File.ReadAllText(outputPath);
            this.testParser = new HostnamectlParser(this.rawText);
            Assert.AreEqual(LinuxDistribution.Flatcar, this.testParser.Parse().LinuxDistribution);
            Assert.AreEqual("Flatcar Container Linux by Kinvolk 3033.2.4 (Oklo)", this.testParser.Parse().OperationSystemFullName);
        }

        [Test]
        public void HostnamectlParserRecognizeCentOS7()
        {
            string outputPath = Path.Combine(this.ExamplePath, "CentOS7Example.txt");
            this.rawText = File.ReadAllText(outputPath);
            this.testParser = new HostnamectlParser(this.rawText);
            Assert.AreEqual(LinuxDistribution.CentOS7, this.testParser.Parse().LinuxDistribution);
            Assert.AreEqual("CentOS Linux 7 (AltArch)", this.testParser.Parse().OperationSystemFullName);
        }

        [Test]
        public void HostnamectlParserRecognizeCentOS8()
        {
            string outputPath = Path.Combine(this.ExamplePath, "CentOS8Example.txt");
            this.rawText = File.ReadAllText(outputPath);
            this.testParser = new HostnamectlParser(this.rawText);
            Assert.AreEqual(LinuxDistribution.CentOS8, this.testParser.Parse().LinuxDistribution);
            Assert.AreEqual("CentOS Linux 8 (Core)", this.testParser.Parse().OperationSystemFullName);
        }

        [Test]
        public void HostnamectlParserRecognizeMariner()
        {
            string outputPath = Path.Combine(this.ExamplePath, "MarinerExample.txt");
            this.rawText = File.ReadAllText(outputPath);
            this.testParser = new HostnamectlParser(this.rawText);
            Assert.AreEqual(LinuxDistribution.Mariner, this.testParser.Parse().LinuxDistribution);
            Assert.AreEqual("CBL-Mariner/Linux", this.testParser.Parse().OperationSystemFullName);
        }

        [Test]
        public void HostnamectlParserRecognizeSUSE()
        {
            string outputPath = Path.Combine(this.ExamplePath, "SUSE15Example.txt");
            this.rawText = File.ReadAllText(outputPath);
            this.testParser = new HostnamectlParser(this.rawText);
            Assert.AreEqual(LinuxDistribution.SUSE, this.testParser.Parse().LinuxDistribution);
            Assert.AreEqual("SUSE Linux Enterprise Server 15 SP3", this.testParser.Parse().OperationSystemFullName);
        }

        [Test]
        public void HostnamectlParserRecognizeRedhat93()
        {
            string outputPath = Path.Combine(this.ExamplePath, "RHEL9Example.txt");
            this.rawText = File.ReadAllText(outputPath);
            this.testParser = new HostnamectlParser(this.rawText);
            Assert.AreEqual(LinuxDistribution.RHEL8, this.testParser.Parse().LinuxDistribution);
            Assert.AreEqual("Red Hat Enterprise Linux 9.3 (Plow)", this.testParser.Parse().OperationSystemFullName);
        }
    }
}