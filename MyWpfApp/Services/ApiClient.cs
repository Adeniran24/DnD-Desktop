using System;
using AdminClientWpf.Models;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace AdminClientWpf.Services
{
    public class ApiClient
    {
        private readonly HttpClient _http;
        public string BaseUrl { get; }

        public ApiClient(string baseUrl)
        {
            BaseUrl = baseUrl.TrimEnd('/');
            _http = new HttpClient
            {
                Timeout = TimeSpan.FromSeconds(20)
            };
        }

        public void SetBearer(string? token)
        {
            _http.DefaultRequestHeaders.Authorization =
                string.IsNullOrWhiteSpace(token) ? null : new AuthenticationHeaderValue("Bearer", token);
        }

        public async Task<string> GetSaltAsync(string email)
        {
            var url = $"{BaseUrl}/api/auth/salt?email={Uri.EscapeDataString(email)}";
            using var resp = await _http.GetAsync(url);
            var body = await resp.Content.ReadAsStringAsync();

            if (!resp.IsSuccessStatusCode)
                throw new Exception($"Salt error ({(int)resp.StatusCode}): {body}");

            using var doc = JsonDocument.Parse(body);
            if (!doc.RootElement.TryGetProperty("salt", out var saltEl))
                throw new Exception("Salt missing in response.");

            return saltEl.GetString() ?? throw new Exception("Salt is null.");
        }

        public async Task<string> LoginAsync(string email, string passwordPlain)
        {
            // 1) salt
            var salt = await GetSaltAsync(email);

            // 2) clientHash (IMPORTANT: if your frontend uses different algo, change THIS)
            var clientHash = ComputeClientHash(passwordPlain, salt);

            // 3) login
            var url = $"{BaseUrl}/api/auth/login?email={Uri.EscapeDataString(email)}&password={Uri.EscapeDataString(clientHash)}";
            using var resp = await _http.PostAsync(url, content: null);
            var body = await resp.Content.ReadAsStringAsync();

            if (!resp.IsSuccessStatusCode)
                throw new Exception($"Login error ({(int)resp.StatusCode}): {body}");

            using var doc = JsonDocument.Parse(body);
            if (!doc.RootElement.TryGetProperty("token", out var tokenEl))
                throw new Exception("Token missing in response.");

            return tokenEl.GetString() ?? throw new Exception("Token is null.");
        }

        public async Task<CurrentUser> GetCurrentUserAsync()
        {
            var url = $"{BaseUrl}/api/auth/me";
            using var resp = await _http.GetAsync(url);
            var body = await resp.Content.ReadAsStringAsync();

            if (!resp.IsSuccessStatusCode)
                throw new Exception($"Me error ({(int)resp.StatusCode}): {body}");

            var user = JsonSerializer.Deserialize<CurrentUser>(body, JsonOptions());
            return user ?? throw new Exception("Invalid profile response.");
        }

        public async Task<List<AdminUser>> GetUsersAsync()
        {
            var url = $"{BaseUrl}/api/admin/users";
            using var resp = await _http.GetAsync(url);
            var body = await resp.Content.ReadAsStringAsync();

            if (!resp.IsSuccessStatusCode)
                throw new Exception($"Users error ({(int)resp.StatusCode}): {body}");

            var users = JsonSerializer.Deserialize<List<AdminUser>>(body, JsonOptions());
            return users ?? new List<AdminUser>();
        }

        public async Task UpdateUserRoleAsync(int userId, string role)
        {
            var url = $"{BaseUrl}/api/admin/users/{userId}/role";
            var payload = JsonSerializer.Serialize(new { role });
            using var resp = await _http.PutAsync(url, new StringContent(payload, Encoding.UTF8, "application/json"));
            var body = await resp.Content.ReadAsStringAsync();

            if (!resp.IsSuccessStatusCode)
                throw new Exception($"Role update error ({(int)resp.StatusCode}): {body}");
        }

        public async Task UpdateUserStatusAsync(int userId, bool isActive)
        {
            var url = $"{BaseUrl}/api/admin/users/{userId}/status";
            var payload = JsonSerializer.Serialize(new { isActive });
            using var resp = await _http.PutAsync(url, new StringContent(payload, Encoding.UTF8, "application/json"));
            var body = await resp.Content.ReadAsStringAsync();

            if (!resp.IsSuccessStatusCode)
                throw new Exception($"Status update error ({(int)resp.StatusCode}): {body}");
        }

        private static JsonSerializerOptions JsonOptions()
            => new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

        // Default assumption: Hex(SHA256(password + salt))
        public static string ComputeClientHash(string passwordPlain, string salt)
        {
            using var sha = SHA256.Create();
            var bytes = Encoding.UTF8.GetBytes(passwordPlain + salt);
            var hash = sha.ComputeHash(bytes);
            return string.Concat(hash.Select(b => b.ToString("x2")));
        }
    }
}
