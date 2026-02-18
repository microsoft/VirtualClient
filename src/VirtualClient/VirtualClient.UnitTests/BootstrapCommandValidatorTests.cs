// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient
{
    using System;
    using System.CommandLine.Builder;
    using System.CommandLine.Parsing;
    using System.Threading;
    using NUnit.Framework;

    [TestFixture]
    [Category("Unit")]
    public class BootstrapCommandValidatorTests
    {
        [Test]
        public void BootstrapCommand_RequiresAtLeastOneOperation()
        {
            using CancellationTokenSource tokenSource = new CancellationTokenSource();
            CommandLineBuilder commandBuilder = Program.SetupCommandLine(Array.Empty<string>(), tokenSource);

            ParseResult parseResult = commandBuilder.Build().Parse(new[] { "bootstrap" });

            ArgumentException exe = Assert.Throws<ArgumentException>(() => parseResult.ThrowOnUsageError());
            StringAssert.Contains("At least one operation must be specified for the bootstrap command.", exe!.Message);
        }

        [Test]
        [TestCase("--cert-name")]
        [TestCase("--certname")]
        [TestCase("--certificate-name")]
        public void BootstrapCommand_CertificateInstall_RequiresKeyVault(string certAlias)
        {
            using CancellationTokenSource tokenSource = new CancellationTokenSource();
            CommandLineBuilder commandBuilder = Program.SetupCommandLine(Array.Empty<string>(), tokenSource);

            ParseResult parseResult = commandBuilder.Build().Parse(new[] { "bootstrap", certAlias, "mycertName" });

            ArgumentException exe = Assert.Throws<ArgumentException>(() => parseResult.ThrowOnUsageError());
            StringAssert.Contains("The Key Vault URI must be provided (--key-vault)", exe!.Message);
        }

        [Test]
        [TestCase("--package")]
        [TestCase("--pkg")]
        public void BootstrapCommand_PackageInstall_DoesNotRequireKeyVault(string packageAlias)
        {
            using CancellationTokenSource tokenSource = new CancellationTokenSource();
            CommandLineBuilder commandBuilder = Program.SetupCommandLine(Array.Empty<string>(), tokenSource);

            ParseResult parseResult = commandBuilder.Build().Parse(new[] { "bootstrap", packageAlias, "mypackage" });

            Assert.DoesNotThrow(() => parseResult.ThrowOnUsageError());
        }

        [Test]
        [TestCase("--kv")]
        [TestCase("--key-vault")]
        public void BootstrapCommand_CertificateInstall_WithKeyVault_IsValid(string kvAlias)
        {
            using CancellationTokenSource tokenSource = new CancellationTokenSource();
            CommandLineBuilder commandBuilder = Program.SetupCommandLine(Array.Empty<string>(), tokenSource);

            ParseResult parseResult = commandBuilder.Build().Parse(new[]
            {
                "bootstrap",
                "--cert-name", "mycert",
                kvAlias, "https://myvault.vault.azure.net/"
            });

            Assert.DoesNotThrow(() => parseResult.ThrowOnUsageError());
        }
    }
}