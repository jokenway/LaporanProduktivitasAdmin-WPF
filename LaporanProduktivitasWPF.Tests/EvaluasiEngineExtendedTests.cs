using LaporanProduktivitasWPF.Models;
using LaporanProduktivitasWPF.Services;

namespace LaporanProduktivitasWPF.Tests;

public class EvaluasiEngineExtendedTests
{
    [Fact]
    public void DynamicUserList_AffectsInvoiceGroupResults()
    {
        // Simpan daftar asli
        var originalUsers = EvaluasiEngine.ADMIN_INVOICE_USERS;

        try
        {
            // Set daftar user kustom
            EvaluasiEngine.ADMIN_INVOICE_USERS = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "AKBAR", "NEWUSER"
            };

            var rows = new[]
            {
                TestRows.Create(user: "AKBAR"),
                TestRows.Create(user: "NEWUSER", invoice: "INV-NEW"),
                TestRows.Create(user: "OUTSIDER", invoice: "INV-OUT")
            };

            var result = EvaluasiEngine.BuildDailyEvaluation(rows, new(), userGroup: "ADMIN_INVOICE");

            // Hanya AKBAR dan NEWUSER yang masuk, OUTSIDER diexclude
            Assert.Equal(2, result.AvailableUsers.Count);
            Assert.Contains("AKBAR", result.AvailableUsers);
            Assert.Contains("NEWUSER", result.AvailableUsers);
            Assert.DoesNotContain(result.Records, r => r.User == "OUTSIDER");
            Assert.Equal(2, result.GroupTotalNota); // 1 dari AKBAR + 1 dari NEWUSER
        }
        finally
        {
            // Kembalikan ke daftar asli
            EvaluasiEngine.ADMIN_INVOICE_USERS = originalUsers;
        }
    }

    [Fact]
    public void Evaluation_EmptyRowList_ReturnsEmptyResult()
    {
        var result = EvaluasiEngine.BuildDailyEvaluation(new List<RawRow>(), new());
        Assert.Empty(result.Records);
        Assert.Empty(result.AvailableDates);
        Assert.Empty(result.AvailableUsers);
        Assert.Equal(0, result.Summary.TotalNota);
    }

    [Fact]
    public void Evaluation_RowsWithMissingUserId_AreSkipped()
    {
        var badRow = new RawRow();
        badRow.Cells["TGBON"] = "2026-10-01";
        badRow.Cells["NOINV"] = "INV-1";
        // USID is missing

        var result = EvaluasiEngine.BuildDailyEvaluation(new[] { badRow }, new());
        Assert.Empty(result.Records);
    }

    [Fact]
    public void Evaluation_RowsWithMissingDate_AreSkipped()
    {
        var badRow = new RawRow();
        badRow.Cells["USID"] = "AKBAR";
        badRow.Cells["NOINV"] = "INV-1";
        // TGBON is missing

        var result = EvaluasiEngine.BuildDailyEvaluation(new[] { badRow }, new());
        Assert.Empty(result.Records);
    }

    [Fact]
    public void Evaluation_DuplicateInvoicesSameUserDate_CountedOnce()
    {
        var rows = new[]
        {
            TestRows.Create(user: "AKBAR", date: "2026-10-01", invoice: "INV-1"),
            TestRows.Create(user: "AKBAR", date: "2026-10-01", invoice: "INV-1"), // duplikat
            TestRows.Create(user: "AKBAR", date: "2026-10-01", invoice: "inv-1"), // duplikat case-insensitive
        };

        var result = EvaluasiEngine.BuildDailyEvaluation(rows, new());
        var record = Assert.Single(result.Records);
        Assert.Equal(1, record.TotalNota); // hanya 1 invoice unik
    }

    [Fact]
    public void Evaluation_SameInvoiceDifferentUsers_CountedSeparately()
    {
        var rows = new[]
        {
            TestRows.Create(user: "AKBAR", date: "2026-10-01", invoice: "INV-SHARED"),
            TestRows.Create(user: "DIDIN", date: "2026-10-01", invoice: "INV-SHARED"),
        };

        var result = EvaluasiEngine.BuildDailyEvaluation(rows, new());
        Assert.Equal(2, result.Records.Count);
        Assert.All(result.Records, r => Assert.Equal(1, r.TotalNota));
    }

    [Fact]
    public void Evaluation_DateFilter_OnlyShowsMatchingDate()
    {
        var rows = new[]
        {
            TestRows.Create(date: "2026-10-01"),
            TestRows.Create(date: "2026-10-02", invoice: "INV-2"),
        };

        var result = EvaluasiEngine.BuildDailyEvaluation(rows, new(), selectedDate: "2026-10-01");
        Assert.Single(result.Records);
        Assert.Equal("2026-10-01", result.Records[0].Tanggal);
    }

    [Fact]
    public void Evaluation_UserFilter_OnlyShowsMatchingUser()
    {
        var rows = new[]
        {
            TestRows.Create(user: "AKBAR"),
            TestRows.Create(user: "DIDIN", invoice: "INV-2"),
        };

        var result = EvaluasiEngine.BuildDailyEvaluation(rows, new(), selectedUser: "DIDIN");
        Assert.Single(result.Records);
        Assert.Equal("DIDIN", result.Records[0].User);
    }

    [Fact]
    public void Evaluation_SundayDates_AreExcluded()
    {
        // 2026-10-04 is a Sunday
        Assert.True(EvaluasiEngine.IsSunday("2026-10-04"));
        Assert.False(EvaluasiEngine.IsSunday("2026-10-01")); // Thursday
    }

    [Fact]
    public void Evaluation_ManualLogs_WithNullManualLogs_DoesNotThrow()
    {
        var rows = new[] { TestRows.Create() };
        var result = EvaluasiEngine.BuildDailyEvaluation(rows, null);
        Assert.Single(result.Records);
    }

    [Fact]
    public void Evaluation_UserMetrics_ContributionPercentagesSum100()
    {
        var originalUsers = EvaluasiEngine.ADMIN_INVOICE_USERS;
        try
        {
            EvaluasiEngine.ADMIN_INVOICE_USERS = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "AKBAR", "DIDIN"
            };

            var rows = new[]
            {
                TestRows.Create(user: "AKBAR", invoice: "INV-1"),
                TestRows.Create(user: "AKBAR", invoice: "INV-2"),
                TestRows.Create(user: "DIDIN", invoice: "INV-3"),
            };

            var result = EvaluasiEngine.BuildDailyEvaluation(rows, new(), userGroup: "ADMIN_INVOICE");
            double totalPct = result.UserMetrics.Sum(m => m.ContributionPercentage);
            Assert.Equal(100, totalPct, precision: 0);
        }
        finally
        {
            EvaluasiEngine.ADMIN_INVOICE_USERS = originalUsers;
        }
    }

    [Fact]
    public void Evaluation_Summary_CalculatesCorrectTotalJamKerja()
    {
        var logs = new Dictionary<string, EvaluasiItem>
        {
            ["AKBAR___2026-10-01"] = new EvaluasiItem { JamDatang = "08:00", JamPulang = "17:00" },
        };
        var rows = new[] { TestRows.Create() };
        var result = EvaluasiEngine.BuildDailyEvaluation(rows, logs);

        Assert.Equal(540, result.Summary.TotalJamKerjaMinutes); // 9 jam = 540 menit
        Assert.Equal("9 jam", result.Summary.TotalJamKerjaText);
    }

    [Fact]
    public void Evaluation_RecordNumbering_IsSequential()
    {
        var rows = new[]
        {
            TestRows.Create(user: "AKBAR", date: "2026-10-01"),
            TestRows.Create(user: "DIDIN", date: "2026-10-01", invoice: "INV-2"),
            TestRows.Create(user: "AKBAR", date: "2026-10-02", invoice: "INV-3"),
        };

        var result = EvaluasiEngine.BuildDailyEvaluation(rows, new());
        var nos = result.Records.Select(r => r.No).ToList();
        Assert.Equal(Enumerable.Range(1, result.Records.Count), nos);
    }
}

