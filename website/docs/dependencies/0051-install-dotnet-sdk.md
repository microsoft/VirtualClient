---
slug: /dependencies/install-dotnet-sdk
---

# Install .NET SDK
The .NET SDK is a set of libraries and tools that allow developers to create .NET applications and libraries. The dependency implementation in the
Virtual Client allows the user to define the version of the .NET SDK. The component defaults to `7.0.401`; shipped profiles select versions appropriate for their workloads.

- [SDK Documentation](https://learn.microsoft.com/en-us/dotnet/core/sdk)

## Supported Platform-Architectures
* linux-x64
* linux-arm64
* win-x64
* win-arm64

## Profile Component Parameters
The following section describes the parameters used by the individual component in the profile.

| **Parameter** | **Required** | **Description**                                         |
|---------------|--------------|---------------------------------------------------------|
| DotNetVersion | No           | The version of the .NET SDK to download and install. Default: `7.0.401`. |
| PackageName   | Yes          | The name/identifier used to reference the .NET SDK package download location by other Virtual Client components (e.g. 'dotnetsdk'...see example below). |
| Scenario      | No           | A name/identifier for the specific component in the profile. This is used for telemetry purposes only with components in dependency sections of the profile (i.e. cannot be used with --scenarios option on the command line).                                                      |

## Profile-to-Installer Flow

`DotNetInstallation` forwards the profile's `DotNetVersion`, the detected CPU
architecture, and the installation directory to the bundled installer. No hash
parameter or profile change is required: the Windows script selects the pinned
SHA-512 for known SDK versions and architectures, or resolves it from Microsoft's
release metadata for other versions.

On Windows, the component copies the bundled script to the `dotnet` package
directory and starts Windows PowerShell. The component's existing launcher and
execution-policy behavior are unchanged.

The script verifies the downloaded archive before extraction. A nonzero installer
exit code fails the dependency installation, and the component registers the
requested package only after the installer succeeds. Other components reference
that installation using the profile's `PackageName`.

## Windows Installer Security

The bundled `dotnet-install.ps1` requires HTTPS for feeds, metadata, and every
download redirect. HTTP and local-file feeds are not supported. Custom HTTPS
feeds remain supported; there is no Microsoft-only host allowlist.
Existing aka.ms resolution and ordinary download fallback behavior are retained;
security-policy and archive-verification failures terminate installation.

Before extracting a downloaded archive, the installer checks its SHA-512 against
an expected hash for the exact product, version, and Windows architecture.
SDK versions `7.0.401` (the component default), `8.0.204`, `9.0.111`, `9.0.203`, and
`10.0.101` have hashes pinned in the script for Windows x64 and ARM64. These
hashes come from Microsoft's public .NET release metadata and require no release
metadata request during installation. Other SDK versions, architectures, and
runtime-only installations resolve their hashes from that metadata.
A missing, ambiguous, or mismatched hash fails installation
without extracting the archive or adding it to PATH. A response `Content-Length`
mismatch also fails; a missing length does not bypass SHA-512 verification.
If an archive fails a pinned hash check, installation fails rather than looking
up a replacement hash. HTTPS protects transport; hash verification is not a
detached-signature check. Unpinned downloads trust Microsoft's metadata service.
For unpinned downloads, allow HTTPS access to
`https://dotnetcli.blob.core.windows.net/dotnet/release-metadata/<major.minor>/releases.json`
in addition to the artifact feeds.

When updating an SDK version in the shipped profiles or the component default,
update the `sdkArchiveHashes` map in `Get-Expected-Archive-Hash` for both Windows
architectures. Obtain each SHA-512 from the matching SDK version's `files` entry
in Microsoft's release metadata, selecting the Windows RID and ZIP filename.
Do not replace an existing pin solely because a downloaded archive fails verification.

For private, internal, daily, or other archives absent from public release metadata,
invoke the script directly with `-ExpectedArchiveHash` and a trusted SHA-512 value
(128 hexadecimal characters). Obtain this value from a trusted publisher or a
separately authenticated release process, not merely from the same custom feed.
The installer does not silently fall back to unverified archives. This script
parameter is not exposed as a `DotNetInstallation` profile parameter.

All ZIP entries are validated before extraction. Traversal, rooted paths, Windows
device names, alternate data streams, and paths through existing symbolic links or
junctions are rejected. The installation root and its ancestors must not be links.
Extraction and reparse-point checks use Windows extended-length paths to support
long SDK filenames, including when installation runs without administrator privileges.
Use an installation directory whose permissions prevent untrusted users from
changing it or its ancestors while installation runs; link checks alone do not
prevent concurrent filesystem replacement attacks.

Existing installed versions are still skipped and are not retroactively
authenticated. PATH changes affect only the installer process, not its parent.
These protections apply to the Windows PowerShell installer; the Linux
`dotnet-install.sh` script is unchanged.

## Example
The following section describes the parameters used by the individual component in the profile.

* [Profile Example](https://github.com/microsoft/VirtualClient/blob/main/src/VirtualClient/VirtualClient.Main/profiles/PERF-WEB-ASPNET-BOMBARDIER.json)

  ```json
  {
      "Type": "DotNetInstallation",
      "Parameters": {
          "Scenario": "InstallDotNetSdk",
          "DotNetVersion": "$.Parameters.DotNetVersion",
          "PackageName": "dotnetsdk"
      }
  }
  ```