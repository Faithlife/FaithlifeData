#!/bin/bash
echo 'setup.sh'
docker exec faithlifedata_mssql_1 /opt/mssql-tools/bin/sqlcmd -S localhost -U sa -P P@ssw0rd -Q "create database mssqlclient2;"
docker exec faithlifedata_mysql_1 mysql -uroot -ptest -e "create schema mysqlconnector collate utf8mb4_bin;"
docker exec -e PGPASSWORD=test faithlifedata_postgres_1 psql -U root -c "CREATE DATABASE npgsql5;"
echo 'done'
