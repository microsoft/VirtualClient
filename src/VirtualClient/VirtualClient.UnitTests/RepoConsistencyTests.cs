// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.IO.Abstractions;
    using System.Linq;
    using System.Runtime.InteropServices;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using Moq;
    using NUnit.Framework;
    using VirtualClient.Common.Contracts;
    using VirtualClient.Contracts;

    [TestFixture]
    [Category("UnitTests")]
    public class RepoConsistencyTests
    {
        /// <summary>
        /// This test detects 
        /// </summary>
        [Test]
        public void DetectUtf8EncodingInCSharp()
        {
            DirectoryInfo currentDirectory = new DirectoryInfo(Directory.GetCurrentDirectory());
            bool repoRootFound = false;
            while (currentDirectory != null)
            {
                if (currentDirectory.GetDirectories(".git")?.Any() == true)
                {
                    repoRootFound = true;
                    break;
                }

                currentDirectory = currentDirectory.Parent;
            }

            if (!repoRootFound)
            {
                throw new FileNotFoundException("Could not locate profiles.");
            }

            // Set the path to the directory where you want to change file encodings
            string directoryPath = Path.Combine(currentDirectory.FullName, "src", "VirtualClient");

            // Get the first two .cs files with UTF-8 BOM in the specified directory and its subdirectories
            var fileList = new DirectoryInfo(directoryPath)
                .GetFiles("*.cs", SearchOption.AllDirectories)
                .Where(file =>
                {
                    using (FileStream fileStream = new FileStream(file.FullName, FileMode.Open, FileAccess.Read))
                    {
                        if (fileStream.Length >= 3)
                        {
                            byte[] buffer = new byte[3];
                            fileStream.Read(buffer, 0, 3);

                            // Check if the first three bytes match the UTF-8 BOM
                            return (buffer[0] == 0xEF && buffer[1] == 0xBB && buffer[2] == 0xBF);
                        }

                        return false;
                    }
                }).ToList();

            Assert.AreEqual(0, fileList.Count, $"You have files in encoding utf-8 with BOM: {string.Join(',', fileList)}. " +
                $"You could manually convert them or use the integrationtests in VirtualClient.IntegrationTests.RepoConsistencyTests");
        }
    }
}
