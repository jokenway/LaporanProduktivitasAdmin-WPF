using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using Npgsql;
using LaporanProduktivitasWPF.Models;

namespace LaporanProduktivitasWPF.Services
{
    /// <summary>
    /// Layanan database PostgreSQL — menggantikan file-based StorageService
    /// untuk operasi yang perlu dibagi antar user.
    /// </summary>
    public static class DatabaseService
    {
        private static string _connectionString;

        public static void Configure(AppConfig config)
        {
            _connectionString = config.BuildConnectionString();
        }

        // ─────────────────────────────────────────────────
        // Inisialisasi: buat tabel jika belum ada + seed users
        // ─────────────────────────────────────────────────

        public static async Task InitAsync()
        {
            using var conn = new NpgsqlConnection(_connectionString);
            await conn.OpenAsync();

            // Buat tabel jika belum ada
            await CreateTablesAsync(conn);

            // Seed users awal jika tabel kosong
            await SeedUsersAsync(conn);
        }

        private static async Task CreateTablesAsync(NpgsqlConnection conn)
        {
            string sql = @"
                CREATE TABLE IF NOT EXISTS app_users (
                    id            SERIAL PRIMARY KEY,
                    username      VARCHAR(50) NOT NULL UNIQUE,
                    password_hash VARCHAR(255) NOT NULL,
                    role          VARCHAR(20) NOT NULL,
                    is_active     BOOLEAN DEFAULT TRUE,
                    created_at    TIMESTAMP DEFAULT NOW()
                );

                CREATE TABLE IF NOT EXISTS month_files (
                    id          SERIAL PRIMARY KEY,
                    month_key   VARCHAR(20) NOT NULL UNIQUE,
                    label       VARCHAR(50),
                    month_name  VARCHAR(20),
                    year        INTEGER,
                    filename    VARCHAR(255),
                    sheet_name  VARCHAR(100),
                    row_count   INTEGER DEFAULT 0,
                    imported_at TIMESTAMP DEFAULT NOW(),
                    imported_by VARCHAR(50)
                );

                CREATE TABLE IF NOT EXISTS excel_rows (
                    id          BIGSERIAL PRIMARY KEY,
                    month_key   VARCHAR(20) NOT NULL REFERENCES month_files(month_key) ON DELETE CASCADE,
                    sheet_name  VARCHAR(100),
                    cells       JSONB NOT NULL
                );

                CREATE INDEX IF NOT EXISTS idx_excel_rows_month ON excel_rows(month_key, sheet_name);

                CREATE TABLE IF NOT EXISTS manual_logs (
                    id          SERIAL PRIMARY KEY,
                    log_key     VARCHAR(255) NOT NULL UNIQUE,
                    nota_salah  INTEGER DEFAULT 0,
                    jam_datang  VARCHAR(10) DEFAULT '',
                    jam_pulang  VARCHAR(10) DEFAULT '',
                    keterangan  TEXT DEFAULT '',
                    updated_at  TIMESTAMP DEFAULT NOW(),
                    updated_by  VARCHAR(50) DEFAULT ''
                );
            ";
            using var cmd = new NpgsqlCommand(sql, conn);
            await cmd.ExecuteNonQueryAsync();
        }

        private static async Task SeedUsersAsync(NpgsqlConnection conn)
        {
            // Cek apakah sudah ada user
            using var checkCmd = new NpgsqlCommand("SELECT COUNT(*) FROM app_users", conn);
            long count = (long)await checkCmd.ExecuteScalarAsync();
            if (count > 0) return;

            // Definisi user awal: username, password (nama+123), role
            var users = new[]
            {
                ("AKBAR",   "akbar123",   "admin"),
                ("DIDIN",   "didin123",   "staff"),
                ("JOE",     "joe123",     "staff"),
                ("RONI",    "roni123",    "staff"),
                ("NOVIANI", "noviani123", "staff"),
                ("STEVI",   "stevi123",   "staff"),
                ("GINA",    "gina123",    "staff"),
                ("ST",      "st123",      "viewer"),
            };

            string insertSql = "INSERT INTO app_users (username, password_hash, role) VALUES (@u, @h, @r)";
            foreach (var (username, password, role) in users)
            {
                string hash = BCrypt.Net.BCrypt.HashPassword(password);
                using var cmd = new NpgsqlCommand(insertSql, conn);
                cmd.Parameters.AddWithValue("u", username);
                cmd.Parameters.AddWithValue("h", hash);
                cmd.Parameters.AddWithValue("r", role);
                await cmd.ExecuteNonQueryAsync();
            }
        }

        // ─────────────────────────────────────────────────
        // Autentikasi
        // ─────────────────────────────────────────────────

