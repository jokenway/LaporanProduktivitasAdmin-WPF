using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
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
                        list.Add(new SheetInfo
                        {
                            Name = name,
                            TargetFile = "xl/worksheets/sheet" + sheetIndex + ".xml"
                        });
                        sheetIndex++;
                    }
                }
            }

            return list;
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
                    rowObj.Cells[sheetData.Headers[c]] = val;
                }
                sheetData.Rows.Add(rowObj);
            }

            return sheetData;
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
                    sw.WriteLine(string.Format("\"{0}\",\"{1}\",\"{2}\",\"{3}\",\"{4}\",{5},{6:F2}",
                        item.No,
                        (item.NMPG ?? "").Replace("\"", "\"\""),
                        (item.NOINV ?? "").Replace("\"", "\"\""),
                        item.KDPRC,
                        item.USID,
                        item.CountJMDOS,
                        item.SumNetto));
                }
                sw.WriteLine(string.Format("\"-\",\"TOTAL\",\"{0} Nota\",\"-\",\"-\",{1},{2:F2}", totalNota, countJmdos, sumNetto));
            }
        }

        public static void ExportEvaluasiToCsv(string targetPath, IEnumerable<EvaluasiItem> items, int totalNota, int totalSalah, double overallAkurasi, string totalJamText)
        {
            using (var sw = new StreamWriter(targetPath, false, Encoding.UTF8))
            {
                sw.WriteLine("NO,TANGGAL,USER FAKTURIS,TOTAL NOTA,NOTA SALAH,AKURASI (%),JAM DATANG,JAM PULANG,DURASI KERJA,KETERANGAN");
                foreach (var item in items)
                {
                    sw.WriteLine(string.Format("\"{0}\",\"{1}\",\"{2}\",{3},{4},{5:F1}%,,\"{6}\",\"{7}\",\"{8}\",\"{9}\"",
                        item.No,
                        item.Tanggal,
                        (item.User ?? "").Replace("\"", "\"\""),
                        item.TotalNota,
                        item.NotaSalah,
                        item.Akurasi,
                        item.JamDatang,
                        item.JamPulang,
                        item.DurasiText,
                        (item.Keterangan ?? "").Replace("\"", "\"\"")));
                }
                sw.WriteLine(string.Format("\"-\",\"TOTAL KESELURUHAN\",\"-\",{0},{1},{2:F1}%,,\"-\",\"-\",\"{3}\",\"-\"",
                    totalNota, totalSalah, overallAkurasi, totalJamText));
            }
        }
    }
}
