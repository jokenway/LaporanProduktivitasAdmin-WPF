using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using Microsoft.Win32;
using LaporanProduktivitasWPF.Models;
using LaporanProduktivitasWPF.Services;

namespace LaporanProduktivitasWPF.ViewModels
{
    public class MainViewModel : ViewModelBase
    {
        private string _activeTab = "dashboard";
        public string ActiveTab
        {
            get { return _activeTab; }
            set
            {
                if (_activeTab != value)
                {
                    _activeTab = value;
                    OnPropertyChanged("ActiveTab");
                    OnPropertyChanged("IsDashboardActive");
                    OnPropertyChanged("IsImportActive");
                    OnPropertyChanged("IsExactActive");
                    OnPropertyChanged("IsEvaluasiActive");
                    OnPropertyChanged("IsEvaluasiInvoiceActive");
                    OnPropertyChanged("IsCustomActive");
                }
            }
        }

        public bool IsDashboardActive { get { return _activeTab == "dashboard"; } }
        public bool IsImportActive { get { return _activeTab == "import"; } }
        public bool IsExactActive { get { return _activeTab == "exact"; } }
        public bool IsEvaluasiActive { get { return _activeTab == "evaluasi"; } }
        public bool IsEvaluasiInvoiceActive { get { return _activeTab == "evaluasiInvoice"; } }
        public bool IsCustomActive { get { return _activeTab == "custom"; } }

        // Workbook info
        private string _fileName = "";
        public string FileName
        {
            get { return _fileName; }
            set { _fileName = value; OnPropertyChanged("FileName"); }
        }

        private string _activeSheet = "";
        public string ActiveSheet
        {
            get { return _activeSheet; }
            set
            {
                if (_activeSheet != value)
                {
                    _activeSheet = value;
                    OnPropertyChanged("ActiveSheet");
                    OnSheetChanged();
                }
            }
        }

        private int _rowCount = 0;
        public int RowCount
        {
            get { return _rowCount; }
            set { _rowCount = value; OnPropertyChanged("RowCount"); }
        }

        private bool _isLoading = false;
        public bool IsLoading
        {
            get { return _isLoading; }
            set { _isLoading = value; OnPropertyChanged("IsLoading"); }
        }

        private string _statusMessage = "Siap.";
        public string StatusMessage
        {
            get { return _statusMessage; }
            set { _statusMessage = value; OnPropertyChanged("StatusMessage"); }
        }

        public ObservableCollection<string> SheetNames { get; set; }

        // Raw active rows
        private List<RawRow> _currentRows = new List<RawRow>();
        private Dictionary<string, ExcelSheetData> _sheetsData = new Dictionary<string, ExcelSheetData>(StringComparer.OrdinalIgnoreCase);
        private Dictionary<string, EvaluasiItem> _manualLogs = new Dictionary<string, EvaluasiItem>(StringComparer.OrdinalIgnoreCase);

        // Exact Pivot state
        private string _selectedDate = "ALL";
        public string SelectedDate
        {
            get { return _selectedDate; }
            set { if (_selectedDate != value) { _selectedDate = value; OnPropertyChanged("SelectedDate"); RefreshExactPivot(); } }
        }

        private string _selectedUser = "ALL";
        public string SelectedUser
        {
            get { return _selectedUser; }
            set { if (_selectedUser != value) { _selectedUser = value; OnPropertyChanged("SelectedUser"); RefreshExactPivot(); } }
        }

        private string _selectedJenisB = "ALL";
        public string SelectedJenisB
        {
            get { return _selectedJenisB; }
            set { if (_selectedJenisB != value) { _selectedJenisB = value; OnPropertyChanged("SelectedJenisB"); RefreshExactPivot(); } }
        }

        private string _searchQuery = "";
        public string SearchQuery
        {
            get { return _searchQuery; }
            set { if (_searchQuery != value) { _searchQuery = value; OnPropertyChanged("SearchQuery"); RefreshExactPivot(); } }
        }

        public ObservableCollection<string> AvailableDates { get; set; }
        public ObservableCollection<string> AvailableUsers { get; set; }
        public ObservableCollection<string> AvailableJenisB { get; set; }

        private ObservableCollection<ExactPivotItem> _exactPivotItems = new ObservableCollection<ExactPivotItem>();
        public ObservableCollection<ExactPivotItem> ExactPivotItems
        {
            get { return _exactPivotItems; }
            set { _exactPivotItems = value; OnPropertyChanged("ExactPivotItems"); }
        }

