using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using LaporanProduktivitasWPF.Models;

namespace LaporanProduktivitasWPF.Services
{
    public class StoredLogItem
    {
        public string Key { get; set; }
        public int NotaSalah { get; set; }
        public string JamDatang { get; set; }
        public string JamPulang { get; set; }
        public string Keterangan { get; set; }
    }

    public static class StorageService
    {
        private static readonly string AppDataFolder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "LaporanProduktivitasAdmin"
        );

        private static readonly string LogsFilePath = Path.Combine(AppDataFolder, "manual_logs.json");
        private static readonly string MetaFilePath = Path.Combine(AppDataFolder, "saved_meta.json");
        private static readonly string FilesFolder = Path.Combine(AppDataFolder, "files");

        static StorageService()
        {
            try
            {
                if (!Directory.Exists(AppDataFolder)) Directory.CreateDirectory(AppDataFolder);
                if (!Directory.Exists(FilesFolder)) Directory.CreateDirectory(FilesFolder);
            }
            catch { }
        }

        // ─────────────────────────────────────────────────
        // Manual Logs (Nota Salah, Jam Datang, Jam Pulang)
        // ─────────────────────────────────────────────────

        public static Dictionary<string, EvaluasiItem> LoadManualLogs()
        {
            var dict = new Dictionary<string, EvaluasiItem>(StringComparer.OrdinalIgnoreCase);
            try
            {
                if (!File.Exists(LogsFilePath)) return dict;
                string json = File.ReadAllText(LogsFilePath);
                var list = JsonSerializer.Deserialize<List<StoredLogItem>>(json);
                if (list != null)
                {
                    foreach (var item in list)
                    {
                        if (!string.IsNullOrEmpty(item.Key))
                        {
                            dict[item.Key] = new EvaluasiItem
                            {
                                Key = item.Key,
                                NotaSalah = item.NotaSalah,
                                JamDatang = item.JamDatang ?? "",
                                JamPulang = item.JamPulang ?? "",
                                Keterangan = item.Keterangan ?? ""
                            };
                        }
                    }
                }
            }
            catch { }
            return dict;
        }

        public static void SaveManualLogs(IEnumerable<EvaluasiItem> items)
        {
            try
            {
                var list = new List<StoredLogItem>();
                if (items != null)
                {
                    foreach (var it in items)
                    {
                        if (it.NotaSalah > 0 || !string.IsNullOrEmpty(it.JamDatang) ||
                            !string.IsNullOrEmpty(it.JamPulang) || !string.IsNullOrEmpty(it.Keterangan))
                        {
                            list.Add(new StoredLogItem
                            {
                                Key = it.Key,
                                NotaSalah = it.NotaSalah,
                                JamDatang = it.JamDatang,
                                JamPulang = it.JamPulang,
                                Keterangan = it.Keterangan
                            });
                        }
                    }
                }

                var options = new JsonSerializerOptions { WriteIndented = true };
                string json = JsonSerializer.Serialize(list, options);
                File.WriteAllText(LogsFilePath, json);
            }
            catch { }
        }

        // ─────────────────────────────────────────────────
        // Penyimpanan Data per Bulan (copy file xlsx)
        // ─────────────────────────────────────────────────

        /// <summary>
        /// Menyimpan file Excel ke cache AppData/files/ dan mencatat metadata-nya.
        /// Jika bulan sudah ada, data lama ditimpa.
        /// </summary>
        public static void SaveMonthFile(string monthKey, string originalFilePath, string defaultSheet, int rowCount)
        {
            try
            {
                string cachedFileName = monthKey.ToUpperInvariant() + ".xlsx";
                string cachedFilePath = Path.Combine(FilesFolder, cachedFileName);

                // Copy file xlsx ke AppData/files/
                File.Copy(originalFilePath, cachedFilePath, overwrite: true);

                // Update metadata
                var metas = LoadSavedFilesMeta();
                var existing = metas.FirstOrDefault(m => string.Equals(m.Key, monthKey, StringComparison.OrdinalIgnoreCase));
                if (existing != null)
                {
                    existing.FileName = Path.GetFileName(originalFilePath);
                    existing.CachedFileName = cachedFileName;
                    existing.DefaultSheet = defaultSheet;
                    existing.RowCount = rowCount;
                    existing.SavedAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                }
                else
                {
                    metas.Add(new SavedFileMeta
                    {
                        Key = monthKey.ToUpperInvariant(),
                        Label = ToTitleCase(monthKey),
                        MonthName = monthKey.ToUpperInvariant(),
                        Year = DateTime.Now.Year,
                        FileName = Path.GetFileName(originalFilePath),
                        CachedFileName = cachedFileName,
                        DefaultSheet = defaultSheet,
                        RowCount = rowCount,
                        SavedAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
                    });
                }

                SaveSavedFilesMeta(metas);
            }
            catch { }
        }

        /// <summary>
        /// Mengembalikan path file Excel yang di-cache berdasarkan monthKey.
        /// Null jika tidak ditemukan.
        /// </summary>
        public static string GetMonthFilePath(string monthKey)
        {
            try
            {
                var metas = LoadSavedFilesMeta();
                var meta = metas.FirstOrDefault(m => string.Equals(m.Key, monthKey, StringComparison.OrdinalIgnoreCase));
                if (meta == null || string.IsNullOrEmpty(meta.CachedFileName)) return null;

                string path = Path.Combine(FilesFolder, meta.CachedFileName);
                return File.Exists(path) ? path : null;
            }
            catch { return null; }
        }

        /// <summary>
        /// Menghapus data bulan dari cache dan metadata.
        /// </summary>
        public static void DeleteMonthData(string monthKey)
        {
            try
            {
                var metas = LoadSavedFilesMeta();
                var meta = metas.FirstOrDefault(m => string.Equals(m.Key, monthKey, StringComparison.OrdinalIgnoreCase));
                if (meta != null)
                {
                    // Hapus file cache
                    if (!string.IsNullOrEmpty(meta.CachedFileName))
                    {
                        string cachedPath = Path.Combine(FilesFolder, meta.CachedFileName);
                        if (File.Exists(cachedPath)) File.Delete(cachedPath);
                    }

                    metas.Remove(meta);
                    SaveSavedFilesMeta(metas);
                }
            }
            catch { }
        }

        // ─────────────────────────────────────────────────
        // Metadata List
        // ─────────────────────────────────────────────────

        public static List<SavedFileMeta> LoadSavedFilesMeta()
        {
            try
            {
                if (!File.Exists(MetaFilePath)) return new List<SavedFileMeta>();
                string json = File.ReadAllText(MetaFilePath);
                return JsonSerializer.Deserialize<List<SavedFileMeta>>(json) ?? new List<SavedFileMeta>();
            }
            catch
            {
                return new List<SavedFileMeta>();
            }
        }

        public static void SaveSavedFilesMeta(List<SavedFileMeta> metas)
        {
            try
            {
                var options = new JsonSerializerOptions { WriteIndented = true };
                string json = JsonSerializer.Serialize(metas, options);
                File.WriteAllText(MetaFilePath, json);
            }
            catch { }
        }

        // ─────────────────────────────────────────────────
        // Helper
        // ─────────────────────────────────────────────────

        private static string ToTitleCase(string s)
        {
            if (string.IsNullOrEmpty(s)) return s;
            return char.ToUpper(s[0]) + s.Substring(1).ToLowerInvariant();
        }
    }
}
