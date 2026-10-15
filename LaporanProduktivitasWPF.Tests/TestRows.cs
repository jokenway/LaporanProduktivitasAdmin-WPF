using LaporanProduktivitasWPF.Models;

namespace LaporanProduktivitasWPF.Tests;

internal static class TestRows
{
    public static RawRow Create(string user = "AKBAR", string date = "2026-10-01",
        string invoice = "INV-1", string netto = "100", string jenis = "A") => new()
    {
        Cells = new(StringComparer.OrdinalIgnoreCase)
        {
            ["USID"] = user, ["TGBON"] = date, ["NOINV"] = invoice,
            ["NETTO"] = netto, ["JENIS_B"] = jenis, ["NMPG"] = "Sales", ["KDPRC"] = "P1"
        }
    };
}
