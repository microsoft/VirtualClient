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
    [Category("Unit")]
    [Ignore("We do not need to worry about byte order marks (BOM) existing in files anymore.")]
    public class RepoConsistencyTests
    {
        [Test]
        public void ValidateCSharpFilesDoNotHaveByteOrderMarkSequences()
        {
            this.ValidateFilesDoNotHaveByteOrderMarkSequences("*.cs", "C-Sharp/*.cs");
        }

        [Test]
        public void ValidateJsonFilesDoNotHaveByteOrderMarkSequences()
        {
            this.ValidateFilesDoNotHaveByteOrderMarkSequences("*.json", "JSON/*.json");
        }

        [Test]
        public void ValidateYamlFilesDoNotHaveByteOrderMarkSequences()
        {
            this.ValidateFilesDoNotHaveByteOrderMarkSequences("*.yml", "YAML/*.yml");
            this.ValidateFilesDoNotHaveByteOrderMarkSequences("*.yaml", "YAML/*.yaml");
        }

        private void ValidateFilesDoNotHaveByteOrderMarkSequences(string fileExtension, string fileType)
        {
            DirectoryInfo currentDirectory = new DirectoryInfo(Directory.GetCurrentDirectory());
            DirectoryInfo repoRootDirectory = null;

            while (currentDirectory != null)
            {
                if (currentDirectory.GetDirectories(".git")?.Any() == true)
                {
                    repoRootDirectory = currentDirectory;
                    break;
                }

                currentDirectory = currentDirectory.Parent;
            }

            if (repoRootDirectory == null)
            {
                throw new FileNotFoundException("Could not locate the root directory of the Git repo.");
            }

            IEnumerable<FileInfo> fileList = new DirectoryInfo(repoRootDirectory.FullName)
                .GetFiles(fileExtension, SearchOption.AllDirectories)
                .Where(file => !file.FullName.Contains("node_modules"));

            IEnumerable<FileInfo> flaggedFiles = fileList.Where(file =>
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

            Assert.AreEqual(
                0,
                flaggedFiles.Count(),
                $"Invalid file encodings. The repo has {fileType} files that are UTF-8 encoded with a byte-order mark (BOM) sequence. Open and save the following files " +
                $"without the byte-order mark: {Environment.NewLine}{string.Join($"{Environment.NewLine}{Environment.NewLine}", flaggedFiles.Select(f => f.FullName).OrderBy(path => path))}");
        }
    }
}