        private int _totalNota;
        public int TotalNota { get { return _totalNota; } set { _totalNota = value; OnPropertyChanged("TotalNota"); } }

        private int _totalCountJMDOS;
        public int TotalCountJMDOS { get { return _totalCountJMDOS; } set { _totalCountJMDOS = value; OnPropertyChanged("TotalCountJMDOS"); } }

        private double _totalSumNetto;
        public double TotalSumNetto { get { return _totalSumNetto; } set { _totalSumNetto = value; OnPropertyChanged("TotalSumNetto"); } }

        private int _grandTotalAllNota;
        public int GrandTotalAllNota { get { return _grandTotalAllNota; } set { _grandTotalAllNota = value; OnPropertyChanged("GrandTotalAllNota"); } }

        private ObservableCollection<UserNotaSummary> _exactUserSummaryItems = new ObservableCollection<UserNotaSummary>();
        public ObservableCollection<UserNotaSummary> ExactUserSummaryItems
        {
            get { return _exactUserSummaryItems; }
            set { _exactUserSummaryItems = value; OnPropertyChanged("ExactUserSummaryItems"); }
        }

        // Dashboard stats
        private int _grandTotalCalculated;
        public int GrandTotalCalculated { get { return _grandTotalCalculated; } set { _grandTotalCalculated = value; OnPropertyChanged("GrandTotalCalculated"); } }

        private int _uniqueUsersCount;
        public int UniqueUsersCount { get { return _uniqueUsersCount; } set { _uniqueUsersCount = value; OnPropertyChanged("UniqueUsersCount"); } }

        private int _uniqueDatesCount;
        public int UniqueDatesCount { get { return _uniqueDatesCount; } set { _uniqueDatesCount = value; OnPropertyChanged("UniqueDatesCount"); } }

        private string _topUserName = "-";
        public string TopUserName { get { return _topUserName; } set { _topUserName = value; OnPropertyChanged("TopUserName"); } }

        private int _topUserTotal;
        public int TopUserTotal { get { return _topUserTotal; } set { _topUserTotal = value; OnPropertyChanged("TopUserTotal"); } }

        private double _topUserPercentage;
        public double TopUserPercentage { get { return _topUserPercentage; } set { _topUserPercentage = value; OnPropertyChanged("TopUserPercentage"); } }

        private double _avgPerUser;
        public double AvgPerUser { get { return _avgPerUser; } set { _avgPerUser = value; OnPropertyChanged("AvgPerUser"); } }

        private double _avgPerDate;
        public double AvgPerDate { get { return _avgPerDate; } set { _avgPerDate = value; OnPropertyChanged("AvgPerDate"); } }

        private string _latestDate = "-";
        public string LatestDate { get { return _latestDate; } set { _latestDate = value; OnPropertyChanged("LatestDate"); } }

        public ObservableCollection<ChartBarItem> TopUsersChart { get; set; }

        // Evaluasi Kinerja state
        private string _evalSelectedDate = "ALL";
        public string EvalSelectedDate
        {
            get { return _evalSelectedDate; }
            set { if (_evalSelectedDate != value) { _evalSelectedDate = value; OnPropertyChanged("EvalSelectedDate"); RefreshEvaluasi(); } }
        }

        private string _evalSelectedUser = "ALL";
        public string EvalSelectedUser
        {
            get { return _evalSelectedUser; }
            set { if (_evalSelectedUser != value) { _evalSelectedUser = value; OnPropertyChanged("EvalSelectedUser"); RefreshEvaluasi(); } }
        }

        private string _evalSearchQuery = "";
        public string EvalSearchQuery
        {
            get { return _evalSearchQuery; }
            set { if (_evalSearchQuery != value) { _evalSearchQuery = value; OnPropertyChanged("EvalSearchQuery"); RefreshEvaluasi(); } }
        }

        public ObservableCollection<string> EvalAvailableDates { get; set; }
        public ObservableCollection<string> EvalAvailableUsers { get; set; }

        private ObservableCollection<EvaluasiItem> _evaluasiItems = new ObservableCollection<EvaluasiItem>();
        public ObservableCollection<EvaluasiItem> EvaluasiItems
        {
            get { return _evaluasiItems; }
            set { _evaluasiItems = value; OnPropertyChanged("EvaluasiItems"); }
        }

