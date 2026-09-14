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

        /// <summary>Hanya admin (AKBAR) yang bisa mengimport file Excel.</summary>
        public bool CanImport => string.Equals(Role, "admin", StringComparison.OrdinalIgnoreCase);

        /// <summary>Hanya admin (AKBAR) yang bisa menginput Nota Salah.</summary>
        public bool CanInputNotaSalah => string.Equals(Role, "admin", StringComparison.OrdinalIgnoreCase);

        /// <summary>Staff dan admin bisa input Jam Datang/Pulang dan Keterangan.</summary>
        public bool CanInputAttendance => !string.Equals(Role, "viewer", StringComparison.OrdinalIgnoreCase);

        /// <summary>Semua role bisa melihat semua tab.</summary>
        public bool CanViewAll => true;

        public override string ToString() => $"{Username} ({Role})";
    }

    /// <summary>
    /// Konfigurasi koneksi database, disimpan lokal di %AppData%.
    /// </summary>
    public class AppConfig
    {
        public string Host { get; set; } = "192.168.179.24";
        public int Port { get; set; } = 5432;
        public string Database { get; set; } = "laporan_produktivitas";
        public string PgUser { get; set; } = "postgres";
        public string PgPassword { get; set; } = "password";

        public string BuildConnectionString()
        {
            return $"Host={Host};Port={Port};Database={Database};Username={PgUser};Password={PgPassword};Timeout=15;CommandTimeout=120;";
        }
    }
}
