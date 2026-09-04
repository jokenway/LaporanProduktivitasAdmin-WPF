using System;
using System.IO;
using System.Windows;
using System.Windows.Threading;

namespace LaporanProduktivitasWPF
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            DispatcherUnhandledException += App_DispatcherUnhandledException;
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
        }

        private void App_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            LogError("Dispatcher Exception", e.Exception);
            MessageBox.Show(
                "Terjadi kesalahan pada aplikasi:\n" + e.Exception.Message + "\n\nDetail tersimpan di log.",
                "Error Aplikasi",
                MessageBoxButton.OK,
                MessageBoxImage.Error
            );
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
                    "LaporanProduktivitasAdmin"
                );
                if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);
                string logPath = Path.Combine(folder, "error_log.txt");
                File.AppendAllText(logPath, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [{tag}] {ex}\n\n");
            }
            catch { }
        }
    }
}
