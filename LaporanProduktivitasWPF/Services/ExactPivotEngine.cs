using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using LaporanProduktivitasWPF.Models;

namespace LaporanProduktivitasWPF.Services
{
    public class ExactPivotResult
    {
        public List<ExactPivotItem> Items { get; set; }
        public List<string> AvailableDates { get; set; }
        public List<string> AvailableUsers { get; set; }
        public List<string> AvailableJenisB { get; set; }
        public int TotalNota { get; set; }
        public int TotalCountJMDOS { get; set; }
        public double TotalSumNetto { get; set; }
        public Dictionary<string, int> UserStats { get; set; }
        public Dictionary<string, int> DateStats { get; set; }
        public int GrandTotalNotaKeseluruhan { get; set; }
        public double GrandTotalNettoKeseluruhan { get; set; }

        public ExactPivotResult()
        {
            Items = new List<ExactPivotItem>();
            AvailableDates = new List<string>();
            AvailableUsers = new List<string>();
            AvailableJenisB = new List<string>();
            UserStats = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            DateStats = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        }
    }

    public static class ExactPivotEngine
    {
        public static ExactPivotResult BuildExactPivot(
            IEnumerable<RawRow> rows,
            string selectedDate = "ALL",
            string selectedUser = "ALL",
            string selectedJenisB = "ALL",
            string searchQuery = "")
        {
            var result = new ExactPivotResult();
            if (rows == null) return result;

            var dateMap = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);
            var userMap = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);
            var jenisBSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var allInvoicesSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            double grandTotalNettoAll = 0.0;

            var rowList = rows.ToList();

            // Pre-scan for all options and grand totals
            foreach (var r in rowList)
            {
                string d = r.Get("TGBON");
                if (string.IsNullOrEmpty(d)) d = r.Get("Tanggal");
                string u = r.Get("USID");
                if (string.IsNullOrEmpty(u)) u = r.Get("User");
                string jb = r.Get("JENIS_B");
                if (string.IsNullOrEmpty(jb)) jb = r.Get("JENIS");
                string noinv = r.Get("NOINV");
                if (string.IsNullOrEmpty(noinv)) noinv = r.Get("No Invoice");

                double netto = ParseDouble(r.Get("NETTO"));

                if (!string.IsNullOrEmpty(d))
                {
                    if (!dateMap.ContainsKey(d)) dateMap[d] = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                    if (!string.IsNullOrEmpty(noinv)) dateMap[d].Add(noinv);
                }

                if (!string.IsNullOrEmpty(u))
                {
                    if (!userMap.ContainsKey(u)) userMap[u] = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                    if (!string.IsNullOrEmpty(noinv)) userMap[u].Add(noinv);
                }

                if (!string.IsNullOrEmpty(jb)) jenisBSet.Add(jb);
                if (!string.IsNullOrEmpty(noinv)) allInvoicesSet.Add(noinv);
                grandTotalNettoAll += netto;
            }

            // Sort dates
            result.AvailableDates = dateMap.Keys.OrderBy(d => d).ToList();
            // Sort users by invoice count desc
            result.AvailableUsers = userMap.OrderByDescending(kv => kv.Value.Count).Select(kv => kv.Key).ToList();
            result.AvailableJenisB = jenisBSet.OrderBy(j => j).ToList();
            result.GrandTotalNotaKeseluruhan = allInvoicesSet.Count;
            result.GrandTotalNettoKeseluruhan = grandTotalNettoAll;

            foreach (var kv in userMap) result.UserStats[kv.Key] = kv.Value.Count;
            foreach (var kv in dateMap) result.DateStats[kv.Key] = kv.Value.Count;

            // Filter rows
            string q = (searchQuery ?? "").Trim().ToLowerInvariant();

