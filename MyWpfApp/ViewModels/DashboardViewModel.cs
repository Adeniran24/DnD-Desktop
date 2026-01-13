using AdminClientWpf.Core;
using AdminClientWpf.Models;
using AdminClientWpf.Services;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

namespace AdminClientWpf.ViewModels
{
    public class DashboardViewModel : INotifyPropertyChanged
    {
        private readonly ApiClient _api;

        public ObservableCollection<AdminUser> Users { get; } = new();
        public ObservableCollection<ToastMessage> Toasts { get; } = new();
        public string[] RoleOptions { get; } = { "User", "DM", "Admin" };

        private AdminUser? _selectedUser;
        public AdminUser? SelectedUser
        {
            get => _selectedUser;
            set
            {
                _selectedUser = value;
                OnPropertyChanged();
                RefreshCommands();
            }
        }

        private bool _isBusy;
        public bool IsBusy
        {
            get => _isBusy;
            set
            {
                _isBusy = value;
                OnPropertyChanged();
                RefreshCommands();
            }
        }

        private string _errorMessage = string.Empty;
        public string ErrorMessage
        {
            get => _errorMessage;
            set
            {
                _errorMessage = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(HasError));
            }
        }

        public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);

        private string _statusMessage = string.Empty;
        public string StatusMessage
        {
            get => _statusMessage;
            set
            {
                _statusMessage = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(HasStatus));
            }
        }

        public bool HasStatus => !string.IsNullOrWhiteSpace(StatusMessage);

        private string _adminLabel = "";
        public string AdminLabel
        {
            get => _adminLabel;
            set { _adminLabel = value; OnPropertyChanged(); }
        }

        private bool _isLightTheme;
        public bool IsLightTheme
        {
            get => _isLightTheme;
            set
            {
                if (_isLightTheme == value)
                {
                    return;
                }

                _isLightTheme = value;
                OnPropertyChanged();
                ApplyTheme(_isLightTheme);
                AddToast(_isLightTheme ? "Light theme enabled." : "Dark theme enabled.");
            }
        }

        public RelayCommand RefreshCommand { get; }
        public RelayCommand UpdateRoleCommand { get; }
        public RelayCommand UpdateStatusCommand { get; }
        public RelayCommand LogoutCommand { get; }

        public event PropertyChangedEventHandler? PropertyChanged;

        public DashboardViewModel()
        {
            _api = new ApiClient(ApiConfig.BaseUrl);

            RefreshCommand = new RelayCommand(() => _ = LoadUsersAsync(), () => !IsBusy);
            UpdateRoleCommand = new RelayCommand(() => _ = UpdateRoleAsync(), CanModifySelected);
            UpdateStatusCommand = new RelayCommand(() => _ = UpdateStatusAsync(), CanModifySelected);
            LogoutCommand = new RelayCommand(Logout);

            var token = TokenStore.Load();
            if (string.IsNullOrWhiteSpace(token))
            {
                ErrorMessage = "Missing token. Please log in again.";
                return;
            }

            _api.SetBearer(token);
            _ = InitializeAsync();
        }

        private bool CanModifySelected() => !IsBusy && SelectedUser != null;

        private async Task InitializeAsync()
        {
            try
            {
                var me = await _api.GetCurrentUserAsync();
                AdminLabel = $"Admin: {me.Username} ({me.Email})";
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
                AddToast(ex.Message, isError: true);
            }

            await LoadUsersAsync();
        }

        private async Task LoadUsersAsync()
        {
            ErrorMessage = "";
            StatusMessage = "";
            IsBusy = true;

            try
            {
                var users = await _api.GetUsersAsync();
                Users.Clear();
                foreach (var user in users.OrderBy(u => u.Id))
                {
                    Users.Add(user);
                }

                AddToast("User list refreshed.");
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
                AddToast(ex.Message, isError: true);
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task UpdateRoleAsync()
        {
            if (SelectedUser == null)
                return;

            ErrorMessage = "";
            StatusMessage = "";
            IsBusy = true;

            try
            {
                await _api.UpdateUserRoleAsync(SelectedUser.Id, SelectedUser.Role);
                StatusMessage = "Role updated successfully.";
                AddToast(StatusMessage);
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
                AddToast(ex.Message, isError: true);
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task UpdateStatusAsync()
        {
            if (SelectedUser == null)
                return;

            ErrorMessage = "";
            StatusMessage = "";
            IsBusy = true;

            try
            {
                await _api.UpdateUserStatusAsync(SelectedUser.Id, SelectedUser.IsActive);
                StatusMessage = "Status updated successfully.";
                AddToast(StatusMessage);
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
                AddToast(ex.Message, isError: true);
            }
            finally
            {
                IsBusy = false;
            }
        }

        private void Logout()
        {
            TokenStore.Clear();
            var login = new MainWindow();
            login.Show();

            foreach (Window window in Application.Current.Windows)
            {
                if (window is Views.DashboardWindow)
                {
                    window.Close();
                    break;
                }
            }
        }

        private void RefreshCommands()
        {
            RefreshCommand.RaiseCanExecuteChanged();
            UpdateRoleCommand.RaiseCanExecuteChanged();
            UpdateStatusCommand.RaiseCanExecuteChanged();
        }

        private void ApplyTheme(bool useLightTheme)
        {
            var resources = Application.Current.Resources.MergedDictionaries;
            if (resources.Count == 0)
            {
                return;
            }

            var themeSource = new Uri(useLightTheme ? "Themes/DnDLightTheme.xaml" : "Themes/DnDTheme.xaml", UriKind.Relative);
            resources[0].Source = themeSource;
        }

        private void AddToast(string message, bool isError = false)
        {
            var toast = new ToastMessage
            {
                Message = message,
                IsError = isError
            };

            Toasts.Add(toast);

            var timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(isError ? 6 : 4)
            };

            timer.Tick += (_, _) =>
            {
                timer.Stop();
                Toasts.Remove(toast);
            };

            timer.Start();
        }

        private void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
