using System.Windows;
using LaporanProduktivitasWPF.Models;
using LaporanProduktivitasWPF.ViewModels;
using LaporanProduktivitasWPF.Views;

namespace LaporanProduktivitasWPF
{
    public partial class MainWindow : Window
    {
        private MainViewModel _vm;

        public MainWindow()
        {
            InitializeComponent();
            LoginView.LoginSucceeded += OnLoginSucceeded;
            ShowLogin();
        }

        public MainWindow(AppUser currentUser) : this()
        {
            ShowApplication(currentUser);
        }

        private void OnLoginSucceeded(object sender, LoginSucceededEventArgs e)
        {
            ShowApplication(e.User);
        }

        private void ShowApplication(AppUser currentUser)
        {
            DetachCurrentViewModel();
            _vm = new MainViewModel(currentUser);
            _vm.LogoutRequested += OnLogoutRequested;
            DataContext = _vm;

            LoginView.Visibility = Visibility.Collapsed;
            ApplicationView.Visibility = Visibility.Visible;
            Title = "Laporan Produktivitas Admin - WPF Desktop Studio";
        }

        private void ShowLogin()
        {
            DetachCurrentViewModel();
            DataContext = null;

            ApplicationView.Visibility = Visibility.Collapsed;
            LoginView.Visibility = Visibility.Visible;
            Title = "Login - Laporan Produktivitas Admin";
            LoginView.Reset();
        }

        private void DetachCurrentViewModel()
        {
            if (_vm == null) return;
            _vm.LogoutRequested -= OnLogoutRequested;
            _vm = null;
        }

        private void OnLogoutRequested(object sender, System.EventArgs e)
        {
            ShowLogin();
        }

        private void OnEvaluasiGridBeginningEdit(object sender, System.Windows.Controls.DataGridBeginningEditEventArgs e)
        {
            if (_vm == null || _vm.CurrentUser == null) return;

            var item = e.Row.Item as EvaluasiItem;
            if (item == null) return;

            string header = e.Column.Header?.ToString() ?? "";
            string loggedInUser = _vm.CurrentUser.Username ?? "";

            // 1. Kolom "Nota Salah": HANYA USER AKBAR yang boleh menginput/mengedit!
            if (header.Contains("Nota Salah"))
            {
                if (!_vm.CurrentUser.CanInputNotaSalah)
                {
                    e.Cancel = true;
                    return;
                }
            }

            // 2. Jika user ST (Viewer) -> kunci semua kolom input
            if (!_vm.CurrentUser.CanInputAttendance && !_vm.CurrentUser.CanInputNotaSalah)
            {
                e.Cancel = true;
                return;
            }

            // 3. Jika Staff (misal: JOE, DIDIN, RONI, NOVIANI, STEVI, GINA):
            //    HANYA boleh menginput/mengedit jam datang, jam pulang, & catatan pada BARIS MILIKNYA SENDIRI!
            //    Jika AKBAR -> boleh edit baris siapa saja.
            if (!string.Equals(loggedInUser, "AKBAR", System.StringComparison.OrdinalIgnoreCase))
            {
                if (!string.Equals(item.User, loggedInUser, System.StringComparison.OrdinalIgnoreCase))
                {
                    e.Cancel = true;
                    return;
                }
            }
        }
    }
}
