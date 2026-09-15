@echo off
title Kompilasi Laporan Produktivitas Admin (WPF .NET 8)
color 0B

echo ========================================================
echo   KOMPILASI PROYEK WPF NATIVE (.NET 8 SDK / C#)
echo ========================================================
echo.

set PATH=%USERPROFILE%\.dotnet;%PATH%

echo [1/2] Mengompilasi proyek WPF dengan .NET 8 SDK...
dotnet build -c Release "LaporanProduktivitasWPF.csproj"

if %ERRORLEVEL% neq 0 (
    echo.
    echo [ERROR] Kompilasi gagal! Periksa pesan error di atas.
    pause
    exit /b %ERRORLEVEL%
)

echo.
echo [2/2] Kompilasi sukses!
echo File executable: bin\Release\net8.0-windows\LaporanProduktivitasAdmin.exe
echo.
echo Menjalankan aplikasi...
start "" "bin\Release\net8.0-windows\LaporanProduktivitasAdmin.exe"
