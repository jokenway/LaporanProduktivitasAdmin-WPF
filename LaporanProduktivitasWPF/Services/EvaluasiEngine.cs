using System;
using System.Collections.Generic;
using System.Linq;
using LaporanProduktivitasWPF.Models;

namespace LaporanProduktivitasWPF.Services
{
    public class EvaluasiSummary
    {
        public int TotalNota { get; set; }
        public int TotalSalah { get; set; }
        public double OverallAkurasi { get; set; }
        public int TotalJamKerjaMinutes { get; set; }
        public string TotalJamKerjaText { get; set; }
        public int UserCount { get; set; }
        public int DateCount { get; set; }
        public int RecordCount { get; set; }
    }

    public class EvaluasiResult
    {
        public List<EvaluasiItem> Records { get; set; }
        public List<string> AvailableDates { get; set; }
        public List<string> AvailableUsers { get; set; }
        public EvaluasiSummary Summary { get; set; }
        public List<AdminInvoiceMetric> UserMetrics { get; set; }
        public int GroupTotalNota { get; set; }

        public EvaluasiResult()
        {
            Records = new List<EvaluasiItem>();
            AvailableDates = new List<string>();
            AvailableUsers = new List<string>();
            Summary = new EvaluasiSummary();
            UserMetrics = new List<AdminInvoiceMetric>();
        }
    }

    public static class EvaluasiEngine
    {
        public static readonly HashSet<string> ADMIN_INVOICE_USERS = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "AKBAR", "DIDIN", "JOE", "RONI", "NOVIANI"
        };

        public static EvaluasiResult BuildDailyEvaluation(
            IEnumerable<RawRow> rows,
            Dictionary<string, EvaluasiItem> manualLogs,
            string selectedDate = "ALL",
            string selectedUser = "ALL",
            string searchQuery = "",
            string userGroup = "ALL")
        {
            var result = new EvaluasiResult();
            if (rows == null) return result;

            var mapData = new Dictionary<string, Tuple<string, string, HashSet<string>>>(StringComparer.OrdinalIgnoreCase);
            var dateSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var userSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var r in rows)
            {
                string u = r.Get("USID");
                if (string.IsNullOrEmpty(u)) u = r.Get("User");
                if (string.IsNullOrEmpty(u)) u = r.Get("Nama Admin");
                if (string.IsNullOrEmpty(u)) u = r.Get("NMPG");

                string d = r.Get("TGBON");
                if (string.IsNullOrEmpty(d)) d = r.Get("Tanggal");

                string noinv = r.Get("NOINV");
                if (string.IsNullOrEmpty(noinv)) noinv = r.Get("No Invoice");

                u = (u ?? "").Trim();
                d = (d ?? "").Trim();
                noinv = (noinv ?? "").Trim();

                if (string.IsNullOrEmpty(u) || string.IsNullOrEmpty(d)) continue;

                dateSet.Add(d);
                userSet.Add(u);

                string key = u + "___" + d;
                Tuple<string, string, HashSet<string>> item;
                if (!mapData.TryGetValue(key, out item))
                {
                    item = new Tuple<string, string, HashSet<string>>(u, d, new HashSet<string>(StringComparer.OrdinalIgnoreCase));
                    mapData[key] = item;
                }

                if (!string.IsNullOrEmpty(noinv))
                {
                    item.Item3.Add(noinv);
                }
            }

            result.AvailableDates = dateSet.OrderBy(x => x).ToList();
            result.AvailableUsers = userSet.OrderBy(x => x).ToList();

            var allRecords = new List<EvaluasiItem>();

            foreach (var kv in mapData)
            {
                string user = kv.Value.Item1;
                string date = kv.Value.Item2;
                int totalNota = kv.Value.Item3.Count;

                EvaluasiItem savedLog = null;
                if (manualLogs != null)
                {
                    manualLogs.TryGetValue(kv.Key, out savedLog);
                }

                var item = new EvaluasiItem
                {
                    Key = kv.Key,
                    User = user,
                    Tanggal = date,
                    TotalNota = totalNota,
                    NotaSalah = savedLog != null ? savedLog.NotaSalah : 0,
                    JamDatang = savedLog != null ? savedLog.JamDatang : "",
                    JamPulang = savedLog != null ? savedLog.JamPulang : "",
                    Keterangan = savedLog != null ? savedLog.Keterangan : ""
                };

                item.UpdateAkurasi();
                item.UpdateDurasi();
                allRecords.Add(item);
            }

