using LaporanProduktivitasWPF.Models;

namespace LaporanProduktivitasWPF.Tests;

public class AppUserTests
{
    [Theory]
    [InlineData("AKBAR", "admin", true, true, true)]
    [InlineData("st", "ADMIN", true, true, true)]
    [InlineData("ADMINLAIN", "admin", false, true, true)]
    [InlineData("AKBAR", "viewer", false, false, false)]
    [InlineData("ST", "staff", false, false, true)]
    [InlineData("DIDIN", "staff", false, false, true)]
    [InlineData("DIDIN", "viewer", false, false, false)]
    public void Permissions_RespectRoleEvenForSpecialUsernames(string name, string role,
        bool superUser, bool admin, bool attendance)
    {
        var user = new AppUser { Username = name, Role = role };
        Assert.Equal(superUser, user.IsSuperUser);
        Assert.Equal(admin, user.CanImport);
        Assert.Equal(admin, user.CanDeleteMonth);
        Assert.Equal(admin, user.CanEditAllEvaluation);
        Assert.Equal(admin, user.CanInputNotaSalah);
        Assert.Equal(admin, user.CanManageUsers);
        Assert.Equal(admin, user.CanCreateAdmin);
        Assert.Equal(attendance, user.CanInputAttendance);
        Assert.Equal(!attendance, user.IsReadOnlyAttendance);
    }
}
