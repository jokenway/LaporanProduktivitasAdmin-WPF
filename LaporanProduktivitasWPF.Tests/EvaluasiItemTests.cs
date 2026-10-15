using LaporanProduktivitasWPF.Models;

namespace LaporanProduktivitasWPF.Tests;

public class EvaluasiItemTests
{
    [Theory]
    [InlineData("08:00", "08:00")]
    [InlineData("8:0", "08:00")]
    [InlineData("8.30", "08:30")]
    [InlineData("0800", "08:00")]
    [InlineData("830", "08:30")]
    [InlineData("8", "08:00")]
    [InlineData("17", "17:00")]
    [InlineData(" 1730 ", "17:30")]
    [InlineData("0", "00:00")]
    [InlineData("2359", "23:59")]
    [InlineData("", "")]
    [InlineData(null, "")]
    public void ParseTime_NormalizesSupportedInput(string? input, string expected)
    {
        Assert.True(EvaluasiItem.TryParseTime(input, out var formatted));
        Assert.Equal(expected, formatted);
    }

    [Theory]
    [InlineData("-8")]
    [InlineData("+8")]
    [InlineData("abc8")]
    [InlineData("08:00xyz")]
    [InlineData("8::30")]
    [InlineData("8:000")]
    [InlineData("24:00")]
    [InlineData("1260")]
    [InlineData("99999")]
    public void ParseTime_RejectsMalformedInput(string input)
    {
        Assert.False(EvaluasiItem.TryParseTime(input, out var formatted));
        Assert.Equal("", formatted);
    }

    [Fact]
    public void InvalidAttendanceEdit_PreservesPreviousValueAndDuration()
    {
        var item = new EvaluasiItem { JamDatang = "8", JamPulang = "17" };
        item.JamDatang = "-9";
        Assert.Equal("08:00", item.JamDatang);
        Assert.Equal(540, item.DurasiMinutes);
    }

    [Theory]
    [InlineData("8", "17", 540, "9 jam")]
    [InlineData("22:30", "06:00", 450, "7 jam 30 mnt")]
    [InlineData("8", "8", 0, "0 jam")]
    [InlineData("8", "", 0, "-")]
    public void Attendance_ComputesDuration(string start, string end, int minutes, string text)
    {
        var item = new EvaluasiItem { JamDatang = start, JamPulang = end };
        Assert.Equal(minutes, item.DurasiMinutes);
        Assert.Equal(text, item.DurasiText);
    }

    [Theory]
    [InlineData(10, 2, 80)]
    [InlineData(0, 0, 100)]
    [InlineData(2, 5, 0)]
    [InlineData(10, -2, 100)]
    public void Accuracy_HandlesErrorsAndEmptyWorkload(int total, int errors, double expected)
    {
        var item = new EvaluasiItem { TotalNota = total, NotaSalah = errors };
        item.UpdateAkurasi();
        Assert.Equal(expected, item.Akurasi);
        Assert.True(item.NotaSalah >= 0);
    }
}
