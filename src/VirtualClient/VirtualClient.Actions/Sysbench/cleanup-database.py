import subprocess, argparse, os
        
parser = argparse.ArgumentParser()

parser.add_argument('-n', '--dbName', type=str, help='Database Name', required=True)
parser.add_argument('-y', '--databaseSystem', type=str, help='Database Type', required=False)
parser.add_argument('-b', '--benchmark', type=str, help="Benchmark Name", required=True)
parser.add_argument('-t', '--tableCount', type=str, help='Number of Tables', required=True)
parser.add_argument('-a', '--hostIpAddress', type=str, help="Host IP Address", required=True)

args = parser.parse_args()
dbName = args.dbName
databaseSystem = args.databaseSystem
benchmark = args.benchmark
tableCount = args.tableCount
hostIp = args.hostIpAddress

if databaseSystem == "MySQL":
    if benchmark == "OLTP":
        subprocess.run(f'sudo src/sysbench oltp_common --tables={tableCount} --mysql-db={dbName} --mysql-host={hostIp} cleanup',shell=True, check=True)
else:
    parser.error("You are running on a database type that has not been onboarded to Virtual Client. Available options are: MySQL")