---
slug: /dependencies/install-postgresql
---

# Install PostgreSQL Database
Virtual Client has dependency components that can be added to a workload or monitor profile to create a PostgreSQL database on the system. The following section illustrates the
details for integrating this into the profile.

- [PostgreSQL Documentation](https://learn.microsoft.com/en-us/dotnet/core/sdk)

## Supported Platform-Architectures
* linux-x64
* linux-arm64
* win-x64
* win-arm64

## Profile Component Parameters
The following section describes the parameters used by the individual component in the profile.

| **Parameter** | **Required** | **Description**                                                                                                 |
|---------------|--------------|-----------------------------------------------------------------------------------------------------------------|
| PostgreSQLPasswordFile | No | Path to a provisioned JSON file containing a non-empty string named `Password`. Supported by PostgreSQL configuration, HammerDB, and Sysbench PostgreSQL actions. |
| Password | Not supported | Do not pass passwords in profiles or command-line arguments. |

## PostgreSQL Server Configuration Credentials

PostgreSQL configuration, HammerDB, and Sysbench PostgreSQL actions resolve the
superuser password in this order:

1. The JSON file specified by the component parameter `PostgreSQLPasswordFile`, if supplied.
2. The existing `PGPASSWORD` environment variable in the Virtual Client process.
3. `Password.json` in the platform-specific `postgresql` package directory, such as
   `linux-x64/Password.json` or `linux-arm64/Password.json`.

Each architecture includes `generate-password.py`. Run it after package extraction
and before PostgreSQL configuration:

```bash
python3 /path/to/postgresql/linux-x64/generate-password.py
```

Use the `linux-arm64` script on ARM64. The generator creates `Password.json` beside
itself by default, or at the path specified by `--output`. It uses Python's `secrets`
module, sets owner-only permissions on Linux, and never prints the password. A valid
existing file is retained; an invalid or empty file is not silently overwritten.
Generation does not set the database password; the later PostgreSQL configuration
step applies the generated credential to the server.

Generate the credential once and securely provision the same file on all participating
client and server VMs. Running the generator independently on each VM produces different
passwords. Alternatively, provision the JSON `Password` value from your secret provider.
An earlier process launched by Virtual Client can create this file; no shell export needs to persist across steps.
The file is parsed as JSON, not sourced or executed as a shell script. Empty passwords,
missing files, malformed JSON, and unreadable files cause the step to fail. An explicit
file path never silently falls back to the environment if its file is invalid.

Virtual Client reads the credential when the step runs and sets `PGPASSWORD` only in
the relevant child process environment before starting it. It does not change Virtual
Client's own environment, include the password in command arguments, or store it in
VC state. The Python scripts and their descendants receive the variable as before.

Provision the same password on each client and server VM. There is no automatic
cross-VM credential distribution. The PostgreSQL Sysbench profiles download the
PostgreSQL package on clients as well as servers so the default file location is
available. Alternatively, set `PostgreSQLPasswordFile` on each relevant component to
use an independently provisioned file outside the package.

The file is plaintext. Restrict it to the account running Virtual Client (for example,
`chmod 600` on Linux), protect its parent directory, and never publish a package archive
containing a real password. Do not print the file, enable shell tracing while provisioning
it, or log child environments. An environment variable remains readable by sufficiently
privileged processes.

### Required Package Script Update

This configuration requires a PostgreSQL package whose `configure-server.py` and
`setup-database.py` scripts read `os.environ["PGPASSWORD"]` instead of requiring a
`--password` argument. These scripts are distributed in the workload package, not in
this repository. Update both scripts before using this configuration change; older
packages that require `--password` are incompatible. Scripts must not log the password
or interpolate it into subprocess command arguments. Environment variables are not
a secret store and can still be read by sufficiently privileged processes.

HammerDB and Sysbench PostgreSQL actions supply the resolved credential through
`PGPASSWORD`, with no default password and no `--password` argument. Sysbench's MySQL
credential behavior is unchanged.

The workload packages must additionally update these scripts to read `PGPASSWORD`
for PostgreSQL without requiring a password argument:

* HammerDB: `configure-workload-generator.py`
* Sysbench: `populate-database.py`
* Sysbench: `run-workload.py`

HammerDB preserves `PGPASSWORD` when invoking its configuration script through
`sudo --preserve-env=PGPASSWORD`. The sudo policy must permit this. Any nested sudo
calls in the package scripts must also preserve the variable when it is needed.
HammerDB scripts must safely escape arbitrary passwords when generating TCL files,
and restrict access to generated files containing credentials.
HammerDB echoes configuration values, including credentials, in its output. Virtual
Client obscures the resolved PostgreSQL credential before writing HammerDB process
output to telemetry or log files, and before including failed-process output in exceptions.

## Example
Here is the usage of the dependency by the PostgreSQL/HammerDB workload as an example. The Postgresql dependency is responsible for downloading the service, but the workload itself is responsible for creating the database and populating it.

* [Profile Example](https://github.com/microsoft/VirtualClient/blob/main/src/VirtualClient/VirtualClient.Main/profiles/PERF-SQL-POSTGRESQL.json)

```json
{
    "Type": "PostgreSQLInstallation",
    "Parameters": {
        "Scenario": "InstallPostgreSQLServer",
        "PackageName": "postgresql"
    }
}
```