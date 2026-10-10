@echo off
setlocal
title PostgreSQL - Laporan Produktivitas

echo Masukkan detail koneksi PostgreSQL. Password akan diminta aman oleh psql.
set /p "PGHOST=Host [127.0.0.1]: "
if "%PGHOST%"=="" set "PGHOST=127.0.0.1"
set /p "PGDATABASE=Database [laporan_produktivitas]: "
if "%PGDATABASE%"=="" set "PGDATABASE=laporan_produktivitas"
set /p "PGUSER=Username [postgres]: "
if "%PGUSER%"=="" set "PGUSER=postgres"

"C:\Program Files\PostgreSQL\18\bin\psql.exe" -h "%PGHOST%" -p 5432 -U "%PGUSER%" -d "%PGDATABASE%"
pause
