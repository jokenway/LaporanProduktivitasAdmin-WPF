using System.Windows;
using LaporanProduktivitasWPF.Services;

namespace LaporanProduktivitasWPF.Views
{
    public partial class EditProfileWindow : Window
    {
        private string _username;

        public EditProfileWindow(string username)
        {
            InitializeComponent();
            _username = username;
            TxtInfo.Text = "Username: " + _username;
        }

        private async void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            TxtError.Visibility = Visibility.Collapsed;
            string pass = TxtPassword.Password;

            if (string.IsNullOrEmpty(pass))
            {
                TxtError.Text = "Password tidak boleh kosong.";
                TxtError.Visibility = Visibility.Visible;
                return;
            }

            bool ok = await DatabaseService.UpdateUserPasswordAsync(_username, pass);
            if (ok)
            {
                MessageBox.Show("Password berhasil diubah!", "Sukses", MessageBoxButton.OK, MessageBoxImage.Information);
                this.DialogResult = true;
                this.Close();
            }
            else
            {
                TxtError.Text = "Gagal mengubah password. Terjadi kesalahan database.";
                TxtError.Visibility = Visibility.Visible;
            }
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }
    }
}

