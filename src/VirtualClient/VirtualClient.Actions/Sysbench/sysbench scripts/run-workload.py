import subprocess, argparse, os
        
parser = argparse.ArgumentParser()

parser.add_argument('-n', '--dbName', type=str, help='Database Name', required=True)
parser.add_argument('-y', '--databaseSystem', type=str, help='Database Type', required=False)
parser.add_argument('-w', '--workload', type=str, help="Workload Name", required=True)
parser.add_argument('-b', '--benchmark', type=str, help="Benchmark Name", required=True)
parser.add_argument('-t', '--tableCount', type=str, help='Number of Tables', required=True)
parser.add_argument('-r', '--recordCount', type=str, help='Number of Records', required=False)
parser.add_argument('-u', '--warehouses', type=str, help='Warehouse Count', required=False)
parser.add_argument('-e', '--threadCount', type=str, help="Number of Threads", required=True)
parser.add_argument('-a', '--hostIpAddress', type=str, help="Host IP Address", required=True)
parser.add_argument('-d', '--durationSecs', type=str, help="Duration in Seconds", required=True)
parser.add_argument('--password', type=str, help="PostgreSQL Password", required=False)

args = parser.parse_args()
dbName = args.dbName
databaseSystem = args.databaseSystem
workload = args.workload
benchmark = args.benchmark
tableCount = args.tableCount
recordCount = args.recordCount
warehouses = args.warehouses
threadCount = args.threadCount
hostIp = args.hostIpAddress
durationSecs = args.durationSecs
password = args.password

if databaseSystem == "MySQL":
    if benchmark == "TPCC":
        subprocess.run(f'sudo src/sysbench tpcc --tables={tableCount} --scale={warehouses} --threads={threadCount} --mysql-db={dbName} --mysql-host={hostIp} --time={durationSecs} run', shell=True, check=True)
    else:
        subprocess.run(f'sudo src/sysbench {workload} --tables={tableCount} --table-size={recordCount} --threads={threadCount} --mysql-db={dbName} --mysql-host={hostIp} --time={durationSecs} run', shell=True, check=True)
elif databaseSystem == "PostgreSQL":
    if benchmark == "TPCC":
        subprocess.run(f'sudo src/sysbench tpcc --db-driver=pgsql --tables={tableCount} --scale={warehouses} --threads={threadCount} --pgsql-user=postgres --pgsql-password={password} --pgsql-db={dbName} --pgsql-host={hostIp} --time={durationSecs} run', shell=True, check=True)
    else:
        subprocess.run(f'sudo src/sysbench {workload} --db-driver=pgsql --tables={tableCount} --table-size={recordCount} --threads={threadCount} --pgsql-user=postgres --pgsql-password={password} --pgsql-db={dbName} --pgsql-host={hostIp} --time={durationSecs} run', shell=True, check=True)
else:
    parser.error("You are running on a database type that has not been onboarded to Virtual Client. Available options are: MySQL, PostgreSQL")