            // Group records for Admin Invoice statistics
            bool isInvoiceFilter = string.Equals(userGroup, "ADMIN_INVOICE", StringComparison.OrdinalIgnoreCase);
            var groupRecords = isInvoiceFilter
                ? allRecords.Where(r => ADMIN_INVOICE_USERS.Contains(r.User)).ToList()
                : allRecords;

            result.GroupTotalNota = groupRecords.Sum(r => r.TotalNota);
            int groupWorkDays = groupRecords.Where(r => r.TotalNota > 0).Select(r => r.Tanggal).Distinct(StringComparer.OrdinalIgnoreCase).Count();

            var distinctGroupUsers = groupRecords.Select(r => r.User).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(u => u).ToList();
            foreach (var u in distinctGroupUsers)
            {
                var uRecs = groupRecords.Where(r => string.Equals(r.User, u, StringComparison.OrdinalIgnoreCase)).ToList();
                int totalNota = uRecs.Sum(r => r.TotalNota);
                int totalSalah = uRecs.Sum(r => r.NotaSalah);

                double salahPct = result.GroupTotalNota > 0 ? ((double)totalSalah / result.GroupTotalNota) * 100.0 : 0.0;
                double avgPerDay = groupWorkDays > 0 ? (double)totalNota / groupWorkDays : 0.0;
                double contPct = result.GroupTotalNota > 0 ? ((double)totalNota / result.GroupTotalNota) * 100.0 : 0.0;

                result.UserMetrics.Add(new AdminInvoiceMetric
                {
                    User = u,
                    TotalNota = totalNota,
                    TotalSalah = totalSalah,
                    SalahPercentage = Math.Round(salahPct, 1),
                    AverageNotaPerDay = Math.Round(avgPerDay, 1),
                    ContributionPercentage = Math.Round(contPct, 1)
                });
            }

            // Apply filters
            string q = (searchQuery ?? "").Trim().ToLowerInvariant();
            bool isInvoiceGroup = string.Equals(userGroup, "ADMIN_INVOICE", StringComparison.OrdinalIgnoreCase);

            var filtered = allRecords.Where(rec =>
            {
                // When ADMIN_INVOICE group: always restrict to those 5 users
                if (isInvoiceGroup && !ADMIN_INVOICE_USERS.Contains(rec.User)) return false;

                if (!string.IsNullOrEmpty(selectedDate) && selectedDate != "ALL" && !string.Equals(rec.Tanggal, selectedDate, StringComparison.OrdinalIgnoreCase))
                    return false;

                if (!string.IsNullOrEmpty(selectedUser) && selectedUser != "ALL" &&
                    !string.Equals(selectedUser, "ADMIN_INVOICE", StringComparison.OrdinalIgnoreCase))
                {
                    if (!string.Equals(rec.User, selectedUser, StringComparison.OrdinalIgnoreCase))
                        return false;
                }

                if (!string.IsNullOrEmpty(q))
                {
                    bool mU = rec.User.ToLowerInvariant().Contains(q);
                    bool mD = rec.Tanggal.ToLowerInvariant().Contains(q);
                    bool mK = rec.Keterangan.ToLowerInvariant().Contains(q);
                    if (!mU && !mD && !mK) return false;
                }

                return true;
            })
            .OrderBy(r => r.Tanggal)
            .ThenBy(r => r.User)
            .ToList();

            for (int i = 0; i < filtered.Count; i++)
            {
                filtered[i].No = i + 1;
            }

            result.Records = filtered;

            // Summary
            int totNota = 0;
            int totSalah = 0;
            int totJamMinutes = 0;
            var userCountSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var dateCountSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var rec in filtered)
            {
                totNota += rec.TotalNota;
                totSalah += rec.NotaSalah;
                totJamMinutes += rec.DurasiMinutes;
                userCountSet.Add(rec.User);
                dateCountSet.Add(rec.Tanggal);
            }

            double overallAk = totNota > 0 ? Math.Max(0.0, 1.0 - ((double)totSalah / totNota)) * 100.0 : 100.0;
            int hours = totJamMinutes / 60;
            int mins = totJamMinutes % 60;
            string totalJamText = mins > 0 ? string.Format("{0} jam {1:D2} mnt", hours, mins) : string.Format("{0} jam", hours);

            result.Summary = new EvaluasiSummary
            {
                TotalNota = totNota,
                TotalSalah = totSalah,
                OverallAkurasi = Math.Round(overallAk, 1),
                TotalJamKerjaMinutes = totJamMinutes,
                TotalJamKerjaText = totalJamText,
                UserCount = userCountSet.Count,
                DateCount = dateCountSet.Count,
                RecordCount = filtered.Count
            };

            return result;
        }
    }
}