        private ObservableCollection<AdminInvoiceMetric> _adminInvoiceMetrics = new ObservableCollection<AdminInvoiceMetric>();
        public ObservableCollection<AdminInvoiceMetric> AdminInvoiceMetrics
        {
            get { return _adminInvoiceMetrics; }
            set { _adminInvoiceMetrics = value; OnPropertyChanged("AdminInvoiceMetrics"); }
        }

        private int _evalTotalNota;
        public int EvalTotalNota { get { return _evalTotalNota; } set { _evalTotalNota = value; OnPropertyChanged("EvalTotalNota"); } }

        private int _evalTotalSalah;
        public int EvalTotalSalah { get { return _evalTotalSalah; } set { _evalTotalSalah = value; OnPropertyChanged("EvalTotalSalah"); } }

        private double _evalOverallAkurasi = 100.0;
        public double EvalOverallAkurasi { get { return _evalOverallAkurasi; } set { _evalOverallAkurasi = value; OnPropertyChanged("EvalOverallAkurasi"); } }

        private string _evalTotalJamText = "0 jam";
        public string EvalTotalJamText { get { return _evalTotalJamText; } set { _evalTotalJamText = value; OnPropertyChanged("EvalTotalJamText"); } }

        private int _evalUserCount;
        public int EvalUserCount { get { return _evalUserCount; } set { _evalUserCount = value; OnPropertyChanged("EvalUserCount"); } }

        private int _evalDateCount;
        public int EvalDateCount { get { return _evalDateCount; } set { _evalDateCount = value; OnPropertyChanged("EvalDateCount"); } }

        private int _adminInvoiceTotalNota;
        public int AdminInvoiceTotalNota { get { return _adminInvoiceTotalNota; } set { _adminInvoiceTotalNota = value; OnPropertyChanged("AdminInvoiceTotalNota"); } }

        // Custom Pivot state
        public ObservableCollection<string> AvailableHeaders { get; set; }
        private string _customRowField = "USID";
        public string CustomRowField { get { return _customRowField; } set { _customRowField = value; OnPropertyChanged("CustomRowField"); } }

        private string _customColField = "TGBON";
        public string CustomColField { get { return _customColField; } set { _customColField = value; OnPropertyChanged("CustomColField"); } }

        private string _customValField = "NOINV";
        public string CustomValField { get { return _customValField; } set { _customValField = value; OnPropertyChanged("CustomValField"); } }

        private string _customAggType = "DISTINCT_COUNT";
        public string CustomAggType { get { return _customAggType; } set { _customAggType = value; OnPropertyChanged("CustomAggType"); } }

        // Commands
        public ICommand SwitchTabCommand { get; private set; }
        public ICommand OpenFileCommand { get; private set; }
        public ICommand LoadSampleCommand { get; private set; }
        public ICommand ExportExactCommand { get; private set; }
        public ICommand ExportEvaluasiCommand { get; private set; }
        public ICommand ApplyDefaultHoursCommand { get; private set; }
        public ICommand ResetInputsCommand { get; private set; }
        public ICommand ApplyPresetCommand { get; private set; }
        public ICommand SaveChangesCommand { get; private set; }

        public MainViewModel()
        {
            SheetNames = new ObservableCollection<string>();
            AvailableDates = new ObservableCollection<string>();
            AvailableUsers = new ObservableCollection<string>();
            AvailableJenisB = new ObservableCollection<string>();
            ExactPivotItems = new ObservableCollection<ExactPivotItem>();
            ExactUserSummaryItems = new ObservableCollection<UserNotaSummary>();

            TopUsersChart = new ObservableCollection<ChartBarItem>();

            EvalAvailableDates = new ObservableCollection<string>();
            EvalAvailableUsers = new ObservableCollection<string>();
            EvaluasiItems = new ObservableCollection<EvaluasiItem>();
            AdminInvoiceMetrics = new ObservableCollection<AdminInvoiceMetric>();
            AvailableHeaders = new ObservableCollection<string>();

            // Setup Commands
            SwitchTabCommand = new RelayCommand(param =>
            {
                if (param != null) ActiveTab = param.ToString();
            });

            OpenFileCommand = new RelayCommand(async () => await ChooseAndOpenFileAsync());
            LoadSampleCommand = new RelayCommand(() => LoadSampleData());
            ExportExactCommand = new RelayCommand(() => ExportExactPivot());
            ExportEvaluasiCommand = new RelayCommand(() => ExportEvaluasi());
            ApplyDefaultHoursCommand = new RelayCommand(() => ApplyDefaultWorkingHours());
            ResetInputsCommand = new RelayCommand(() => ResetManualInputs());
            ApplyPresetCommand = new RelayCommand(() => ApplyCustomPreset());
            SaveChangesCommand = new RelayCommand(() => SaveManualLogsToStorage());

            // Load saved manual logs
            _manualLogs = StorageService.LoadManualLogs();

            // Auto-load default bundled file if present
            TryLoadBundledFile();
        }