        public static async Task<AppUser> AuthenticateAsync(string username, string password)
        {
            try
            {
                using var conn = new NpgsqlConnection(_connectionString);
                await conn.OpenAsync();

                string sql = "SELECT username, password_hash, role, is_active FROM app_users WHERE UPPER(username) = UPPER(@u)";
                using var cmd = new NpgsqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("u", username.Trim());
                using var reader = await cmd.ExecuteReaderAsync();

                if (!await reader.ReadAsync()) return null;

                string dbUsername = reader.GetString(0);
                string hash = reader.GetString(1);
                string role = reader.GetString(2);
                bool isActive = reader.GetBoolean(3);

                if (!isActive) return null;
                if (!BCrypt.Net.BCrypt.Verify(password, hash)) return null;

                return new AppUser { Username = dbUsername, Role = role };
            }
            catch { return null; }
        }

        // ─────────────────────────────────────────────────
        // Manajemen bulan
        // ─────────────────────────────────────────────────

        public static async Task<List<SavedFileMeta>> GetSavedMonthsAsync()
        {
            var result = new List<SavedFileMeta>();
            try
            {
                using var conn = new NpgsqlConnection(_connectionString);
                await conn.OpenAsync();

                string sql = "SELECT month_key, label, month_name, year, filename, sheet_name, row_count FROM month_files ORDER BY month_key";
                using var cmd = new NpgsqlCommand(sql, conn);
                using var reader = await cmd.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    result.Add(new SavedFileMeta
                    {
                        Key = reader.GetString(0),
                        Label = reader.IsDBNull(1) ? "" : reader.GetString(1),
                        MonthName = reader.IsDBNull(2) ? "" : reader.GetString(2),
                        Year = reader.IsDBNull(3) ? DateTime.Now.Year : reader.GetInt32(3),
                        FileName = reader.IsDBNull(4) ? "" : reader.GetString(4),
                        DefaultSheet = reader.IsDBNull(5) ? "" : reader.GetString(5),
                        RowCount = reader.IsDBNull(6) ? 0 : reader.GetInt32(6),
                    });
                }
            }
            catch { }
            return result;
        }

        /// <summary>
        /// Simpan data bulan ke PostgreSQL.
        /// Jika bulan sudah ada, hapus dulu lalu insert ulang.
        /// </summary>
        public static async Task SaveMonthDataAsync(
            string monthKey, string label, string fileName,
            string sheetName, int rowCount, string importedBy,
            List<RawRow> rows)
        {
            using var conn = new NpgsqlConnection(_connectionString);
            await conn.OpenAsync();
            using var tx = await conn.BeginTransactionAsync();
            try
            {
                // Hapus jika sudah ada (CASCADE hapus excel_rows)
                using (var delCmd = new NpgsqlCommand("DELETE FROM month_files WHERE month_key = @k", conn, tx))
                {
                    delCmd.Parameters.AddWithValue("k", monthKey);
                    await delCmd.ExecuteNonQueryAsync();
                }

                // Insert metadata
                string metaSql = @"
                    INSERT INTO month_files (month_key, label, month_name, year, filename, sheet_name, row_count, imported_by)
                    VALUES (@k, @lbl, @mn, @yr, @fn, @sn, @rc, @ib)";
                using (var metaCmd = new NpgsqlCommand(metaSql, conn, tx))
                {
                    metaCmd.Parameters.AddWithValue("k", monthKey);
                    metaCmd.Parameters.AddWithValue("lbl", label ?? monthKey);
                    metaCmd.Parameters.AddWithValue("mn", monthKey);
                    metaCmd.Parameters.AddWithValue("yr", DateTime.Now.Year);
                    metaCmd.Parameters.AddWithValue("fn", fileName ?? "");
                    metaCmd.Parameters.AddWithValue("sn", sheetName ?? "");
                    metaCmd.Parameters.AddWithValue("rc", rowCount);
                    metaCmd.Parameters.AddWithValue("ib", importedBy ?? "");
                    await metaCmd.ExecuteNonQueryAsync();
                }

                // Bulk insert rows menggunakan COPY untuk performa tinggi
                using (var writer = await conn.BeginBinaryImportAsync(
                    "COPY excel_rows (month_key, sheet_name, cells) FROM STDIN (FORMAT BINARY)"))
                {
                    foreach (var row in rows)
                    {
                        string json = JsonSerializer.Serialize(row.Cells);
                        await writer.StartRowAsync();
                        await writer.WriteAsync(monthKey, NpgsqlTypes.NpgsqlDbType.Varchar);
                        await writer.WriteAsync(sheetName ?? "", NpgsqlTypes.NpgsqlDbType.Varchar);
                        await writer.WriteAsync(json, NpgsqlTypes.NpgsqlDbType.Jsonb);
                    }
                    await writer.CompleteAsync();
                }

                await tx.CommitAsync();
            }
            catch
            {
                await tx.RollbackAsync();
                throw;
            }
        }

