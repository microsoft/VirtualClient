import subprocess, argparse, os

parser = argparse.ArgumentParser()

parser.add_argument('-n', '--dbName', type=str, help='Database Name', required=True)
parser.add_argument('-y', '--databaseSystem', type=str, help='Database Type', required=False)
parser.add_argument('-b', '--benchmark', type=str, help="Benchmark Name", required=True)
parser.add_argument('-t', '--tableCount', type=str, help='Number of Tables', required=True)
parser.add_argument('-r', '--recordCount', type=str, help='Number of Records', required=False)
parser.add_argument('-u', '--warehouses', type=str, help='Warehouse Count', required=False)
parser.add_argument('-e', '--threadCount', type=str, help="Number of Threads", required=True)
parser.add_argument('--password', type=str, help="PostgreSQL Password", required=False)
parser.add_argument('--host', type=str, help="Database Server Host IP Address", required=False)


args = parser.parse_args()
dbName = args.dbName
databaseSystem = args.databaseSystem
benchmark = args.benchmark
warehouses = args.warehouses
tableCount = args.tableCount
recordCount = args.recordCount
threadCount = args.threadCount
password = args.password
host = args.host

def add_host_if_needed(command_base, host, benchmark):
    if host and benchmark != "TPCC":
        if "sysbench" not in command_base:
            return command_base.replace('-u', f'-h {host} -u')
        else:
            return f"{command_base} --mysql-host={host}"
    else:
        return command_base

if databaseSystem == "MySQL":
    if benchmark == "TPCC":
        subprocess.run(f'sudo src/sysbench tpcc --tables={tableCount} --scale={warehouses} --threads={threadCount} --mysql-db={dbName} --use_fk=0 prepare', shell=True, check=True)
        if int(warehouses) == 1:
            for i in range(1,int(tableCount)+1):
                table = str(i)
                # drop idxs
                subprocess.run(f'sudo mysql -u {dbName} {dbName} -e "DROP INDEX idx_customer{i} ON customer{i};"', shell=True, check=True)
                subprocess.run(f'sudo mysql -u {dbName} {dbName} -e "DROP INDEX idx_orders{i} ON orders{i};"', shell=True, check=True)
                subprocess.run(f'sudo mysql -u {dbName} {dbName} -e "DROP INDEX fkey_stock_2{i} ON stock{i};"', shell=True, check=True)
                subprocess.run(f'sudo mysql -u {dbName} {dbName} -e "DROP INDEX fkey_order_line_2{i} ON order_line{i};"', shell=True, check=True)
                subprocess.run(f'sudo mysql -u {dbName} {dbName} -e "DROP INDEX fkey_history_1{i} ON history{i};"', shell=True, check=True)
                subprocess.run(f'sudo mysql -u {dbName} {dbName} -e "DROP INDEX fkey_history_2{i} ON history{i};"', shell=True, check=True)

                # truncate, to make distributing faster
                subprocess.run(f'sudo mysql -u {dbName} {dbName} -e "TRUNCATE TABLE warehouse{i};"', shell=True, check=True)
                subprocess.run(f'sudo mysql -u {dbName} {dbName} -e "TRUNCATE TABLE district{i};"', shell=True, check=True)
                subprocess.run(f'sudo mysql -u {dbName} {dbName} -e "TRUNCATE TABLE customer{i};"', shell=True, check=True)
                subprocess.run(f'sudo mysql -u {dbName} {dbName} -e "TRUNCATE TABLE history{i};"', shell=True, check=True)
                subprocess.run(f'sudo mysql -u {dbName} {dbName} -e "TRUNCATE TABLE orders{i};"', shell=True, check=True)
                subprocess.run(f'sudo mysql -u {dbName} {dbName} -e "TRUNCATE TABLE new_orders{i};"', shell=True, check=True)
                subprocess.run(f'sudo mysql -u {dbName} {dbName} -e "TRUNCATE TABLE order_line{i};"', shell=True, check=True)
                subprocess.run(f'sudo mysql -u {dbName} {dbName} -e "TRUNCATE TABLE stock{i};"', shell=True, check=True)
                subprocess.run(f'sudo mysql -u {dbName} {dbName} -e "TRUNCATE TABLE item{i};"', shell=True, check=True)
    else:
         command_base = f'sudo src/sysbench oltp_common --tables={tableCount} --table-size={recordCount} --threads={threadCount} --mysql-db={dbName} prepare'
         command = add_host_if_needed(command_base, host, benchmark)
         subprocess.run(command, shell=True, check=True)
         if int(recordCount) == 1:
            for i in range(1, int(tableCount) + 1):
                drop_index_command = f'sudo mysql -u {dbName} {dbName} -e "DROP INDEX k_{i} ON sbtest{i};"'
                drop_index_command = add_host_if_needed(drop_index_command, host, benchmark)
                subprocess.run(drop_index_command, shell=True, check=True)
