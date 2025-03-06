// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.IO.Abstractions;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using VirtualClient.Contracts;

namespace VirtualClient
{
    [TestFixture]
    [Category("Unit")]
    public class PackageManagerExtensionsTests
    {
        private MockFixture mockFixture;
        private DependencyPath mockPackage;

        [SetUp]
        public void SetupTest()
        {
            this.mockFixture = new MockFixture();
            this.mockPackage = new DependencyPath("any_package", this.mockFixture.GetPackagePath("any_package.1.0.0"));

            this.mockFixture
                .Setup(PlatformID.Unix)
                .SetupPackage(this.mockPackage);
        }

        [Test]
        public async Task GetPackageExtensionReturnsTheExpectedPackage()
        {
            this.mockFixture.PackageManager.OnGetPackage(this.mockPackage.Name)
                .ReturnsAsync(this.mockPackage);

            DependencyPath package = await this.mockFixture.PackageManager.Object.GetPackageAsync(this.mockPackage.Name, CancellationToken.None);

            Assert.IsNotNull(package);
            Assert.IsTrue(object.ReferenceEquals(this.mockPackage, package));
        }

        [Test]
        public void GetPackageExtensionDefaultsThrowWhenThePackageIsNotFound()
        {
            this.mockFixture.PackageManager.OnGetPackage(this.mockPackage.Name)
                .ReturnsAsync(null as DependencyPath);

            DependencyException error = Assert.ThrowsAsync<DependencyException>(
                () => this.mockFixture.PackageManager.Object.GetPackageAsync(this.mockPackage.Name, CancellationToken.None, throwIfNotfound: true));

            Assert.AreEqual(ErrorReason.WorkloadDependencyMissing, error.Reason);
            Assert.AreEqual($"A package with the name '{this.mockPackage.Name}' was not found on the system.", error.Message);
        }

        [Test]
        public void GetPackageExtensionDoesNotThrowWhenAPackageIsNotFoundIfRequestedToDoOtherwise()
        {
            this.mockFixture.PackageManager.OnGetPackage(this.mockPackage.Name)
                .ReturnsAsync(null as DependencyPath);

            DependencyPath package = null;
            Assert.DoesNotThrowAsync(async() => package = await this.mockFixture.PackageManager.Object.GetPackageAsync(
                this.mockPackage.Name, 
                CancellationToken.None, 
                throwIfNotfound: false));

            Assert.IsNull(package);
        }
    }
}
