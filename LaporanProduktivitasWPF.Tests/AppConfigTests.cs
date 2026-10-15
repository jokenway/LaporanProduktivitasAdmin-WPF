using LaporanProduktivitasWPF.Models;

namespace LaporanProduktivitasWPF.Tests;

public class AppConfigTests
{
    [Fact]
    public void IsComplete_ReturnsFalse_WhenHostIsEmpty()
    {
        var cfg = new AppConfig { Host = "", PgUser = "u", PgPassword = "p", Database = "db" };
        Assert.False(cfg.IsComplete);
    }

    [Fact]
    public void IsComplete_ReturnsFalse_WhenPgUserIsEmpty()
    {
        var cfg = new AppConfig { Host = "localhost", PgUser = "", PgPassword = "p", Database = "db" };
        Assert.False(cfg.IsComplete);
    }

    [Fact]
    public void IsComplete_ReturnsFalse_WhenPgPasswordIsEmpty()
    {
        var cfg = new AppConfig { Host = "localhost", PgUser = "u", PgPassword = "", Database = "db" };
        Assert.False(cfg.IsComplete);
    }

    [Fact]
    public void IsComplete_ReturnsFalse_WhenDatabaseIsEmpty()
    {
        var cfg = new AppConfig { Host = "localhost", PgUser = "u", PgPassword = "p", Database = "" };
        Assert.False(cfg.IsComplete);
    }

    [Fact]
    public void IsComplete_ReturnsFalse_WhenWhitespaceOnly()
    {
        var cfg = new AppConfig { Host = "  ", PgUser = "u", PgPassword = "p", Database = "db" };
        Assert.False(cfg.IsComplete);
    }

    [Fact]
    public void IsComplete_ReturnsTrue_WhenAllFieldsFilled()
    {
        var cfg = new AppConfig { Host = "localhost", PgUser = "u", PgPassword = "p", Database = "db" };
        Assert.True(cfg.IsComplete);
    }

    [Fact]
    public void BuildConnectionString_ContainsAllFields()
    {
        var cfg = new AppConfig
        {
            Host = "192.168.1.1",
            Port = 5433,
            Database = "mydb",
            PgUser = "admin",
            PgPassword = "secret"
        };

        string cs = cfg.BuildConnectionString();
        Assert.Contains("Host=192.168.1.1", cs);
        Assert.Contains("Port=5433", cs);
        Assert.Contains("Database=mydb", cs);
        Assert.Contains("Username=admin", cs);
        Assert.Contains("Password=secret", cs);
        Assert.Contains("Timeout=15", cs);
        Assert.Contains("CommandTimeout=120", cs);
    }

    [Fact]
    public void DefaultValues_AreCorrect()
    {
        var cfg = new AppConfig();
        Assert.Equal("", cfg.Host);
        Assert.Equal(5432, cfg.Port);
        Assert.Equal("laporan_produktivitas", cfg.Database);
        Assert.Equal("", cfg.PgUser);
        Assert.Equal("", cfg.PgPassword);
        Assert.Equal("", cfg.BootstrapAdminUsername);
        Assert.Equal("", cfg.BootstrapAdminPassword);
    }
}

