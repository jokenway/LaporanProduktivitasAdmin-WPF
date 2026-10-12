using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using LaporanProduktivitasWPF.Models;

namespace LaporanProduktivitasWPF.Services
{
    public class ExcelSheetData
    {
        public string SheetName { get; set; }
        public List<string> Headers { get; set; }
        public List<RawRow> Rows { get; set; }

        public ExcelSheetData()
        {
            Headers = new List<string>();
            Rows = new List<RawRow>();
        }
    }

    public class ExcelParseResult
    {
        public string FileName { get; set; }
        public List<string> SheetNames { get; set; }
        public Dictionary<string, ExcelSheetData> Sheets { get; set; }

        public ExcelParseResult()
        {
            SheetNames = new List<string>();
            Sheets = new Dictionary<string, ExcelSheetData>(StringComparer.OrdinalIgnoreCase);
        }
    }

    public static class ExcelService
    {
        // Nama bulan Indonesia yang didukung
        public static readonly string[] INDONESIAN_MONTHS = {
            "JANUARI", "FEBRUARI", "MARET", "APRIL", "MEI", "JUNI",
            "JULI", "AGUSTUS", "SEPTEMBER", "OKTOBER", "NOVEMBER", "DESEMBER"
        };

        // Teks yang diabaikan saat parsing bulan dari nama file
        private static readonly string[] IGNORED_KEYWORDS = { "CLOSING", "LAPORAN", "DATA", "REKAP", "REPORT", "FINAL" };

        /// <summary>
        /// Mengambil nama bulan dari nama file.
        /// Format yang didukung: "JULI - CLOSING.xlsx", "AGUSTUS CLOSING 2024.xlsx", dst.
        /// Mengembalikan nama bulan (uppercase) atau null jika tidak ditemukan.
        /// </summary>
        public static string ExtractMonthFromFileName(string fileName)
        {
            if (string.IsNullOrEmpty(fileName)) return null;

            string upper = Path.GetFileNameWithoutExtension(fileName).ToUpperInvariant();

            // Cari nama bulan Indonesia dalam nama file
            foreach (var month in INDONESIAN_MONTHS)
            {
                if (upper.Contains(month))
                    return month;
            }

            return null;
        }

