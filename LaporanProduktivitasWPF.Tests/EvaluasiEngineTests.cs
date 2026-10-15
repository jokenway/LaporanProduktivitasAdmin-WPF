using LaporanProduktivitasWPF.Models;
using LaporanProduktivitasWPF.Services;

namespace LaporanProduktivitasWPF.Tests;

public class EvaluasiEngineTests
{
    [Theory]
    [InlineData("45200", 2023, 10, 1)]
    [InlineData("01 OKTOBER 2026", 2026, 10, 1)]
    [InlineData("1/10/2026", 2026, 10, 1)]
    [InlineData("2026-10-01", 2026, 10, 1)]
    public void DateParser_AcceptsWorkbookFormats(string input, int year, int month, int day) =>
        Assert.Equal(new DateTime(year, month, day), EvaluasiEngine.TryParseDate(input));

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("not-a-date")]
    [InlineData("31/02/2026")]
    public void DateParser_RejectsInvalidDates(string? input) =>
        Assert.Null(EvaluasiEngine.TryParseDate(input));

    [Fact]
    public void Evaluation_UsesDominantYearAndMonthForRowsAndManualLogs()
    {
        var rows = new[]
        {
            TestRows.Create(date: "2026-10-01"),
            TestRows.Create(date: "2026-10-02", invoice: "INV-2"),
            TestRows.Create(date: "2025-10-01", invoice: "OLD"),
            TestRows.Create(date: "2026-09-01", invoice: "OTHER")
        };
        var logs = new Dictionary<string, EvaluasiItem>
        {
            ["AKBAR___2025-10-02"] = new(),
            ["AKBAR___2026-10-03"] = new()
        };

        var result = EvaluasiEngine.BuildDailyEvaluation(rows, logs, userGroup: "ADMIN_INVOICE");

        Assert.Equal(new[] { "2026-10-01", "2026-10-02", "2026-10-03" }, result.AvailableDates);
        Assert.Equal(2, result.Summary.TotalNota);
        Assert.DoesNotContain(result.Records, r => r.Tanggal.StartsWith("2025"));
    }

    [Fact]
    public void Evaluation_CountsDistinctInvoicesAndExcludesSunday()
    {
        var result = EvaluasiEngine.BuildDailyEvaluation(new[]
        {
            TestRows.Create(), TestRows.Create(invoice: "inv-1"),
            TestRows.Create(invoice: "INV-2"), TestRows.Create(date: "2026-10-04")
        }, new());

        Assert.Equal(2, Assert.Single(result.Records).TotalNota);
        Assert.DoesNotContain("2026-10-04", result.AvailableDates);
    }

    [Fact]
    public void Evaluation_AppliesManualLogsFiltersAndSummary()
    {
        var saved = new EvaluasiItem { NotaSalah = 1, JamDatang = "8", JamPulang = "17", Keterangan = "Audit" };
        var result = EvaluasiEngine.BuildDailyEvaluation(new[]
        {
            TestRows.Create(), TestRows.Create(invoice: "INV-2"), TestRows.Create(user: "DIDIN")
        }, new() { ["AKBAR___2026-10-01"] = saved }, selectedUser: "akbar", searchQuery: "audit");

        var record = Assert.Single(result.Records);
        Assert.Equal(1, record.No);
        Assert.Equal(2, result.Summary.TotalNota);
        Assert.Equal(1, result.Summary.TotalSalah);
        Assert.Equal(50, result.Summary.OverallAkurasi);
        Assert.Equal(540, result.Summary.TotalJamKerjaMinutes);
        Assert.Equal(1, result.Summary.UserCount);
    }

    [Fact]
    public void InvoiceGroup_IncludesZeroWorkloadMembersAndExcludesOutsiders()
    {
        var result = EvaluasiEngine.BuildDailyEvaluation(new[]
        {
            TestRows.Create(), TestRows.Create(user: "OUTSIDER", invoice: "X")
        }, new(), userGroup: "ADMIN_INVOICE");

        Assert.Equal(EvaluasiEngine.ADMIN_INVOICE_USERS.Count, result.Records.Count);
        Assert.DoesNotContain(result.Records, r => r.User == "OUTSIDER");
        Assert.Equal(1, result.GroupTotalNota);
        Assert.Equal(100, result.UserMetrics.Single(m => m.User == "AKBAR").ContributionPercentage);
    }

    [Fact]
    public void Evaluation_NullInputReturnsEmptyResult() =>
        Assert.Empty(EvaluasiEngine.BuildDailyEvaluation(null, null).Records);
}
