#!/bin/bash
set -e

if [ $# -ge 1 ]; then
    for path in "$@"; do
        sudo chown postgres:postgres "$path"
        sudo -u postgres psql -c "DROP TABLESPACE IF EXISTS \"$path\";"
        sudo -u postgres psql -c "CREATE TABLESPACE \"$path\" LOCATION '$path';"
    done

    sudo -u postgres psql -c "ALTER DATABASE tpcc SET TABLESPACE \"$1\";"
fi

if [ $# -eq 2 ]; then
    sudo -u postgres psql tpcc -c "ALTER TABLE stock SET TABLESPACE \"$2\";"
    sudo -u postgres psql tpcc-c "ALTER TABLE customer SET TABLESPACE \"$2\";"
elif [ $# -eq 3 ]; then
    sudo -u postgres psql tpcc -c "ALTER TABLE stock SET TABLESPACE \"$2\";"
    sudo -u postgres psql tpcc -c "ALTER TABLE order_line SET TABLESPACE \"$3\";"
elif [ $# -eq 4 ]; then
    sudo -u postgres psql tpcc -c "ALTER TABLE stock SET TABLESPACE \"$2\";"
    sudo -u postgres psql tpcc -c "ALTER TABLE order_line SET TABLESPACE \"$3\";"
    sudo -u postgres psql tpcc -c "ALTER TABLE customer SET TABLESPACE \"$4\";"
fi