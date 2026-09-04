using System;
using System.Collections.Generic;
using System.IO;
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

        static StorageService()
        {
            try
            {
                if (!Directory.Exists(AppDataFolder))
                {
                    Directory.CreateDirectory(AppDataFolder);
                }
            }
            catch { }
        }

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
    }
}
