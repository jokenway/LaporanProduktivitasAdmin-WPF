using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using LaporanProduktivitasWPF.Models;
using LaporanProduktivitasWPF.Services;

namespace LaporanProduktivitasWPF.Views
{
    public class LoginSucceededEventArgs : EventArgs
    {
        public AppUser User { get; }

        public LoginSucceededEventArgs(AppUser user)
        {
            User = user;
        }
    }

    public partial class LoginWindow : UserControl
    {
        public AppUser LoggedInUser { get; private set; }
        public event EventHandler<LoginSucceededEventArgs> LoginSucceeded;
        private int _failedAttempts = 0;

        public LoginWindow()
        {
            InitializeComponent();
            Loaded += (s, e) => TxtUsername.Focus();
        }

        public void Reset()
        {
            LoggedInUser = null;
            _failedAttempts = 0;
            TxtUsername.Clear();
            TxtPassword.Clear();
            TxtError.Text = string.Empty;
            TxtError.Visibility = Visibility.Collapsed;
            BtnLogin.IsEnabled = true;
            BtnLogin.Content = "Masuk";
            Dispatcher.BeginInvoke(new Action(() => TxtUsername.Focus()));
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
            if (!BtnLogin.IsEnabled) return;

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
                _failedAttempts = 0;
                TxtPassword.Clear();
                LoginSucceeded?.Invoke(this, new LoginSucceededEventArgs(user));
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

