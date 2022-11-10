using System.IO;
using System.Reflection;
using NUnit.Framework;
using VirtualClient.Actions;

// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient
{
    [TestFixture]
    [Category("Unit")]
    public class DataTableExtensionsUnitTests
    {
        private string diskSpdResultRawText;

        [SetUp]
        public void Setup()
        {
            string workingDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string gbOutputPath = Path.Combine(workingDirectory, @"Examples\DiskSpdExample.txt");
            this.diskSpdResultRawText = File.ReadAllText(gbOutputPath);
        }

        [Test]
        public void DataTableExtensionCanSectionizeBasedOnRegex()
        {
            DiskSpdMetricsParser parser = new DiskSpdMetricsParser(this.diskSpdResultRawText);
            parser.Parse();
        }
    }
}