        /// <summary>
        /// Muat semua baris Excel untuk bulan tertentu dari PostgreSQL.
        /// </summary>
        public static async Task<List<RawRow>> LoadMonthRowsAsync(string monthKey, string sheetName)
        {
            var result = new List<RawRow>();
            try
            {
                using var conn = new NpgsqlConnection(_connectionString);
                await conn.OpenAsync();

                string sql = "SELECT cells FROM excel_rows WHERE month_key = @k AND sheet_name = @s ORDER BY id";
                using var cmd = new NpgsqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("k", monthKey);
                cmd.Parameters.AddWithValue("s", sheetName ?? "");
                using var reader = await cmd.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    string json = reader.GetString(0);
                    var cells = JsonSerializer.Deserialize<Dictionary<string, string>>(json);
                    result.Add(new RawRow { Cells = cells ?? new Dictionary<string, string>() });
                }
            }
            catch { }
            return result;
        }

        public static async Task DeleteMonthAsync(string monthKey)
        {
            try
            {
                using var conn = new NpgsqlConnection(_connectionString);
                await conn.OpenAsync();
                using var cmd = new NpgsqlCommand("DELETE FROM month_files WHERE month_key = @k", conn);
                cmd.Parameters.AddWithValue("k", monthKey);
                await cmd.ExecuteNonQueryAsync();
            }
            catch { }
        }

        // ─────────────────────────────────────────────────
        // Manual Logs
        // ─────────────────────────────────────────────────

        public static async Task<Dictionary<string, EvaluasiItem>> LoadManualLogsAsync()
        {
            var dict = new Dictionary<string, EvaluasiItem>(StringComparer.OrdinalIgnoreCase);
            try
            {
                using var conn = new NpgsqlConnection(_connectionString);
                await conn.OpenAsync();

                string sql = "SELECT log_key, nota_salah, jam_datang, jam_pulang, keterangan FROM manual_logs";
                using var cmd = new NpgsqlCommand(sql, conn);
                using var reader = await cmd.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    string key = reader.GetString(0);
                    dict[key] = new EvaluasiItem
                    {
                        Key = key,
                        NotaSalah = reader.IsDBNull(1) ? 0 : reader.GetInt32(1),
                        JamDatang = reader.IsDBNull(2) ? "" : reader.GetString(2),
                        JamPulang = reader.IsDBNull(3) ? "" : reader.GetString(3),
                        Keterangan = reader.IsDBNull(4) ? "" : reader.GetString(4),
                    };
                }
            }
            catch { }
            return dict;
        }

        public static async Task SaveManualLogAsync(EvaluasiItem item, string updatedBy)
        {
            try
            {
                if (item == null || string.IsNullOrEmpty(item.Key)) return;
                using var conn = new NpgsqlConnection(_connectionString);
                await conn.OpenAsync();

                bool isEmpty = item.NotaSalah <= 0 &&
                               string.IsNullOrEmpty(item.JamDatang) &&
                               string.IsNullOrEmpty(item.JamPulang) &&
                               string.IsNullOrEmpty(item.Keterangan);

                if (isEmpty)
                {
                    using var delCmd = new NpgsqlCommand("DELETE FROM manual_logs WHERE log_key = @k", conn);
                    delCmd.Parameters.AddWithValue("k", item.Key);
                    await delCmd.ExecuteNonQueryAsync();
                    return;
                }

                string sql = @"
                    INSERT INTO manual_logs (log_key, nota_salah, jam_datang, jam_pulang, keterangan, updated_by, updated_at)
                    VALUES (@k, @ns, @jd, @jp, @ket, @ub, NOW())
                    ON CONFLICT (log_key) DO UPDATE SET
                        nota_salah  = EXCLUDED.nota_salah,
                        jam_datang  = EXCLUDED.jam_datang,
                        jam_pulang  = EXCLUDED.jam_pulang,
                        keterangan  = EXCLUDED.keterangan,
                        updated_by  = EXCLUDED.updated_by,
                        updated_at  = NOW()";

                using var cmd = new NpgsqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("k", item.Key);
                cmd.Parameters.AddWithValue("ns", item.NotaSalah);
                cmd.Parameters.AddWithValue("jd", item.JamDatang ?? "");
                cmd.Parameters.AddWithValue("jp", item.JamPulang ?? "");
                cmd.Parameters.AddWithValue("ket", item.Keterangan ?? "");
                cmd.Parameters.AddWithValue("ub", updatedBy ?? "");
                await cmd.ExecuteNonQueryAsync();
            }
            catch { }
        }

        /// <summary>
        /// Simpan semua manual logs sekaligus (dipakai saat SaveChanges).
        /// </summary>
        public static async Task SaveAllManualLogsAsync(IEnumerable<EvaluasiItem> items, string updatedBy)
        {
            foreach (var item in items)
            {
                await SaveManualLogAsync(item, updatedBy);
            }
        }

        // ─────────────────────────────────────────────────
        // Test koneksi
        // ─────────────────────────────────────────────────

        public static async Task<bool> TestConnectionAsync(AppConfig config)
        {
            try
            {
                using var conn = new NpgsqlConnection(config.BuildConnectionString());
                await conn.OpenAsync();
                return true;
            }
            catch { return false; }
        }
    }
}
