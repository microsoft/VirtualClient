import subprocess
import argparse
import os

# Define arguments
parser = argparse.ArgumentParser()

parser.add_argument('-n', '--dbName', type=str, help='Database Name', required=True)
parser.add_argument('-y', '--databaseSystem', type=str, help='Database Type (MySQL/PostgreSQL)', required=False)
parser.add_argument('-b', '--benchmark', type=str, help="Benchmark Name", required=True)
parser.add_argument('-t', '--tableCount', type=str, help='Number of Tables', required=True)
parser.add_argument('-r', '--recordCount', type=str, help='Number of Records', required=False)
parser.add_argument('-u', '--warehouses', type=str, help='Warehouse Count', required=False)
parser.add_argument('-e', '--threadCount', type=str, help="Number of Threads", required=True)
parser.add_argument('--password', type=str, help="PostgreSQL Password", required=False)
parser.add_argument('--host', type=str, help="Database Server Host IP Address", required=False)

# Parse arguments
args = parser.parse_args()
dbName = args.dbName
databaseSystem = args.databaseSystem
benchmark = args.benchmark
warehouses = args.warehouses
tableCount = args.tableCount
recordCount = args.recordCount
threadCount = args.threadCount
password = args.password
host = args.host  # Host can be None if not provided

# Function to add host if provided
def add_host_to_command(command_base, host):
    if host:
        return f"{command_base} --mysql-host={host}" if "mysql" in command_base else f"{command_base} --pgsql-host={host}"
    return command_base

# MySQL handling
if databaseSystem == "MySQL":
    if benchmark == "TPCC":
        command_base = f'sudo src/sysbench tpcc --tables={tableCount} --scale={warehouses} --threads={threadCount} --mysql-db={dbName} --use_fk=0 prepare'
        command = add_host_to_command(command_base, host)
        subprocess.run(command, shell=True, check=True)
        
        if int(warehouses) == 1:
            for i in range(1, int(tableCount) + 1):
                table = str(i)
                # drop indexes
                for index in ["idx_customer", "idx_orders", "fkey_stock_2", "fkey_order_line_2", "fkey_history_1", "fkey_history_2"]:
                    drop_index_command = f'mysql -u {dbName} -p -e "DROP INDEX {index}{i} ON {index.split("_")[1]}{i};"'
                    drop_index_command = add_host_to_command(drop_index_command, host)
                    subprocess.run(drop_index_command, shell=True, check=True)

                # truncate tables
                for table in ["warehouse", "district", "customer", "history", "orders", "new_orders", "order_line", "stock", "item"]:
                    truncate_command = f'mysql -u {dbName} -p -e "TRUNCATE TABLE {table}{i};"'
                    truncate_command = add_host_to_command(truncate_command, host)
                    subprocess.run(truncate_command, shell=True, check=True)
    else:
        command_base = f'sudo src/sysbench oltp_common --tables={tableCount} --table-size={recordCount} --threads={threadCount} --mysql-db={dbName} prepare'
        command = add_host_to_command(command_base, host)
        subprocess.run(command, shell=True, check=True)
        
        if int(recordCount) == 1:
            for i in range(1, int(tableCount) + 1):
                drop_index_command = f'mysql -u {dbName} -p -e "DROP INDEX k_{i} ON sbtest{i};"'
                drop_index_command = add_host_to_command(drop_index_command, host)
                subprocess.run(drop_index_command, shell=True, check=True)

# PostgreSQL handling
elif databaseSystem == "PostgreSQL":
    os.environ['PGPASSWORD'] = password
    if benchmark == "TPCC":
        command_base = f'sudo src/sysbench tpcc --db-driver=pgsql --tables={tableCount} --scale={warehouses} --threads={threadCount} --pgsql-user=postgres --pgsql-password={password} --pgsql-db={dbName} --use_fk=0 prepare'
        command = add_host_to_command(command_base, host)
        subprocess.run(command, shell=True, check=True)
        
        if int(warehouses) == 1:
            for i in range(1, int(tableCount) + 1):
                table = str(i)
                # drop indexes
                for index in ["idx_customer", "idx_orders", "fkey_stock_2", "fkey_order_line_2", "fkey_history_1", "fkey_history_2"]:
                    drop_index_command = f'psql -U postgres -d {dbName} -c "DROP INDEX IF EXISTS {index}{i};"'
                    drop_index_command = add_host_to_command(drop_index_command, host)
                    subprocess.run(drop_index_command, shell=True, check=True)

                # truncate tables
                for table in ["warehouse", "district", "customer", "history", "orders", "new_orders", "order_line", "stock", "item"]:
                    truncate_command = f'psql -U postgres -d {dbName} -c "TRUNCATE TABLE {table}{i};"'
                    truncate_command = add_host_to_command(truncate_command, host)
                    subprocess.run(truncate_command, shell=True, check=True)
    else:
        command_base = f'sudo src/sysbench oltp_common --db-driver=pgsql --tables={tableCount} --table-size={recordCount} --threads={threadCount} --pgsql-user=postgres --pgsql-password={password} --pgsql-db={dbName} prepare'
        command = add_host_to_command(command_base, host)
        subprocess.run(command, shell=True, check=True)
        
        if int(recordCount) == 1:
            for i in range(1, int(tableCount) + 1):
                drop_index_command = f'psql -U postgres -d {dbName} -c "DROP INDEX IF EXISTS k_{i};"'
                drop_index_command = add_host_to_command(drop_index_command, host)
                subprocess.run(drop_index_command, shell=True, check=True)

else:
    parser.error("You are running on a database system type that has not been onboarded to Virtual Client. Available options are: MySQL, PostgreSQL")
