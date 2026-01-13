using System.Windows;
using AdminClientWpf.ViewModels;

namespace AdminClientWpf.Views
{
    public partial class DashboardWindow : Window
    {
        public DashboardWindow()
        {
            InitializeComponent();
            DataContext = new DashboardViewModel();
        }
    }
}
