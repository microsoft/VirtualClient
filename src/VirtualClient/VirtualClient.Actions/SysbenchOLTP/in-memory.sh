#!/bin/bash
set -e

# Stop the MySQL service
systemctl stop mysql.service

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

sed -i "s|.*key_buffer_size.*|key_buffer_size = ${buffer_size}M|" /etc/mysql/mysql.conf.d/mysqld.cnf

# Restart the MySQL service
systemctl start mysql.service