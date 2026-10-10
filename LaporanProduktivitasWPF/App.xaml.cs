using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using LaporanProduktivitasWPF.Models;
using LaporanProduktivitasWPF.Services;

namespace LaporanProduktivitasWPF
{
    public partial class App : Application
    {
        protected override async void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            DispatcherUnhandledException += App_DispatcherUnhandledException;
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;

            // 1. Baca konfigurasi koneksi.
            var config = LoadOrCreateConfig(out string configPath);

            if (!config.IsComplete)
            {
                MessageBox.Show(
                    "Konfigurasi database belum lengkap. Isi Host, Database, PgUser, dan PgPassword di config.json, lalu jalankan ulang aplikasi.\n\n" +
                    "Lokasi config: " + configPath,
                    "Konfigurasi Database", MessageBoxButton.OK, MessageBoxImage.Warning);
                Shutdown();
                return;
            }

            // 2. Inisialisasi koneksi + buat tabel + seed users
            DatabaseService.Configure(config);
            try
            {
                await DatabaseService.InitAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Gagal terhubung ke database:\n" + ex.Message +
                    "\n\nPeriksa nilai koneksi pada config.json.",
                    "Koneksi Database Gagal",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                Shutdown();
                return;
            }

            // MainWindow menjadi satu-satunya window dan menampilkan login sebagai halaman penuh.
            ShutdownMode = ShutdownMode.OnMainWindowClose;
            var main = new MainWindow();
            MainWindow = main;
            main.Show();
        }

        private AppConfig LoadOrCreateConfig(out string activeConfigPath)
        {
            // 1. Cek dulu apakah ada config.json di folder aplikasi (sebelah .exe)
            string exeFolderConfig = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config.json");
            if (File.Exists(exeFolderConfig))
            {
                try
                {
                    string json = File.ReadAllText(exeFolderConfig);
                    var cfg = JsonSerializer.Deserialize<AppConfig>(json);
                    if (cfg != null)
                    {
                        activeConfigPath = exeFolderConfig;
                        return cfg;
                    }
                }
                catch (Exception ex) { AppLogger.LogError("Membaca konfigurasi", ex); }
            }

            // 2. Jika tidak ada di folder .exe, cek di %AppData%
            string folder = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "LaporanProduktivitasAdmin");
            if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);

            string configPath = Path.Combine(folder, "config.json");
            if (File.Exists(configPath))
            {
                try
                {
                    string json = File.ReadAllText(configPath);
                    var cfg = JsonSerializer.Deserialize<AppConfig>(json) ?? new AppConfig();
                    activeConfigPath = configPath;
                    return cfg;
                }
                catch (Exception ex) { AppLogger.LogError("Membaca konfigurasi", ex); }
            }

            // 3. Buat template tanpa kredensial. File ini tidak boleh di-commit.
            var defaultCfg = new AppConfig();
            File.WriteAllText(configPath, JsonSerializer.Serialize(defaultCfg,
                new JsonSerializerOptions { WriteIndented = true }));
            activeConfigPath = configPath;
            return defaultCfg;
        }

        private void App_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            LogError("Dispatcher Exception", e.Exception);
            MessageBox.Show(
                "Terjadi kesalahan pada aplikasi:\n" + e.Exception.Message,
                "Error Aplikasi", MessageBoxButton.OK, MessageBoxImage.Error);
            e.Handled = true;
        }

        private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            var ex = e.ExceptionObject as Exception;
            LogError("Domain Exception", ex);
        }

        private void LogError(string tag, Exception ex)
        {
            AppLogger.LogError(tag, ex);
        }
    }
}
