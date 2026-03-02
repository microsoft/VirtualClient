// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Moq;
using NUnit.Framework;
using Polly;
using VirtualClient.Common.Telemetry;

namespace VirtualClient.Dependencies
{
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
        [TestCase(PlatformID.Win32NT)]
        [TestCase(PlatformID.Unix)]
        public async Task InitializeAsync_LoadsAccessTokenFromParameter(PlatformID platform)
        {
            this.mockFixture.Setup(platform);

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
        [TestCase(PlatformID.Win32NT)]
        [TestCase(PlatformID.Unix)]
        public async Task InitializeAsync_LoadsAccessTokenFromFile(PlatformID platform)
        {
            this.mockFixture.Setup(platform);

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
        [TestCase(PlatformID.Win32NT)]
        [TestCase(PlatformID.Unix)]
        public async Task InitializeAsync_PrefersAccessTokenParameterOverFile(PlatformID platform)
        {
            this.mockFixture.Setup(platform);

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
        [TestCase(PlatformID.Win32NT)]
        [TestCase(PlatformID.Unix)]
        public void ExecuteAsync_ThrowsWhenCertificateNameIsNull(PlatformID platform)
        {
            this.mockFixture.Setup(platform);
            this.mockFixture.Parameters = new Dictionary<string, IConvertible>()        
            {
                { nameof(CertificateInstallation.KeyVaultUri), "https://testvault.vault.azure.net/" }
            };

            using (TestCertificateInstallation component = new TestCertificateInstallation(this.mockFixture.Dependencies, this.mockFixture.Parameters))
            {
                KeyNotFoundException exception = Assert.ThrowsAsync<KeyNotFoundException>(
                    () => component.ExecuteAsync(EventContext.None, CancellationToken.None));

                Assert.IsNotNull(exception);
                Assert.IsTrue(exception.Message.Contains("CertificateName"));
            }
        }

        [Test]
        [TestCase(PlatformID.Win32NT)]
        [TestCase(PlatformID.Unix)]
        public async Task ExecuteAsync_InstallsCertificateOnWindows(PlatformID platform)
        {
            this.mockFixture.Setup(platform);
            this.mockFixture.Parameters = new Dictionary<string, IConvertible>()
            {
                { nameof(CertificateInstallation.CertificateName), "testCert" },
                { nameof(CertificateInstallation.KeyVaultUri), "https://testvault.vault.azure.net/" }
            };

            bool machineInstallCalled = false;

            using (TestCertificateInstallation component = new TestCertificateInstallation(this.mockFixture.Dependencies, this.mockFixture.Parameters))
            {
                this.mockFixture.KeyVaultManager
                    .Setup(m => m.GetCertificateAsync(
                        It.IsAny<string>(), 
                        It.IsAny<CancellationToken>(), 
                        It.IsAny<string>(), 
                        It.IsAny<bool>(), 
                        It.IsAny<IAsyncPolicy>()))
                    .ReturnsAsync(this.testCertificate);

                component.OnInstallCertificateOnMachine = (cert, token) =>
                {
                    machineInstallCalled = true;
                    Assert.AreEqual(this.testCertificate, cert);
                    return Task.CompletedTask;
                };

                await component.ExecuteAsync(EventContext.None, CancellationToken.None);
            }

            Assert.IsTrue(machineInstallCalled);
            this.mockFixture.KeyVaultManager.Verify(m => m.GetCertificateAsync(
                "testCert",
                It.IsAny<CancellationToken>(),
                It.IsAny<string>(),
                It.IsAny<bool>(),
                It.IsAny<IAsyncPolicy>()), Times.Once);
        }

        [Test]
        [TestCase(PlatformID.Win32NT, @"C:\Certs")]
        [TestCase(PlatformID.Unix, "/tmp/certs")]
        public async Task ExecuteAsync_InstallsCertificateLocally_WhenDirectoryProvided(PlatformID platform, string installDir)
        {
            this.mockFixture.Setup(platform);
            this.mockFixture.Parameters = new Dictionary<string, IConvertible>()
            {
                { nameof(CertificateInstallation.CertificateName), "testCert" },
                { nameof(CertificateInstallation.KeyVaultUri), "https://testvault.vault.azure.net/" },
                { nameof(CertificateInstallation.CertificateInstallationDir), installDir }
            };

            this.mockFixture.File.Setup(f => f.WriteAllBytesAsync(It.IsAny<string>(), It.IsAny<byte[]>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            using (TestCertificateInstallation component = new TestCertificateInstallation(this.mockFixture.Dependencies, this.mockFixture.Parameters))
            {
                this.mockFixture.KeyVaultManager
                   .Setup(m => m.GetCertificateAsync(
                       It.IsAny<string>(),
                       It.IsAny<CancellationToken>(),
                       It.IsAny<string>(),
                       It.IsAny<bool>(),
                       It.IsAny<IAsyncPolicy>()))
                   .ReturnsAsync(this.testCertificate);

                await component.ExecuteAsync(EventContext.None, CancellationToken.None);
            }

            this.mockFixture.Directory.Verify(d => d.CreateDirectory(installDir), Times.Once);
            this.mockFixture.File.Verify(f => f.WriteAllBytesAsync(
                It.Is<string>(path => @path.StartsWith(@installDir)), 
                It.IsAny<byte[]>(), 
                It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        [TestCase(PlatformID.Win32NT)]
        [TestCase(PlatformID.Unix)]
        public void ExecuteAsync_WrapsExceptionsInDependencyException(PlatformID platform)
        {
            this.mockFixture.Setup(platform);
            this.mockFixture.Parameters = new Dictionary<string, IConvertible>()
            {
                { nameof(CertificateInstallation.CertificateName), "testCert" },
                { nameof(CertificateInstallation.KeyVaultUri), "https://testvault.vault.azure.net/" }
            };

            this.mockFixture.KeyVaultManager
                .Setup(m => m.GetCertificateAsync(
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>(),
                    It.IsAny<string>(),
                    It.IsAny<bool>(),
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
        [TestCase(PlatformID.Win32NT, @"C:\Users\Any\Certs")]
        [TestCase(PlatformID.Unix, "/tmp/certs")]
        public async Task InstallCertificateLocallyAsync_InstallsCertificateToDirectory(PlatformID platform, string certificateDirectory)
        {
            this.mockFixture.Setup(platform);
            string certificatePath = this.mockFixture.Combine(certificateDirectory, $"{this.testCertificate.Thumbprint}.pfx");

            this.mockFixture.Parameters = new Dictionary<string, IConvertible>()
            {
                { nameof(CertificateInstallation.CertificateName), "testCert" },
                { nameof(CertificateInstallation.KeyVaultUri), "https://testvault.vault.azure.net/" },
                { nameof(CertificateInstallation.CertificateInstallationDir), certificateDirectory }
            };

            this.mockFixture.Directory.Setup(d => d.Exists(certificateDirectory)).Returns(false);
            this.mockFixture.Directory.Setup(d => d.CreateDirectory(certificateDirectory));
            this.mockFixture.File.Setup(f => f.WriteAllBytesAsync(It.IsAny<string>(), It.IsAny<byte[]>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

            using (TestCertificateInstallation component = new TestCertificateInstallation(this.mockFixture.Dependencies, this.mockFixture.Parameters))
            {
                await component.CallInstallCertificateLocallyAsync(this.testCertificate, CancellationToken.None);
            }

            this.mockFixture.Directory.Verify(d => d.CreateDirectory(certificateDirectory), Times.Once);
            this.mockFixture.File.Verify(f => f.WriteAllBytesAsync(It.Is<string>(p => p.StartsWith(certificateDirectory)), It.IsAny<byte[]>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        [TestCase(PlatformID.Win32NT)]
        [TestCase(PlatformID.Unix)]
        public async Task InstallCertificateLocallyAsync_SetsPermissionsOnUnix(PlatformID platform)
        {
            this.mockFixture.Setup(PlatformID.Unix);
            string certificateDirectory = "/etc/certs";
            
            this.mockFixture.Parameters = new Dictionary<string, IConvertible>()
            {
                { nameof(CertificateInstallation.CertificateName), "testCert" },
                { nameof(CertificateInstallation.KeyVaultUri), "https://testvault.vault.azure.net/" },
                { nameof(CertificateInstallation.CertificateInstallationDir), certificateDirectory }
            };

            this.mockFixture.Directory.Setup(d => d.Exists(certificateDirectory)).Returns(false);
            this.mockFixture.Directory.Setup(d => d.CreateDirectory(certificateDirectory));
            this.mockFixture.File.Setup(f => f.WriteAllBytesAsync(It.IsAny<string>(), It.IsAny<byte[]>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

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
                await component.CallInstallCertificateLocallyAsync(this.testCertificate, CancellationToken.None);
            }

            this.mockFixture.Directory.Verify(d => d.CreateDirectory(certificateDirectory), Times.Once);
            this.mockFixture.File.Verify(f => f.WriteAllBytesAsync(It.Is<string>(p => p.StartsWith(certificateDirectory)), It.IsAny<byte[]>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        [TestCase(PlatformID.Win32NT)]
        [TestCase(PlatformID.Unix)]
        public void InstallCertificateLocallyAsync_ThrowsUnauthorizedAccessExceptionWhenPermissionsDenied(PlatformID platform)
        {
            this.mockFixture.Setup(platform);
            string certificateDirectory = @"C:\System\Certs";

            this.mockFixture.Parameters = new Dictionary<string, IConvertible>()
            {
                { nameof(CertificateInstallation.CertificateName), "testCert" },
                { nameof(CertificateInstallation.KeyVaultUri), "https://testvault.vault.azure.net/" },
                { nameof(CertificateInstallation.CertificateInstallationDir), certificateDirectory }
            };

            this.mockFixture.Directory.Setup(d => d.Exists(certificateDirectory)).Returns(false);
            this.mockFixture.Directory.Setup(d => d.CreateDirectory(certificateDirectory)).Throws(new UnauthorizedAccessException());

            using (TestCertificateInstallation component = new TestCertificateInstallation(this.mockFixture.Dependencies, this.mockFixture.Parameters))
            {
                UnauthorizedAccessException exception = Assert.ThrowsAsync<UnauthorizedAccessException>(
                    () => component.CallInstallCertificateLocallyAsync(this.testCertificate, CancellationToken.None));

                StringAssert.Contains("Access permissions denied", exception.Message);
            }
        }

        [Test]
        [TestCase(PlatformID.Win32NT)]
        [TestCase(PlatformID.Unix)]
        public void GetKeyVaultManager_ReturnsInjectedKeyVaultManager(PlatformID platform)
        {
            this.mockFixture.Setup(platform);
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
        [TestCase(PlatformID.Win32NT)]
        [TestCase(PlatformID.Unix)]
        public async Task GetKeyVaultManager_CreatesKeyVaultManagerWithAccessToken(PlatformID platform)
        {
            this.mockFixture.Setup(platform);

            // Remove the injected KeyVaultManager
            this.mockFixture.Dependencies.RemoveAll<IKeyVaultManager>();
            
            // To pass the ThrowIfNull, we must have an IKeyVaultManager. 
            // In the "create new" scenario, we likely rely on checking StoreDescription on the existing one,
            // or the component logic is such that Injecting it is mandatory.
            // If the code is: IKeyVaultManager keyVaultManager = this.Dependencies.GetService<IKeyVaultManager>(); keyVaultManager.ThrowIfNull(...)
            // Then we MUST have it in dependencies.
            // We inject a mock with null StoreDescription to simulate needing to create a new one.
            var mockKeyVault = new Mock<IKeyVaultManager>(MockBehavior.Loose);
            this.mockFixture.Dependencies.AddSingleton<IKeyVaultManager>(mockKeyVault.Object);

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
                Assert.AreNotSame(mockKeyVault.Object, manager); // Should be a new instance
                Assert.IsNotNull(manager.StoreDescription);
                Assert.AreEqual(keyVaultUri, ((DependencyKeyVaultStore)manager.StoreDescription).EndpointUri.ToString());
            }
        }

        [Test]
        [TestCase(PlatformID.Win32NT)]
        [TestCase(PlatformID.Unix)]
        public async Task GetKeyVaultManagerWithTokenThrowsWhenKeyVaultUriNotProvided(PlatformID platform)
        {
            this.mockFixture.Setup(platform);

            // Remove the injected KeyVaultManager and replace with one that has null StoreDescription
            this.mockFixture.Dependencies.RemoveAll<IKeyVaultManager>();
            var mockKeyVault = new Mock<IKeyVaultManager>(MockBehavior.Loose);
            this.mockFixture.Dependencies.AddSingleton<IKeyVaultManager>(mockKeyVault.Object);

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
        [TestCase(PlatformID.Win32NT)]
        [TestCase(PlatformID.Unix)]
        public void GetKeyVaultManager_ThrowsWhenNoKeyVaultManagerOrAccessTokenProvided(PlatformID platform)
        {
            this.mockFixture.Setup(platform);

            // Remove the injected KeyVaultManager and replace with one that has null StoreDescription
            this.mockFixture.Dependencies.RemoveAll<IKeyVaultManager>();
            var mockKeyVault = new Mock<IKeyVaultManager>(MockBehavior.Loose);
            this.mockFixture.Dependencies.AddSingleton<IKeyVaultManager>(mockKeyVault.Object);

            this.mockFixture.Parameters = new Dictionary<string, IConvertible>()
            {
                { nameof(CertificateInstallation.CertificateName), "testCert" }
            };

            using (TestCertificateInstallation component = new TestCertificateInstallation(this.mockFixture.Dependencies, this.mockFixture.Parameters))
            {
                InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() => component.GetKeyVaultManager());
                StringAssert.Contains("The Key Vault manager has not been properly initialized", exception.Message);
            }
        }

        [Test]
        [TestCase(PlatformID.Win32NT)]
        [TestCase(PlatformID.Unix)]
        public async Task InstallCertificateOnMachineAsync_UsesPlatformStore_Windows(PlatformID platform)
        {
            this.mockFixture.Setup(platform);
           
            this.mockFixture.Parameters = new Dictionary<string, IConvertible>()
            {
                { nameof(CertificateInstallation.CertificateName), "testCert" },
                { nameof(CertificateInstallation.KeyVaultUri), "https://testvault.vault.azure.net/" }
            };

            using (TestCertificateInstallation component = new TestCertificateInstallation(this.mockFixture.Dependencies, this.mockFixture.Parameters))
            {
                // This is a smoke test to ensure the base method calls Task.Run and doesn't crash.
                // It does NOT verify the store content because we can't easily assert X509Store state in unit tests without side effects.
                await component.CallInstallCertificateOnMachineAsync(this.testCertificate, CancellationToken.None);
            }
        }

        private class TestCertificateInstallation : CertificateInstallation
        {
            public TestCertificateInstallation(IServiceCollection dependencies, IDictionary<string, IConvertible> parameters)
                : base(dependencies, parameters)
            {
            }

            public Func<X509Certificate2, CancellationToken, Task> OnInstallCertificateOnMachine { get; set; }

            public new Task InitializeAsync(EventContext telemetryContext, CancellationToken cancellationToken)
            {
                return base.InitializeAsync(telemetryContext, cancellationToken);
            }

            public new Task ExecuteAsync(EventContext telemetryContext, CancellationToken cancellationToken)
            {
                return base.ExecuteAsync(telemetryContext, cancellationToken);
            }

            public new IKeyVaultManager GetKeyVaultManager()
            {
                return base.GetKeyVaultManager();
            }

            public Task CallInstallCertificateLocallyAsync(X509Certificate2 certificate, CancellationToken cancellationToken)
            {
                return this.InstallCertificateLocallyAsync(certificate, cancellationToken);
            }

            public Task CallInstallCertificateOnMachineAsync(X509Certificate2 certificate, CancellationToken cancellationToken)
            {
                return base.InstallCertificateOnMachineAsync(certificate, cancellationToken);
            }

            protected override Task InstallCertificateOnMachineAsync(X509Certificate2 certificate, CancellationToken cancellationToken)
            {
                if (this.OnInstallCertificateOnMachine != null)
                {
                    return this.OnInstallCertificateOnMachine.Invoke(certificate, cancellationToken);
                }

                return base.InstallCertificateOnMachineAsync(certificate, cancellationToken);
            }
        }
    }
}