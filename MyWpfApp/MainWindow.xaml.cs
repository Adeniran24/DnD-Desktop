using System.Windows;
using System.Windows.Input;

namespace AdminClientWpf
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void WindowDrag(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
                DragMove();
        }

        private void PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (DataContext is ViewModels.LoginViewModel vm)
            {
                vm.Password = Pwd.Password;
                // Always refresh CanLogin state
                vm.Refresh();
                // Show error if password is empty
                if (string.IsNullOrWhiteSpace(vm.Password))
                {
                    vm.ErrorMessage = "Password cannot be empty.";
                }
                else
                {
                    vm.ErrorMessage = string.Empty;
                }
            }
        }

        private void EmailChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            if (DataContext is ViewModels.LoginViewModel vm)
            {
                // Always update password in ViewModel to keep in sync
                vm.Password = Pwd.Password;
                vm.Refresh();
            }
        }
    }
}
