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
    }
}
