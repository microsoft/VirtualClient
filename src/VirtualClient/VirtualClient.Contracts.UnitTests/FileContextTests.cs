using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Moq;
using NUnit.Framework;

namespace VirtualClient.Contracts
{
    [TestFixture]
    [Category("Unit")]
    internal class FileContextTests
    {
        private MockFixture mockFixture;

        public void SetupDefaults()
        {
            this.mockFixture = new MockFixture();
            this.mockFixture.Setup(PlatformID.Unix);
        }

        [Test]
        [TestCase("{experimentId}-{agentId}-summary.txt", "ab43a99d-eddd-44f8-ac25-f368f02dbc21-agent01-summary.txt")]
        [TestCase("{experimentId}-{toolName}-summary.txt", "ab43a99d-eddd-44f8-ac25-f368f02dbc21-tool01-summary.txt")]
        public void FileUploadDescriptorFactoryCreatesTheExpectedDescriptor_When_Timestamped(string pathTemplate, string expectedResolvedPath)
        {
            this.SetupDefaults();

            IDictionary<string, IConvertible> replacements = new Dictionary<string, IConvertible>
            {
                ["experimentId"] = "ab43a99d-eddd-44f8-ac25-f368f02dbc21",
                ["agentId"] = "agent01",
                ["toolName"] = "tool01"
            };

            string actualResolvedPath = FileContext.ResolvePathTemplate(pathTemplate, replacements);
            Assert.AreEqual(expectedResolvedPath, actualResolvedPath);
        }
    }
}
