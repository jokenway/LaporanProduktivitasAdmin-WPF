using System;

namespace LaporanProduktivitasWPF.Models
{
    /// <summary>
    /// User yang sedang login ke aplikasi.
    /// </summary>
    public class AppUser
    {
        public string Username { get; set; }
        public string Role { get; set; }   // "admin" | "staff" | "viewer"

        public bool IsSuperUser => IsSuperUserName(Username);

        public static bool IsSuperUserName(string username) =>
            string.Equals(username, "AKBAR", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(username, "ST", StringComparison.OrdinalIgnoreCase);

        /// <summary>Super user atau admin dapat mengimport file Excel.</summary>
        public bool CanImport => IsSuperUser || string.Equals(Role, "admin", StringComparison.OrdinalIgnoreCase);
        /// <summary>Hanya administrator yang dapat menghapus data bulan dari database.</summary>
        public bool CanDeleteMonth => CanImport;


        /// <summary>Hanya super user yang bisa menginput/mengedit Nota Salah.</summary>
        public bool CanInputNotaSalah => IsSuperUser;
        public bool IsReadOnlyNotaSalah => !CanInputNotaSalah;

        /// <summary>Super user dan staff dapat menginput jam kerja dan keterangan.</summary>
        public bool CanInputAttendance => IsSuperUser || !string.Equals(Role, "viewer", StringComparison.OrdinalIgnoreCase);
        public bool IsReadOnlyAttendance => !CanInputAttendance;

        /// <summary>Semua role bisa melihat semua tab.</summary>
        public bool CanViewAll => true;

        /// <summary>Super user dan administrator dapat menambah user.</summary>
        public bool CanManageUsers => IsSuperUser || string.Equals(Role, "admin", StringComparison.OrdinalIgnoreCase);

        /// <summary>Super user dan administrator dapat membuat akun administrator.</summary>
        public bool CanCreateAdmin => CanManageUsers;

        public override string ToString() => $"{Username} ({Role})";
    }

    /// <summary>
    /// Konfigurasi koneksi database, disimpan lokal di %AppData%.
    /// </summary>
    public class AppConfig
    {
        public string Host { get; set; } = "";
        public int Port { get; set; } = 5432;
        public string Database { get; set; } = "laporan_produktivitas";
        public string PgUser { get; set; } = "";
        public string PgPassword { get; set; } = "";
        public string BootstrapAdminUsername { get; set; } = "";
        public string BootstrapAdminPassword { get; set; } = "";

        public bool IsComplete =>
            !string.IsNullOrWhiteSpace(Host) &&
            !string.IsNullOrWhiteSpace(Database) &&
            !string.IsNullOrWhiteSpace(PgUser) &&
            !string.IsNullOrWhiteSpace(PgPassword);

        public string BuildConnectionString()
        {
            return $"Host={Host};Port={Port};Database={Database};Username={PgUser};Password={PgPassword};Timeout=15;CommandTimeout=120;";
        }
    }
}
