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

namespace AdminClientWpf.ViewModels
{
    public class DashboardViewModel : INotifyPropertyChanged
    {
        private readonly ApiClient _api;

        public ObservableCollection<AdminUser> Users { get; } = new();
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

        private void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
