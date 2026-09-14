using System.Windows;
using LaporanProduktivitasWPF.Models;
using LaporanProduktivitasWPF.ViewModels;
using LaporanProduktivitasWPF.Views;

namespace LaporanProduktivitasWPF
{
    public partial class MainWindow : Window
    {
        private MainViewModel _vm;

        public MainWindow(AppUser currentUser)
        {
            InitializeComponent();
            _vm = new MainViewModel(currentUser);
            DataContext = _vm;

            // Subscribe logout event
            _vm.LogoutRequested += OnLogoutRequested;
        }

        private void OnLogoutRequested(object sender, System.EventArgs e)
        {
            // Buka LoginWindow lagi
            var login = new LoginWindow();
            bool? result = login.ShowDialog();

            if (result == true && login.LoggedInUser != null)
            {
                // Buka MainWindow baru dengan user baru
                var newMain = new MainWindow(login.LoggedInUser);
                Application.Current.MainWindow = newMain;
                newMain.Show();
            }
            else
            {
                // User tutup login window — matikan aplikasi
                Application.Current.Shutdown();
                return;
            }

            // Tutup window ini
            Close();
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
