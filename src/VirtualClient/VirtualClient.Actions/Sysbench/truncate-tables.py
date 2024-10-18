import subprocess
import argparse

# Set up argument parsing
parser = argparse.ArgumentParser()

parser.add_argument('--dbName', type=str, help='Database Name', required=True)
parser.add_argument('--clientIps', type=str, help='Client IP Address (separate multiple with semicolons)', required=False)

args = parser.parse_args()
dbName = args.dbName
clientIps = args.clientIps

                    # Function to truncate all tables in the specified database
def truncate_tables(db_user, host):
    # Get all table names from the database
    output = subprocess.run(f'sudo mysql -u {db_user} -h {host} {dbName} -e "SHOW TABLES;"', shell=True, check=True, capture_output=True, text=True)

    # Extract table names from the output
    tables = [line.strip() for line in output.stdout.strip().split('\n') if line.startswith('sbtest')]

    # Truncate each table
    for table in tables:
        subprocess.run(f'sudo mysql -u {db_user} -h {host} {dbName} -e "TRUNCATE TABLE {table};"', shell=True, check=True)

# Multi-VM: If client IPs are specified, truncate tables for each client VM IP
if clientIps:
    clientIps = clientIps.split(';')
    clientIps = list(filter(None, clientIps))

    for clientIp in clientIps:
        truncate_tables(db_user=dbName, host=clientIp)
else:
    truncate_tables(db_user=dbName, host='localhost')
print(f"Successfully truncated tables in database '{dbName}' for all specified hosts.")