        public static ExcelParseResult ParseExcelFile(string filePath)
        {
            var result = new ExcelParseResult();
            result.FileName = Path.GetFileName(filePath);

            using (var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            using (var archive = new ZipArchive(fs, ZipArchiveMode.Read))
            {
                // 1. Read Shared Strings
                var sharedStrings = ReadSharedStrings(archive);

                // 2. Read Workbook to get Sheet names and target files
                var sheetInfoList = ReadWorkbookSheets(archive);

                foreach (var sInfo in sheetInfoList)
                {
                    result.SheetNames.Add(sInfo.Name);

                    var entry = archive.GetEntry(sInfo.TargetFile);
                    if (entry == null)
                    {
                        // Fallback to xl/worksheets/sheet1.xml format
                        entry = archive.GetEntry("xl/" + sInfo.TargetFile) ??
                                archive.GetEntry("xl/worksheets/" + Path.GetFileName(sInfo.TargetFile));
                    }

                    if (entry != null)
                    {
                        var sheetData = ReadWorksheet(entry, sharedStrings, sInfo.Name);
                        result.Sheets[sInfo.Name] = sheetData;
                    }
                }
            }

            return result;
        }

        private static List<string> ReadSharedStrings(ZipArchive archive)
        {
            var list = new List<string>();
            var entry = archive.GetEntry("xl/sharedStrings.xml");
            if (entry == null) return list;

            using (var stream = entry.Open())
            using (var reader = XmlReader.Create(stream, new XmlReaderSettings { IgnoreWhitespace = true }))
            {
                var sb = new StringBuilder();
                bool inText = false;
                bool inSi = false;

                while (reader.Read())
                {
                    if (reader.NodeType == XmlNodeType.Element)
                    {
                        if (reader.LocalName == "si")
                        {
                            inSi = true;
                            sb.Clear();
                        }
                        else if (inSi && reader.LocalName == "t")
                        {
                            inText = true;
                        }
                    }
                    else if (reader.NodeType == XmlNodeType.Text && inText)
                    {
                        sb.Append(reader.Value);
                    }
                    else if (reader.NodeType == XmlNodeType.EndElement)
                    {
                        if (reader.LocalName == "t")
                        {
                            inText = false;
                        }
                        else if (reader.LocalName == "si")
                        {
                            inSi = false;
                            list.Add(sb.ToString());
                        }
                    }
                }
            }

            return list;
        }

        private class SheetInfo
        {
            public string Name { get; set; }
            public string TargetFile { get; set; }
        }

        private static List<SheetInfo> ReadWorkbookSheets(ZipArchive archive)
        {
            var list = new List<SheetInfo>();
            var wbEntry = archive.GetEntry("xl/workbook.xml");
            if (wbEntry == null) return list;

            var relationshipTargets = ReadWorkbookRelationshipTargets(archive);
            int sheetIndex = 1;
            using (var stream = wbEntry.Open())
            using (var reader = XmlReader.Create(stream, new XmlReaderSettings { IgnoreWhitespace = true }))
            {
                while (reader.Read())
                {
                    if (reader.NodeType == XmlNodeType.Element && reader.LocalName == "sheet")
                    {
                        string name = reader.GetAttribute("name") ?? ("Sheet" + sheetIndex);
                        string rId = reader.GetAttribute("id", "http://schemas.openxmlformats.org/officeDocument/2006/relationships") ?? ("rId" + sheetIndex);
                        string targetFile;
                        if (!relationshipTargets.TryGetValue(rId, out targetFile))
                        {
                            targetFile = "xl/worksheets/sheet" + sheetIndex + ".xml";
                        }
                        list.Add(new SheetInfo
                        {
                            Name = name,
                            TargetFile = targetFile
                        });
                        sheetIndex++;
                    }
                }
            }

            return list;
        }

        private static Dictionary<string, string> ReadWorkbookRelationshipTargets(ZipArchive archive)
        {
            var targets = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var relsEntry = archive.GetEntry("xl/_rels/workbook.xml.rels");
            if (relsEntry == null) return targets;

            using (var stream = relsEntry.Open())
            using (var reader = XmlReader.Create(stream, new XmlReaderSettings { IgnoreWhitespace = true }))
            {
                while (reader.Read())
                {
                    if (reader.NodeType != XmlNodeType.Element || reader.LocalName != "Relationship") continue;

                    string id = reader.GetAttribute("Id");
                    string target = reader.GetAttribute("Target");
                    string targetMode = reader.GetAttribute("TargetMode");
                    if (string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(target) ||
                        string.Equals(targetMode, "External", StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    targets[id] = ResolveWorkbookTarget(target);
                }
            }

            return targets;
        }

        private static string ResolveWorkbookTarget(string target)
        {
            string normalized = (target ?? string.Empty).Replace('\\', '/');
            bool isAbsolute = normalized.StartsWith("/", StringComparison.Ordinal);
            var parts = new List<string>();
            if (!isAbsolute) parts.Add("xl");

            foreach (string part in normalized.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries))
            {
                if (part == ".") continue;
                if (part == "..")
                {
                    if (parts.Count > 0) parts.RemoveAt(parts.Count - 1);
                    continue;
                }
                parts.Add(part);
            }

            return string.Join("/", parts);
        }

        private static ExcelSheetData ReadWorksheet(ZipArchiveEntry entry, List<string> sharedStrings, string sheetName)
        {
            var sheetData = new ExcelSheetData { SheetName = sheetName };
            var rawMatrix = new List<List<string>>();

            using (var stream = entry.Open())
            using (var reader = XmlReader.Create(stream, new XmlReaderSettings { IgnoreWhitespace = true }))
            {
                List<string> currentRow = null;
                int currentColIdx = 0;
                string cellType = "";
                string cellValue = "";

                while (reader.Read())
                {
                    if (reader.NodeType == XmlNodeType.Element)
                    {
                        if (reader.LocalName == "row")
                        {
                            currentRow = new List<string>();
                            currentColIdx = 0;
                        }
                        else if (reader.LocalName == "c")
                        {
                            cellType = reader.GetAttribute("t") ?? "";
                            cellValue = "";
                            string cellRef = reader.GetAttribute("r") ?? "";
                            int colFromRef = GetColumnIndexFromRef(cellRef);
                            while (currentColIdx < colFromRef)
                            {
                                currentRow.Add(string.Empty);
                                currentColIdx++;
                            }
                        }
                        else if (reader.LocalName == "v")
                        {
                            cellValue = reader.ReadElementContentAsString();
                            string finalVal = cellValue;
                            if (cellType == "s")
                            {
                                int sIndex;
                                if (int.TryParse(cellValue, out sIndex) && sIndex >= 0 && sIndex < sharedStrings.Count)
                                {
                                    finalVal = sharedStrings[sIndex];
                                }
                            }
                            currentRow.Add(finalVal);
                            currentColIdx++;
                        }
                        else if (reader.LocalName == "t" && cellType == "inlineStr")
                        {
                            // Inline strings (umum dari Google Sheets/LibreOffice) tidak
                            // memiliki elemen <v>. Beberapa rich-text cell memiliki beberapa <t>.
                            cellValue += reader.ReadElementContentAsString();
                        }
                    }
                    else if (reader.NodeType == XmlNodeType.EndElement && reader.LocalName == "c" && cellType == "inlineStr")
                    {
                        currentRow.Add(cellValue);
                        currentColIdx++;
                    }
                    else if (reader.NodeType == XmlNodeType.EndElement && reader.LocalName == "row")
                    {
                        if (currentRow != null && currentRow.Count > 0)
                        {
                            rawMatrix.Add(currentRow);
                        }
                    }
                }
            }

            if (rawMatrix.Count == 0) return sheetData;

            // Header is the first row with values
            int headerIdx = 0;
            while (headerIdx < rawMatrix.Count && rawMatrix[headerIdx].TrueForAll(string.IsNullOrWhiteSpace))
            {
                headerIdx++;
            }

            if (headerIdx >= rawMatrix.Count) return sheetData;

            var rawHeaders = rawMatrix[headerIdx];
            var usedHeaders = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

            for (int i = 0; i < rawHeaders.Count; i++)
            {
                string h = (rawHeaders[i] ?? string.Empty).Trim();
                if (string.IsNullOrEmpty(h)) h = "Kolom_" + (i + 1);

                if (!usedHeaders.ContainsKey(h))
                {
                    usedHeaders[h] = 1;
                    sheetData.Headers.Add(h);
                }
                else
                {
                    usedHeaders[h]++;
                    sheetData.Headers.Add(h + "_" + usedHeaders[h]);
                }
            }

            for (int r = headerIdx + 1; r < rawMatrix.Count; r++)
            {
                var rList = rawMatrix[r];
                if (rList.TrueForAll(string.IsNullOrWhiteSpace)) continue;

                var rowObj = new RawRow();
                for (int c = 0; c < sheetData.Headers.Count; c++)
                {
                    string val = c < rList.Count ? (rList[c] ?? string.Empty).Trim() : string.Empty;
                    if (IsDateHeader(sheetData.Headers[c]) && TryFormatExcelSerialDate(val, out string formattedDate))
                    {
                        val = formattedDate;
                    }
                    rowObj.Cells[sheetData.Headers[c]] = val;
                }
                sheetData.Rows.Add(rowObj);
            }

            return sheetData;
        }

        private static bool IsDateHeader(string header)
        {
            return string.Equals(header, "TGBON", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(header, "Tanggal", StringComparison.OrdinalIgnoreCase);
        }

        private static bool TryFormatExcelSerialDate(string value, out string formattedDate)
        {
            formattedDate = null;
            if (!double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out double serial) ||
                serial < 20000 || serial > 2958465)
            {
                return false;
            }

            try
            {
                formattedDate = DateTime.FromOADate(serial).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
                return true;
            }
            catch (ArgumentException)
            {
                return false;
            }
        }

        private static int GetColumnIndexFromRef(string cellRef)
        {
            if (string.IsNullOrEmpty(cellRef)) return 0;
            int col = 0;
            foreach (char ch in cellRef)
            {
                if (char.IsLetter(ch))
                {
                    col = col * 26 + (char.ToUpper(ch) - 'A' + 1);
                }
                else break;
            }
            return Math.Max(0, col - 1);
        }

        public static void ExportExactPivotToCsv(string targetPath, IEnumerable<ExactPivotItem> items, string user, string date, int totalNota, int countJmdos, double sumNetto)
        {
            using (var sw = new StreamWriter(targetPath, false, Encoding.UTF8))
            {
                sw.WriteLine("NO,NMPG,NOINV,KDPRC,USID,Count JMDOS,Sum of NETTO");
                foreach (var item in items)
                {
                    sw.WriteLine(string.Format(CultureInfo.InvariantCulture, "{0},{1},{2},{3},{4},{5},{6:F2}",
                        CsvField(item.No.ToString(CultureInfo.InvariantCulture)),
                        CsvField(item.NMPG),
                        CsvField(item.NOINV),
                        CsvField(item.KDPRC),
                        CsvField(item.USID),
                        item.CountJMDOS,
                        item.SumNetto));
                }
                sw.WriteLine(string.Format(CultureInfo.InvariantCulture, "{0},{1},{2},{3},{4},{5},{6:F2}", CsvField("-"), CsvField("TOTAL"), CsvField(totalNota + " Nota"), CsvField("-"), CsvField("-"), countJmdos, sumNetto));
            }
        }

        private static readonly Dictionary<string, string> USER_CODES = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "MERANGGA RASUMA", "642" },
            { "DIDIN KURNIAWAN", "692" },
            { "DIDIN", "692" },
            { "IQBAL JUL AKBAR", "556" },
            { "AKBAR", "556" },
            { "JOE", "665" },
            { "ABDUL RONI", "619" },
            { "RONI", "619" },
            { "NOVIANI KUSUMA DEWI", "706" },
            { "NOVIANI", "706" },
            { "STEPHANIE LADO PHILNATHA", "870" },
            { "STEVI", "870" },
            { "ANGGINAVANCA ANATAGRACIA LAROBU", "875" },
            { "GINA", "875" }
        };

