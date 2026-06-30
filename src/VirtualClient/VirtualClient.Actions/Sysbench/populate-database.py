import subprocess, argparse, os, sys

parser = argparse.ArgumentParser()

parser.add_argument('-n', '--dbName', type=str, help='Database Name', required=True)
parser.add_argument('-y', '--databaseSystem', type=str, help='Database Type', required=False)
parser.add_argument('-b', '--benchmark', type=str, help="Benchmark Name", required=True)
parser.add_argument('-t', '--tableCount', type=str, help='Number of Tables', required=True)
parser.add_argument('-r', '--recordCount', type=str, help='Number of Records', required=False)
parser.add_argument('-u', '--warehouses', type=str, help='Warehouse Count', required=False)
parser.add_argument('-e', '--threadCount', type=str, help="Number of Threads", required=True)
parser.add_argument('--password', type=str, help="PostgreSQL Password", required=False)
parser.add_argument('--hostIpAddress', type=str, help="Database Server Host IP Address", required=False)


args = parser.parse_args()
dbName = args.dbName
databaseSystem = args.databaseSystem
benchmark = args.benchmark
warehouses = args.warehouses
tableCount = args.tableCount
recordCount = args.recordCount
threadCount = args.threadCount
password = args.password
host = args.hostIpAddress

def run_command(command):
    """Run a shell command, check exit code AND scan for FATAL errors in output."""
    result = subprocess.run(command, shell=True, capture_output=True, text=True)
    combined_output = result.stdout + result.stderr

    if result.stdout:
        print(result.stdout, end='')
    if result.stderr:
        print(result.stderr, end='', file=sys.stderr)

    if result.returncode != 0:
        sys.exit(result.returncode)

    # sysbench may exit 0 even on FATAL connection errors
    if "FATAL:" in combined_output:
        print("ERROR: sysbench reported FATAL errors but exited with code 0. Failing the population step.", file=sys.stderr)
        sys.exit(1)

if databaseSystem == "MySQL":
    if benchmark == "TPCC":
        # Drop any tables left from a prior (possibly crash-interrupted) run so
        # 'prepare' is idempotent across VirtualClient restart-on-crash. Otherwise
        # re-running 'prepare' against populated tables fails with MySQL error
        # 1062 (Duplicate entry) / 1061 (Duplicate key name).
        subprocess.run(f'sudo src/sysbench tpcc --tables={tableCount} --scale={warehouses} --threads={threadCount} --mysql-db={dbName} --mysql-host={host} --use_fk=0 cleanup', shell=True)
        run_command(f'sudo src/sysbench tpcc --tables={tableCount} --scale={warehouses} --threads={threadCount} --mysql-db={dbName} --mysql-host={host} --use_fk=0 prepare')
    else:
        run_command(f'sudo src/sysbench oltp_common --tables={tableCount} --table-size={recordCount} --threads={threadCount} --mysql-db={dbName} --mysql-host={host} prepare')
elif databaseSystem == "PostgreSQL":
    os.environ['PGPASSWORD'] = password
    if benchmark == "TPCC":
        run_command(f'sudo src/sysbench tpcc --db-driver=pgsql --tables={tableCount} --scale={warehouses} --threads={threadCount} --pgsql-user=postgres --pgsql-password={password} --pgsql-db={dbName} --use_fk=0 prepare')
    else:
        run_command(f'sudo src/sysbench oltp_common --db-driver=pgsql --tables={tableCount} --table-size={recordCount} --threads={threadCount} --pgsql-user=postgres --pgsql-password={password} --pgsql-db={dbName} prepare')
else:
    parser.error("You are running on a database system type that has not been onboarded to Virtual Client. Available options are: MySQL, PostgreSQL")