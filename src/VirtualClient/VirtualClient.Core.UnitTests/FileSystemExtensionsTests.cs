// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.IO.Abstractions;
    using System.Text;
    using System.Threading.Tasks;
    using NUnit.Framework;
    using Moq;
    using Polly;
    using System.IO;
    using System.Threading;
    using System.Text.RegularExpressions;

    [TestFixture]
    [Category("Unit")]
    public class FileSystemExtensionsTests
    {
        private Mock<IFile> mockFile;
        private Mock<ISyncPolicy> mockPolicy;

        [OneTimeSetUp]
        public void SetupTests()
        {
            this.mockFile = new Mock<IFile>();
            this.mockPolicy = new Mock<ISyncPolicy>();
        }

        [Test]
        [TestCase(null)]
        [TestCase("")]
        [TestCase(" ")]
        public void ThrowIfFileDoesNotExistValidatesStringParameters(string invalidParameter)
        {
            Assert.Throws<ArgumentException>(() => FileSystemExtensions.ThrowIfFileDoesNotExist(this.mockFile.Object, invalidParameter)); 
        }

        [Test]
        public void ThrowIfFileDoesNotExistValidatesNonStringParameters()
        {
            Assert.Throws<ArgumentException>(() => FileSystemExtensions.ThrowIfFileDoesNotExist(null, "some file"));
        }

        [Test]
        public void ThrowIfFileDoesNotExistThrowsExceptionIfFileDoesNotExist()
        {
            this.mockFile.Setup(f => f.Exists(It.IsAny<string>()))
                .Returns(false);

            Assert.Throws<FileNotFoundException>(() => FileSystemExtensions.ThrowIfFileDoesNotExist(this.mockFile.Object, "some file"));
        }

        [Test]
        public void ThrowIfFileDoesNotExistDoesNotThrowExceptionIfFileExists()
        {
            this.mockFile.Setup(f => f.Exists(It.IsAny<string>()))
                .Returns(true);

            Assert.DoesNotThrow(() => FileSystemExtensions.ThrowIfFileDoesNotExist(this.mockFile.Object, "some file"));
        }

        [Test]
        [TestCase(null)]
        [TestCase("")]
        [TestCase(" ")]
        public void DeleteRetryableValidatesStringParameters(string invalidParameter)
        {
            Assert.Throws<ArgumentException>(() => FileSystemExtensions.ThrowIfFileDoesNotExist(this.mockFile.Object, invalidParameter));
        }

        [Test]
        public void DeleteRetryableValidatesNonStringParameters()
        {
            Assert.Throws<ArgumentException>(() => FileSystemExtensions.ThrowIfFileDoesNotExist(null, "some string"));
        }

        [Test]
        public void DeleteRetrableAppliesGivenRetryPolicy()
        {
            int attempts = 0;
            IAsyncPolicy mockPolicy = Policy.Handle<Exception>().WaitAndRetryAsync(5, (retries) =>
            {
                attempts++;
                return TimeSpan.Zero;
            });

            this.mockFile.Setup(f => f.Delete(It.IsAny<string>()))
                .Throws(new IOException());

            Assert.ThrowsAsync<IOException>(() => FileSystemExtensions.DeleteAsync(this.mockFile.Object, "some file", mockPolicy));
            Assert.IsTrue(attempts == 5);
        }

        [Test]
        public void FileSystemExtensionsThrowsIfFileDoesNotExistsForTextReplace()
        {
            this.mockFile.Setup(f => f.Exists(It.IsAny<string>()))
                .Returns(false);

            Assert.ThrowsAsync<FileNotFoundException>(() => FileSystemExtensions.ReplaceInFileAsync(this.mockFile.Object, "some file", "pat", "replace", CancellationToken.None));
        }

        [Test]
        [TestCase("aB1","replacement","start aB1 end", "start replacement end")]
        [TestCase("aB1", "replacement", "start aB1 middle aB1 end", "start replacement middle replacement end")]
        [TestCase("[vV][cC]", "replacement", "vc VC vC", "replacement replacement replacement")]
        [TestCase("aB1", "replacement", "start aB1 ab1 end", "start replacement ab1 end")]
        [TestCase("aB1", "replacement", "start aB1 ab1 end", "start replacement replacement end",RegexOptions.IgnoreCase)]

        public async Task FileSystemExtensionsReplaceInFileRunsAsExpected(string pattern, string replacement, string originalContent, string expectedContent,RegexOptions options = RegexOptions.None)
        {
            this.mockFile.Setup(f => f.Exists(It.IsAny<string>()))
                .Returns(true);
            this.mockFile.Setup(f => f.ReadAllTextAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(originalContent));

            this.mockFile.Setup(f => f.WriteAllTextAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Callback<string, string, CancellationToken>((path,cont,can) =>
                {
                    Assert.AreEqual(expectedContent, cont);
                })
                .Returns(Task.CompletedTask);

            await FileSystemExtensions.ReplaceInFileAsync(this.mockFile.Object, "some file", pattern, replacement, CancellationToken.None, options).ConfigureAwait(false);
        }
    }
}
