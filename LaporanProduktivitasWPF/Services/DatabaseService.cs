using System;
using System.Collections.Generic;
using System.Linq;
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
        private static AppConfig _config;

        public static void Configure(AppConfig config)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _connectionString = _config.BuildConnectionString();
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
            await MigrateLegacyManualLogMonthsAsync(conn);
            await AddManualLogForeignKeyAsync(conn);

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
                    month_key   VARCHAR(20) NOT NULL DEFAULT '',
                    log_key     VARCHAR(255) NOT NULL UNIQUE,
                    nota_salah  INTEGER DEFAULT 0,
                    jam_datang  VARCHAR(10) DEFAULT '',
                    jam_pulang  VARCHAR(10) DEFAULT '',
                    keterangan  TEXT DEFAULT '',
                    updated_at  TIMESTAMP DEFAULT NOW(),
                    updated_by  VARCHAR(50) DEFAULT ''
                );

                -- Migrasi instalasi lama: input manual harus dipisahkan per bulan.
                ALTER TABLE manual_logs ADD COLUMN IF NOT EXISTS month_key VARCHAR(20) NOT NULL DEFAULT '';
                ALTER TABLE manual_logs DROP CONSTRAINT IF EXISTS manual_logs_log_key_key;
                CREATE UNIQUE INDEX IF NOT EXISTS uq_manual_logs_month_key_log_key ON manual_logs(month_key, log_key);
            ";
            using var cmd = new NpgsqlCommand(sql, conn);
            await cmd.ExecuteNonQueryAsync();
        }

        private static async Task SeedUsersAsync(NpgsqlConnection conn)
        {
            using var checkCmd = new NpgsqlCommand("SELECT COUNT(*) FROM app_users", conn);
            long count = (long)await checkCmd.ExecuteScalarAsync();
            if (count > 0) return;

            if (_config == null || string.IsNullOrWhiteSpace(_config.BootstrapAdminUsername) ||
                string.IsNullOrWhiteSpace(_config.BootstrapAdminPassword) || _config.BootstrapAdminPassword.Length < 8)
            {
                throw new InvalidOperationException(
                    "Database belum memiliki pengguna. Isi BootstrapAdminUsername dan BootstrapAdminPassword (minimal 8 karakter) di config.json untuk membuat admin pertama.");
            }

            string username = _config.BootstrapAdminUsername.Trim();
            string passwordHash = BCrypt.Net.BCrypt.HashPassword(_config.BootstrapAdminPassword);
            const string insertSql = "INSERT INTO app_users (username, password_hash, role) VALUES (@u, @h, 'admin')";
            using var cmd = new NpgsqlCommand(insertSql, conn);
            cmd.Parameters.AddWithValue("u", username);
            cmd.Parameters.AddWithValue("h", passwordHash);
            await cmd.ExecuteNonQueryAsync();
        }

        private static async Task MigrateLegacyManualLogMonthsAsync(NpgsqlConnection conn)
        {
            var existingMonthKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            using (var monthsCmd = new NpgsqlCommand("SELECT month_key FROM month_files", conn))
            using (var monthsReader = await monthsCmd.ExecuteReaderAsync())
                while (await monthsReader.ReadAsync()) existingMonthKeys.Add(monthsReader.GetString(0));

            var updates = new List<(int Id, string MonthKey)>();
            using (var cmd = new NpgsqlCommand("SELECT id, log_key FROM manual_logs WHERE month_key = ''", conn))
            using (var reader = await cmd.ExecuteReaderAsync())
            {
                while (await reader.ReadAsync())
                {
                    string key = reader.GetString(1);
                    string[] parts = key.Split(new[] { "___" }, StringSplitOptions.None);
                    if (parts.Length < 2) continue;
                    var date = EvaluasiEngine.TryParseDate(parts[parts.Length - 1]);
                    if (date.HasValue)
                    {
                        string modernKey = string.Format("{0:D4}-{1:D2}", date.Value.Year, date.Value.Month);
                        // Instalasi lama menyimpan bulan sebagai nama saja (mis. JULI).
                        // Pertahankan referensi itu agar log historis tidak terhapus saat FK ditambahkan.
                        string legacyKey = ExcelService.INDONESIAN_MONTHS[date.Value.Month - 1];
                        updates.Add((reader.GetInt32(0), existingMonthKeys.Contains(modernKey) ? modernKey : legacyKey));
                    }
                }
            }

            foreach (var update in updates)
            {
                using var cmd = new NpgsqlCommand("UPDATE manual_logs SET month_key = @monthKey WHERE id = @id", conn);
                cmd.Parameters.AddWithValue("monthKey", update.MonthKey);
                cmd.Parameters.AddWithValue("id", update.Id);
                await cmd.ExecuteNonQueryAsync();
            }
        }

        private static async Task AddManualLogForeignKeyAsync(NpgsqlConnection conn)
        {
            // Log untuk bulan yang sudah tidak ada tidak lagi dapat ditampilkan dan
            // harus dibuang sebelum FK/cascade dapat menjaga integritas berikutnya.
            using (var cleanup = new NpgsqlCommand("DELETE FROM manual_logs ml WHERE NOT EXISTS (SELECT 1 FROM month_files mf WHERE mf.month_key = ml.month_key)", conn))
                await cleanup.ExecuteNonQueryAsync();
            using var constraint = new NpgsqlCommand(@"DO $$ BEGIN
                ALTER TABLE manual_logs ADD CONSTRAINT fk_manual_logs_month
                FOREIGN KEY (month_key) REFERENCES month_files(month_key) ON DELETE CASCADE;
            EXCEPTION WHEN duplicate_object THEN NULL; END $$;", conn);
            await constraint.ExecuteNonQueryAsync();
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
            catch (Exception ex) { AppLogger.LogError("Autentikasi database", ex); return null; }
        }

        // ─────────────────────────────────────────────────
        // Manajemen User
        // ─────────────────────────────────────────────────

        public static async Task<List<string>> GetAllStaffAndAdminUsersAsync()
        {
            var list = new List<string>();
            try
            {
                using var conn = new NpgsqlConnection(_connectionString);
                await conn.OpenAsync();
                string sql = "SELECT username FROM app_users WHERE role IN ('admin', 'staff') AND is_active = true ORDER BY username";
                using var cmd = new NpgsqlCommand(sql, conn);
                using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    list.Add(reader.GetString(0).ToUpperInvariant());
                }
            }
            catch (Exception ex) { AppLogger.LogError("Operasi database", ex); }
            return list;
        }

        public static async Task<bool> AddUserAsync(string username, string password, string role, AppUser createdBy)
        {
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrEmpty(password))
                return false;
            username = username.Trim();
            if (username.Length > 50 || password.Length < 8)
                return false;
            foreach (char character in username)
            {
                if (!char.IsLetterOrDigit(character) && character != '_' && character != '-')
                    return false;
            }
            if (createdBy == null || !createdBy.CanManageUsers)
                return false;

            string normalizedRole = (role ?? string.Empty).Trim().ToLowerInvariant();
            if (normalizedRole != "admin" && normalizedRole != "staff" && normalizedRole != "viewer")
                return false;
            if (normalizedRole == "admin" && !createdBy.CanCreateAdmin)
                return false;

            try
            {
                using var conn = new NpgsqlConnection(_connectionString);
                await conn.OpenAsync();
                string hash = BCrypt.Net.BCrypt.HashPassword(password);
                string sql = "INSERT INTO app_users (username, password_hash, role) VALUES (@u, @p, @r)";
                using var cmd = new NpgsqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("u", username.Trim());
                cmd.Parameters.AddWithValue("p", hash);
                cmd.Parameters.AddWithValue("r", normalizedRole);
                await cmd.ExecuteNonQueryAsync();
                return true;
            }
            catch (Exception ex) { AppLogger.LogError("Operasi database", ex); return false; }
        }

        public static async Task<bool> UpdateUserPasswordAsync(string username, string newPassword)
        {
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrEmpty(newPassword) || newPassword.Length < 8)
                return false;
            try
            {
                using var conn = new NpgsqlConnection(_connectionString);
                await conn.OpenAsync();
                string hash = BCrypt.Net.BCrypt.HashPassword(newPassword);
                string sql = "UPDATE app_users SET password_hash = @p WHERE UPPER(username) = UPPER(@u)";
                using var cmd = new NpgsqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("u", username.Trim());
                cmd.Parameters.AddWithValue("p", hash);
                int rows = await cmd.ExecuteNonQueryAsync();
                return rows > 0;
            }
            catch (Exception ex) { AppLogger.LogError("Operasi database", ex); return false; }
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
            catch (Exception ex) { AppLogger.LogError("Operasi database", ex); }
            return result;
        }

        /// <summary>
        /// Simpan data bulan ke PostgreSQL.
        /// Jika bulan sudah ada, hapus dulu lalu insert ulang.
        /// </summary>
        public static async Task SaveMonthDataAsync(
            string monthKey, string label, string fileName,
            string defaultSheetName, string monthName, int year, int rowCount, string importedBy,
            IEnumerable<ExcelSheetData> sheets)
        {
            using var conn = new NpgsqlConnection(_connectionString);
            await conn.OpenAsync();
            using var tx = conn.BeginTransaction();
            try
            {
                using (var lockCmd = new NpgsqlCommand("SELECT pg_advisory_xact_lock(hashtext(@lockKey))", conn, tx))
                {
                    lockCmd.Parameters.AddWithValue("lockKey", "month:" + monthKey);
                    await lockCmd.ExecuteNonQueryAsync();
                }
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
                    metaCmd.Parameters.AddWithValue("mn", monthName ?? "");
                    metaCmd.Parameters.AddWithValue("yr", year);
                    metaCmd.Parameters.AddWithValue("fn", fileName ?? "");
                    metaCmd.Parameters.AddWithValue("sn", defaultSheetName ?? "");
                    metaCmd.Parameters.AddWithValue("rc", rowCount);
                    metaCmd.Parameters.AddWithValue("ib", importedBy ?? "");
                    await metaCmd.ExecuteNonQueryAsync();
                }

                // Bulk insert rows menggunakan COPY untuk performa tinggi
                using (var writer = await conn.BeginBinaryImportAsync(
                    "COPY excel_rows (month_key, sheet_name, cells) FROM STDIN (FORMAT BINARY)"))
                {
                    foreach (var sheet in sheets ?? Enumerable.Empty<ExcelSheetData>())
                    {
                        foreach (var row in sheet.Rows ?? new List<RawRow>())
                        {
                            string json = JsonSerializer.Serialize(row.Cells);
                            await writer.StartRowAsync();
                            await writer.WriteAsync(monthKey, NpgsqlTypes.NpgsqlDbType.Varchar);
                            await writer.WriteAsync(sheet.SheetName ?? "", NpgsqlTypes.NpgsqlDbType.Varchar);
                            await writer.WriteAsync(json, NpgsqlTypes.NpgsqlDbType.Jsonb);
                        }
                    }
                    await writer.CompleteAsync();
                }

                tx.Commit();
            }
            catch
            {
                tx.Rollback();
                throw;
            }
        }

        public static async Task<List<string>> GetMonthSheetNamesAsync(string monthKey)
        {
            var result = new List<string>();
            using var conn = new NpgsqlConnection(_connectionString);
            await conn.OpenAsync();
            using var cmd = new NpgsqlCommand("SELECT DISTINCT sheet_name FROM excel_rows WHERE month_key = @k ORDER BY sheet_name", conn);
            cmd.Parameters.AddWithValue("k", monthKey);
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync()) result.Add(reader.GetString(0));
            return result;
        }

        private static readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions
        {
            AllowTrailingCommas = true
        };

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
                    var cells = JsonSerializer.Deserialize<Dictionary<string, string>>(json, _jsonOptions);
                    // Dictionary hasil deserialisasi default-nya case-sensitive, tidak sama
                    // dengan RawRow baru dan dapat membuat header Excel tak ditemukan.
                    result.Add(new RawRow
                    {
                        Cells = new Dictionary<string, string>(cells ?? new Dictionary<string, string>(), StringComparer.OrdinalIgnoreCase)
                    });
                }
            }
            catch (Exception ex) { AppLogger.LogError("Operasi database", ex); }
            return result;
        }

        public static async Task<bool> DeleteMonthAsync(string monthKey)
        {
            try
            {
                using var conn = new NpgsqlConnection(_connectionString);
                await conn.OpenAsync();
                using var tx = conn.BeginTransaction();
                using (var lockCmd = new NpgsqlCommand("SELECT pg_advisory_xact_lock(hashtext(@lockKey))", conn, tx))
                {
                    lockCmd.Parameters.AddWithValue("lockKey", "month:" + monthKey);
                    await lockCmd.ExecuteNonQueryAsync();
                }
                using var cmd = new NpgsqlCommand("DELETE FROM month_files WHERE month_key = @k", conn, tx);
                cmd.Parameters.AddWithValue("k", monthKey);
                bool deleted = await cmd.ExecuteNonQueryAsync() > 0;
                tx.Commit();
                return deleted;
            }
            catch (Exception ex) { AppLogger.LogError("Operasi database", ex); return false; }
        }

        // ─────────────────────────────────────────────────
        // Manual Logs
        // ─────────────────────────────────────────────────

        public static async Task<Dictionary<string, EvaluasiItem>> LoadManualLogsAsync(string monthKey)
        {
            var dict = new Dictionary<string, EvaluasiItem>(StringComparer.OrdinalIgnoreCase);
            try
            {
                using var conn = new NpgsqlConnection(_connectionString);
                await conn.OpenAsync();

                string sql = "SELECT log_key, nota_salah, jam_datang, jam_pulang, keterangan, updated_at FROM manual_logs WHERE month_key = @monthKey";
                using var cmd = new NpgsqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("monthKey", monthKey ?? "");
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
                        ManualLogUpdatedAt = reader.IsDBNull(5) ? null : reader.GetDateTime(5),
                    };
                }
            }
            catch (Exception ex) { AppLogger.LogError("Operasi database", ex); }
            return dict;
        }

        public static async Task SaveManualLogAsync(EvaluasiItem item, string updatedBy, string monthKey)
        {
            if (item == null || string.IsNullOrEmpty(item.Key)) return;
            await SaveAllManualLogsAsync(new[] { item }, updatedBy, monthKey);
        }

        /// <summary>
        /// Simpan semua manual logs sekaligus (dipakai saat SaveChanges).
        /// </summary>
        public static async Task<Dictionary<string, DateTime?>> SaveAllManualLogsAsync(IEnumerable<EvaluasiItem> items, string updatedBy, string monthKey)
        {
            var itemList = new List<EvaluasiItem>(items ?? Array.Empty<EvaluasiItem>());
            var savedVersions = new Dictionary<string, DateTime?>();
            if (itemList.Count == 0) return savedVersions;

            using var conn = new NpgsqlConnection(_connectionString);
            await conn.OpenAsync();
            using var transaction = conn.BeginTransaction();

            try
            {
                using (var lockCmd = new NpgsqlCommand("SELECT pg_advisory_xact_lock(hashtext(@lockKey))", conn, transaction))
                {
                    lockCmd.Parameters.AddWithValue("lockKey", "month:" + (monthKey ?? ""));
                    await lockCmd.ExecuteNonQueryAsync();
                }
                foreach (var item in itemList)
                {
                    if (item == null || string.IsNullOrEmpty(item.Key)) continue;

                    bool isEmpty = item.NotaSalah <= 0 &&
                                   string.IsNullOrEmpty(item.JamDatang) &&
                                   string.IsNullOrEmpty(item.JamPulang) &&
                                   string.IsNullOrEmpty(item.Keterangan);

                    if (isEmpty)
                    {
                        if (!item.ManualLogUpdatedAt.HasValue) continue;
                        using var delCmd = new NpgsqlCommand("DELETE FROM manual_logs WHERE month_key = @monthKey AND log_key = @k AND updated_at = @version", conn, transaction);
                        delCmd.Parameters.AddWithValue("monthKey", monthKey ?? "");
                        delCmd.Parameters.AddWithValue("k", item.Key);
                        delCmd.Parameters.AddWithValue("version", item.ManualLogUpdatedAt.Value);
                        if (await delCmd.ExecuteNonQueryAsync() == 0)
                            throw new InvalidOperationException("Data telah diubah oleh pengguna lain. Muat ulang bulan sebelum menyimpan kembali.");
                        continue;
                    }

                    const string updateSql = @"
                        UPDATE manual_logs SET
                            nota_salah  = @ns,
                            jam_datang  = @jd,
                            jam_pulang  = @jp,
                            keterangan  = @ket,
                            updated_by  = @ub,
                            updated_at  = NOW()
                        WHERE month_key = @monthKey AND log_key = @k AND updated_at = @version
                        RETURNING updated_at";

                    const string insertSql = @"
                        INSERT INTO manual_logs (month_key, log_key, nota_salah, jam_datang, jam_pulang, keterangan, updated_by, updated_at)
                        VALUES (@monthKey, @k, @ns, @jd, @jp, @ket, @ub, NOW())
                        ON CONFLICT (month_key, log_key) DO NOTHING
                        RETURNING updated_at";

                    using var cmd = new NpgsqlCommand(item.ManualLogUpdatedAt.HasValue ? updateSql : insertSql, conn, transaction);
                    cmd.Parameters.AddWithValue("monthKey", monthKey ?? "");
                    cmd.Parameters.AddWithValue("k", item.Key);
                    cmd.Parameters.AddWithValue("ns", item.NotaSalah);
                    cmd.Parameters.AddWithValue("jd", item.JamDatang ?? "");
                    cmd.Parameters.AddWithValue("jp", item.JamPulang ?? "");
                    cmd.Parameters.AddWithValue("ket", item.Keterangan ?? "");
                    cmd.Parameters.AddWithValue("ub", updatedBy ?? "");
                    if (item.ManualLogUpdatedAt.HasValue) cmd.Parameters.AddWithValue("version", item.ManualLogUpdatedAt.Value);
                    var version = await cmd.ExecuteScalarAsync();
                    if (version == null)
                        throw new InvalidOperationException("Data telah diubah oleh pengguna lain. Muat ulang bulan sebelum menyimpan kembali.");
                    savedVersions[item.Key] = (DateTime)version;
                }

                transaction.Commit();
                return savedVersions;
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }

        /// <summary>Menghapus seluruh input manual. Hanya dipanggil oleh aksi reset administrator.</summary>
        public static async Task DeleteAllManualLogsAsync(string monthKey)
        {
            using var conn = new NpgsqlConnection(_connectionString);
            await conn.OpenAsync();
            using var tx = conn.BeginTransaction();
            using (var lockCmd = new NpgsqlCommand("SELECT pg_advisory_xact_lock(hashtext(@lockKey))", conn, tx))
            {
                lockCmd.Parameters.AddWithValue("lockKey", "month:" + (monthKey ?? ""));
                await lockCmd.ExecuteNonQueryAsync();
            }
            using var cmd = new NpgsqlCommand("DELETE FROM manual_logs WHERE month_key = @monthKey", conn, tx);
            cmd.Parameters.AddWithValue("monthKey", monthKey ?? "");
            await cmd.ExecuteNonQueryAsync();
            tx.Commit();
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
            catch (Exception ex) { AppLogger.LogError("Operasi database", ex); return false; }
        }
    }
}
