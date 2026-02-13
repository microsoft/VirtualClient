* ### bootstrap
  Command is used to bootstrap/install dependency packages and/or install a certificate on the system.

  * **Package bootstrapping**: Install an extensions/dependency package to the Virtual Client runtime.  
    Requires `--package` (and typically `--package-store/--packages` to fetch it from storage).

  * **Certificate bootstrapping**: Install a certificate to the system for use by workloads that require certificate-based authentication.  
    Requires `--cert-name` **and** `--key-vault`.  
    Optionally supports `--token` to provide an access token for Key Vault authentication (if not provided, the default Azure credential flow is used).

  | Option                                                          | Required | Data Type                    | Description |
  |-----------------------------------------------------------------|----------|------------------------------|-------------|
  | --pkg, --package=\<blobName\>                                   | No*      | string/blob name             | Name/ID of a package to bootstrap/install (e.g. `anypackage.1.0.0.zip`). Required when doing **package bootstrapping**. |
  | --ps, --packages, --package-store=\<connection\>                | No       | string/connection string/SAS | Connection description for an Azure Storage Account/container to download packages from. See [Azure Storage Account Integration](./0600-integration-blob-storage.md). |
  | --certificateName, --cert-name=\<certificateName\>              | No*      | string/certificate name      | Name of the certificate in Key Vault to bootstrap/install (e.g. `--cert-name="crc-sdk-cert"`). Required when doing **certificate bootstrapping**. |
  | --key-vault, --kv=\<keyVaultUri\>                               | No*      | uri                          | Azure Key Vault URI to source the certificate from (e.g. `https://myvault.vault.azure.net/`). Required when doing **certificate bootstrapping**. |
  | --token, --access-token=\<accessToken\>                         | No       | string                       | Optional access token used to authenticate to Key Vault when installing certificates. If not provided, Virtual Client uses the default Azure credential flow (e.g. Azure CLI, Managed Identity, etc.). |
  | --c, --client-id=\<id\>                                         | No       | string/text                  | Identifier to uniquely identify the instance (telemetry correlation). |
  | --clean=\<target,target...\>                                    | No       | string                       | Perform an initial cleanup (logs/packages/state/temp/all). |
  | --cs, --content, --content-store=\<connection\>                 | No       | string/connection string/SAS | Storage connection for uploading files/content (e.g. logs). |
  | --cp, --content-path, --content-path-template=\<folderPattern\> | No       | string/text                  | Upload folder structure template. |
  | --event-hub=\<connection\>                                      | No       | string/connection string     | Event Hub connection for telemetry upload (deprecated in favor of `--logger=eventhub;...`). |
  | --e, --experiment-id=\<guid\>                                   | No       | guid                         | Experiment identifier. |
  | --isolated                                                      | No       |                              | Run with dependency isolation (unique logs/packages/state/temp per experiment). |
  | --logger=\<reference\>                                          | No       | string/path                  | One or more logger definitions. |
  | --ldir, --log-dir=\<path\>                                      | No       | string/path                  | Alternate logs directory. |
  | --ll, --log-level=\<level\>                                     | No       | integer/string               | Trace severity level. |
  | --lr, --log-retention=\<mins_or_timespan\>                      | No       | timespan or integer          | Log retention period. |
  | --mt, --metadata=\<key=value,,,key=value...\>                   | No       | string/text                  | Metadata to include with telemetry output. |
  | --n, --name=\<name\>                                            | No       | string/name                  | Logical name to register a package as. |
  | --pdir, --package-dir=\<path\>                                  | No       | string/path                  | Alternate packages directory. |
  | --sdir, --state-dir=\<path\>                                    | No       | string/path                  | Alternate state directory. |
  | --s, --system=\<executionSystem\>                               | No       | string/text                  | Execution system/platform identifier (e.g. Azure). |
  | --tdir, --temp-dir=\<path\>                                     | No       | string/path                  | Alternate temp directory. |
  | --wait, --exit-wait=\<mins_or_timespan\>                        | No       | timespan or integer          | Wait for graceful exit/telemetry flush. |
  | --verbose                                                       | No       |                              | Verbose console logging (equivalent to `--log-level=Trace`). |
  | -?, -h, --help                                                  | No       |                              | Show help. |
  | --version                                                       | No       |                              | Show version.

  \*Note: at least one operation must be specified. Use either `--package` (package bootstrapping) or `--cert-name` with `--key-vault` (certificate bootstrapping), or both.

* ### get-token
  Command is used to retrieve an Azure access token for the **current user**. This token can be supplied to other commands (e.g. `bootstrap`) using the `--token/--access-token` option for explicit authentication against Azure Key Vault.

  This is useful when:
  - the default Azure credential flow is not available on the machine, or
  - you want to explicitly pass a token to `bootstrap` for Key Vault certificate installation.

  **Authentication experience**
  - If browser-based authentication is available, Virtual Client will open/prompt a sign-in in your browser.
  - If browser-based authentication is not available, Virtual Client will automatically switch to **device code flow** and display a URL and a short code. Complete sign-in on another authenticated device using the provided URL and code.

  | Option                                         | Required | Data Type     | Description |
  |-----------------------------------------------|----------|---------------|-------------|
  | --kv, --keyvault, --key-vault=\<uri\>         | Yes      | uri           | Azure Key Vault URI used as the authentication resource (e.g. `https://myvault.vault.azure.net/`). |
  | --clean=\<target,target...\>                  | No       | string        | Perform an initial cleanup (logs/packages/state/temp/all). |
  | --c, --client-id=\<id\>                       | No       | string/text   | Identifier to uniquely identify the instance (telemetry correlation). |
  | --e, --experiment-id=\<guid\>                 | No       | guid          | Experiment identifier. |
  | --pm, --parameters=\<key=value,,,key=value\>  | No       | string/text   | Additional parameters/overrides (optional). |
  | --verbose                                     | No       |               | Verbose console logging (equivalent to `--log-level=Trace`). |
  | -?, -h, --help                                | No       |               | Show help information. |
  | --version                                     | No       |               | Show application version information. |
