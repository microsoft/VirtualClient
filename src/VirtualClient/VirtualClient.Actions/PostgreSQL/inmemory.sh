#!/bin/bash
set -e

# Stop the PostgreSQL services (e.g. postgresql@14-main.service)
systemctl stop postgresql

buffer_size=$1

if [ -z "$buffer_size" ]; then
	echo
	echo "Invalid usage. RAM size required on command line."
	echo
	echo "Usage:"
	echo "inmemory.sh {buffer_size}"
	echo
	exit 1
fi

sed -i "s|.*shared_buffers.*|shared_buffers = ${buffer_size}MB|" /etc/postgresql/14/main/postgresql.conf
sed -i "s|.*max_wal_size.*|max_wal_size = ${buffer_size}MB|" /etc/postgresql/14/main/postgresql.conf

# Restart the PostgreSQL services (e.g. postgresql@14-main.service)
systemctl start postgresql