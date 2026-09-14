using System.Windows;
using System.Windows.Input;
using LaporanProduktivitasWPF.Models;
using LaporanProduktivitasWPF.Services;

namespace LaporanProduktivitasWPF.Views
{
    public partial class LoginWindow : Window
    {
        public AppUser LoggedInUser { get; private set; }
        private int _failedAttempts = 0;

        public LoginWindow()
        {
            InitializeComponent();
            TxtUsername.Focus();
        }

        private async void BtnLogin_Click(object sender, RoutedEventArgs e)
        {
            await TryLoginAsync();
        }

        private async void OnKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return || e.Key == Key.Enter)
                await TryLoginAsync();
        }

        private async System.Threading.Tasks.Task TryLoginAsync()
        {
            string username = TxtUsername.Text?.Trim() ?? "";
            string password = TxtPassword.Password ?? "";

            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                ShowError("Username dan password tidak boleh kosong.");
                return;
            }

            BtnLogin.IsEnabled = false;
            BtnLogin.Content = "Memverifikasi...";
            TxtError.Visibility = Visibility.Collapsed;

            var user = await DatabaseService.AuthenticateAsync(username, password);

            BtnLogin.IsEnabled = true;
            BtnLogin.Content = "Masuk";

            if (user != null)
            {
                LoggedInUser = user;
                DialogResult = true;
                Close();
            }
            else
            {
                _failedAttempts++;
                if (_failedAttempts >= 3)
                {
                    ShowError("Terlalu banyak percobaan gagal. Aplikasi akan ditutup.");
                    await System.Threading.Tasks.Task.Delay(2000);
                    Application.Current.Shutdown();
                }
                else
                {
                    ShowError($"Username atau password salah. ({_failedAttempts}/3 percobaan)");
                    TxtPassword.Clear();
                    TxtPassword.Focus();
                }
            }
        }

        private void ShowError(string msg)
        {
            TxtError.Text = msg;
            TxtError.Visibility = Visibility.Visible;
        }
    }
}

