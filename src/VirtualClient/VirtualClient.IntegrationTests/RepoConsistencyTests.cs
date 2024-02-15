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
    [Category("Integration")]
    public class RepoConsistencyTests
    {
        /// <summary>
        /// This test converts files with utf-8-bom to utf-8. Modify to convert from any encoding to any other encoding.
        /// </summary>
        [Test]
        public void ChangeFilesToUtf8()
        {
            // Set the path to the directory where you want to change file encodings
            string directoryPath = @"E:\Source\Github\VirtualClient\src\VirtualClient";

            // Get the first two .cs files with UTF-8 BOM in the specified directory and its subdirectories
            var fileList = new DirectoryInfo(directoryPath)
                .GetFiles("*.json", SearchOption.AllDirectories)
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

            // Change the encoding of each file from UTF-8 BOM to UTF-8
            foreach (var file in fileList)
            {
                string content = File.ReadAllText(file.FullName);
                
                File.WriteAllText(file.FullName, content, new UTF8Encoding(false));
                Console.WriteLine($"Converted {file.FullName} to UTF-8");
            }

            Console.WriteLine("Encoding conversion completed.");
        }
    }
}