elif databaseSystem == "PostgreSQL":
    os.environ['PGPASSWORD'] = password
    if benchmark == "TPCC":
        subprocess.run(f'sudo src/sysbench tpcc --db-driver=pgsql --tables={tableCount} --scale={warehouses} --threads={threadCount} --pgsql-user=postgres --pgsql-password={password} --pgsql-db={dbName} --use_fk=0 prepare', shell=True, check=True)
        if int(warehouses) == 1:
            for i in range(1,int(tableCount)+1):
                table = str(i)
                # drop idxs
                subprocess.run(f'psql -U postgres -d {dbName} -c "DROP INDEX IF EXISTS idx_customer{i};"', shell=True, check=True)
                subprocess.run(f'psql -U postgres -d {dbName} -c "DROP INDEX IF EXISTS idx_orders{i};"', shell=True, check=True)
                subprocess.run(f'psql -U postgres -d {dbName} -c "DROP INDEX IF EXISTS fkey_stock_2{i};"', shell=True, check=True)
                subprocess.run(f'psql -U postgres -d {dbName} -c "DROP INDEX IF EXISTS fkey_order_line_2{i};"', shell=True, check=True)
                subprocess.run(f'psql -U postgres -d {dbName} -c "DROP INDEX IF EXISTS fkey_history_1{i};"', shell=True, check=True)
                subprocess.run(f'psql -U postgres -d {dbName} -c "DROP INDEX IF EXISTS fkey_history_2{i};"', shell=True, check=True)

                # truncate, to make distributing faster
                subprocess.run(f'psql -U postgres -d {dbName} -c "TRUNCATE TABLE warehouse{i};"', shell=True, check=True)
                subprocess.run(f'psql -U postgres -d {dbName} -c "TRUNCATE TABLE district{i};"', shell=True, check=True)
                subprocess.run(f'psql -U postgres -d {dbName} -c "TRUNCATE TABLE customer{i};"', shell=True, check=True)
                subprocess.run(f'psql -U postgres -d {dbName} -c "TRUNCATE TABLE history{i};"', shell=True, check=True)
                subprocess.run(f'psql -U postgres -d {dbName} -c "TRUNCATE TABLE orders{i};"', shell=True, check=True)
                subprocess.run(f'psql -U postgres -d {dbName} -c "TRUNCATE TABLE new_orders{i};"', shell=True, check=True)
                subprocess.run(f'psql -U postgres -d {dbName} -c "TRUNCATE TABLE order_line{i};"', shell=True, check=True)
                subprocess.run(f'psql -U postgres -d {dbName} -c "TRUNCATE TABLE stock{i};"', shell=True, check=True)
                subprocess.run(f'psql -U postgres -d {dbName} -c "TRUNCATE TABLE item{i};"', shell=True, check=True)
    else:
        subprocess.run(f'sudo src/sysbench oltp_common --db-driver=pgsql --tables={tableCount} --table-size={recordCount} --threads={threadCount} --pgsql-user=postgres --pgsql-password={password} --pgsql-db={dbName} prepare', shell=True, check=True)
        if int(recordCount) == 1:
            for i in range(1,int(tableCount)+1):
                table = str(i)
                subprocess.run(f'psql -U postgres -d {dbName} -c "DROP INDEX IF EXISTS k_{i};"', shell=True, check=True)
else:
    parser.error("You are running on a database system type that has not been onboarded to Virtual Client. Available options are: MySQL, PostgreSQL")