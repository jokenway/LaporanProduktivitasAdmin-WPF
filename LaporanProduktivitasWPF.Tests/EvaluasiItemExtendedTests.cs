using LaporanProduktivitasWPF.Models;

namespace LaporanProduktivitasWPF.Tests;

public class EvaluasiItemExtendedTests
{
    [Fact]
    public void Keterangan_NullValue_BecomesEmptyString()
    {
        var item = new EvaluasiItem();
        item.Keterangan = null;
        Assert.Equal("", item.Keterangan);
    }

    [Fact]
    public void Keterangan_SetValue_FiresPropertyChanged()
    {
        var item = new EvaluasiItem();
        string? changed = null;
        item.PropertyChanged += (s, e) => changed = e.PropertyName;

        item.Keterangan = "Test note";
        Assert.Equal("Keterangan", changed);
        Assert.Equal("Test note", item.Keterangan);
    }

    [Fact]
    public void NotaSalah_NegativeValue_ClampedToZero()
    {
        var item = new EvaluasiItem { TotalNota = 10 };
        item.NotaSalah = -5;
        Assert.Equal(0, item.NotaSalah);
    }

    [Fact]
    public void NotaSalah_Set_FiresPropertyChangedAndUpdatesAkurasi()
    {
        var item = new EvaluasiItem { TotalNota = 10 };
        var changedProps = new List<string>();
        item.PropertyChanged += (s, e) => changedProps.Add(e.PropertyName!);

        item.NotaSalah = 2;
        Assert.Contains("NotaSalah", changedProps);
        Assert.Contains("Akurasi", changedProps);
        Assert.Equal(80, item.Akurasi);
    }

    [Fact]
    public void JamDatang_InvalidInput_PreservesOldValue()
    {
        var item = new EvaluasiItem { JamDatang = "08:00" };
        item.JamDatang = "invalid!";
        Assert.Equal("08:00", item.JamDatang);
    }

    [Fact]
    public void JamPulang_InvalidInput_PreservesOldValue()
    {
        var item = new EvaluasiItem { JamPulang = "17:00" };
        item.JamPulang = "bad_time";
        Assert.Equal("17:00", item.JamPulang);
    }

    [Fact]
    public void UpdateDurasi_BothEmpty_ResultsDash()
    {
        var item = new EvaluasiItem { JamDatang = "", JamPulang = "" };
        Assert.Equal(0, item.DurasiMinutes);
        Assert.Equal("-", item.DurasiText);
    }

    [Fact]
    public void UpdateDurasi_CrossMidnight_CalculatesCorrectly()
    {
        var item = new EvaluasiItem { JamDatang = "23:00", JamPulang = "01:00" };
        Assert.Equal(120, item.DurasiMinutes); // 2 jam
        Assert.Equal("2 jam", item.DurasiText);
    }

    [Fact]
    public void UpdateAkurasi_ErrorsExceedTotal_ZeroPercent()
    {
        var item = new EvaluasiItem { TotalNota = 3 };
        item.NotaSalah = 10;
        Assert.Equal(0, item.Akurasi);
    }

    [Fact]
    public void UpdateAkurasi_ZeroTotalZeroErrors_100Percent()
    {
        var item = new EvaluasiItem { TotalNota = 0 };
        item.UpdateAkurasi();
        Assert.Equal(100, item.Akurasi);
    }

    [Fact]
    public void Key_CanBeSetAndRetrieved()
    {
        var item = new EvaluasiItem { Key = "AKBAR___2026-10-01" };
        Assert.Equal("AKBAR___2026-10-01", item.Key);
    }

    [Fact]
    public void PropertyChanged_MultipleSets_SameValue_OnlyFiresOnce()
    {
        var item = new EvaluasiItem();
        int count = 0;
        item.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == "NotaSalah") count++;
        };

        item.NotaSalah = 5;
        item.NotaSalah = 5; // same value, should not fire
        Assert.Equal(1, count);
    }

    [Theory]
    [InlineData("08:30", "17:30", 540, "9 jam")]
    [InlineData("06:00", "14:30", 510, "8 jam 30 mnt")]
    [InlineData("09:00", "09:30", 30, "0 jam 30 mnt")]
    public void UpdateDurasi_VariousTimeRanges(string start, string end, int expectedMin, string expectedText)
    {
        var item = new EvaluasiItem { JamDatang = start, JamPulang = end };
        Assert.Equal(expectedMin, item.DurasiMinutes);
        Assert.Equal(expectedText, item.DurasiText);
    }
}
