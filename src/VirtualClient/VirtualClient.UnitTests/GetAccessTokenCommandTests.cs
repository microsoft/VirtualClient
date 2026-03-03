// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.UnitTests
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using NUnit.Framework;
    using VirtualClient.Contracts;

    [TestFixture]
    [Category("Unit")]
    public class GetAccessTokenCommandTests
    {
        [Test]
        public void ShouldInitializeKeyVault_IsFalse_Always()
        {
            var command = new TestGetAccessTokenCommand();

            Assert.That(command.ShouldInitializeKeyVaultPublic(), Is.False);
        }

        [Test]
        public async Task ExecuteAsync_InitializesParametersDictionary_WhenNull()
        {
            var command = new TestGetAccessTokenCommand
            {
                Parameters = null,
                KeyVault = "https://myvault.vault.azure.net/",
                TenantId = "00000000-0000-0000-0000-000000000001"
            };

            await command.ExecuteAsync(Array.Empty<string>(), new CancellationTokenSource());

            Assert.That(command.Parameters, Is.Not.Null);
            Assert.That(command.Parameters, Is.InstanceOf<Dictionary<string, IConvertible>>());
        }

        [Test]
        public async Task ExecuteAsync_AddsExpectedProfile()
        {
            var command = new TestGetAccessTokenCommand
            {
                Parameters = new Dictionary<string, IConvertible>(StringComparer.OrdinalIgnoreCase),
                KeyVault = "https://myvault.vault.azure.net/",
                TenantId = "00000000-0000-0000-0000-000000000001"
            };

            await command.ExecuteAsync(Array.Empty<string>(), new CancellationTokenSource());

            AssertProfileNames(command, "GET-ACCESS-TOKEN.json");
        }

        [Test]
        public async Task ExecuteAsync_SetsExpectedParameters()
        {
            var command = new TestGetAccessTokenCommand
            {
                Parameters = new Dictionary<string, IConvertible>(StringComparer.OrdinalIgnoreCase),
                KeyVault = "https://myvault.vault.azure.net/",
                TenantId = "00000000-0000-0000-0000-000000000001"
            };

            await command.ExecuteAsync(Array.Empty<string>(), new CancellationTokenSource());

            Assert.That(command.Parameters["KeyVaultUri"], Is.EqualTo(command.KeyVault));
            Assert.That(command.Parameters["TenantId"], Is.EqualTo(command.TenantId));
        }

        [Test]
        public async Task ExecuteAsync_OverwritesExistingParameterValues()
        {
            var command = new TestGetAccessTokenCommand
            {
                Parameters = new Dictionary<string, IConvertible>(StringComparer.OrdinalIgnoreCase)
                {
                    ["KeyVaultUri"] = "https://old.vault.azure.net/",
                    ["TenantId"] = "old-tenant"
                },
                KeyVault = "https://myvault.vault.azure.net/",
                TenantId = "00000000-0000-0000-0000-000000000001"
            };

            await command.ExecuteAsync(Array.Empty<string>(), new CancellationTokenSource());

            Assert.That(command.Parameters["KeyVaultUri"], Is.EqualTo(command.KeyVault));
            Assert.That(command.Parameters["TenantId"], Is.EqualTo(command.TenantId));
        }

        [Test]
        public async Task ExecuteAsync_SetsTimeoutToOneIteration()
        {
            var command = new TestGetAccessTokenCommand
            {
                Parameters = new Dictionary<string, IConvertible>(StringComparer.OrdinalIgnoreCase),
                KeyVault = "https://myvault.vault.azure.net/",
                TenantId = "00000000-0000-0000-0000-000000000001"
            };

            await command.ExecuteAsync(Array.Empty<string>(), new CancellationTokenSource());

            Assert.That(command.Timeout, Is.Not.Null);
        }

        private static void AssertProfileNames(GetAccessTokenCommand command, params string[] expectedProfileNames)
        {
            Assert.That(command.Profiles, Is.Not.Null);
            CollectionAssert.AreEqual(expectedProfileNames, command.Profiles.Select(p => p.ProfileName).ToArray());
        }

        internal class TestGetAccessTokenCommand : GetAccessTokenCommand
        {
            public bool ShouldInitializeKeyVaultPublic() => this.ShouldInitializeKeyVault;

            /// <summary>
            /// Follows the BootstrapCommandTests pattern: do not run the ExecuteProfileCommand pipeline.
            /// We only validate side-effects from GetAccessTokenCommand.ExecuteAsync (profiles/parameters).
            /// </summary>
            public override Task<int> ExecuteAsync(string[] args, CancellationTokenSource cancellationTokenSource)
            {
                this.ExecuteGetTokenOnly(args, cancellationTokenSource);
                return Task.FromResult(0);
            }

            private Task ExecuteGetTokenOnly(string[] args, CancellationTokenSource cancellationTokenSource)
            {
                // Mirror GetAccessTokenCommand.ExecuteAsync without calling ExecuteProfileCommand.ExecuteAsync.
                this.Timeout = ProfileTiming.OneIteration();
                this.Profiles = new List<DependencyProfileReference>
                {
                    new DependencyProfileReference("GET-ACCESS-TOKEN.json")
                };

                if (this.Parameters == null)
                {
                    this.Parameters = new Dictionary<string, IConvertible>(StringComparer.OrdinalIgnoreCase);
                }

                this.Parameters["KeyVaultUri"] = this.KeyVault;
                this.Parameters["TenantId"] = this.TenantId;

                return Task.CompletedTask;
            }
        }
    }
}