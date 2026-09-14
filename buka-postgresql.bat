@echo off
title PostgreSQL - laporan_produktivitas
set PGPASSWORD=password
"C:\Program Files\PostgreSQL\18\bin\psql.exe" -h 192.168.179.24 -p 5432 -U postgres -d laporan_produktivitas
pause
