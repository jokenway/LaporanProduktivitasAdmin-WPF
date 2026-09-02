@echo off
title Laporan Produktivitas Admin - Mode Browser
color 0A

echo ========================================================
echo   LAPORAN PRODUKTIVITAS ADMIN (MODE BROWSER)
echo ========================================================
echo.
echo Menyiapkan server lokal...
echo.

:: Build jika dist belum ada
if not exist "dist\index.html" (
    echo [INFO] Menyiapkan file web...
    call npm.cmd run build
)

echo Membuka aplikasi di browser default Anda...
echo (Jika port sedang digunakan, server akan otomatis memilih port baru yang kosong)
echo.
call npm.cmd run preview
pause
