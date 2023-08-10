#!/bin/bash
set -e

hostName=$1
tableCount=$2
dbName=$3
directory=$4

if [ $# -eq 4 ]; then
    for i in $(seq 1 $tableCount);
    do
        tableName=$dbName$i
        tempName=t$i
        sudo mysql -u $dbName -h $hostName $dbName -e "CREATE TABLE $tempName DATA DIRECTORY = '$directory' AS (SELECT * FROM $tableName);"
        sudo mysql -u $dbName -h $hostName $dbName -e "DROP TABLE $tableName;"
        sudo mysql -u $dbName -h $hostName $dbName -e "RENAME TABLE $tempName TO $tableName;"
    done
elif [ $# -eq 5 ]; then
    let firstHalf=$tableCount/2

    for i in $(seq 1 $tableCount);
    do
        if [ $i -gt $firstHalf ]; then
            directory=$5
        fi
        tableName=$dbName$i
        tempName=t$i
        sudo mysql -u $dbName -h $hostName $dbName -e "CREATE TABLE $tempName DATA DIRECTORY = '$directory' AS (SELECT * FROM $tableName);"
        sudo mysql -u $dbName -h $hostName $dbName -e "DROP TABLE $tableName;"
        sudo mysql -u $dbName -h $hostName $dbName -e "RENAME TABLE $tempName TO $tableName;"
    done
elif [ $# -eq 6 ]; then
    let firstThird=$tableCount/3
    let secondThird=$firstThird+$firstThird

    for i in $(seq 1 $tableCount);
    do
        if [ $i -gt $secondThird ]; then
            directory=$6
        elif [ $i -gt $firstThird ]; then
            directory=$5
        fi
        tableName=$dbName$i
        tempName=t$i
        sudo mysql -u $dbName -h $hostName $dbName -e "CREATE TABLE $tempName DATA DIRECTORY = '$directory' AS (SELECT * FROM $tableName);"
        sudo mysql -u $dbName -h $hostName $dbName -e "DROP TABLE $tableName;"
        sudo mysql -u $dbName -h $hostName $dbName -e "RENAME TABLE $tempName TO $tableName;"
    done
elif [ $# -eq 7 ]; then
    let firstQuarter=$tableCount/4
    let secondQuarter=$firstQuarter+$firstQuarter
    let thirdQuarter=$firstQuarter+$secondQuarter

    for i in $(seq 1 $tableCount);
    do
        if [ $i -gt $thirdQuarter ]; then
            directory=$7
        elif [ $i -gt $secondQuarter ]; then
            directory=$6
        elif [ $i -gt $firstQuarter ]; then
            directory=$5
        fi
        tableName=$dbName$i
        tempName=t$i
        sudo mysql -u $dbName -h $hostName $dbName -e "CREATE TABLE $tempName DATA DIRECTORY = '$directory' AS (SELECT * FROM $tableName);"
        sudo mysql -u $dbName -h $hostName $dbName -e "DROP TABLE $tableName;"
        sudo mysql -u $dbName -h $hostName $dbName -e "RENAME TABLE $tempName TO $tableName;"
    done
elif [ $# -eq 8 ]; then
    let firstFifth=$tableCount/5
    let secondFifth=$firstFifth+$firstFifth
    let thirdFifth=$firstFifth+$secondFifth
    let fourthFifth=$secondFifth+$secondFifth

    for i in $(seq 1 $tableCount);
    do
        if [ $i -gt $fourthFifth ]; then
            directory=$8
        elif [ $i -gt $thirdFifth ]; then
            directory=$7
        elif [ $i -gt $secondFifth ]; then
            directory=$6
        elif [ $i -gt $firstFifth ]; then
            directory=$5
        fi
        tableName=$dbName$i
        tempName=t$i
        sudo mysql -u $dbName -h $hostName $dbName -e "CREATE TABLE $tempName DATA DIRECTORY = '$directory' AS (SELECT * FROM $tableName);"
        sudo mysql -u $dbName -h $hostName $dbName -e "DROP TABLE $tableName;"
        sudo mysql -u $dbName -h $hostName $dbName -e "RENAME TABLE $tempName TO $tableName;"
    done
else
    echo
	echo "Invalid usage. Balanced Scenario supports between 1 and 5 additional data disks."
	echo
	exit 1
fi

