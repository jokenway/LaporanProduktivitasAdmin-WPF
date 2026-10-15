using LaporanProduktivitasWPF.Services;

namespace LaporanProduktivitasWPF.Tests;

public class ExactPivotEngineTests
{
    [Fact]
    public void Pivot_GroupsLinesAndKeepsDateAndJenisDimensionsSeparate()
    {
        var result = ExactPivotEngine.BuildExactPivot(new[]
        {
            TestRows.Create(netto: "100.25"), TestRows.Create(netto: "50.75"),
            TestRows.Create(date: "2026-10-02", netto: "20"),
            TestRows.Create(jenis: "B", netto: "30")
        });

        Assert.Equal(3, result.Items.Count);
        Assert.Equal(4, result.TotalCountJMDOS);
        Assert.Equal(201, result.TotalSumNetto);
        Assert.Equal(1, result.GrandTotalNotaKeseluruhan);
        Assert.Equal(151, result.Items.Single(i => i.TGBON == "2026-10-01" && i.JENIS_B == "A").SumNetto);
        Assert.Equal(new[] { 1, 2, 3 }, result.Items.Select(i => i.No));
    }

    [Fact]
    public void Pivot_FiltersWithoutChangingGrandTotalsOrOptions()
    {
        var result = ExactPivotEngine.BuildExactPivot(new[]
        {
            TestRows.Create(), TestRows.Create(user: "DIDIN", invoice: "INV-2", jenis: "B", netto: "200")
        }, selectedUser: "akbar", selectedDate: "2026-10-01", selectedJenisB: "a", searchQuery: " inv-1 ");

        Assert.Single(result.Items);
        Assert.Equal(100, result.TotalSumNetto);
        Assert.Equal(300, result.GrandTotalNettoKeseluruhan);
        Assert.Equal(2, result.GrandTotalNotaKeseluruhan);
        Assert.Equal(2, result.AvailableUsers.Count);
    }

    [Fact]
    public void Pivot_UsesAlternativeHeadersForGroupingAndDimensions()
    {
        var first = TestRows.Create();
        var second = TestRows.Create(date: "2026-10-02", jenis: "B");
        foreach (var row in new[] { first, second })
        {
            foreach (var (primary, alias) in new[]
            {
                ("USID", "User"), ("TGBON", "Tanggal"), ("NOINV", "No Invoice"), ("JENIS_B", "JENIS")
            })
            {
                row.Cells[alias] = row.Get(primary);
                row.Cells.Remove(primary);
            }
        }

        var result = ExactPivotEngine.BuildExactPivot(new[] { first, second });

        Assert.Equal(2, result.Items.Count);
        Assert.All(result.Items, item =>
        {
            Assert.Equal("AKBAR", item.USID);
            Assert.Equal("INV-1", item.NOINV);
        });
        Assert.Contains(result.Items, i => i.TGBON == "2026-10-02" && i.JENIS_B == "B");
    }

    [Theory]
    [InlineData("akbar")]
    [InlineData("inv-1")]
    public void Pivot_SearchesAlternativeHeaders(string query)
    {
        var row = TestRows.Create();
        row.Cells["User"] = row.Get("USID");
        row.Cells["No Invoice"] = row.Get("NOINV");
        row.Cells.Remove("USID");
        row.Cells.Remove("NOINV");
        Assert.Single(ExactPivotEngine.BuildExactPivot(new[] { row }, searchQuery: query).Items);
    }

    [Fact]
    public void Pivot_NullInputReturnsEmptyResult() =>
        Assert.Empty(ExactPivotEngine.BuildExactPivot(null).Items);
}
