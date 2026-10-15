using LaporanProduktivitasWPF.Models;

namespace LaporanProduktivitasWPF.Tests;

public class RawRowTests
{
    [Fact]
    public void Get_ReturnsValue_WhenKeyExists()
    {
        var row = new RawRow();
        row.Cells["USID"] = "AKBAR";
        Assert.Equal("AKBAR", row.Get("USID"));
    }

    [Fact]
    public void Get_ReturnsEmpty_WhenKeyDoesNotExist()
    {
        var row = new RawRow();
        Assert.Equal(string.Empty, row.Get("MISSING"));
    }

    [Fact]
    public void Get_ReturnsEmpty_WhenKeyIsNull()
    {
        var row = new RawRow();
        Assert.Equal(string.Empty, row.Get(null));
    }

    [Fact]
    public void Get_ReturnsEmpty_WhenKeyIsEmpty()
    {
        var row = new RawRow();
        Assert.Equal(string.Empty, row.Get(""));
    }

    [Fact]
    public void Get_ReturnsEmpty_WhenValueIsNull()
    {
        var row = new RawRow();
        row.Cells["KEY"] = null!;
        Assert.Equal(string.Empty, row.Get("KEY"));
    }

    [Fact]
    public void Get_IsCaseInsensitive()
    {
        var row = new RawRow();
        row.Cells["usid"] = "DIDIN";
        Assert.Equal("DIDIN", row.Get("USID"));
        Assert.Equal("DIDIN", row.Get("Usid"));
    }

    [Fact]
    public void Cells_DefaultsToEmptyDictionary()
    {
        var row = new RawRow();
        Assert.NotNull(row.Cells);
        Assert.Empty(row.Cells);
    }
}

