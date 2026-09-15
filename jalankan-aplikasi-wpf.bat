@echo off
title Laporan Produktivitas Admin (WPF .NET 8 Desktop)
color 0B

echo ========================================================
echo   LAPORAN PRODUKTIVITAS ADMIN - NATIVE WPF .NET 8
echo ========================================================
echo.

set DOTNET_ROOT=%USERPROFILE%\.dotnet
set PATH=%USERPROFILE%\.dotnet;%PATH%

if exist "LaporanProduktivitasWPF\bin\Release\standalone\LaporanProduktivitasAdmin.exe" (
    echo Membuka aplikasi Standalone Desktop Native WPF .NET 8...
    start "" "LaporanProduktivitasWPF\bin\Release\standalone\LaporanProduktivitasAdmin.exe"
    exit /b 0
)

if exist "LaporanProduktivitasWPF\bin\Release\net8.0-windows\LaporanProduktivitasAdmin.exe" (
    echo Membuka aplikasi Desktop Native WPF .NET 8...
    start "" "LaporanProduktivitasWPF\bin\Release\net8.0-windows\LaporanProduktivitasAdmin.exe"
    exit /b 0
)

echo [INFO] Mengompilasi aplikasi WPF .NET 8...
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o "LaporanProduktivitasWPF\bin\Release\standalone" "LaporanProduktivitasWPF\LaporanProduktivitasWPF.csproj"
start "" "LaporanProduktivitasWPF\bin\Release\standalone\LaporanProduktivitasAdmin.exe"
