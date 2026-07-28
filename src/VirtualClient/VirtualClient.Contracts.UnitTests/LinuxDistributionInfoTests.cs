// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Contracts
{
    using System.IO;
    using NUnit.Framework;

    [TestFixture]
    [Category("Unit")]
    public class LinuxDistributionInfoTests
    {
        // Great examples could be found at https://github.com/chef/os_release
        private static readonly string HostnamectlExamples = MockFixture.GetDirectory(
            typeof(LinuxDistributionInfoTests), 
            "TestResources",
            "Unix",
            "hostnamectl");

        private static readonly string OSReleaseExamples = MockFixture.GetDirectory(
            typeof(LinuxDistributionInfoTests), 
            "TestResources",
            "Unix",
            "os-release");

        [Test]
        [TestCase("debian_7", "Debian GNU/Linux 7 (wheezy)")]
        [TestCase("debian_8", "Debian GNU/Linux 8 (jessie)")]
        [TestCase("debian_9", "Debian GNU/Linux 9 (stretch)")]
        [TestCase("debian_10", "Debian GNU/Linux 10 (buster)")]
        [TestCase("debian_11", "Debian GNU/Linux 11 (bullseye)")]
        [TestCase("debian_12", "Debian GNU/Linux 12 (bookworm)")]
        [TestCase("debian_13", "Debian GNU/Linux 13 (trixie)")]
        public void LinuxDistributionInfoRecognizesDebianDistros_OS_Release_File_Contents(string example, string expectedName)
        {
            string releaseDetails = File.ReadAllText(Path.Combine(OSReleaseExamples, example));
            LinuxDistributionInfo result = LinuxDistributionInfo.Create(releaseDetails);

            Assert.AreEqual(LinuxDistribution.Debian, result.Distribution);
            Assert.AreEqual(LinuxUpstreamDistribution.Debian, result.UpstreamDistribution);
            Assert.AreEqual(expectedName, result.Name);
            Assert.AreEqual(releaseDetails, result.Details);
        }

        [Test]
        [TestCase("debian_7", "Debian GNU/Linux 7 (wheezy)")]
        [TestCase("debian_8", "Debian GNU/Linux 8 (jessie)")]
        [TestCase("debian_9", "Debian GNU/Linux 9 (stretch)")]
        [TestCase("debian_10", "Debian GNU/Linux 10 (buster)")]
        [TestCase("debian_11", "Debian GNU/Linux 11 (bullseye)")]
        [TestCase("debian_12", "Debian GNU/Linux 12 (bookworm)")]
        [TestCase("debian_13", "Debian GNU/Linux 13 (trixie)")]
        public void LinuxDistributionInfoRecognizesDebianDistros_Hostnamectl_Output(string example, string expectedName)
        {
            string releaseDetails = File.ReadAllText(Path.Combine(HostnamectlExamples, example));
            LinuxDistributionInfo result = LinuxDistributionInfo.Create(releaseDetails);

            Assert.AreEqual(LinuxDistribution.Debian, result.Distribution);
            Assert.AreEqual(LinuxUpstreamDistribution.Debian, result.UpstreamDistribution);
            Assert.AreEqual(expectedName, result.Name);
            Assert.AreEqual(releaseDetails, result.Details);
        }

        [Test]
        [TestCase("fedora_28", "Fedora 28 (Cloud Edition)")]
        [TestCase("fedora_29", "Fedora 29 (Container Image)")]
        [TestCase("fedora_30", "Fedora 30 (Container Image)")]
        [TestCase("fedora_31", "Fedora 31 (Container Image)")]
        [TestCase("fedora_32", "Fedora 32 (Container Image)")]
        [TestCase("fedora_33", "Fedora 33 (Container Image)")]
        [TestCase("fedora_34", "Fedora 34 (Container Image)")]
        [TestCase("fedora_35", "Fedora Linux 35 (Container Image)")]
        [TestCase("fedora_36", "Fedora Linux 36 (Container Image)")]
        [TestCase("fedora_37", "Fedora Linux 37 (Container Image)")]
        [TestCase("fedora_38", "Fedora Linux 38 (Workstation Edition)")]
        [TestCase("fedora_39", "Fedora Linux 39 (Workstation Edition)")]
        [TestCase("fedora_40", "Fedora Linux 40 (Workstation Edition)")]
        [TestCase("fedora_41", "Fedora Linux 41 (Container Image)")]
        [TestCase("fedora_42", "Fedora Linux 42 (Container Image)")]
        [TestCase("fedora_43", "Fedora Linux 43 (Container Image)")]
        [TestCase("fedora_44", "Fedora Linux 44 (KDE Plasma Desktop Edition)")]
        public void LinuxDistributionInfoRecognizesFedoraDistros_OS_Release_File_Contents(string example, string expectedName)
        {
            string releaseDetails = File.ReadAllText(Path.Combine(OSReleaseExamples, example));
            LinuxDistributionInfo result = LinuxDistributionInfo.Create(releaseDetails);

            Assert.AreEqual(LinuxDistribution.Fedora, result.Distribution);
            Assert.AreEqual(LinuxUpstreamDistribution.Fedora, result.UpstreamDistribution);
            Assert.AreEqual(expectedName, result.Name);
            Assert.AreEqual(releaseDetails, result.Details);
        }

        [Test]
        [TestCase("fedora_28", "Fedora 28 (Cloud Edition)")]
        [TestCase("fedora_29", "Fedora 29 (Container Image)")]
        [TestCase("fedora_30", "Fedora 30 (Container Image)")]
        [TestCase("fedora_31", "Fedora 31 (Container Image)")]
        [TestCase("fedora_32", "Fedora 32 (Container Image)")]
        [TestCase("fedora_33", "Fedora 33 (Container Image)")]
        [TestCase("fedora_34", "Fedora 34 (Container Image)")]
        [TestCase("fedora_35", "Fedora Linux 35 (Container Image)")]
        [TestCase("fedora_36", "Fedora Linux 36 (Container Image)")]
        [TestCase("fedora_37", "Fedora Linux 37 (Container Image)")]
        [TestCase("fedora_38", "Fedora Linux 38 (Workstation Edition)")]
        [TestCase("fedora_39", "Fedora Linux 39 (Workstation Edition)")]
        [TestCase("fedora_40", "Fedora Linux 40 (Workstation Edition)")]
        [TestCase("fedora_41", "Fedora Linux 41 (Container Image)")]
        [TestCase("fedora_42", "Fedora Linux 42 (Container Image)")]
        [TestCase("fedora_43", "Fedora Linux 43 (Container Image)")]
        [TestCase("fedora_44", "Fedora Linux 44 (KDE Plasma Desktop Edition)")]
        public void LinuxDistributionInfoRecognizesFedoraDistros_Hostnamectl_Output(string example, string expectedName)
        {
            string releaseDetails = File.ReadAllText(Path.Combine(HostnamectlExamples, example));
            LinuxDistributionInfo result = LinuxDistributionInfo.Create(releaseDetails);

            Assert.AreEqual(LinuxDistribution.Fedora, result.Distribution);
            Assert.AreEqual(LinuxUpstreamDistribution.Fedora, result.UpstreamDistribution);
            Assert.AreEqual(expectedName, result.Name);
            Assert.AreEqual(releaseDetails, result.Details);
        }

        [Test]
        [TestCase("opensuse_13", "openSUSE 13.2 (Harlequin) (x86_64)")]
        [TestCase("opensuseleap_15", "openSUSE Leap 15.6")]
        [TestCase("opensuseleap_16", "openSUSE Leap 16.0")]
        [TestCase("opensuseleap_42_3", "openSUSE Leap 42.3")]
        public void LinuxDistributionInfoRecognizesSuseDistros_OS_Release_File_Contents(string example, string expectedName)
        {
            string releaseDetails = File.ReadAllText(Path.Combine(OSReleaseExamples, example));
            LinuxDistributionInfo result = LinuxDistributionInfo.Create(releaseDetails);

            Assert.AreEqual(LinuxDistribution.OpenSuse, result.Distribution);
            Assert.AreEqual(LinuxUpstreamDistribution.OpenSuse, result.UpstreamDistribution);
            Assert.AreEqual(expectedName, result.Name);
            Assert.AreEqual(releaseDetails, result.Details);
        }

        [Test]
        [TestCase("opensuse_13", "openSUSE 13.2 (Harlequin) (x86_64)")]
        [TestCase("opensuseleap_15", "openSUSE Leap 15.6")]
        [TestCase("opensuseleap_16", "openSUSE Leap 16.0")]
        [TestCase("opensuseleap_42_3", "openSUSE Leap 42.3")]
        public void LinuxDistributionInfoRecognizesSuseDistros_Hostnamectl_Output(string example, string expectedName)
        {
            string releaseDetails = File.ReadAllText(Path.Combine(HostnamectlExamples, example));
            LinuxDistributionInfo result = LinuxDistributionInfo.Create(releaseDetails);

            Assert.AreEqual(LinuxDistribution.OpenSuse, result.Distribution);
            Assert.AreEqual(LinuxUpstreamDistribution.OpenSuse, result.UpstreamDistribution);
            Assert.AreEqual(expectedName, result.Name);
            Assert.AreEqual(releaseDetails, result.Details);
        }

        [Test]
        [TestCase("amazon_2018", "Amazon Linux AMI 2018.03")]
        [TestCase("amazon_2023", "Amazon Linux 2023.8.20250915")]
        public void LinuxDistributionInfoRecognizesAmazonLinuxDistros_OS_Release_File_Contents(string example, string expectedName)
        {
            string releaseDetails = File.ReadAllText(Path.Combine(OSReleaseExamples, example));
            LinuxDistributionInfo result = LinuxDistributionInfo.Create(releaseDetails);

            Assert.AreEqual(LinuxDistribution.AmazonLinux, result.Distribution);
            Assert.AreEqual(LinuxUpstreamDistribution.Fedora, result.UpstreamDistribution);
            Assert.AreEqual(expectedName, result.Name);
            Assert.AreEqual(releaseDetails, result.Details);
        }

        [Test]
        [TestCase("amazon_2", "Amazon Linux 2")]
        [TestCase("amazon_2018", "Amazon Linux AMI 2018.03")]
        [TestCase("amazon_2023", "Amazon Linux 2023.8.20250915")]
        public void LinuxDistributionInfoRecognizesAmazonLinuxDistros_Hostnamectl_Output(string example, string expectedName)
        {
            string releaseDetails = File.ReadAllText(Path.Combine(HostnamectlExamples, example));
            LinuxDistributionInfo result = LinuxDistributionInfo.Create(releaseDetails);

            Assert.AreEqual(LinuxDistribution.AmazonLinux, result.Distribution);
            Assert.AreEqual(LinuxUpstreamDistribution.Fedora, result.UpstreamDistribution);
            Assert.AreEqual(expectedName, result.Name);
            Assert.AreEqual(releaseDetails, result.Details);
        }

        [Test]
        [TestCase("azurelinux_3", "Microsoft Azure Linux 3.0")]
        public void LinuxDistributionInfoRecognizesAzureLinux3Distros_OS_Release_File_Contents(string example, string expectedName)
        {
            string releaseDetails = File.ReadAllText(Path.Combine(OSReleaseExamples, example));
            LinuxDistributionInfo result = LinuxDistributionInfo.Create(releaseDetails);

            Assert.AreEqual(LinuxDistribution.AzureLinux, result.Distribution);
            Assert.AreEqual(LinuxUpstreamDistribution.Fedora, result.UpstreamDistribution);
            Assert.AreEqual(expectedName, result.Name);
            Assert.AreEqual(releaseDetails, result.Details);
        }

        [Test]
        [TestCase("azurelinux_3", "Microsoft Azure Linux 3.0")]
        public void LinuxDistributionInfoRecognizesAzureLinux3Distros_Hostnamectl_Output(string example, string expectedName)
        {
            string releaseDetails = File.ReadAllText(Path.Combine(HostnamectlExamples, example));
            LinuxDistributionInfo result = LinuxDistributionInfo.Create(releaseDetails);

            Assert.AreEqual(LinuxDistribution.AzureLinux, result.Distribution);
            Assert.AreEqual(LinuxUpstreamDistribution.Fedora, result.UpstreamDistribution);
            Assert.AreEqual(expectedName, result.Name);
            Assert.AreEqual(releaseDetails, result.Details);
        }

        [Test]
        [TestCase("azurelinux_4", "Azure Linux 4.0 (Cloud Variant Beta)")]
        public void LinuxDistributionInfoRecognizesAzureLinux4Distros_OS_Release_File_Contents(string example, string expectedName)
        {
            string releaseDetails = File.ReadAllText(Path.Combine(OSReleaseExamples, example));
            LinuxDistributionInfo result = LinuxDistributionInfo.Create(releaseDetails);

            Assert.AreEqual(LinuxDistribution.AzureLinux, result.Distribution);
            Assert.AreEqual(LinuxUpstreamDistribution.Fedora, result.UpstreamDistribution);
            Assert.AreEqual(expectedName, result.Name);
            Assert.AreEqual(releaseDetails, result.Details);
        }

        [Test]
        [TestCase("azurelinux_4", "Azure Linux 4.0 (Cloud Variant Beta)")]
        public void LinuxDistributionInfoRecognizesAzureLinux4Distros_Hostnamectl_Output(string example, string expectedName)
        {
            string releaseDetails = File.ReadAllText(Path.Combine(HostnamectlExamples, example));
            LinuxDistributionInfo result = LinuxDistributionInfo.Create(releaseDetails);

            Assert.AreEqual(LinuxDistribution.AzureLinux, result.Distribution);
            Assert.AreEqual(LinuxUpstreamDistribution.Fedora, result.UpstreamDistribution);
            Assert.AreEqual(expectedName, result.Name);
            Assert.AreEqual(releaseDetails, result.Details);
        }

        [Test]
        [TestCase("centos_7", "CentOS Linux 7 (Core)")]
        [TestCase("centos_8", "CentOS Linux 8")]
        [TestCase("centos_stream_8", "CentOS Stream 8")]
        [TestCase("centos_stream_9", "CentOS Stream 9")]
        public void LinuxDistributionInfoRecognizesCentOSDistros_OS_Release_File_Contents(string example, string expectedName)
        {
            string releaseDetails = File.ReadAllText(Path.Combine(OSReleaseExamples, example));
            LinuxDistributionInfo result = LinuxDistributionInfo.Create(releaseDetails);

            Assert.AreEqual(LinuxDistribution.CentOS, result.Distribution);
            Assert.AreEqual(LinuxUpstreamDistribution.Fedora, result.UpstreamDistribution);
            Assert.AreEqual(expectedName, result.Name);
            Assert.AreEqual(releaseDetails, result.Details);
        }

        [Test]
        [TestCase("centos_7", "CentOS Linux 7 (Core)")]
        [TestCase("centos_8", "CentOS Linux 8")]
        [TestCase("centos_stream_8", "CentOS Stream 8")]
        [TestCase("centos_stream_9", "CentOS Stream 9")]
        public void LinuxDistributionInfoRecognizesCentOSDistros_Hostnamectl_Output(string example, string expectedName)
        {
            string releaseDetails = File.ReadAllText(Path.Combine(HostnamectlExamples, example));
            LinuxDistributionInfo result = LinuxDistributionInfo.Create(releaseDetails);

            Assert.AreEqual(LinuxDistribution.CentOS, result.Distribution);
            Assert.AreEqual(LinuxUpstreamDistribution.Fedora, result.UpstreamDistribution);
            Assert.AreEqual(expectedName, result.Name);
            Assert.AreEqual(releaseDetails, result.Details);
        }

        [Test]
        [TestCase("flatcar", "Flatcar Container Linux by Kinvolk 4459.0.0 (Oklo)")]
        public void LinuxDistributionInfoRecognizesFlatcarDistros_OS_Release_File_Contents(string example, string expectedName)
        {
            string releaseDetails = File.ReadAllText(Path.Combine(OSReleaseExamples, example));
            LinuxDistributionInfo result = LinuxDistributionInfo.Create(releaseDetails);

            Assert.AreEqual(LinuxDistribution.Flatcar, result.Distribution);
            Assert.AreEqual(LinuxUpstreamDistribution.Gentoo, result.UpstreamDistribution);
            Assert.AreEqual(expectedName, result.Name);
            Assert.AreEqual(releaseDetails, result.Details);
        }

        [Test]
        [TestCase("flatcar", "Flatcar Container Linux by Kinvolk 4459.0.0 (Oklo)")]
        public void LinuxDistributionInfoRecognizesFlatcarDistros_Hostnamectl_Output(string example, string expectedName)
        {
            string releaseDetails = File.ReadAllText(Path.Combine(HostnamectlExamples, example));
            LinuxDistributionInfo result = LinuxDistributionInfo.Create(releaseDetails);

            Assert.AreEqual(LinuxDistribution.Flatcar, result.Distribution);
            Assert.AreEqual(LinuxUpstreamDistribution.Gentoo, result.UpstreamDistribution);
            Assert.AreEqual(expectedName, result.Name);
            Assert.AreEqual(releaseDetails, result.Details);
        }

        [Test]
        [TestCase("gentoo_214", "Gentoo/Linux")]
        [TestCase("gentoo_218", "Gentoo Linux")]
        public void LinuxDistributionInfoRecognizesGentooDistros_OS_Release_File_Contents(string example, string expectedName)
        {
            string releaseDetails = File.ReadAllText(Path.Combine(OSReleaseExamples, example));
            LinuxDistributionInfo result = LinuxDistributionInfo.Create(releaseDetails);

            Assert.AreEqual(LinuxDistribution.Gentoo, result.Distribution);
            Assert.AreEqual(LinuxUpstreamDistribution.Gentoo, result.UpstreamDistribution);
            Assert.AreEqual(expectedName, result.Name);
            Assert.AreEqual(releaseDetails, result.Details);
        }

        [Test]
        [TestCase("gentoo_214", "Gentoo/Linux")]
        [TestCase("gentoo_218", "Gentoo Linux")]
        public void LinuxDistributionInfoRecognizesGentooDistros_Hostnamectl_Output(string example, string expectedName)
        {
            string releaseDetails = File.ReadAllText(Path.Combine(HostnamectlExamples, example));
            LinuxDistributionInfo result = LinuxDistributionInfo.Create(releaseDetails);

            Assert.AreEqual(LinuxDistribution.Gentoo, result.Distribution);
            Assert.AreEqual(LinuxUpstreamDistribution.Gentoo, result.UpstreamDistribution);
            Assert.AreEqual(expectedName, result.Name);
            Assert.AreEqual(releaseDetails, result.Details);
        }

        [Test]
        [TestCase("redhat_7", "Red Hat Enterprise Linux Server 7.5 (Maipo)")]
        [TestCase("redhat_8", "Red Hat Enterprise Linux 8.10 (Ootpa)")]
        [TestCase("redhat_9", "Red Hat Enterprise Linux 9.6 (Plow)")]
        [TestCase("redhat_10", "Red Hat Enterprise Linux 10.0 (Coughlan)")]
        public void LinuxDistributionInfoRecognizesRedHatDistros_OS_Release_File_Contents(string example, string expectedName)
        {
            string releaseDetails = File.ReadAllText(Path.Combine(OSReleaseExamples, example));
            LinuxDistributionInfo result = LinuxDistributionInfo.Create(releaseDetails);

            Assert.AreEqual(LinuxDistribution.RedHat, result.Distribution);
            Assert.AreEqual(LinuxUpstreamDistribution.Fedora, result.UpstreamDistribution);
            Assert.AreEqual(expectedName, result.Name);
            Assert.AreEqual(releaseDetails, result.Details);
        }

        [Test]
        [TestCase("redhat_7", "Red Hat Enterprise Linux Server 7.5 (Maipo)")]
        [TestCase("redhat_8", "Red Hat Enterprise Linux 8.10 (Ootpa)")]
        [TestCase("redhat_9", "Red Hat Enterprise Linux 9.6 (Plow)")]
        [TestCase("redhat_10", "Red Hat Enterprise Linux 10.0 (Coughlan)")]
        public void LinuxDistributionInfoRecognizesRedHatDistros_Hostnamectl_Output(string example, string expectedName)
        {
            string releaseDetails = File.ReadAllText(Path.Combine(HostnamectlExamples, example));
            LinuxDistributionInfo result = LinuxDistributionInfo.Create(releaseDetails);

            Assert.AreEqual(LinuxDistribution.RedHat, result.Distribution);
            Assert.AreEqual(LinuxUpstreamDistribution.Fedora, result.UpstreamDistribution);
            Assert.AreEqual(expectedName, result.Name);
            Assert.AreEqual(releaseDetails, result.Details);
        }

        [Test]
        [TestCase("ubuntu_1404", "Ubuntu 14.04.6 LTS")]
        [TestCase("ubuntu_1604", "Ubuntu 16.04.7 LTS")]
        [TestCase("ubuntu_1804", "Ubuntu 18.04.6 LTS")]
        [TestCase("ubuntu_2004", "Ubuntu 20.04.6 LTS")]
        [TestCase("ubuntu_2204", "Ubuntu 22.04.5 LTS")]
        [TestCase("ubuntu_2404", "Ubuntu 24.04.3 LTS")]
        [TestCase("ubuntu_2504", "Ubuntu 25.04")]
        [TestCase("ubuntu_2510", "Ubuntu 25.10")]
        [TestCase("ubuntu_2604", "Ubuntu 26.04 LTS")]
        public void LinuxDistributionInfoRecognizesUbuntuDistros_OS_Release_File_Contents(string example, string expectedName)
        {
            string releaseDetails = File.ReadAllText(Path.Combine(OSReleaseExamples, example));
            LinuxDistributionInfo result = LinuxDistributionInfo.Create(releaseDetails);

            Assert.AreEqual(LinuxDistribution.Ubuntu, result.Distribution);
            Assert.AreEqual(LinuxUpstreamDistribution.Debian, result.UpstreamDistribution);
            Assert.AreEqual(expectedName, result.Name);
            Assert.AreEqual(releaseDetails, result.Details);
        }

        [Test]
        [TestCase("ubuntu_1404", "Ubuntu 14.04.6 LTS")]
        [TestCase("ubuntu_1604", "Ubuntu 16.04.7 LTS")]
        [TestCase("ubuntu_1804", "Ubuntu 18.04.6 LTS")]
        [TestCase("ubuntu_2004", "Ubuntu 20.04.6 LTS")]
        [TestCase("ubuntu_2204", "Ubuntu 22.04.5 LTS")]
        [TestCase("ubuntu_2404", "Ubuntu 24.04.3 LTS")]
        [TestCase("ubuntu_2504", "Ubuntu 25.04")]
        [TestCase("ubuntu_2510", "Ubuntu 25.10")]
        [TestCase("ubuntu_2604", "Ubuntu 26.04 LTS")]
        public void LinuxDistributionInfoRecognizesUbuntuDistros_Hostnamectl_Output(string example, string expectedName)
        {
            string releaseDetails = File.ReadAllText(Path.Combine(HostnamectlExamples, example));
            LinuxDistributionInfo result = LinuxDistributionInfo.Create(releaseDetails);

            Assert.AreEqual(LinuxDistribution.Ubuntu, result.Distribution);
            Assert.AreEqual(LinuxUpstreamDistribution.Debian, result.UpstreamDistribution);
            Assert.AreEqual(expectedName, result.Name);
            Assert.AreEqual(releaseDetails, result.Details);
        }
    }
}