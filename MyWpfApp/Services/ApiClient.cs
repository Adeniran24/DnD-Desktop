using System;
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

        // Default assumption: Base64(SHA256(password + salt))
        public static string ComputeClientHash(string passwordPlain, string salt)
        {
            using var sha = SHA256.Create();
            var bytes = Encoding.UTF8.GetBytes(passwordPlain + salt);
            var hash = sha.ComputeHash(bytes);
            return Convert.ToBase64String(hash);
        }
    }
}
