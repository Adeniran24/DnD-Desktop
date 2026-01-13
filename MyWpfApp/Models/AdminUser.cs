using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace AdminClientWpf.Models
{
    public class AdminUser : INotifyPropertyChanged
    {
        private int _id;
        private string _email = string.Empty;
        private string _username = string.Empty;
        private string _role = string.Empty;
        private bool _isActive;
        private DateTime _createdAt;
        private DateTime? _lastLoginAt;

        public int Id { get => _id; set { _id = value; OnPropertyChanged(); } }
        public string Email { get => _email; set { _email = value; OnPropertyChanged(); } }
        public string Username { get => _username; set { _username = value; OnPropertyChanged(); } }
        public string Role { get => _role; set { _role = value; OnPropertyChanged(); } }
        public bool IsActive { get => _isActive; set { _isActive = value; OnPropertyChanged(); } }
        public DateTime CreatedAt { get => _createdAt; set { _createdAt = value; OnPropertyChanged(); } }
        public DateTime? LastLoginAt { get => _lastLoginAt; set { _lastLoginAt = value; OnPropertyChanged(); } }

        public event PropertyChangedEventHandler? PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
