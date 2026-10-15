using LaporanProduktivitasWPF.Models;

namespace LaporanProduktivitasWPF.Tests;

public class AppUserExtendedTests
{
    [Theory]
    [InlineData("AKBAR")]
    [InlineData("akbar")]
    [InlineData("Akbar")]
    [InlineData("ST")]
    [InlineData("st")]
    [InlineData("St")]
    public void IsSuperUserName_CaseInsensitive(string name)
    {
        Assert.True(AppUser.IsSuperUserName(name));
    }

    [Theory]
    [InlineData("DIDIN")]
    [InlineData("JOE")]
    [InlineData("RONI")]
    [InlineData("NOVIANI")]
    [InlineData("")]
    public void IsSuperUserName_ReturnsFalse_ForNonSuperUsers(string name)
    {
        Assert.False(AppUser.IsSuperUserName(name));
    }

    [Fact]
    public void SuperUserWithViewerRole_HasNoSpecialPermissions()
    {
        var user = new AppUser { Username = "AKBAR", Role = "viewer" };
        Assert.False(user.IsSuperUser);
        Assert.False(user.CanImport);
        Assert.False(user.CanDeleteMonth);
        Assert.False(user.CanManageUsers);
        Assert.False(user.CanCreateAdmin);
        Assert.False(user.CanInputNotaSalah);
        Assert.False(user.CanInputAttendance);
        Assert.True(user.IsReadOnlyAttendance);
    }

    [Fact]
    public void StaffUser_CanInputAttendance_ButNotImport()
    {
        var user = new AppUser { Username = "DIDIN", Role = "staff" };
        Assert.True(user.CanInputAttendance);
        Assert.False(user.IsReadOnlyAttendance);
        Assert.False(user.CanImport);
        Assert.False(user.CanManageUsers);
    }

    [Fact]
    public void AdminNonSuperUser_HasAdminPermissions()
    {
        var user = new AppUser { Username = "ADMINLAIN", Role = "admin" };
        Assert.False(user.IsSuperUser);
        Assert.True(user.CanImport);
        Assert.True(user.CanDeleteMonth);
        Assert.True(user.CanManageUsers);
        Assert.True(user.CanCreateAdmin);
        Assert.True(user.CanInputNotaSalah);
        Assert.True(user.CanInputAttendance);
    }

    [Fact]
    public void AllUsers_CanViewAll()
    {
        Assert.True(new AppUser { Username = "AKBAR", Role = "admin" }.CanViewAll);
        Assert.True(new AppUser { Username = "DIDIN", Role = "staff" }.CanViewAll);
        Assert.True(new AppUser { Username = "VISITOR", Role = "viewer" }.CanViewAll);
    }

    [Fact]
    public void ToString_FormatsCorrectly()
    {
        var user = new AppUser { Username = "AKBAR", Role = "admin" };
        Assert.Equal("AKBAR (admin)", user.ToString());
    }

    [Fact]
    public void ReadOnlyNotaSalah_IsInverseOfCanInputNotaSalah()
    {
        var admin = new AppUser { Username = "AKBAR", Role = "admin" };
        Assert.Equal(!admin.CanInputNotaSalah, admin.IsReadOnlyNotaSalah);

        var staff = new AppUser { Username = "JOE", Role = "staff" };
        Assert.Equal(!staff.CanInputNotaSalah, staff.IsReadOnlyNotaSalah);

        var viewer = new AppUser { Username = "GUEST", Role = "viewer" };
        Assert.Equal(!viewer.CanInputNotaSalah, viewer.IsReadOnlyNotaSalah);
    }
}

