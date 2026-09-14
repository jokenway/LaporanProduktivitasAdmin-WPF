using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using LaporanProduktivitasWPF.Models;
using LaporanProduktivitasWPF.Services;
using LaporanProduktivitasWPF.Views;

namespace LaporanProduktivitasWPF
{
    public partial class App : Application
    {
        protected override async void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            DispatcherUnhandledException += App_DispatcherUnhandledException;
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;

            // 1. Baca konfigurasi koneksi (hardcoded default)
            var config = LoadOrCreateConfig();

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
                    "\n\nPastikan server PostgreSQL di 192.168.179.24 dapat diakses.",
                    "Koneksi Database Gagal",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                Shutdown();
                return;
            }

            // 3. Tampilkan Login window
            var login = new LoginWindow();
            bool? result = login.ShowDialog();

            if (result != true || login.LoggedInUser == null)
            {
                Shutdown();
                return;
            }

            // 4. Buka Main window dengan user yang sudah login
            var main = new MainWindow(login.LoggedInUser);
            main.Show();
        }

        private AppConfig LoadOrCreateConfig()
        {
            // Config default — bisa diubah di file config.json
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
                    return JsonSerializer.Deserialize<AppConfig>(json) ?? new AppConfig();
                }
                catch { }
            }

            // Simpan config default
            var cfg = new AppConfig();
            File.WriteAllText(configPath, JsonSerializer.Serialize(cfg,
                new JsonSerializerOptions { WriteIndented = true }));
            return cfg;
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
            try
            {
                string folder = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "LaporanProduktivitasAdmin");
                if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);
                string logPath = Path.Combine(folder, "error_log.txt");
                File.AppendAllText(logPath, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [{tag}] {ex}\n\n");
            }
            catch { }
        }
    }
}
