using System;
using System.IO;

namespace LaporanProduktivitasWPF.Services
{
    /// <summary>Writes diagnostic errors locally without exposing them in the UI.</summary>
    internal static class AppLogger
    {
        private static readonly object SyncRoot = new object();

        public static void LogError(string context, Exception exception)
        {
            try
            {
                string folder = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "LaporanProduktivitasAdmin");
                Directory.CreateDirectory(folder);

                lock (SyncRoot)
                {
                    File.AppendAllText(Path.Combine(folder, "error_log.txt"),
                        $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [{context}] {exception}\n\n");
                }
            }
            catch
            {
                // Logging must never interrupt the primary application flow.
            }
        }
    }
}
