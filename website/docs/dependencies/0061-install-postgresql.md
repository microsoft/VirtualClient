---
slug: /dependencies/install-postgresql
---

# Install PostgreSQL Database
Virtual Client has dependency components that can be added to a workload or monitor profile to create a PostgreSQL database on the system. The following section illustrates the
details for integrating this into the profile.

- [PostgreSQL Documentation](https://www.postgresql.org/docs/)

## Supported Configuration and Workload Platforms
* linux-x64
* linux-arm64

HammerDB is limited to `linux-x64`. The installation component also accepts Windows,
but `PostgreSQLServerConfiguration` and the workload profiles described here are Linux-only.

## Profile Component Parameters
The following section describes the parameters used by the individual component in the profile.

| **Parameter** | **Required** | **Description**                                                                                                 |
|---------------|--------------|-----------------------------------------------------------------------------------------------------------------|
| PostgreSQLPasswordFile | No | Legacy override supported only by Sysbench PostgreSQL actions. PostgreSQL configuration and HammerDB always use the package's `Password.json`. |
| Password | Not supported | Do not pass passwords in profiles or command-line arguments. |

## PostgreSQL Server Configuration Credentials

PostgreSQL configuration and HammerDB always read `Password.json` in the
platform-specific `postgresql` package directory. They do not use custom password
paths or an inherited `PGPASSWORD` as input.

The HammerDB profiles run the server-only `ConfigureCredentials` action before
`ConfigureServer`. With `postgresql.14.0.0.rev6.zip`, this invokes
`generate-password.py` in the selected architecture directory. The generator uses
Python's `secrets` module, creates an owner-only file, and never prints the password.
A valid existing file is retained; invalid or empty files fail explicitly.
Generation does not change the database password: the later `ConfigureServer`
action applies the generated credential to PostgreSQL.

Other profiles must provision the default file before configuration or opt in to
`ConfigureCredentials` with the updated package. Do not generate independently on
clients, as that produces different passwords. Sysbench's PostgreSQL actions retain
their existing explicit-file and `PGPASSWORD` fallbacks; leave those overrides unset
when using the generated package credential.

Virtual Client reads the credential when the step runs and sets `PGPASSWORD` only in
the relevant child process environment before starting it. It does not change Virtual
Client's own environment, include the password in command arguments, or store it in
VC state. The Python scripts and their descendants receive the variable as before.

### Automatic Client Distribution

For multi-VM layouts, `ConfigureCredentials` passes the client IP addresses to
`generate-password.py --clientIps`. The generator copies the server credential over
SSH to the same absolute package path on each client. The MySQL package uses the
same transfer implementation after configuring its database accounts.

Before starting the workload:

* Deploy VC with matching absolute database package paths on all VMs, including the
  architecture directory. Client IPs alone cannot identify a different deployment path.
* Configure OpenSSH key authentication and trusted host keys for each client IP under
  the account executing the configuration script. MySQL's configuration script runs
  elevated, so its SSH configuration and keys must be accessible to root.
* Allow SSH network access and passwordless `sudo -n python3` on clients. The SSH
  username and identity can be selected in that execution account's SSH configuration;
  no SSH passwords or private keys are passed in profiles or command arguments.

Transfers use noninteractive SSH with strict host-key checking and no pseudo-terminal.
The password is sent through encrypted stdin, not command arguments. The receiver
waits up to 40 minutes for package extraction, then atomically writes `Password.json`
with permissions `0600` and ownership matching the package directory. HammerDB
clients also wait up to 40 minutes for the file before initialization. Invalid
existing files still fail rather than being ignored.

Any transfer failure fails `ConfigureCredentials`; VC saves success only after all
copies succeed. Retries reuse the server's password. Like other configuration
actions, a successfully completed action is skipped on subsequent runs. This does
not bootstrap SSH access or automatically synchronize newly added clients.
Standalone runs do not invoke SSH. No secret is served by VC's coordination API.

The file is plaintext. Restrict it to the account running Virtual Client (for example,
`chmod 600` on Linux), protect its parent directory, and never publish a package archive
containing a real password. Do not print the file, enable shell tracing while provisioning
it, or log child environments. An environment variable remains readable by sufficiently
privileged processes.

### Required Package Script Update

This configuration requires a PostgreSQL package whose `configure-server.py` and
`setup-database.py` scripts read `os.environ["PGPASSWORD"]` instead of requiring a
`--password` argument. These scripts are distributed in the workload package, not in
this repository. Older or custom packages that require `--password` are incompatible.
Scripts must not log the password
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
The profile downloads the package with `DependencyPackageInstallation`, installs
PostgreSQL with `PostgreSQLServerInstallation`, and configures the server with
`PostgreSQLServerConfiguration`. HammerDB or Sysbench then populates the benchmark tables.

* [Profile Example](https://github.com/microsoft/VirtualClient/blob/main/src/VirtualClient/VirtualClient.Main/profiles/PERF-POSTGRESQL-HAMMERDB-TPCC.json)

```json
{
    "Type": "PostgreSQLServerInstallation",
    "Parameters": {
        "Scenario": "InstallPostgreSQLServer",
        "Action": "InstallServer",
        "PackageName": "postgresql"
    }
}
```