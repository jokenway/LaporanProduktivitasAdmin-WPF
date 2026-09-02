@echo off
title Laporan Produktivitas Admin - Desktop App
color 0B

echo ========================================================
echo   LAPORAN PRODUKTIVITAS ADMIN - PIVOT EXCEL STUDIO
echo ========================================================
echo.
echo [1/2] Memeriksa file distribusi...

:: Build jika dist belum ada
if not exist "dist\index.html" (
    echo [INFO] Melakukan build pertama kali...
    call npm.cmd run build
)

echo [2/2] Membuka jendela aplikasi desktop...
echo.
call npm.cmd start

if %ERRORLEVEL% neq 0 (
    echo.
    echo [INFO] Membuka di browser lokal sebagai cadangan...
    call npm.cmd run preview
)

pause
