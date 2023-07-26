#!/bin/bash
set -e

directories=""

systemctl stop mysql.service

if [ $# -ge 1 ]; then
    for path in "$@"; do
        sudo chown mysql:mysql "$path"
        sudo bash -c "echo '$path/ r,' >> /etc/apparmor.d/local/usr.sbin.mysqld"
        sudo bash -c "echo '$path/** rwk,' >> /etc/apparmor.d/local/usr.sbin.mysqld"
        directories+="$path;"
    done

    sudo sed -i "s|.*[mysql].*|[mysqld]|" /etc/mysql/conf.d/mysql.cnf
    sudo bash -c "echo 'innodb_directories=\"$directories\"' >> /etc/mysql/conf.d/mysql.cnf"
fi

sudo service apparmor reload
systemctl start mysql.service