        private async void TryLoadBundledFile()
        {
            string bundled = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "JULI - CLOSING.xlsx");
            if (!File.Exists(bundled))
            {
                bundled = @"c:\Laporan Produktifitas Admin\JULI - CLOSING.xlsx";
            }

            if (File.Exists(bundled))
            {
                await LoadExcelAsync(bundled);
            }
            else
            {
                LoadSampleData();
            }
        }

        private async Task ChooseAndOpenFileAsync()
        {
            var ofd = new OpenFileDialog
            {
                Title = "Pilih File Laporan Excel",
                Filter = "File Excel (*.xlsx;*.xlsm)|*.xlsx;*.xlsm|Semua File (*.*)|*.*"
            };

            if (ofd.ShowDialog() == true)
            {
                await LoadExcelAsync(ofd.FileName);
            }
        }

        public async Task LoadExcelAsync(string filePath)
        {
            try
            {
                IsLoading = true;
                StatusMessage = "Membaca file Excel: " + Path.GetFileName(filePath) + "...";

                var parsed = await Task.Run(() => ExcelService.ParseExcelFile(filePath));

                FileName = parsed.FileName;
                _sheetsData = parsed.Sheets;

                SheetNames.Clear();
                foreach (var name in parsed.SheetNames) SheetNames.Add(name);

                ActiveSheet = parsed.SheetNames.Count > 0 ? parsed.SheetNames[0] : "";
                StatusMessage = "Berhasil membaca " + RowCount.ToString("N0") + " baris.";
                ActiveTab = "dashboard";
            }
            catch (Exception ex)
            {
                MessageBox.Show("Gagal membuka file Excel: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                StatusMessage = "Gagal membuka file.";
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void OnSheetChanged()
        {
            if (string.IsNullOrEmpty(_activeSheet) || !_sheetsData.ContainsKey(_activeSheet))
            {
                _currentRows = new List<RawRow>();
                RowCount = 0;
                AvailableHeaders.Clear();
                return;
            }

            var sheet = _sheetsData[_activeSheet];
            _currentRows = sheet.Rows;
            RowCount = sheet.Rows.Count;

            AvailableHeaders.Clear();
            foreach (var h in sheet.Headers) AvailableHeaders.Add(h);

            // Reset filters
            _selectedDate = "ALL";
            _selectedUser = "ALL";
            _selectedJenisB = "ALL";
            _searchQuery = "";
            OnPropertyChanged("SelectedDate");
            OnPropertyChanged("SelectedUser");
            OnPropertyChanged("SelectedJenisB");
            OnPropertyChanged("SearchQuery");

            _evalSelectedDate = "ALL";
            _evalSelectedUser = "ALL";
            _evalSearchQuery = "";
            OnPropertyChanged("EvalSelectedDate");
            OnPropertyChanged("EvalSelectedUser");
            OnPropertyChanged("EvalSearchQuery");

            RefreshExactPivot();
            RefreshEvaluasi();
            RefreshDashboard();
        }

        private void RefreshExactPivot()
        {
            var res = ExactPivotEngine.BuildExactPivot(
                _currentRows,
                SelectedDate,
                SelectedUser,
                SelectedJenisB,
                SearchQuery
            );

            // Populate Available filter options if changed
            if (AvailableDates.Count != res.AvailableDates.Count + 1)
            {
                AvailableDates.Clear();
                AvailableDates.Add("ALL");
                foreach (var d in res.AvailableDates) AvailableDates.Add(d);
            }

            if (AvailableUsers.Count != res.AvailableUsers.Count + 1)
            {
                AvailableUsers.Clear();
                AvailableUsers.Add("ALL");
                foreach (var u in res.AvailableUsers) AvailableUsers.Add(u);
            }

            if (AvailableJenisB.Count != res.AvailableJenisB.Count + 1)
            {
                AvailableJenisB.Clear();
                AvailableJenisB.Add("ALL");
                foreach (var j in res.AvailableJenisB) AvailableJenisB.Add(j);
            }

            ExactPivotItems = new ObservableCollection<ExactPivotItem>(res.Items);

            TotalNota = res.TotalNota;
            TotalCountJMDOS = res.TotalCountJMDOS;
            TotalSumNetto = res.TotalSumNetto;

            // Build per-user summary from FILTERED items only (reflects active filter)
            GrandTotalAllNota = res.GrandTotalNotaKeseluruhan;

            // Count unique NOINV per USID from filtered items only
            var filteredNotaPerUser = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);
            foreach (var it in res.Items)
            {
                if (!string.IsNullOrEmpty(it.USID))
                {
                    if (!filteredNotaPerUser.ContainsKey(it.USID))
                        filteredNotaPerUser[it.USID] = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                    if (!string.IsNullOrEmpty(it.NOINV))
                        filteredNotaPerUser[it.USID].Add(it.NOINV);
                }
            }

            // Filtered grand total = sum of all user filtered nota
            int filteredGrandTotal = filteredNotaPerUser.Values.Sum(s => s.Count);

            var summaryList = filteredNotaPerUser
                .OrderByDescending(kv => kv.Value.Count)
                .Select(kv =>
                {
                    int cnt = kv.Value.Count;
                    double pct = filteredGrandTotal > 0 ? Math.Round((double)cnt / filteredGrandTotal * 100.0, 1) : 0.0;
                    return new UserNotaSummary
                    {
                        User = kv.Key,
                        TotalNota = cnt,
                        ContributionPct = pct
                    };
                })
                .ToList();
            ExactUserSummaryItems = new ObservableCollection<UserNotaSummary>(summaryList);
        }

        private void RefreshDashboard()
        {
            var res = ExactPivotEngine.BuildExactPivot(_currentRows, "ALL", "ALL", "ALL", "");
            GrandTotalCalculated = res.GrandTotalNotaKeseluruhan;
            UniqueUsersCount = res.AvailableUsers.Count;
            UniqueDatesCount = res.AvailableDates.Count;

            if (res.AvailableUsers.Count > 0)
            {
                string top = res.AvailableUsers[0];
                int topCount = res.UserStats.ContainsKey(top) ? res.UserStats[top] : 0;
                TopUserName = top;
                TopUserTotal = topCount;
                TopUserPercentage = GrandTotalCalculated > 0 ? Math.Round(((double)topCount / GrandTotalCalculated) * 100.0, 1) : 0.0;
                AvgPerUser = Math.Round((double)GrandTotalCalculated / res.AvailableUsers.Count, 1);
            }
            else
            {
                TopUserName = "-";
                TopUserTotal = 0;
                TopUserPercentage = 0.0;
                AvgPerUser = 0.0;
            }

            if (res.AvailableDates.Count > 0)
            {
                AvgPerDate = Math.Round((double)GrandTotalCalculated / res.AvailableDates.Count, 1);
                LatestDate = res.AvailableDates[res.AvailableDates.Count - 1];
            }
            else
            {
                AvgPerDate = 0.0;
                LatestDate = "-";
            }

            // Top 10 chart
            TopUsersChart.Clear();
            var top10 = res.AvailableUsers.Take(10);
            foreach (var u in top10)
            {
                int val = res.UserStats.ContainsKey(u) ? res.UserStats[u] : 0;
                double pct = GrandTotalCalculated > 0 ? ((double)val / GrandTotalCalculated) * 100.0 : 0.0;
                TopUsersChart.Add(new ChartBarItem
                {
                    Label = u,
                    Value = val,
                    Percentage = Math.Round(pct, 1)
                });
            }
        }

        private void RefreshEvaluasi()
        {
            // Always filter to ADMIN_INVOICE users only (AKBAR, DIDIN, JOE, RONI, NOVIANI)
            // EvalSelectedUser can still narrow within that group
            var res = EvaluasiEngine.BuildDailyEvaluation(
                _currentRows,
                _manualLogs,
                EvalSelectedDate,
                EvalSelectedUser,
                EvalSearchQuery,
                "ADMIN_INVOICE"
            );

            if (EvalAvailableDates.Count != res.AvailableDates.Count + 1)
            {
                EvalAvailableDates.Clear();
                EvalAvailableDates.Add("ALL");
                foreach (var d in res.AvailableDates) EvalAvailableDates.Add(d);
            }

            // Only repopulate user dropdown if content changed (prevents WPF binding loop causing duplicates)
            var expectedUsers = res.AvailableUsers;
            bool userListChanged = EvalAvailableUsers.Count != expectedUsers.Count + 1;
            if (!userListChanged)
            {
                // also verify the actual users match
                for (int i = 0; i < expectedUsers.Count; i++)
                {
                    if (i + 1 >= EvalAvailableUsers.Count || !string.Equals(EvalAvailableUsers[i + 1], expectedUsers[i], StringComparison.OrdinalIgnoreCase))
                    {
                        userListChanged = true;
                        break;
                    }
                }
            }
            if (userListChanged)
            {
                // Temporarily disconnect the selected user binding to prevent re-trigger
                string currentUser = _evalSelectedUser;
                EvalAvailableUsers.Clear();
                EvalAvailableUsers.Add("ALL");
                foreach (var u in expectedUsers) EvalAvailableUsers.Add(u);
                // Restore selection if it's still valid
                if (!string.IsNullOrEmpty(currentUser) && currentUser != "ALL" && !expectedUsers.Contains(currentUser, StringComparer.OrdinalIgnoreCase))
                {
                    _evalSelectedUser = "ALL";
                    OnPropertyChanged("EvalSelectedUser");
                }
            }

            foreach (var it in res.Records)
            {
                it.PropertyChanged += (s, e) =>
                {
                    var item = (EvaluasiItem)s;
                    _manualLogs[item.Key] = item;
                    SaveManualLogsToStorage();
                };
            }
            EvaluasiItems = new ObservableCollection<EvaluasiItem>(res.Records);
            AdminInvoiceMetrics = new ObservableCollection<AdminInvoiceMetric>(res.UserMetrics);
            AdminInvoiceTotalNota = res.GroupTotalNota;

            EvalTotalNota = res.Summary.TotalNota;
            EvalTotalSalah = res.Summary.TotalSalah;
            EvalOverallAkurasi = res.Summary.OverallAkurasi;
            EvalTotalJamText = res.Summary.TotalJamKerjaText;
            EvalUserCount = res.Summary.UserCount;
            EvalDateCount = res.Summary.DateCount;
        }

        private void ApplyDefaultWorkingHours()
        {
            int count = 0;
            foreach (var it in EvaluasiItems)
            {
                if (string.IsNullOrEmpty(it.JamDatang) || string.IsNullOrEmpty(it.JamPulang))
                {
                    it.JamDatang = "08:00";
                    it.JamPulang = "17:00";
                    _manualLogs[it.Key] = it;
                    count++;
                }
            }
            SaveManualLogsToStorage();
            RefreshEvaluasi();
            MessageBox.Show(string.Format("Berhasil menerapkan jam standar (08:00 - 17:00) untuk {0} baris.", count), "Informasi", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void ResetManualInputs()
        {
            if (MessageBox.Show("Apakah Anda yakin ingin menghapus seluruh data inputan nota salah dan jam kerja?", "Konfirmasi Reset", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                _manualLogs.Clear();
                SaveManualLogsToStorage();
                RefreshEvaluasi();
            }
        }

        private void SaveManualLogsToStorage()
        {
            StorageService.SaveManualLogs(_manualLogs.Values);
            StatusMessage = "Input tersimpan otomatis (" + DateTime.Now.ToString("HH:mm:ss") + ")";
        }

        private void ExportExactPivot()
        {
            if (ExactPivotItems.Count == 0)
            {
                MessageBox.Show("Tidak ada data untuk diekspor.", "Peringatan", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var sfd = new SaveFileDialog
            {
                Title = "Simpan Laporan Pivot",
                Filter = "File CSV (*.csv)|*.csv",
                FileName = string.Format("Laporan_Pivot_{0}_{1}.csv", SelectedUser, SelectedDate)
            };

            if (sfd.ShowDialog() == true)
            {
                try
                {
                    ExcelService.ExportExactPivotToCsv(sfd.FileName, ExactPivotItems, SelectedUser, SelectedDate, TotalNota, TotalCountJMDOS, TotalSumNetto);
                    MessageBox.Show("Laporan berhasil diekspor ke: " + sfd.FileName, "Sukses", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Gagal ekspor: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void ExportEvaluasi()
        {
            if (EvaluasiItems.Count == 0)
            {
                MessageBox.Show("Tidak ada data evaluasi untuk diekspor.", "Peringatan", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var sfd = new SaveFileDialog
            {
                Title = "Simpan Laporan Kinerja & Absensi",
                Filter = "File CSV (*.csv)|*.csv",
                FileName = string.Format("Laporan_Kinerja_Absensi_{0}_{1}.csv", EvalSelectedUser, EvalSelectedDate)
            };

            if (sfd.ShowDialog() == true)
            {
                try
                {
                    ExcelService.ExportEvaluasiToCsv(sfd.FileName, EvaluasiItems, EvalTotalNota, EvalTotalSalah, EvalOverallAkurasi, EvalTotalJamText);
                    MessageBox.Show("Laporan evaluasi berhasil diekspor ke: " + sfd.FileName, "Sukses", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Gagal ekspor: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void ApplyCustomPreset()
        {
            string rowF = AvailableHeaders.FirstOrDefault(h => string.Equals(h, "USID", StringComparison.OrdinalIgnoreCase) || string.Equals(h, "User", StringComparison.OrdinalIgnoreCase)) ?? "USID";
            string colF = AvailableHeaders.FirstOrDefault(h => string.Equals(h, "TGBON", StringComparison.OrdinalIgnoreCase) || string.Equals(h, "Tanggal", StringComparison.OrdinalIgnoreCase)) ?? "TGBON";
            string valF = AvailableHeaders.FirstOrDefault(h => string.Equals(h, "NOINV", StringComparison.OrdinalIgnoreCase) || string.Equals(h, "No Invoice", StringComparison.OrdinalIgnoreCase)) ?? "NOINV";

            CustomRowField = rowF;
            CustomColField = colF;
            CustomValField = valF;
            CustomAggType = "DISTINCT_COUNT";

            MessageBox.Show("Preset otomatis diterapkan:\nBaris: " + rowF + "\nKolom: " + colF + "\nNilai: " + valF + " (DISTINCT_COUNT)", "Preset Aktif", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void LoadSampleData()
        {
            var rows = new List<RawRow>();
            var rnd = new Random(42);
            string[] users = new string[] { "AKBAR", "DIDIN", "JOE", "RONI", "NOVIANI", "SUSANTO", "HERI" };
            string[] sales = new string[] { "AGUS", "BUDI", "CHARLES", "DENI", "EKO" };
            string[] dates = new string[] { "01-07-2026", "02-07-2026", "03-07-2026", "04-07-2026", "05-07-2026" };

            for (int i = 1; i <= 300; i++)
            {
                var r = new RawRow();
                string inv = string.Format("INV/{0:D5}", i);
                string u = users[rnd.Next(users.Length)];
                string d = dates[rnd.Next(dates.Length)];
                string s = sales[rnd.Next(sales.Length)];
                int jmdos = rnd.Next(1, 5);
                double netto = rnd.Next(10, 250) * 10000.0;

                r.Cells["NOINV"] = inv;
                r.Cells["USID"] = u;
                r.Cells["TGBON"] = d;
                r.Cells["NMPG"] = s;
                r.Cells["KDPRC"] = "UCI";
                r.Cells["JMDOS"] = jmdos.ToString();
                r.Cells["NETTO"] = netto.ToString();
                r.Cells["JENIS_B"] = "INVOICE";

                rows.Add(r);
            }

            var sheet = new ExcelSheetData
            {
                SheetName = "Data_Contoh",
                Headers = new List<string> { "NOINV", "USID", "TGBON", "NMPG", "KDPRC", "JMDOS", "NETTO", "JENIS_B" },
                Rows = rows
            };

            _sheetsData.Clear();
            _sheetsData["Data_Contoh"] = sheet;
            SheetNames.Clear();
            SheetNames.Add("Data_Contoh");

            FileName = "Contoh_Data_Produktivitas.xlsx";
            ActiveSheet = "Data_Contoh";
            StatusMessage = "Memuat data contoh berhasil (300 baris).";
        }
    }
}
