#!/bin/bash
set -e

sysbenchPath=$1
dbName=$2
tableCount=$3
recordCount=$4
threadCount=$5
directory=$6

sudo $sysbenchPath/src/sysbench oltp_common --tables=$tableCount --table-size=1 --mysql-db=$dbName prepare

if [ $# -eq 6 ]; then
    for i in $(seq 1 $tableCount);
    do
        tableName=$dbName$i
        tempName=t$i
        sudo mysql -u $dbName $dbName -e "CREATE TABLE $tempName DATA DIRECTORY = '$directory' AS (SELECT * FROM $tableName);"
        sudo mysql -u $dbName $dbName -e "DROP TABLE $tableName;"
        sudo mysql -u $dbName $dbName -e "RENAME TABLE $tempName TO $tableName;"
    done
elif [ $# -eq 7 ]; then
    let firstHalf=$tableCount/2

    for i in $(seq 1 $tableCount);
    do
        if [ $i -gt $firstHalf ]; then
            directory=$7
        fi
        tableName=$dbName$i
        tempName=t$i
        sudo mysql -u $dbName $dbName -e "CREATE TABLE $tempName DATA DIRECTORY = '$directory' AS (SELECT * FROM $tableName);"
        sudo mysql -u $dbName $dbName -e "DROP TABLE $tableName;"
        sudo mysql -u $dbName $dbName -e "RENAME TABLE $tempName TO $tableName;"
    done
elif [ $# -eq 8 ]; then
    let firstThird=$tableCount/3
    let secondThird=$firstThird+$firstThird

    for i in $(seq 1 $tableCount);
    do
        if [ $i -gt $secondThird ]; then
            directory=$8
        elif [ $i -gt $firstThird ]; then
            directory=$7
        fi
        tableName=$dbName$i
        tempName=t$i
        sudo mysql -u $dbName $dbName -e "CREATE TABLE $tempName DATA DIRECTORY = '$directory' AS (SELECT * FROM $tableName);"
        sudo mysql -u $dbName $dbName -e "DROP TABLE $tableName;"
        sudo mysql -u $dbName $dbName -e "RENAME TABLE $tempName TO $tableName;"
    done
elif [ $# -eq 9 ]; then
    let firstQuarter=$tableCount/4
    let secondQuarter=$firstQuarter+$firstQuarter
    let thirdQuarter=$firstQuarter+$secondQuarter

    for i in $(seq 1 $tableCount);
    do
        if [ $i -gt $thirdQuarter ]; then
            directory=$9
        elif [ $i -gt $secondQuarter ]; then
            directory=$8
        elif [ $i -gt $firstQuarter ]; then
            directory=$7
        fi
        tableName=$dbName$i
        tempName=t$i
        sudo mysql -u $dbName $dbName -e "CREATE TABLE $tempName DATA DIRECTORY = '$directory' AS (SELECT * FROM $tableName);"
        sudo mysql -u $dbName $dbName -e "DROP TABLE $tableName;"
        sudo mysql -u $dbName $dbName -e "RENAME TABLE $tempName TO $tableName;"
    done
elif [ $# -eq 10 ]; then
    let firstFifth=$tableCount/5
    let secondFifth=$firstFifth+$firstFifth
    let thirdFifth=$firstFifth+$secondFifth
    let fourthFifth=$secondFifth+$secondFifth

    for i in $(seq 1 $tableCount);
    do
        if [ $i -gt $fourthFifth ]; then
            directory=$10
        elif [ $i -gt $thirdFifth ]; then
            directory=$9
        elif [ $i -gt $secondFifth ]; then
            directory=$8
        elif [ $i -gt $firstFifth ]; then
            directory=$7
        fi
        tableName=$dbName$i
        tempName=t$i
        sudo mysql -u $dbName $dbName -e "CREATE TABLE $tempName DATA DIRECTORY = '$directory' AS (SELECT * FROM $tableName);"
        sudo mysql -u $dbName $dbName -e "DROP TABLE $tableName;"
        sudo mysql -u $dbName $dbName -e "RENAME TABLE $tempName TO $tableName;"
    done
else
    echo
	echo "Invalid usage. VC Sysbench supports between 1 and 5 additional data disks."
	echo
	exit 1
fi

sudo $sysbenchPath/src/sysbench oltp_common --tables=$tableCount --table-size=$recordCount --mysql-db=$dbName --threads=$threadCount prepare