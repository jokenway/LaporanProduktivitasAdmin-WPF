using System.Windows;
using System.Windows.Controls;
using LaporanProduktivitasWPF.Services;

namespace LaporanProduktivitasWPF.Views
{
    public partial class AddUserWindow : Window
    {
        private readonly string _createdBy;
        private readonly bool _canCreateAdmin;

        public AddUserWindow(string createdBy, bool canCreateAdmin)
        {
            InitializeComponent();
            _createdBy = createdBy ?? string.Empty;
            _canCreateAdmin = canCreateAdmin;
            AdminRoleItem.Visibility = canCreateAdmin ? Visibility.Visible : Visibility.Collapsed;
        }

        private async void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            TxtError.Visibility = Visibility.Collapsed;
            string user = TxtUsername.Text.Trim();
            string pass = TxtPassword.Password;
            string role = (CmbRole.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "staff";

            if (string.IsNullOrEmpty(user) || string.IsNullOrEmpty(pass))
            {
                TxtError.Text = "Username dan Password tidak boleh kosong.";
                TxtError.Visibility = Visibility.Visible;
                return;
            }

            if (string.Equals(role, "admin", System.StringComparison.OrdinalIgnoreCase) && !_canCreateAdmin)
            {
                TxtError.Text = "Hanya AKBAR yang dapat membuat akun admin.";
                TxtError.Visibility = Visibility.Visible;
                return;
            }

            bool ok = await DatabaseService.AddUserAsync(user, pass, role, _createdBy);
            if (ok)
            {
                MessageBox.Show("User berhasil ditambahkan!", "Sukses", MessageBoxButton.OK, MessageBoxImage.Information);
                this.DialogResult = true;
                this.Close();
            }
            else
            {
                TxtError.Text = "Gagal menyimpan. Username mungkin sudah ada atau terjadi kesalahan database.";
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