        private static string CsvField(string value)
        {
            string safeValue = value ?? string.Empty;
            if (safeValue.Length > 0 && "=+-@".IndexOf(safeValue[0]) >= 0)
                safeValue = "'" + safeValue;
            return "\"" + safeValue.Replace("\"", "\"\"") + "\"";
        }

        private static string EscapeXml(string text)
        {
            if (string.IsNullOrEmpty(text)) return string.Empty;
            return text.Replace("&", "&amp;")
                       .Replace("<", "&lt;")
                       .Replace(">", "&gt;")
                       .Replace("\"", "&quot;")
                       .Replace("'", "&apos;");
        }

        /// <summary>
        /// Mengekspor Ringkasan Performa Tim Fakturis langsung ke format file Excel (.xlsx) resmi.
        /// Kompatibel 100% dengan semua versi Microsoft Excel, Google Sheets, dan LibreOffice.
        /// </summary>
        public static void ExportRingkasanFakturisToExcel(
            string targetPath,
            string monthKey,
            IEnumerable<AdminInvoiceMetric> metrics,
            int totalNotaTim)
        {
            var metricList = metrics != null ? metrics.ToList() : new List<AdminInvoiceMetric>();
            string mUpper = string.IsNullOrWhiteSpace(monthKey) ? "SEMUA BULAN" : monthKey.Trim().ToUpperInvariant();

            int sumNota = metricList.Sum(m => m.TotalNota);
            int sumSalah = metricList.Sum(m => m.TotalSalah);
            double totalErrorPct = totalNotaTim > 0 ? ((double)sumSalah / totalNotaTim) * 100.0 : 0.0;
            double sumAvgPerDay = metricList.Sum(m => m.AverageNotaPerDay);
            double sumKontribusi = metricList.Sum(m => m.ContributionPercentage);

            if (File.Exists(targetPath))
            {
                try { File.Delete(targetPath); } catch { }
            }

            using (var fs = new FileStream(targetPath, FileMode.Create, FileAccess.Write, FileShare.None))
            using (var zip = new ZipArchive(fs, ZipArchiveMode.Create))
            {
                // 1. [Content_Types].xml
                var ctEntry = zip.CreateEntry("[Content_Types].xml");
                using (var sw = new StreamWriter(ctEntry.Open(), Encoding.UTF8))
                {
                    sw.Write("<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>\n" +
                             "<Types xmlns=\"http://schemas.openxmlformats.org/package/2006/content-types\">\n" +
                             "  <Default Extension=\"rels\" ContentType=\"application/vnd.openxmlformats-package.relationships+xml\"/>\n" +
                             "  <Default Extension=\"xml\" ContentType=\"application/xml\"/>\n" +
                             "  <Override PartName=\"/xl/workbook.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml\"/>\n" +
                             "  <Override PartName=\"/xl/worksheets/sheet1.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml\"/>\n" +
                             "  <Override PartName=\"/xl/styles.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.styles+xml\"/>\n" +
                             "</Types>");
                }

                // 2. _rels/.rels
                var relsEntry = zip.CreateEntry("_rels/.rels");
                using (var sw = new StreamWriter(relsEntry.Open(), Encoding.UTF8))
                {
                    sw.Write("<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>\n" +
                             "<Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\">\n" +
                             "  <Relationship Id=\"rId1\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument\" Target=\"xl/workbook.xml\"/>\n" +
                             "</Relationships>");
                }

                // 3. xl/_rels/workbook.xml.rels
                var wbRelsEntry = zip.CreateEntry("xl/_rels/workbook.xml.rels");
                using (var sw = new StreamWriter(wbRelsEntry.Open(), Encoding.UTF8))
                {
                    sw.Write("<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>\n" +
                             "<Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\">\n" +
                             "  <Relationship Id=\"rId1\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet\" Target=\"worksheets/sheet1.xml\"/>\n" +
                             "  <Relationship Id=\"rId2\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/styles\" Target=\"styles.xml\"/>\n" +
                             "</Relationships>");
                }

                // 4. xl/workbook.xml
                var wbEntry = zip.CreateEntry("xl/workbook.xml");
                using (var sw = new StreamWriter(wbEntry.Open(), Encoding.UTF8))
                {
                    sw.Write("<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>\n" +
                             "<workbook xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\" xmlns:r=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships\">\n" +
                             "  <sheets>\n" +
                             "    <sheet name=\"REKAP INVOICE ADMIN\" sheetId=\"1\" r:id=\"rId1\"/>\n" +
                             "  </sheets>\n" +
                             "</workbook>");
                }

                // 5. xl/styles.xml (Definisi Font, Fill, Border, dan Alignment)
                var stylesEntry = zip.CreateEntry("xl/styles.xml");
                using (var sw = new StreamWriter(stylesEntry.Open(), Encoding.UTF8))
                {
                    sw.Write("<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>\n" +
                             "<styleSheet xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\">\n" +
                             "  <fonts count=\"3\">\n" +
                             "    <font><name val=\"Calibri\"/><sz val=\"11\"/></font>\n" +
                             "    <font><b/><name val=\"Calibri\"/><sz val=\"14\"/></font>\n" +
                             "    <font><b/><name val=\"Calibri\"/><sz val=\"11\"/></font>\n" +
                             "  </fonts>\n" +
                             "  <fills count=\"4\">\n" +
                             "    <fill><patternFill patternType=\"none\"/></fill>\n" +
                             "    <fill><patternFill patternType=\"gray125\"/></fill>\n" +
                             "    <fill><patternFill patternType=\"solid\"><fgColor rgb=\"FFD9E1F2\"/></patternFill></fill>\n" +
                             "    <fill><patternFill patternType=\"solid\"><fgColor rgb=\"FFF2F2F2\"/></patternFill></fill>\n" +
                             "  </fills>\n" +
                             "  <borders count=\"3\">\n" +
                             "    <border><left/><right/><top/><bottom/><diagonal/></border>\n" +
                             "    <border>\n" +
                             "      <left style=\"thin\"><color auto=\"1\"/></left>\n" +
                             "      <right style=\"thin\"><color auto=\"1\"/></right>\n" +
                             "      <top style=\"thin\"><color auto=\"1\"/></top>\n" +
                             "      <bottom style=\"thin\"><color auto=\"1\"/></bottom>\n" +
                             "      <diagonal/>\n" +
                             "    </border>\n" +
                             "    <border>\n" +
                             "      <left style=\"thin\"><color auto=\"1\"/></left>\n" +
                             "      <right style=\"thin\"><color auto=\"1\"/></right>\n" +
                             "      <top style=\"thin\"><color auto=\"1\"/></top>\n" +
                             "      <bottom style=\"double\"><color auto=\"1\"/></bottom>\n" +
                             "      <diagonal/>\n" +
                             "    </border>\n" +
                             "  </borders>\n" +
                             "  <cellStyleXfs count=\"1\">\n" +
                             "    <xf numFmtId=\"0\" fontId=\"0\" fillId=\"0\" borderId=\"0\"/>\n" +
                             "  </cellStyleXfs>\n" +
                             "  <cellXfs count=\"10\">\n" +
                             "    <xf numFmtId=\"0\" fontId=\"0\" fillId=\"0\" borderId=\"0\" xfId=\"0\"/>\n" +
                             "    <xf numFmtId=\"0\" fontId=\"1\" fillId=\"0\" borderId=\"0\" xfId=\"0\" applyFont=\"1\"/>\n" +
                             "    <xf numFmtId=\"0\" fontId=\"2\" fillId=\"0\" borderId=\"0\" xfId=\"0\" applyFont=\"1\"/>\n" +
                             "    <xf numFmtId=\"0\" fontId=\"2\" fillId=\"2\" borderId=\"1\" xfId=\"0\" applyFont=\"1\" applyFill=\"1\" applyBorder=\"1\" applyAlignment=\"1\">\n" +
                             "      <alignment horizontal=\"center\" vertical=\"center\" wrapText=\"1\"/>\n" +
                             "    </xf>\n" +
                             "    <xf numFmtId=\"0\" fontId=\"0\" fillId=\"0\" borderId=\"1\" xfId=\"0\" applyBorder=\"1\" applyAlignment=\"1\">\n" +
                             "      <alignment horizontal=\"center\" vertical=\"center\"/>\n" +
                             "    </xf>\n" +
                             "    <xf numFmtId=\"0\" fontId=\"0\" fillId=\"0\" borderId=\"1\" xfId=\"0\" applyBorder=\"1\" applyAlignment=\"1\">\n" +
                             "      <alignment horizontal=\"left\" vertical=\"center\"/>\n" +
                             "    </xf>\n" +
                             "    <xf numFmtId=\"0\" fontId=\"0\" fillId=\"0\" borderId=\"1\" xfId=\"0\" applyBorder=\"1\" applyAlignment=\"1\">\n" +
                             "      <alignment horizontal=\"right\" vertical=\"center\"/>\n" +
                             "    </xf>\n" +
                             "    <xf numFmtId=\"0\" fontId=\"2\" fillId=\"3\" borderId=\"2\" xfId=\"0\" applyFont=\"1\" applyFill=\"1\" applyBorder=\"1\" applyAlignment=\"1\">\n" +
                             "      <alignment horizontal=\"center\" vertical=\"center\"/>\n" +
                             "    </xf>\n" +
                             "    <xf numFmtId=\"0\" fontId=\"2\" fillId=\"3\" borderId=\"2\" xfId=\"0\" applyFont=\"1\" applyFill=\"1\" applyBorder=\"1\" applyAlignment=\"1\">\n" +
                             "      <alignment horizontal=\"left\" vertical=\"center\"/>\n" +
                             "    </xf>\n" +
                             "    <xf numFmtId=\"0\" fontId=\"2\" fillId=\"3\" borderId=\"2\" xfId=\"0\" applyFont=\"1\" applyFill=\"1\" applyBorder=\"1\" applyAlignment=\"1\">\n" +
                             "      <alignment horizontal=\"right\" vertical=\"center\"/>\n" +
                             "    </xf>\n" +
                             "  </cellXfs>\n" +
                             "</styleSheet>");
                }

                // 6. xl/worksheets/sheet1.xml
                var wsEntry = zip.CreateEntry("xl/worksheets/sheet1.xml");
                using (var sw = new StreamWriter(wsEntry.Open(), Encoding.UTF8))
                {
                    sw.WriteLine("<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>");
                    sw.WriteLine("<worksheet xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\">");
                    sw.WriteLine("  <cols>");
                    sw.WriteLine("    <col min=\"1\" max=\"1\" width=\"6\" customWidth=\"1\"/>");
                    sw.WriteLine("    <col min=\"2\" max=\"2\" width=\"24\" customWidth=\"1\"/>");
                    sw.WriteLine("    <col min=\"3\" max=\"3\" width=\"16\" customWidth=\"1\"/>");
                    sw.WriteLine("    <col min=\"4\" max=\"4\" width=\"16\" customWidth=\"1\"/>");
                    sw.WriteLine("    <col min=\"5\" max=\"5\" width=\"26\" customWidth=\"1\"/>");
                    sw.WriteLine("    <col min=\"6\" max=\"6\" width=\"30\" customWidth=\"1\"/>");
                    sw.WriteLine("    <col min=\"7\" max=\"7\" width=\"28\" customWidth=\"1\"/>");
                    sw.WriteLine("  </cols>");
                    sw.WriteLine("  <sheetData>");

                    int rowIdx = 1;

                    // Row 1: Title
                    sw.WriteLine(string.Format("    <row r=\"{0}\"><c r=\"A{0}\" s=\"1\" t=\"inlineStr\"><is><t>{1}</t></is></c></row>",
                        rowIdx++, EscapeXml("REKAP INVOICE ADMIN")));

                    // Row 2: Bulan
                    sw.WriteLine(string.Format("    <row r=\"{0}\"><c r=\"A{0}\" s=\"2\" t=\"inlineStr\"><is><t>Bulan :</t></is></c><c r=\"B{0}\" t=\"inlineStr\"><is><t>{1}</t></is></c></row>",
                        rowIdx++, EscapeXml(mUpper)));

                    // Row 3: Total Nota Tim
                    sw.WriteLine(string.Format("    <row r=\"{0}\"><c r=\"A{0}\" s=\"2\" t=\"inlineStr\"><is><t>Total Nota Tim :</t></is></c><c r=\"B{0}\"><v>{1}</v></c></row>",
                        rowIdx++, totalNotaTim));

                    // Row 4: Waktu Export
                    sw.WriteLine(string.Format("    <row r=\"{0}\"><c r=\"A{0}\" s=\"2\" t=\"inlineStr\"><is><t>Tanggal Export :</t></is></c><c r=\"B{0}\" t=\"inlineStr\"><is><t>{1}</t></is></c></row>",
                        rowIdx++, DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss")));

                    // Row 5: Spasi
                    sw.WriteLine(string.Format("    <row r=\"{0}\"/>", rowIdx++));

                    // Row 6: Header Tabel dengan garis border dan background biru muda (s="3")
                    string[] headers = {
                        "NO", "USER FAKTURIS", "TOTAL NOTA", "TOTAL SALAH",
                        "ERROR / TOTAL TIM (%)", "RATA-RATA NOTA / HARI KERJA", "RATA-RATA INPUT NOTA / TOTAL NOTA (%)"
                    };
                    string[] colLetters = { "A", "B", "C", "D", "E", "F", "G" };

                    var sbH = new StringBuilder(string.Format("    <row r=\"{0}\" ht=\"28\" customHeight=\"1\">", rowIdx));
                    for (int i = 0; i < headers.Length; i++)
                    {
                        sbH.Append(string.Format("<c r=\"{0}{1}\" s=\"3\" t=\"inlineStr\"><is><t>{2}</t></is></c>",
                            colLetters[i], rowIdx, EscapeXml(headers[i])));
                    }
                    sbH.Append("</row>");
                    sw.WriteLine(sbH.ToString());
                    rowIdx++;

                    // Baris Data Per User dengan garis border tipis (s="4" center, s="5" left, s="6" right)
                    int no = 1;
                    foreach (var m in metricList)
                    {
                        var sbRow = new StringBuilder(string.Format("    <row r=\"{0}\">", rowIdx));
                        sbRow.Append(string.Format("<c r=\"A{0}\" s=\"4\"><v>{1}</v></c>", rowIdx, no));
                        sbRow.Append(string.Format("<c r=\"B{0}\" s=\"5\" t=\"inlineStr\"><is><t>{1}</t></is></c>", rowIdx, EscapeXml(m.User)));
                        sbRow.Append(string.Format("<c r=\"C{0}\" s=\"6\"><v>{1}</v></c>", rowIdx, m.TotalNota));
                        sbRow.Append(string.Format("<c r=\"D{0}\" s=\"6\"><v>{1}</v></c>", rowIdx, m.TotalSalah));
                        sbRow.Append(string.Format(CultureInfo.InvariantCulture, "<c r=\"E{0}\" s=\"6\" t=\"inlineStr\"><is><t>{1:F1}%</t></is></c>", rowIdx, m.SalahPercentage));
                        sbRow.Append(string.Format(CultureInfo.InvariantCulture, "<c r=\"F{0}\" s=\"6\" t=\"inlineStr\"><is><t>{1:F1} nota</t></is></c>", rowIdx, m.AverageNotaPerDay));
                        sbRow.Append(string.Format(CultureInfo.InvariantCulture, "<c r=\"G{0}\" s=\"6\" t=\"inlineStr\"><is><t>{1:F1}%</t></is></c>", rowIdx, m.ContributionPercentage));
                        sbRow.Append("</row>");
                        sw.WriteLine(sbRow.ToString());

                        rowIdx++;
                        no++;
                    }

                    // Baris Total / Footer dengan garis border dobel di bawah (s="7" center, s="8" left, s="9" right)
                    var sbTot = new StringBuilder(string.Format("    <row r=\"{0}\">", rowIdx));
                    sbTot.Append(string.Format("<c r=\"A{0}\" s=\"7\" t=\"inlineStr\"><is><t>-</t></is></c>", rowIdx));
                    sbTot.Append(string.Format("<c r=\"B{0}\" s=\"8\" t=\"inlineStr\"><is><t>TOTAL KESELURUHAN</t></is></c>", rowIdx));
                    sbTot.Append(string.Format("<c r=\"C{0}\" s=\"9\"><v>{1}</v></c>", rowIdx, sumNota));
                    sbTot.Append(string.Format("<c r=\"D{0}\" s=\"9\"><v>{1}</v></c>", rowIdx, sumSalah));
                    sbTot.Append(string.Format(CultureInfo.InvariantCulture, "<c r=\"E{0}\" s=\"9\" t=\"inlineStr\"><is><t>{1:F1}%</t></is></c>", rowIdx, totalErrorPct));
                    sbTot.Append(string.Format(CultureInfo.InvariantCulture, "<c r=\"F{0}\" s=\"9\" t=\"inlineStr\"><is><t>{1:F1} nota</t></is></c>", rowIdx, sumAvgPerDay));
                    sbTot.Append(string.Format(CultureInfo.InvariantCulture, "<c r=\"G{0}\" s=\"9\" t=\"inlineStr\"><is><t>{1:F1}%</t></is></c>", rowIdx, sumKontribusi));
                    sbTot.Append("</row>");
                    sw.WriteLine(sbTot.ToString());

                    sw.WriteLine("  </sheetData>");
                    sw.WriteLine("</worksheet>");
                }
            }
        }

        public static void ExportRingkasanFakturisToCsv(
            string targetPath,
            string monthKey,
            IEnumerable<AdminInvoiceMetric> metrics,
            int totalNotaTim)
        {
            var metricList = metrics != null ? metrics.ToList() : new List<AdminInvoiceMetric>();
            string mUpper = string.IsNullOrWhiteSpace(monthKey) ? "SEMUA BULAN" : monthKey.Trim().ToUpperInvariant();

            int sumNota = metricList.Sum(m => m.TotalNota);
            int sumSalah = metricList.Sum(m => m.TotalSalah);
            double totalErrorPct = totalNotaTim > 0 ? ((double)sumSalah / totalNotaTim) * 100.0 : 0.0;
            double sumAvgPerDay = metricList.Sum(m => m.AverageNotaPerDay);
            double sumKontribusi = metricList.Sum(m => m.ContributionPercentage);

            // Gunakan UTF-8 BOM agar Excel mengenali encoding secara akurat
            using (var sw = new StreamWriter(targetPath, false, new UTF8Encoding(true)))
            {
                // Menambahkan instruksi 'sep=,' agar Excel otomatis memecah kolom dengan koma di semua versi & locale Windows
                sw.WriteLine("sep=,");

                // Header Ringkasan
                sw.WriteLine(CsvField("REKAP INVOICE ADMIN"));
                sw.WriteLine(string.Format("{0},{1}", CsvField("Bulan :"), CsvField(mUpper)));
                sw.WriteLine(string.Format("{0},{1}", CsvField("Total Nota Tim :"), totalNotaTim.ToString(CultureInfo.InvariantCulture)));
                sw.WriteLine(string.Format("{0},{1}", CsvField("Tanggal Export :"), CsvField(DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss"))));
                sw.WriteLine("");

                // Header Tabel
                sw.WriteLine(string.Format("{0},{1},{2},{3},{4},{5},{6}",
                    CsvField("NO"),
                    CsvField("USER FAKTURIS"),
                    CsvField("TOTAL NOTA"),
                    CsvField("TOTAL SALAH"),
                    CsvField("ERROR / TOTAL TIM (%)"),
                    CsvField("RATA-RATA NOTA / HARI KERJA"),
                    CsvField("RATA-RATA INPUT NOTA / TOTAL NOTA (%)")
                ));

                // Baris Data
                int no = 1;
                foreach (var item in metricList)
                {
                    sw.WriteLine(string.Format(CultureInfo.InvariantCulture, "{0},{1},{2},{3},{4},{5},{6}",
                        no,
                        CsvField(item.User),
                        item.TotalNota,
                        item.TotalSalah,
                        CsvField(string.Format(CultureInfo.InvariantCulture, "{0:F1}%", item.SalahPercentage)),
                        CsvField(string.Format(CultureInfo.InvariantCulture, "{0:F1} nota", item.AverageNotaPerDay)),
                        CsvField(string.Format(CultureInfo.InvariantCulture, "{0:F1}%", item.ContributionPercentage))
                    ));
                    no++;
                }

                // Baris Total / Footer Tabel
                sw.WriteLine(string.Format(CultureInfo.InvariantCulture, "{0},{1},{2},{3},{4},{5},{6}",
                    CsvField("-"),
                    CsvField("TOTAL KESELURUHAN"),
                    sumNota,
                    sumSalah,
                    CsvField(string.Format(CultureInfo.InvariantCulture, "{0:F1}%", totalErrorPct)),
                    CsvField(string.Format(CultureInfo.InvariantCulture, "{0:F1} nota", sumAvgPerDay)),
                    CsvField(string.Format(CultureInfo.InvariantCulture, "{0:F1}%", sumKontribusi))
                ));
            }
        }

        public static void ExportEvaluasiToCsv(
            string targetPath,
            string monthKey,
            IEnumerable<RawRow> rows,
            Dictionary<string, EvaluasiItem> manualLogs,
            IEnumerable<EvaluasiItem> items,
            int totalNota,
            int totalSalah,
            double overallAkurasi,
            string totalJamText)
        {
            var recordList = items != null ? items.ToList() : new List<EvaluasiItem>();

            // Determine year and month number
            int year = DateTime.Now.Year;
            if (recordList.Count > 0)
            {
                foreach (var rec in recordList)
                {
                    var dt = EvaluasiEngine.TryParseDate(rec.Tanggal);
                    if (dt.HasValue)
                    {
                        year = dt.Value.Year;
                        break;
                    }
                }
            }
            else if (rows != null)
            {
                foreach (var r in rows)
                {
                    string d = r.Get("TGBON");
                    if (string.IsNullOrEmpty(d)) d = r.Get("Tanggal");
                    var dt = EvaluasiEngine.TryParseDate(d);
                    if (dt.HasValue)
                    {
                        year = dt.Value.Year;
                        break;
                    }
                }
            }

            int monthNum = 1;
            string mUpper = (monthKey ?? "JANUARI").Trim().ToUpperInvariant();
            for (int i = 0; i < INDONESIAN_MONTHS.Length; i++)
            {
                if (INDONESIAN_MONTHS[i].Equals(mUpper, StringComparison.OrdinalIgnoreCase))
                {
                    monthNum = i + 1;
                    break;
                }
            }

            int daysInMonth = DateTime.DaysInMonth(year, monthNum);

            // Get complete evaluation records from EvaluasiEngine if rows available
            EvaluasiResult evalRes = null;
            if (rows != null)
            {
                evalRes = EvaluasiEngine.BuildDailyEvaluation(rows, manualLogs, "ALL", "ALL", "", "ADMIN_INVOICE");
                recordList = evalRes.Records;
            }

            // Map user daily data: user -> (day -> EvaluasiItem)
            var userDailyMap = new Dictionary<string, Dictionary<int, EvaluasiItem>>(StringComparer.OrdinalIgnoreCase);
            foreach (var rec in recordList)
            {
                var dt = EvaluasiEngine.TryParseDate(rec.Tanggal);
                if (!dt.HasValue) continue;

                int day = dt.Value.Day;
                string u = (rec.User ?? "").Trim().ToUpperInvariant();

                if (!userDailyMap.TryGetValue(u, out var dayMap))
                {
                    dayMap = new Dictionary<int, EvaluasiItem>();
                    userDailyMap[u] = dayMap;
                }
                dayMap[day] = rec;
            }

            var users = evalRes != null && evalRes.AvailableUsers.Count > 0
                ? evalRes.AvailableUsers
                : userDailyMap.Keys.OrderBy(x => x).ToList();

            if (users.Count == 0)
            {
                users = EvaluasiEngine.ADMIN_INVOICE_USERS.ToList();
            }

            int grandTotalInput = recordList.Sum(r => r.TotalNota);
            int grandTotalSalah = recordList.Sum(r => r.NotaSalah);

            int workDaysCount = 0;
            for (int d = 1; d <= daysInMonth; d++)
            {
                var dt = new DateTime(year, monthNum, d);
                if (dt.DayOfWeek != DayOfWeek.Sunday) workDaysCount++;
            }
            if (workDaysCount == 0) workDaysCount = 26;

            using (var sw = new StreamWriter(targetPath, false, Encoding.UTF8))
            {
                string commas(int count) => new string(',', count);

                sw.WriteLine(commas(100));
                sw.WriteLine("REKAP ADMIN NOTA / INVOICING" + commas(99));
                sw.WriteLine(string.Format("BULAN :,,,{0}", mUpper) + commas(96));
                sw.WriteLine(string.Format("TAHUN :,,,{0}", year) + commas(96));
                sw.WriteLine(commas(100));

                var sbHeader1 = new StringBuilder("NO,Kode,NAMA");
                for (int d = 1; d <= 31; d++)
                {
                    sbHeader1.Append(",input,salah,jam ");
                }
                sbHeader1.Append(",,TOTAL,,percentase,");
                sw.WriteLine(sbHeader1.ToString());

                var sbHeader2 = new StringBuilder("tanggal,,");
                for (int d = 1; d <= 31; d++)
                {
                    if (d <= daysInMonth)
                    {
                        string dateStr = string.Format("{0:D4}-{1:D2}-{2:D2}", year, monthNum, d);
                        sbHeader2.Append(string.Format(",{0},,selesai", dateStr));
                    }
                    else
                    {
                        sbHeader2.Append(",,,");
                    }
                }
                sbHeader2.Append(",,INPUT,SALAH,,");
                sw.WriteLine(sbHeader2.ToString());

                sw.WriteLine(commas(100));
                sw.WriteLine(commas(100));

                int userNo = 1;
                foreach (var u in users)
                {
                    string uUpper = u.Trim().ToUpperInvariant();
                    string code = USER_CODES.TryGetValue(uUpper, out var c) ? c : "";
                    userDailyMap.TryGetValue(uUpper, out var dayMap);

                    int userTotalInput = 0;
                    int userTotalSalah = 0;

                    var sbUserRow1 = new StringBuilder(string.Format(CultureInfo.InvariantCulture, "{0},{1},{2}", userNo, CsvField(code), CsvField(uUpper)));

                    for (int d = 1; d <= 31; d++)
                    {
                        if (d <= daysInMonth && dayMap != null && dayMap.TryGetValue(d, out var item))
                        {
                            int inp = item.TotalNota;
                            int sal = item.NotaSalah;
                            string jam = "";

                            if (!string.IsNullOrWhiteSpace(item.Keterangan))
                            {
                                jam = item.Keterangan.Trim();
                            }
                            else if (!string.IsNullOrWhiteSpace(item.JamDatang) || !string.IsNullOrWhiteSpace(item.JamPulang))
                            {
                                jam = string.Format("{0}-{1}", item.JamDatang, item.JamPulang);
                            }

                            userTotalInput += inp;
                            userTotalSalah += sal;

                            string inpStr = inp > 0 ? inp.ToString(CultureInfo.InvariantCulture) : "";
                            string salStr = sal > 0 ? sal.ToString(CultureInfo.InvariantCulture) : "";

                            sbUserRow1.Append(string.Format(CultureInfo.InvariantCulture, ",{0},{1},{2}", inpStr, salStr, CsvField(jam)));
                        }
                        else
                        {
                            sbUserRow1.Append(",,,");
                        }
                    }

                    double pctSalahPerInput = userTotalInput > 0 ? (double)userTotalSalah / userTotalInput : 0.0;
                    double avgInputPerDay = (double)userTotalInput / workDaysCount;
                    double pctOfGrandTotal = grandTotalInput > 0 ? (double)userTotalInput / grandTotalInput : 0.0;

                    sbUserRow1.Append(string.Format(CultureInfo.InvariantCulture, ",√,{0},{1},{2},*rata2 salah nota/input nota", userTotalInput, userTotalSalah, pctSalahPerInput));
                    sw.WriteLine(sbUserRow1.ToString());

                    var sbUserRow2 = new StringBuilder(commas(98));
                    sbUserRow2.Append(string.Format(CultureInfo.InvariantCulture, "{0},*rata2 input nota/hari kerja", avgInputPerDay));
                    sw.WriteLine(sbUserRow2.ToString());

                    var sbUserRow3 = new StringBuilder(commas(98));
                    sbUserRow3.Append(string.Format(CultureInfo.InvariantCulture, "{0},*rata2 input nota/total nota", pctOfGrandTotal));
                    sw.WriteLine(sbUserRow3.ToString());

                    userNo++;
                }

                sw.WriteLine(commas(100));
                sw.WriteLine(commas(100));
                sw.WriteLine(commas(97) + string.Format(",{0},{1},,", grandTotalInput, grandTotalSalah));
            }
        }
    }
}
