using System.Windows;
using LaporanProduktivitasWPF.Models;
using LaporanProduktivitasWPF.ViewModels;

namespace LaporanProduktivitasWPF
{
    public partial class MainWindow : Window
    {
        public MainWindow(AppUser currentUser)
        {
            InitializeComponent();
            var vm = new MainViewModel(currentUser);
            DataContext = vm;
        }
    }
}
