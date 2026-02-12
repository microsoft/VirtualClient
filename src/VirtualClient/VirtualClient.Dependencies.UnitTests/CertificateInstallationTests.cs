// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Dependencies
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Net.Http;
    using System.Security.Cryptography;
    using System.Security.Cryptography.X509Certificates;
    using System.Threading;
    using System.Threading.Tasks;
    using Azure.Core;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.DependencyInjection.Extensions;
    using Moq;
    using NUnit.Framework;
    using Polly;
    using VirtualClient.Common.Telemetry;
    using VirtualClient.Contracts;
    using VirtualClient.Identity;

    [TestFixture]
    [Category("Unit")]
    public class CertificateInstallationTests
    {
        private MockFixture mockFixture;
        private X509Certificate2 testCertificate;

        [SetUp]
        public void SetupDefaultBehaviors()
        {
            this.mockFixture = new MockFixture();

            using (RSA rsa = RSA.Create(2048))
            {
                var request = new CertificateRequest(
                    "CN=TestCertificate",
                    rsa,
                    HashAlgorithmName.SHA256,
                    RSASignaturePadding.Pkcs1);

                this.testCertificate = request.CreateSelfSigned(DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddYears(1));
            }
        }

        [TearDown]
        public void Cleanup()
        {
            this.testCertificate?.Dispose();
        }

        [Test]
        public async Task InitializeAsync_LoadsAccessTokenFromParameter()
        {
            this.mockFixture.Setup(PlatformID.Win32NT);

            string expectedToken = "test-access-token-12345";
            this.mockFixture.Parameters = new Dictionary<string, IConvertible>()
            {
                { nameof(CertificateInstallation.AccessToken), expectedToken },
                { nameof(CertificateInstallation.CertificateName), "testCert" },
                { nameof(CertificateInstallation.KeyVaultUri), "https://testvault.vault.azure.net/" }
            };

            using (TestCertificateInstallation component = new TestCertificateInstallation(this.mockFixture.Dependencies, this.mockFixture.Parameters))
            {
                await component.InitializeAsync(EventContext.None, CancellationToken.None);
                Assert.AreEqual(expectedToken, component.AccessToken);
            }
        }

        [Test]
        public async Task InitializeAsync_LoadsAccessTokenFromFile()
        {
            this.mockFixture.Setup(PlatformID.Win32NT);

            string expectedToken = "file-access-token-67890";
            string tokenFilePath = "/tmp/token.txt";

            this.mockFixture.File.Setup(f => f.ReadAllTextAsync(tokenFilePath, It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedToken);

            this.mockFixture.Parameters = new Dictionary<string, IConvertible>()
            {
                { nameof(CertificateInstallation.AccessTokenPath), tokenFilePath },
                { nameof(CertificateInstallation.CertificateName), "testCert" },
                { nameof(CertificateInstallation.KeyVaultUri), "https://testvault.vault.azure.net/" }
            };

            using (TestCertificateInstallation component = new TestCertificateInstallation(this.mockFixture.Dependencies, this.mockFixture.Parameters))
            {
                await component.InitializeAsync(EventContext.None, CancellationToken.None);
                Assert.AreEqual(expectedToken, component.AccessToken);
            }
        }

        [Test]
        public async Task InitializeAsync_PrefersAccessTokenParameterOverFile()
        {
            this.mockFixture.Setup(PlatformID.Win32NT);

            string parameterToken = "parameter-token-123";
            string fileToken = "file-token-321";
            string tokenFilePath = "/tmp/token.txt";

            this.mockFixture.File.Setup(f => f.ReadAllTextAsync(tokenFilePath, It.IsAny<CancellationToken>()))
                .ReturnsAsync(fileToken);

            this.mockFixture.Parameters = new Dictionary<string, IConvertible>()
            {
                { nameof(CertificateInstallation.AccessToken), parameterToken },
                { nameof(CertificateInstallation.AccessTokenPath), tokenFilePath },
                { nameof(CertificateInstallation.CertificateName), "testCert" },
                { nameof(CertificateInstallation.KeyVaultUri), "https://testvault.vault.azure.net/" }
            };

            using (TestCertificateInstallation component = new TestCertificateInstallation(this.mockFixture.Dependencies, this.mockFixture.Parameters))
            {
                await component.InitializeAsync(EventContext.None, CancellationToken.None);
                Assert.AreEqual(parameterToken, component.AccessToken);
            }
        }

        [Test]
        public async Task GetKeyVaultManager_DefaultsToPredefinedKVManager_ThenCreatesNewOneWithToken()
        {
            // todo: nirjan to fill
        }

        [Test]
        public void ExecuteAsync_ThrowsWhenCertificateNameIsNull()
        {
            this.mockFixture.Setup(PlatformID.Win32NT);
            this.mockFixture.Parameters = new Dictionary<string, IConvertible>()
            {
                { nameof(CertificateInstallation.KeyVaultUri), "https://testvault.vault.azure.net/" }
            };

            using (TestCertificateInstallation component = new TestCertificateInstallation(this.mockFixture.Dependencies, this.mockFixture.Parameters))
            {
                Exception exception = Assert.ThrowsAsync<KeyNotFoundException>(
                    () => component.ExecuteAsync(EventContext.None, CancellationToken.None));

                Assert.IsNotNull(exception);
                Assert.IsTrue(exception.Message.Contains("An entry with key 'CertificateName' does not exist in the dictionary."));
            }
        }

        [Test]
        public async Task ExecuteAsyncInstallsCertificateOnWindows()
        {
            this.mockFixture.Setup(PlatformID.Win32NT);
            this.mockFixture.Parameters = new Dictionary<string, IConvertible>()
            {
                { nameof(CertificateInstallation.CertificateName), "testCert" },
                { nameof(CertificateInstallation.KeyVaultUri), "https://testvault.vault.azure.net/" }
            };

            bool windowsInstallCalled = false;

            using (TestCertificateInstallation component = new TestCertificateInstallation(this.mockFixture.Dependencies, this.mockFixture.Parameters))
            {
                this.mockFixture.KeyVaultManager
                    .Setup(m => m.GetCertificateAsync(It.IsAny<PlatformID>(), It.IsAny<string>(), It.IsAny<CancellationToken>(), It.IsAny<string>(), It.IsAny<IAsyncPolicy>()))
                    .ReturnsAsync(this.testCertificate);

                component.OnInstallCertificateOnWindows = (cert, token) =>
                {
                    windowsInstallCalled = true;
                    Assert.AreEqual(this.testCertificate, cert);
                    return Task.CompletedTask;
                };

                await component.ExecuteAsync(EventContext.None, CancellationToken.None);
            }

            Assert.IsTrue(windowsInstallCalled);
            this.mockFixture.KeyVaultManager.Verify(m => m.GetCertificateAsync(
                PlatformID.Win32NT,
                "testCert",
                It.IsAny<CancellationToken>(),
                It.IsAny<string>(),
                It.IsAny<IAsyncPolicy>()), Times.Once);
        }

        [Test]
        public async Task ExecuteAsyncInstallsCertificateOnUnix()
        {
            this.mockFixture.Setup(PlatformID.Unix);
            this.mockFixture.Parameters = new Dictionary<string, IConvertible>()
            {
                { nameof(CertificateInstallation.CertificateName), "testCert" },
                { nameof(CertificateInstallation.KeyVaultUri), "https://testvault.vault.azure.net/" }
            };

            bool unixInstallCalled = false;

            using (TestCertificateInstallation component = new TestCertificateInstallation(this.mockFixture.Dependencies, this.mockFixture.Parameters))
            {
                this.mockFixture.KeyVaultManager
                    .Setup(m => m.GetCertificateAsync(It.IsAny<PlatformID>(), It.IsAny<string>(), It.IsAny<CancellationToken>(), It.IsAny<string>(), It.IsAny<IAsyncPolicy>()))
                    .ReturnsAsync(this.testCertificate);

                component.OnInstallCertificateOnUnix = (cert, token) =>
                {
                    unixInstallCalled = true;
                    Assert.AreEqual(this.testCertificate, cert);
                    return Task.CompletedTask;
                };

                await component.ExecuteAsync(EventContext.None, CancellationToken.None);
            }

            Assert.IsTrue(unixInstallCalled);
            this.mockFixture.KeyVaultManager.Verify(m => m.GetCertificateAsync(
                PlatformID.Unix,
                "testCert",
                It.IsAny<CancellationToken>(),
                It.IsAny<string>(),
                It.IsAny<IAsyncPolicy>()), Times.Once);
        }

        [Test]
        public void ExecuteAsync_WrapsExceptionsInDependencyException()
        {
            this.mockFixture.Setup(PlatformID.Win32NT);
            this.mockFixture.Parameters = new Dictionary<string, IConvertible>()
            {
                { nameof(CertificateInstallation.CertificateName), "testCert" },
                { nameof(CertificateInstallation.KeyVaultUri), "https://testvault.vault.azure.net/" }
            };

            this.mockFixture.KeyVaultManager
                .Setup(m => m.GetCertificateAsync(
                    It.IsAny<PlatformID>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>(),
                    It.IsAny<string>(),
                    It.IsAny<IAsyncPolicy>()))
                .ThrowsAsync(new InvalidOperationException("KeyVault error"));

            using (TestCertificateInstallation component = new TestCertificateInstallation(this.mockFixture.Dependencies, this.mockFixture.Parameters))
            {
                DependencyException exception = Assert.ThrowsAsync<DependencyException>(
                    () => component.ExecuteAsync(EventContext.None, CancellationToken.None));

                StringAssert.Contains("error occurred installing the certificate", exception.Message);
                Assert.IsInstanceOf<InvalidOperationException>(exception.InnerException);
            }
        }

        [Test]
        public async Task InstallCertificateOnWindowsAsync_InstallsCertificateToCurrentUserStore()
        {
            this.mockFixture.Setup(PlatformID.Win32NT);
            this.mockFixture.Parameters = new Dictionary<string, IConvertible>()
            {
                { nameof(CertificateInstallation.CertificateName), "testCert" },
                { nameof(CertificateInstallation.KeyVaultUri), "https://testvault.vault.azure.net/" }
            };

            using (TestCertificateInstallation component = new TestCertificateInstallation(this.mockFixture.Dependencies, this.mockFixture.Parameters))
            {
                // This test verifies the method completes without throwing errors. 
                // Better approach is to create an abstraction. 
                await component.InstallCertificateRespectively(PlatformID.Win32NT, this.testCertificate, CancellationToken.None);
            }
        }

        [Test]
        public async Task InstallCertificateOnUnixAsync_InstallsCertificateForRegularUser()
        {
            this.mockFixture.Setup(PlatformID.Unix);
            string certificateDirectory = "/home/testuser/.dotnet/corefx/cryptography/x509stores/my";
            string certificatePath = this.mockFixture.Combine(certificateDirectory, $"{this.testCertificate.Thumbprint}.pfx");

            this.mockFixture.Parameters = new Dictionary<string, IConvertible>()
            {
                { nameof(CertificateInstallation.CertificateName), "testCert" },
                { nameof(CertificateInstallation.KeyVaultUri), "https://testvault.vault.azure.net/" }
            };

            this.mockFixture.Directory.Setup(d => d.Exists(certificateDirectory)).Returns(false);
            this.mockFixture.Directory.Setup(d => d.CreateDirectory(certificateDirectory));
            this.mockFixture.File.Setup(f => f.WriteAllBytesAsync(certificatePath, It.IsAny<byte[]>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

            this.mockFixture.ProcessManager.OnCreateProcess = (exe, arguments, workingDir) =>
            {
                if (exe == "chmod")
                {
                    Assert.AreEqual($"-R 777 {certificateDirectory}", arguments);
                }

                return new InMemoryProcess()
                {
                    ExitCode = 0,
                    OnStart = () => true,
                    OnHasExited = () => true
                };
            };

            using (TestCertificateInstallation component = new TestCertificateInstallation(this.mockFixture.Dependencies, this.mockFixture.Parameters))
            {
                component.SetEnvironmentVariable(EnvironmentVariable.USER, "testuser");
                await component.InstallCertificateRespectively(PlatformID.Unix, this.testCertificate, CancellationToken.None);
            }

            this.mockFixture.Directory.Verify(d => d.CreateDirectory(certificateDirectory), Times.Once);
            this.mockFixture.File.Verify(f => f.WriteAllBytesAsync(certificatePath, It.IsAny<byte[]>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public async Task InstallCertificateOnUnixAsync_InstallsCertificateForSudoUser()
        {
            this.mockFixture.Setup(PlatformID.Unix);

            string certificateDirectory = "/home/sudouser/.dotnet/corefx/cryptography/x509stores/my";
            string certificatePath = this.mockFixture.Combine(certificateDirectory, $"{this.testCertificate.Thumbprint}.pfx");

            this.mockFixture.Parameters = new Dictionary<string, IConvertible>()
            {
                { nameof(CertificateInstallation.CertificateName), "testCert" },
                { nameof(CertificateInstallation.KeyVaultUri), "https://testvault.vault.azure.net/" }
            };

            this.mockFixture.Directory.Setup(d => d.Exists(certificateDirectory)).Returns(false);
            this.mockFixture.Directory.Setup(d => d.CreateDirectory(certificateDirectory));
            this.mockFixture.File.Setup(f => f.WriteAllBytesAsync(certificatePath, It.IsAny<byte[]>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

            this.mockFixture.ProcessManager.OnCreateProcess = (exe, arguments, workingDir) =>
            {
                return new InMemoryProcess()
                {
                    ExitCode = 0,
                    OnStart = () => true,
                    OnHasExited = () => true
                };
            };

            using (TestCertificateInstallation component = new TestCertificateInstallation(this.mockFixture.Dependencies, this.mockFixture.Parameters))
            {
                component.SetEnvironmentVariable(EnvironmentVariable.USER, "root");
                component.SetEnvironmentVariable(EnvironmentVariable.SUDO_USER, "sudouser");
                await component.InstallCertificateRespectively(PlatformID.Unix, this.testCertificate, CancellationToken.None);
            }

            this.mockFixture.Directory.Verify(d => d.CreateDirectory(certificateDirectory), Times.Once);
            this.mockFixture.File.Verify(f => f.WriteAllBytesAsync(certificatePath, It.IsAny<byte[]>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public async Task InstallCertificateOnUnixAsync_InstallsCertificateForRootUser()
        {
            this.mockFixture.Setup(PlatformID.Unix);

            string certificateDirectory = "/root/.dotnet/corefx/cryptography/x509stores/my";
            string certificatePath = this.mockFixture.Combine(certificateDirectory, $"{this.testCertificate.Thumbprint}.pfx");

            this.mockFixture.Parameters = new Dictionary<string, IConvertible>()
            {
                { nameof(CertificateInstallation.CertificateName), "testCert" },
                { nameof(CertificateInstallation.KeyVaultUri), "https://testvault.vault.azure.net/" }
            };

            this.mockFixture.Directory.Setup(d => d.Exists(certificateDirectory)).Returns(true);
            this.mockFixture.File.Setup(f => f.WriteAllBytesAsync(certificatePath, It.IsAny<byte[]>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

            this.mockFixture.ProcessManager.OnCreateProcess = (exe, arguments, workingDir) =>
            {
                return new InMemoryProcess()
                {
                    ExitCode = 0,
                    OnStart = () => true,
                    OnHasExited = () => true
                };
            };

            using (TestCertificateInstallation component = new TestCertificateInstallation(this.mockFixture.Dependencies, this.mockFixture.Parameters))
            {
                component.SetEnvironmentVariable(EnvironmentVariable.USER, "root");
                await component.InstallCertificateRespectively(PlatformID.Unix, this.testCertificate, CancellationToken.None);
            }

            this.mockFixture.Directory.Verify(d => d.CreateDirectory(certificateDirectory), Times.Never);
            this.mockFixture.File.Verify(f => f.WriteAllBytesAsync(certificatePath, It.IsAny<byte[]>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public void InstallCertificateOnUnixAsync_ThrowsUnauthorizedAccessExceptionWithAppropriateMessage()
        {
            this.mockFixture.Setup(PlatformID.Unix);

            string certificateDirectory = "/home/testuser/.dotnet/corefx/cryptography/x509stores/my";

            this.mockFixture.Parameters = new Dictionary<string, IConvertible>()
            {
                { nameof(CertificateInstallation.CertificateName), "testCert" },
                { nameof(CertificateInstallation.KeyVaultUri), "https://testvault.vault.azure.net/" }
            };

            this.mockFixture.Directory.Setup(d => d.Exists(certificateDirectory)).Returns(false);
            this.mockFixture.Directory.Setup(d => d.CreateDirectory(certificateDirectory)).Throws(new UnauthorizedAccessException());

            using (TestCertificateInstallation component = new TestCertificateInstallation(this.mockFixture.Dependencies, this.mockFixture.Parameters))
            {
                component.SetEnvironmentVariable(EnvironmentVariable.USER, "testuser");

                UnauthorizedAccessException exception = Assert.ThrowsAsync<UnauthorizedAccessException>(
                    () => component.InstallCertificateRespectively(PlatformID.Unix, this.testCertificate, CancellationToken.None));

                StringAssert.Contains("Access permissions denied", exception.Message);
                StringAssert.Contains("sudo/root privileges", exception.Message);
            }
        }

        [Test]
        public void GetKeyVaultManager_ReturnsInjectedKeyVaultManager()
        {
            this.mockFixture.Setup(PlatformID.Win32NT);
            this.mockFixture.Parameters = new Dictionary<string, IConvertible>()
            {
                { nameof(CertificateInstallation.CertificateName), "testCert" }
            };

            using (TestCertificateInstallation component = new TestCertificateInstallation(this.mockFixture.Dependencies, this.mockFixture.Parameters))
            {
                IKeyVaultManager manager = component.GetKeyVaultManager();

                Assert.IsNotNull(manager);
                Assert.AreSame(this.mockFixture.KeyVaultManager.Object, manager);
            }
        }

        [Test]
        public async Task GetKeyVaultManager_CreatesKeyVaultManagerWithAccessToken()
        {
            this.mockFixture.Setup(PlatformID.Win32NT);

            // Remove the injected KeyVaultManager
            this.mockFixture.Dependencies.RemoveAll<IKeyVaultManager>();

            this.mockFixture.KeyVaultManager = new Mock<IKeyVaultManager>(MockBehavior.Loose);
            this.mockFixture.Dependencies.AddSingleton<IKeyVaultManager>((p) => this.mockFixture.KeyVaultManager.Object);

            string accessToken = "test-token-abc123";
            string keyVaultUri = "https://testvault.vault.azure.net/";

            this.mockFixture.Parameters = new Dictionary<string, IConvertible>()
            {
                { nameof(CertificateInstallation.CertificateName), "testCert" },
                { nameof(CertificateInstallation.AccessToken), accessToken },
                { nameof(CertificateInstallation.KeyVaultUri), keyVaultUri }
            };

            using (TestCertificateInstallation component = new TestCertificateInstallation(this.mockFixture.Dependencies, this.mockFixture.Parameters))
            {
                await component.InitializeAsync(EventContext.None, CancellationToken.None);
                IKeyVaultManager manager = component.GetKeyVaultManager();

                Assert.IsNotNull(manager);
                Assert.IsNotNull(manager.StoreDescription);
                Assert.AreEqual(keyVaultUri, ((DependencyKeyVaultStore)manager.StoreDescription).EndpointUri.ToString());
            }
        }

        [Test]
        public async Task GetKeyVaultManagerWithTokenThrowsWhenKeyVaultUriNotProvided()
        {
            this.mockFixture.Setup(PlatformID.Win32NT);

            // Remove the injected KeyVaultManager
            this.mockFixture.Dependencies.RemoveAll<IKeyVaultManager>();

            this.mockFixture.KeyVaultManager = new Mock<IKeyVaultManager>(MockBehavior.Loose);
            this.mockFixture.Dependencies.AddSingleton<IKeyVaultManager>((p) => this.mockFixture.KeyVaultManager.Object);

            this.mockFixture.Parameters = new Dictionary<string, IConvertible>()
            {
                { nameof(CertificateInstallation.CertificateName), "testCert" },
                { nameof(CertificateInstallation.AccessToken), "test-token" }
            };

            using (TestCertificateInstallation component = new TestCertificateInstallation(this.mockFixture.Dependencies, this.mockFixture.Parameters))
            {
                await component.InitializeAsync(EventContext.None, CancellationToken.None);
                Assert.Throws<KeyNotFoundException>(() => component.GetKeyVaultManager());
            }
        }

        [Test]
        public void GetKeyVaultManager_ThrowsWhenNoKeyVaultManagerOrAccessTokenProvided()
        {
            this.mockFixture.Setup(PlatformID.Win32NT);

            // Remove the injected KeyVaultManager and setup one without StoreDescription
            this.mockFixture.Dependencies.RemoveAll<IKeyVaultManager>();
            var emptyMock = new Mock<IKeyVaultManager>();
            emptyMock.Setup(m => m.StoreDescription).Returns((DependencyKeyVaultStore)null);
            this.mockFixture.Dependencies.AddSingleton(emptyMock.Object);

            this.mockFixture.Parameters = new Dictionary<string, IConvertible>()
            {
                { nameof(CertificateInstallation.CertificateName), "testCert" }
            };

            using (TestCertificateInstallation component = new TestCertificateInstallation(this.mockFixture.Dependencies, this.mockFixture.Parameters))
            {
                InvalidOperationException exception = Assert.Throws<InvalidOperationException>(
                    () => component.GetKeyVaultManager());

                StringAssert.Contains("Key Vault manager has not been properly initialized", exception.Message);
            }
        }

        private class TestCertificateInstallation : CertificateInstallation
        {
            public TestCertificateInstallation(IServiceCollection dependencies, IDictionary<string, IConvertible> parameters)
                : base(dependencies, parameters)
            {
            }

            public Func<X509Certificate2, CancellationToken, Task> OnInstallCertificateOnWindows { get; set; }

            public Func<X509Certificate2, CancellationToken, Task> OnInstallCertificateOnUnix { get; set; }

            public new Task InitializeAsync(EventContext context, CancellationToken cancellationToken)
            {
                return base.InitializeAsync(context, cancellationToken);
            }

            public new Task ExecuteAsync(EventContext context, CancellationToken cancellationToken)
            {
                return base.ExecuteAsync(context, cancellationToken);
            }

            public new IKeyVaultManager GetKeyVaultManager()
            {
                return base.GetKeyVaultManager();
            }

            public Task InstallCertificateRespectively(PlatformID platformID, X509Certificate2 certificate, CancellationToken cancellationToken)
            {
                if (platformID == PlatformID.Unix)
                {
                    return this.InstallCertificateOnUnixAsync(certificate, cancellationToken);
                }

                return this.InstallCertificateOnWindowsAsync(certificate, cancellationToken);
            }

            protected override Task InstallCertificateOnUnixAsync(X509Certificate2 certificate, CancellationToken cancellationToken)
            {
                return this.OnInstallCertificateOnUnix != null
                    ? this.OnInstallCertificateOnUnix(certificate, cancellationToken)
                    : base.InstallCertificateOnUnixAsync(certificate, cancellationToken);
            }

            protected override Task InstallCertificateOnWindowsAsync(X509Certificate2 certificate, CancellationToken cancellationToken)
            {
                return this.OnInstallCertificateOnWindows != null
                    ? this.OnInstallCertificateOnWindows(certificate, cancellationToken)
                    : base.InstallCertificateOnWindowsAsync(certificate, cancellationToken);
            }
        }
    }
}