using System.IO.Compression;
using System.Text;
using LaporanProduktivitasWPF.Models;
using LaporanProduktivitasWPF.Services;

namespace LaporanProduktivitasWPF.Tests;

public class ExcelServiceTests
{
    [Theory]
    [InlineData("worksheets/custom.xml")]
    [InlineData("/xl/worksheets/custom.xml")]
    [InlineData("./worksheets/../worksheets/custom.xml")]
    public void Import_ResolvesWorkbookRelationshipsAndSerialDates(string target)
    {
        WithTempFile(".xlsx", path =>
        {
            WriteWorkbook(path, target,
                "<row r='1'><c r='A1' t='inlineStr'><is><t>USID</t></is></c>" +
                "<c r='B1' t='inlineStr'><is><t>TGBON</t></is></c></row>" +
                "<row r='2'><c r='A2' t='inlineStr'><is><t>AKBAR</t></is></c>" +
                "<c r='B2'><v>45200</v></c></row>");

            var result = ExcelService.ParseExcelFile(path);

            Assert.Equal(new[] { "Data" }, result.SheetNames);
            var row = Assert.Single(result.Sheets["Data"].Rows);
            Assert.Equal("AKBAR", row.Get("USID"));
            Assert.Equal("2023-10-01", row.Get("TGBON"));
        });
    }

    [Fact]
    public void Import_ReadsSharedStringsAndPreservesSparseColumns()
    {
        WithTempFile(".xlsx", path =>
        {
            WriteWorkbook(path, "worksheets/custom.xml",
                "<row r='1'><c r='A1' t='s'><v>0</v></c><c r='C1' t='s'><v>1</v></c></row>" +
                "<row r='2'><c r='A2' t='s'><v>2</v></c><c r='C2'><v>123.50</v></c></row>",
                "<sst xmlns='http://schemas.openxmlformats.org/spreadsheetml/2006/main'>" +
                "<si><t>USID</t></si><si><t>NETTO</t></si>" +
                "<si><r><t>AK</t></r><r><t>BAR</t></r></si></sst>");

            var sheet = ExcelService.ParseExcelFile(path).Sheets["Data"];
            Assert.Equal(new[] { "USID", "Kolom_2", "NETTO" }, sheet.Headers);
            var row = Assert.Single(sheet.Rows);
            Assert.Equal("AKBAR", row.Get("USID"));
            Assert.Equal("", row.Get("Kolom_2"));
            Assert.Equal("123.50", row.Get("NETTO"));
        });
    }

    [Fact]
    public void ExportCsv_EscapesQuotesCommasAndFormulaPrefixes()
    {
        WithTempFile(".csv", path =>
        {
            ExcelService.ExportExactPivotToCsv(path, new[]
            {
                new ExactPivotItem
                {
                    No = 1, NMPG = "Sales, \"A\"", NOINV = "=1+1", KDPRC = "P1",
                    USID = "AKBAR", CountJMDOS = 2, SumNetto = 1234.5
                }
            }, "ALL", "ALL", 1, 2, 1234.5);

            var lines = File.ReadAllLines(path);
            Assert.Equal(3, lines.Length);
            Assert.Contains("\"Sales, \"\"A\"\"\"", lines[1]);
            Assert.Contains("\"'=1+1\"", lines[1]);
            Assert.EndsWith(",2,1234.50", lines[1]);
            Assert.Contains("TOTAL", lines[2]);
        });
    }

    [Theory]
    [InlineData("JULI - CLOSING.xlsx", "JULI")]
    [InlineData("rekap oktober 2026.xlsx", "OKTOBER")]
    [InlineData("data.xlsx", null)]
    public void ExtractMonth_RecognizesIndonesianFileNames(string fileName, string? expected) =>
        Assert.Equal(expected, ExcelService.ExtractMonthFromFileName(fileName));

    private static void WriteWorkbook(string path, string target, string rows, string? sharedStrings = null)
    {
        using var archive = ZipFile.Open(path, ZipArchiveMode.Create);
        WriteEntry(archive, "xl/workbook.xml",
            "<workbook xmlns='http://schemas.openxmlformats.org/spreadsheetml/2006/main' " +
            "xmlns:r='http://schemas.openxmlformats.org/officeDocument/2006/relationships'>" +
            "<sheets><sheet name='Data' sheetId='1' r:id='rId7'/></sheets></workbook>");
        WriteEntry(archive, "xl/_rels/workbook.xml.rels",
            "<Relationships xmlns='http://schemas.openxmlformats.org/package/2006/relationships'>" +
            $"<Relationship Id='rId7' Target='{target}' Type='http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet'/>" +
            "</Relationships>");
        WriteEntry(archive, "xl/worksheets/custom.xml",
            "<worksheet xmlns='http://schemas.openxmlformats.org/spreadsheetml/2006/main'><sheetData>" + rows + "</sheetData></worksheet>");
        if (sharedStrings != null) WriteEntry(archive, "xl/sharedStrings.xml", sharedStrings);
    }

    private static void WriteEntry(ZipArchive archive, string name, string content)
    {
        using var writer = new StreamWriter(archive.CreateEntry(name).Open(), Encoding.UTF8);
        writer.Write(content);
    }

    private static void WithTempFile(string extension, Action<string> action)
    {
        var path = Path.Combine(Path.GetTempPath(), "productivity-test-" + Guid.NewGuid().ToString("N") + extension);
        try { action(path); }
        finally { if (File.Exists(path)) File.Delete(path); }
    }
}
