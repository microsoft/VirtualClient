# Azure Key Vault Integration

Azure Key Vault is a secure cloud service for storing and accessing secrets, keys, and certificates. Virtual Client supports integration with Azure Key Vault, allowing you to retrieve secrets and certificates for use in your workloads, monitors, automation, and telemetry scenarios.

This guide covers the requirements, supported authentication methods, command-line options, and usage examples for integrating Azure Key Vault with Virtual Client.

---

## Table of Contents

- [Overview](#overview)
- [Authentication Preliminaries](#authentication-preliminaries)
  - [Certificates on Linux](#referencing-certificates-on-linux)
  - [Certificates on Windows](#referencing-certificates-on-windows)
- [Key Vault Integration](#key-vault-integration)
  - [Supported Reference Styles](#supported-reference-styles)
    - [URI-Style References](#uri-style-references)
    - [Connection String-Style References](#connection-string-style-references)
- [Command-Line Options](#command-line-options)
- [Usage Examples](#usage-examples)
- [Error Handling](#error-handling)
- [Best Practices](#best-practices)
- [References](#references)

---

## Overview

Virtual Client can retrieve secrets and certificates from Azure Key Vault for use in authentication, secure configuration, and workload execution. You can specify a Key Vault endpoint or connection string using command-line options. Virtual Client supports both Microsoft Entra ID (formerly Azure AD) and Managed Identity authentication.

---

## Authentication Preliminaries

### Installing Certificates on Linux

Virtual Client is a .NET application. Certificates used on a Linux system must be X.509 certificates containing a private key (e.g., PKCS#12, *.pfx). Certificates must be installed in the expected location for the user running Virtual Client:

- **Root:** `/root/.dotnet/corefx/cryptography/x509stores/my/`
- **Specific User:** `/home/{user}/.dotnet/corefx/cryptography/x509stores/my/`

Ensure the user has read/write access to this directory and the certificate files. Set permissions as follows:

`sudo chmod -R 700 /home/{user}/.dotnet/corefx/cryptography/x509stores/my/`

### Installing Certificates on Windows

Virtual Client will look for certificates in these stores:

- **CurrentUser/Personal:** The personal certificate store for the current user.
- **LocalMachine/Personal:** The local machine certificate store.

It is recommended to install certificates into the user-specific store.

---

## Key Vault Integration

Virtual Client supports referencing Azure Key Vault using both URI-style and connection string-style formats. The Key Vault can be used to retrieve secrets (e.g., API keys, passwords) and certificates for use in your workloads.

### Supported Reference Styles

#### URI-Style References

You can specify the Key Vault endpoint as a URI, optionally with authentication parameters:

- **Microsoft Entra ID/App with Certificate (by thumbprint):**

``` bash
--keyvault="https://myvault.vault.azure.net?cid={clientId}&tid={tenantId}&crtt={certificateThumbprint}"
```

- **Microsoft Entra ID/App with Certificate (by issuer and subject):**

``` bash
--keyvault="https://myvault.vault.azure.net?cid={clientId}&tid={tenantId}&crti={issuer}&crts={subject}"
```

- **Managed Identity:**

``` bash
--keyvault="https://myvault.vault.azure.net?miid={managedIdentityClientId}"
```

#### Connection String-Style References

You can also use a connection string format:

- **Microsoft Entra ID/App with Certificate (by thumbprint):**

``` bash
--keyvault="EndpointUrl=https://myvault.vault.azure.net;ClientId={clientId};TenantId={tenantId};CertificateThumbprint={certificateThumbprint}"
```

- **Microsoft Entra ID/App with Certificate (by issuer and subject):**

``` bash
--keyvault="EndpointUrl=https://myvault.vault.azure.net;ClientId={clientId};TenantId={tenantId};CertificateIssuer={issuer};CertificateSubject={subject}"
```

- **Managed Identity:**

``` bash
--keyvault="EndpointUrl=https://myvault.vault.azure.net;ManagedIdentityId={managedIdentityClientId}"
```

---

## Command-Line Options

The following options are available for Key Vault integration:

- `--kv`, `--keyvault`, `--key-vault`, `--keyVault`, `--key-Vault`
- **Description:** Specifies the Azure Key Vault endpoint or connection string.
- **Example:** `--keyvault="https://myvault.vault.azure.net"`

Other options may be used depending on your authentication method (see above).

---

## Usage Examples

### Retrieve a Secret Using Managed Identity

``` bash
VirtualClient --keyvault="https://myvault.vault.azure.net?miid=6d3c5db8-e14b-44b7-9887-d168b5f659f6" --other-options
```

### Retrieve a Secret Using Microsoft Entra ID and Certificate Thumbprint

``` bash
VirtualClient --keyvault="EndpointUrl=https://myvault.vault.azure.net;ClientId=08331e3b-1458-4de2-b1d6-7007bc7221d5;TenantId=573b5dBbe-c477-4a10-8986-a7fe10e2d79B;CertificateThumbprint=f5b114e61c6a81b40c1e7a5e4d11ac47da6e445f" --other-options
```

### Retrieve a Secret Using Microsoft Entra ID and Certificate Issuer/Subject

``` bash
VirtualClient --keyvault="EndpointUrl=https://myvault.vault.azure.net;ClientId=08331e3b-1458-4de2-b1d6-7007bc7221d5;TenantId=573b5dBbe-c477-4a10-8986-a7fe10e2d79B;CertificateIssuer=CN=ABC CA 01, DC=ABC, DC=COM;CertificateSubject=CN=any.domain.com" --other-options
```

---

## Error Handling

Virtual Client will report errors if:

- The Key Vault endpoint or credentials are invalid.
- The specified secret or certificate is not found.
- Permissions are insufficient (e.g., forbidden).
- The certificate is missing or inaccessible.

Refer to the logs for detailed error messages. See unit tests in `KeyVaultManagerTests.cs` for expected error handling scenarios.

---

## Best Practices

- Use Managed Identity where possible for secure, passwordless authentication.
- Store certificates securely and restrict access to certificate files.
- Grant only necessary permissions to the Key Vault and its secrets/certificates.
- Test your Key Vault integration using the provided unit tests and error scenarios.

---

## References

- [Azure Key Vault Documentation](https://learn.microsoft.com/en-us/azure/key-vault/)
- [Virtual Client Command-Line Reference](https://microsoft.github.io/VirtualClient/docs/guides/0010-command-line/)

