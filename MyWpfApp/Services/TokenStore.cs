using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace AdminClientWpf.Services
{
    public static class TokenStore
    {
        private static readonly string Dir =
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "DnDToolAdmin");

        private static readonly string FilePath = Path.Combine(Dir, "token.dat");

        public static void Save(string token)
        {
            Directory.CreateDirectory(Dir);
            var raw = Encoding.UTF8.GetBytes(token);
            var protectedBytes = ProtectedData.Protect(raw, optionalEntropy: null, DataProtectionScope.CurrentUser);
            File.WriteAllBytes(FilePath, protectedBytes);
        }

        public static string? Load()
        {
            if (!File.Exists(FilePath)) return null;
            var protectedBytes = File.ReadAllBytes(FilePath);
            var raw = ProtectedData.Unprotect(protectedBytes, optionalEntropy: null, DataProtectionScope.CurrentUser);
            return Encoding.UTF8.GetString(raw);
        }

        public static void Clear()
        {
            if (File.Exists(FilePath))
                File.Delete(FilePath);
        }
    }
}
