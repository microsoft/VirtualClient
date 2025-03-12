import subprocess
import argparse

parser = argparse.ArgumentParser()

parser.add_argument('-d', '--distro', type=str, help='Distribution', required=True)
parser.add_argument('-y', '--databaseSystem', type=str, help='Database System', required=False)
parser.add_argument('-p', '--packagePath', type=str, help='Workload Package Path', required=True)

args = parser.parse_args()
distro = args.distro
databaseSystem = args.databaseSystem
path = args.packagePath

subprocess.run('sudo git clone https://github.com/akopytov/sysbench.git', shell=True, check=True)
subprocess.run(f'sudo mv -v {path}/sysbench/* {path}', shell=True, check=True)

subprocess.run('sudo git clone https://github.com/Percona-Lab/sysbench-tpcc.git', shell=True, check=True)
subprocess.run(f'sudo mv -v {path}/sysbench-tpcc/*.lua {path}/src/lua', shell=True, check=True)

if distro == "Ubuntu" or distro == "Debian":
    subprocess.run('sudo apt-get update -y', shell=True, check=True)
    subprocess.run('sudo apt-get install make automake libtool pkg-config libaio-dev libmysqlclient-dev libssl-dev libpq-dev -y --quiet', shell=True, check=True)
elif distro == "CentOS8" or distro == "RHEL8" or distro == "Mariner":
    subprocess.run('sudo dnf update -y', shell=True, check=True)
    subprocess.run('sudo dnf install make automake libtool pkg-config libaio-devel, mariadb-devel, openssl-devel, postgresql-devel -y --quiet', shell=True, check=True)
elif distro == "CentOS7" or distro == "RHEL7":
    subprocess.run('sudo yum update -y', shell=True, check=True)
    subprocess.run('sudo yum install make automake libtool pkg-config libaio-devel, mariadb-devel, openssl-devel, postgresql-devel -y --quiet', shell=True, check=True)
elif distro == "SUSE":
    subprocess.run('sudo zypper update', shell=True, check=True)
    subprocess.run('sudo zypper --non-interactive install -y make automake libtool pkg-config libaio-dev, libmysqlclient-devel, openssl-devel, postgresql-devel', shell=True, check=True)
else:
    parser.error("You are on a Linux distribution that has not been onboarded to Virtual Client.")

subprocess.run('sudo sed -i "s/CREATE TABLE/CREATE TABLE IF NOT EXISTS/g" src/lua/oltp_common.lua', shell=True, check=True)
subprocess.run('sudo ./autogen.sh', shell=True, check=True)
if databaseSystem is None or databaseSystem == "MySQL":
    subprocess.run('sudo ./configure', shell=True, check=True)
else:
    subprocess.run('sudo ./configure --with-pgsql', shell=True, check=True)
subprocess.run('sudo make -j', shell=True, check=True)
subprocess.run('sudo make install', shell=True, check=True)