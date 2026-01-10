using AdminClientWpf.Core;
using AdminClientWpf.Services;
using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;

namespace AdminClientWpf.ViewModels
{
    public class LoginViewModel : INotifyPropertyChanged
    {
        // TODO: change if needed
        private const string ApiBaseUrl = "http://212.48.254.1:5000";

        private readonly ApiClient _api = new(ApiBaseUrl);

        public RelayCommand LoginCommand { get; }
        public RelayCommand CloseCommand { get; }
        public RelayCommand MinimizeCommand { get; }

        private string _email = "";
        public string Email { get => _email; set { _email = value; OnPropertyChanged(); Refresh(); } }

        // Set from code-behind PasswordBox
        private string _password = "";
        public string Password { get => _password; set { _password = value; OnPropertyChanged(); Refresh(); } }

        private bool _isBusy;
        public bool IsBusy { get => _isBusy; set { _isBusy = value; OnPropertyChanged(); Refresh(); OnPropertyChanged(nameof(ButtonText)); } }

        private string _errorMessage = "";
        public string ErrorMessage { get => _errorMessage; set { _errorMessage = value; OnPropertyChanged(); OnPropertyChanged(nameof(HasError)); } }
        public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);

        public string ButtonText => IsBusy ? "Signing in..." : "Sign in";

        public bool CanLogin => !IsBusy
                               && !string.IsNullOrWhiteSpace(Email)
                               && !string.IsNullOrWhiteSpace(Password);

        public event PropertyChangedEventHandler? PropertyChanged;

        public LoginViewModel()
        {
            LoginCommand = new RelayCommand(() => _ = LoginAsync(), () => CanLogin);
            CloseCommand = new RelayCommand(() => Application.Current.Shutdown());
            MinimizeCommand = new RelayCommand(() =>
            {
                if (Application.Current.MainWindow != null)
                    Application.Current.MainWindow.WindowState = WindowState.Minimized;
            });
        }

        private void Refresh()
        {
            LoginCommand.RaiseCanExecuteChanged();
        }

        private async Task LoginAsync()
        {
            ErrorMessage = "";
            IsBusy = true;

            try
            {
                var token = await _api.LoginAsync(Email.Trim(), Password);
                TokenStore.Save(token);
                _api.SetBearer(token);

                // Placeholder: open dashboard window
                var dash = new Views.DashboardWindow();
                dash.Show();

                // close login window
                Application.Current.MainWindow?.Close();
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
            }
            finally
            {
                IsBusy = false;
            }
        }

        private void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