            var filtered = rowList.Where(r =>
            {
                string d = r.Get("TGBON");
                if (string.IsNullOrEmpty(d)) d = r.Get("Tanggal");
                string u = r.Get("USID");
                if (string.IsNullOrEmpty(u)) u = r.Get("User");
                string jb = r.Get("JENIS_B");
                if (string.IsNullOrEmpty(jb)) jb = r.Get("JENIS");

                if (!string.IsNullOrEmpty(selectedDate) && selectedDate != "ALL" && !string.Equals(d, selectedDate, StringComparison.OrdinalIgnoreCase))
                    return false;

                if (!string.IsNullOrEmpty(selectedUser) && selectedUser != "ALL" && !string.Equals(u, selectedUser, StringComparison.OrdinalIgnoreCase))
                    return false;

                if (!string.IsNullOrEmpty(selectedJenisB) && selectedJenisB != "ALL" && !string.Equals(jb, selectedJenisB, StringComparison.OrdinalIgnoreCase))
                    return false;

                if (!string.IsNullOrEmpty(q))
                {
                    string nmpg = r.Get("NMPG").ToLowerInvariant();
                    string noinv = r.Get("NOINV").ToLowerInvariant();
                    string kdprc = r.Get("KDPRC").ToLowerInvariant();
                    string usid = r.Get("USID").ToLowerInvariant();
                    if (!nmpg.Contains(q) && !noinv.Contains(q) && !kdprc.Contains(q) && !usid.Contains(q))
                        return false;
                }

                return true;
            });

            // Group by (NMPG, NOINV, KDPRC, USID)
            var groupMap = new Dictionary<string, ExactPivotItem>(StringComparer.OrdinalIgnoreCase);

            foreach (var r in filtered)
            {
                string nmpg = r.Get("NMPG");
                if (string.IsNullOrEmpty(nmpg)) nmpg = "(Tanpa Sales)";
                string noinv = r.Get("NOINV");
                if (string.IsNullOrEmpty(noinv)) noinv = "(Tanpa NoInv)";
                string kdprc = r.Get("KDPRC");
                if (string.IsNullOrEmpty(kdprc)) kdprc = "-";
                string usid = r.Get("USID");
                if (string.IsNullOrEmpty(usid)) usid = "-";
                string tgbon = r.Get("TGBON");
                string jb = r.Get("JENIS_B");

                string key = nmpg + "|||" + noinv + "|||" + kdprc + "|||" + usid;

                ExactPivotItem item;
                if (!groupMap.TryGetValue(key, out item))
                {
                    item = new ExactPivotItem
                    {
                        NMPG = nmpg,
                        NOINV = noinv,
                        KDPRC = kdprc,
                        USID = usid,
                        TGBON = tgbon,
                        JENIS_B = jb,
                        CountJMDOS = 0,
                        SumNetto = 0.0
                    };
                    groupMap[key] = item;
                }

                item.CountJMDOS += 1;
                double nettoVal = ParseDouble(r.Get("NETTO"));
                item.SumNetto += nettoVal;

                result.TotalCountJMDOS += 1;
                result.TotalSumNetto += nettoVal;
            }

            var sortedItems = groupMap.Values
                .OrderBy(item => item.NMPG)
                .ThenBy(item => item.NOINV)
                .ToList();

            for (int i = 0; i < sortedItems.Count; i++)
            {
                sortedItems[i].No = i + 1;
            }

            result.Items = sortedItems;
            result.TotalNota = sortedItems.Count;

            return result;
        }

        private static double ParseDouble(string val)
        {
            if (string.IsNullOrEmpty(val)) return 0.0;
            double d;
            if (double.TryParse(val, NumberStyles.Any, CultureInfo.InvariantCulture, out d)) return d;
            if (double.TryParse(val, NumberStyles.Any, new CultureInfo("id-ID"), out d)) return d;
            string clean = val.Replace(",", ".");
            if (double.TryParse(clean, NumberStyles.Any, CultureInfo.InvariantCulture, out d)) return d;
            return 0.0;
        }
    }
}
