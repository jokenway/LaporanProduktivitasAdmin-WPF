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
        public bool CanImport => string.Equals(Username, "AKBAR", StringComparison.OrdinalIgnoreCase) || string.Equals(Role, "admin", StringComparison.OrdinalIgnoreCase);

        /// <summary>HANYA user AKBAR yang bisa menginput/mengedit Nota Salah.</summary>
        public bool CanInputNotaSalah => string.Equals(Username, "AKBAR", StringComparison.OrdinalIgnoreCase);
        public bool IsReadOnlyNotaSalah => !CanInputNotaSalah;

        /// <summary>Staff dan AKBAR bisa input Jam Datang, Jam Pulang, dan Keterangan. Viewer (ST) tidak bisa.</summary>
        public bool CanInputAttendance => !string.Equals(Role, "viewer", StringComparison.OrdinalIgnoreCase);
        public bool IsReadOnlyAttendance => !CanInputAttendance;

        /// <summary>Semua role bisa melihat semua tab.</summary>
        public bool CanViewAll => true;

        /// <summary>Hanya ST dan AKBAR yang bisa menambah user.</summary>
        public bool CanManageUsers => string.Equals(Username, "AKBAR", StringComparison.OrdinalIgnoreCase) || string.Equals(Username, "ST", StringComparison.OrdinalIgnoreCase);

        /// <summary>Hanya AKBAR yang bisa membuat akun dengan role admin.</summary>
        public bool CanCreateAdmin => string.Equals(Username, "AKBAR", StringComparison.OrdinalIgnoreCase);

        public override string ToString() => $"{Username} ({Role})";
    }

    /// <summary>
    /// Konfigurasi koneksi database, disimpan lokal di %AppData%.
    /// </summary>
    public class AppConfig
    {
        public string Host { get; set; } = "127.0.0.1";
        public int Port { get; set; } = 5432;
        public string Database { get; set; } = "laporan_produktivitas";
        public string PgUser { get; set; } = "postgres";
        public string PgPassword { get; set; } = "admin";

        public string BuildConnectionString()
        {
            return $"Host={Host};Port={Port};Database={Database};Username={PgUser};Password={PgPassword};Timeout=15;CommandTimeout=120;";
        }
    }
}
